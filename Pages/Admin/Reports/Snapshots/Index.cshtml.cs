using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YardOps.Data;
using YardOps.Models;
using YardOps.Models.ViewModels.Snapshots;
using YardOps.Services;

namespace YardOps.Pages.Admin.Reports.Snapshots
{
    [Authorize(Roles = "Admin,YardManager")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SnapshotService _snapshotService;
        private const int PageSize = 10;

        public IndexModel(ApplicationDbContext context, SnapshotService snapshotService)
        {
            _context = context;
            _snapshotService = snapshotService;
        }

        public List<SnapshotRunViewModel> Runs { get; set; } = [];
        public List<SnapshotItemViewModel> Items { get; set; } = [];

        [BindProperty(SupportsGet = true)] public int? SelectedRunId { get; set; }
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        [BindProperty] public CaptureSnapshotInput CaptureInput { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "Snapshot Report";
            ViewData["PageHeader"] = "Snapshot Report";

            CurrentPage = PageNumber < 1 ? 1 : PageNumber;

            await LoadRunsAsync();

            if (!Runs.Any())
            {
                Items = [];
                TotalItems = 0;
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            if (!SelectedRunId.HasValue || !Runs.Any(r => r.SnapshotRunId == SelectedRunId.Value))
            {
                SelectedRunId = Runs.First().SnapshotRunId; // latest by default
            }

            await LoadItemsAsync();
        }

        public async Task<IActionResult> OnPostCaptureSnapshotAsync()
        {
            var result = await _snapshotService.CaptureCurrentInYardSnapshotAsync(User);

            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;

            if (result.IsSuccess && result.SnapshotRunId.HasValue)
            {
                return RedirectToPage(new
                {
                    SelectedRunId = result.SnapshotRunId.Value,
                    SearchTerm,
                    PageNumber = 1
                });
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            await LoadRunsAsync();

            if (!Runs.Any())
                return BadRequest("No snapshot runs available.");

            if (!SelectedRunId.HasValue || !Runs.Any(r => r.SnapshotRunId == SelectedRunId.Value))
            {
                SelectedRunId = Runs.First().SnapshotRunId;
            }

            var selectedRun = Runs.First(r => r.SnapshotRunId == SelectedRunId.Value);

            var data = await BuildFilteredQuery()
                .OrderBy(si => si.TrailerCode)
                .ToListAsync();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Snapshot Report").FontSize(16).SemiBold();
                        col.Item().Text(
                            $"Run #{selectedRun.SnapshotRunId} | Captured: {selectedRun.CapturedAt.ToLocalTime():yyyy-MM-dd hh:mm tt} | Captured By: {selectedRun.CapturedBy}"
                        ).FontColor(Colors.Grey.Darken1);

                        if (!string.IsNullOrWhiteSpace(SearchTerm))
                        {
                            col.Item().Text($"Search: '{SearchTerm}'").FontColor(Colors.Grey.Darken1);
                        }
                    });

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.1f); // Trailer
                            c.RelativeColumn(1.0f); // Status
                            c.RelativeColumn(1.6f); // Carrier
                            c.RelativeColumn(1.6f); // Driver
                            c.RelativeColumn(1.0f); // GoodsType
                            c.RelativeColumn(1.5f); // Location
                            c.RelativeColumn(1.5f); // Arrival
                            c.RelativeColumn(1.5f); // Captured
                        });

                        void HeaderCell(string text) => table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).SemiBold();

                        HeaderCell("Trailer");
                        HeaderCell("Status");
                        HeaderCell("Carrier");
                        HeaderCell("Driver");
                        HeaderCell("Goods Type");
                        HeaderCell("Location");
                        HeaderCell("Arrival");
                        HeaderCell("Captured At");

                        if (data.Any())
                        {
                            foreach (var si in data)
                            {
                                table.Cell().Padding(3).Text(si.TrailerCode);
                                table.Cell().Padding(3).Text(si.Status);
                                table.Cell().Padding(3).Text(si.CarrierName);
                                table.Cell().Padding(3).Text(si.DriverName);
                                table.Cell().Padding(3).Text(si.GoodsType);
                                table.Cell().Padding(3).Text($"{si.LocationName} ({si.LocationType})");
                                table.Cell().Padding(3).Text(si.ArrivalTime.HasValue ? si.ArrivalTime.Value.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt") : "—");
                                table.Cell().Padding(3).Text(si.CapturedAt.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt"));
                            }
                        }
                        else
                        {
                            table.Cell().ColumnSpan(8).Padding(6).Text("No snapshot data found for selected filters.").Italic().FontColor(Colors.Grey.Darken1);
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf();

            var fileName = $"snapshot_report_run_{selectedRun.SnapshotRunId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task LoadRunsAsync()
        {
            Runs = await _context.SnapshotRuns
                .Include(sr => sr.CapturedByUser)
                .Include(sr => sr.SnapshotItems)
                .OrderByDescending(sr => sr.CapturedAt)
                .Select(sr => new SnapshotRunViewModel
                {
                    SnapshotRunId = sr.SnapshotRunId,
                    CapturedAt = sr.CapturedAt,
                    CapturedBy = sr.CapturedByUser != null
                        ? $"{sr.CapturedByUser.FirstName} {sr.CapturedByUser.LastName}".Trim()
                        : "Unknown",
                    ItemCount = sr.SnapshotItems != null ? sr.SnapshotItems.Count : 0
                })
                .ToListAsync();
        }

        private async Task LoadItemsAsync()
        {
            var query = BuildFilteredQuery().OrderBy(si => si.TrailerCode);

            TotalItems = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
            CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

            Items = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(si => new SnapshotItemViewModel
                {
                    SnapshotItemId = si.SnapshotItemId,
                    SnapshotRunId = si.SnapshotRunId,
                    TrailerId = si.TrailerId,
                    TrailerCode = si.TrailerCode,
                    LocationId = si.LocationId,
                    LocationName = si.LocationName,
                    LocationType = si.LocationType,
                    Status = si.Status,
                    ArrivalTime = si.ArrivalTime,
                    CapturedAt = si.CapturedAt,
                    CarrierName = si.CarrierName,
                    DriverName = si.DriverName,
                    GoodsType = si.GoodsType
                })
                .ToListAsync();
        }

        private IQueryable<SnapshotItem> BuildFilteredQuery()
        {
            var query = _context.SnapshotItems
                .Where(si => si.SnapshotRunId == SelectedRunId!.Value)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();

                query = query.Where(si =>
                    si.TrailerCode.Contains(term) ||
                    si.CarrierName.Contains(term) ||
                    si.DriverName.Contains(term) ||
                    si.LocationName.Contains(term) ||
                    si.GoodsType.Contains(term));
            }

            return query;
        }
    }
}