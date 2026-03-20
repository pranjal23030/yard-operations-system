using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YardOps.Data;
using YardOps.Models;

namespace YardOps.Pages.Admin.Reports
{
    [Authorize(Roles = "Admin")]
    public class TrailerReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10;

        public TrailerReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TrailerReportRow> Rows { get; set; } = [];
        public string? ValidationMessage { get; set; }
        public bool SearchIgnoredBecauseTooShort { get; set; }

        public List<SelectListItem> StatusOptions { get; set; } = [];
        public List<SelectListItem> LocationOptions { get; set; } = [];

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRows { get; set; }

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? LocationFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "Trailer Report";
            ViewData["PageHeader"] = "Trailer Report";

            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            await LoadFilterOptionsAsync();

            if (!ApplyDefaultDatesAndValidate())
            {
                Rows = [];
                TotalRows = 0;
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            var query = BuildFilteredQuery();

            TotalRows = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRows / (double)PageSize));
            CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages);

            var data = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Rows = data.Select(MapToReportRow).ToList();
        }

        public IActionResult OnGetClear()
        {
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            await LoadFilterOptionsAsync();

            if (!ApplyDefaultDatesAndValidate())
                return BadRequest("Invalid date range.");

            var data = await BuildFilteredQuery().ToListAsync();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Trailer Report").FontSize(16).SemiBold();
                        col.Item().Text(
                            $"Range: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} | Generated: {DateTime.Now:yyyy-MM-dd hh:mm tt}"
                        ).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f); // Trailer
                            c.RelativeColumn(1.8f); // Carrier
                            c.RelativeColumn(1.8f); // Driver
                            c.RelativeColumn(1.2f); // Status
                            c.RelativeColumn(1.6f); // Location
                            c.RelativeColumn(1.5f); // Arrival
                            c.RelativeColumn(1.5f); // Departure
                            c.RelativeColumn(1.5f); // Created On
                        });

                        void HeaderCell(string text) => table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).SemiBold();

                        HeaderCell("Trailer");
                        HeaderCell("Carrier");
                        HeaderCell("Driver");
                        HeaderCell("Status");
                        HeaderCell("Location");
                        HeaderCell("Arrival");
                        HeaderCell("Departure");
                        HeaderCell("Created On");

                        foreach (var t in data)
                        {
                            var row = MapToReportRow(t);

                            table.Cell().Padding(3).Text(row.TrailerCode);
                            table.Cell().Padding(3).Text(row.CarrierName);
                            table.Cell().Padding(3).Text(row.DriverName);
                            table.Cell().Padding(3).Text(row.Status);
                            table.Cell().Padding(3).Text(row.LocationName);
                            table.Cell().Padding(3).Text(row.Arrival?.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt") ?? "—");
                            table.Cell().Padding(3).Text(row.Departure?.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt") ?? "—");
                            table.Cell().Padding(3).Text(row.CreatedOn.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt"));
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

            var fileName = $"trailer_report_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task LoadFilterOptionsAsync()
        {
            StatusOptions =
            [
                new("All Status", "all"),
                new("Incoming", "Incoming"),
                new("In-Yard", "In-Yard"),
                new("Outgoing", "Outgoing"),
                new("Checked Out", "Checked Out")
            ];

            LocationOptions =
            [
                new("All Locations", "all"),
                .. (await _context.Locations
                    .OrderBy(l => l.LocationType)
                    .ThenBy(l => l.LocationName)
                    .Select(l => new SelectListItem($"{l.LocationName} ({l.LocationType})", l.LocationId.ToString()))
                    .ToListAsync())
            ];
        }

        private bool ApplyDefaultDatesAndValidate()
        {
            var now = DateTime.Now;
            var defaultStart = new DateTime(now.Year, now.Month, 1);
            var defaultEnd = defaultStart.AddMonths(1).AddDays(-1);

            StartDate ??= defaultStart;
            EndDate ??= defaultEnd;

            if (EndDate < StartDate)
            {
                ValidationMessage = "End Date cannot be earlier than Start Date.";
                return false;
            }

            return true;
        }

        private IQueryable<Trailer> BuildFilteredQuery()
        {
            var startInclusive = StartDate!.Value.Date;
            var endExclusive = EndDate!.Value.Date.AddDays(1);

            var query = _context.Trailers
                .Include(t => t.Carrier)
                .Include(t => t.DriverUser)
                .Include(t => t.CurrentLocation)
                .Where(t => t.CreatedOn >= startInclusive && t.CreatedOn < endExclusive)
                .AsQueryable();

            SearchIgnoredBecauseTooShort = false;

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();

                if (term.Length >= 3)
                {
                    query = query.Where(t =>
                        (t.Carrier != null && t.Carrier.CompanyName.Contains(term)) ||
                        t.TrailerCode.Contains(term) ||
                        (t.DriverUser != null &&
                         ((t.DriverUser.FirstName + " " + t.DriverUser.LastName).Contains(term) ||
                          (t.DriverUser.Email != null && t.DriverUser.Email.Contains(term)))));

                }
                else
                {
                    SearchIgnoredBecauseTooShort = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "all")
                query = query.Where(t => t.CurrentStatus == StatusFilter);

            if (!string.IsNullOrWhiteSpace(LocationFilter) && LocationFilter != "all" && int.TryParse(LocationFilter, out var locationId))
                query = query.Where(t => t.CurrentLocationId == locationId);

            return query.OrderByDescending(t => t.CreatedOn);
        }

        private static TrailerReportRow MapToReportRow(Trailer t)
        {
            return new TrailerReportRow
            {
                TrailerCode = t.TrailerCode,
                CarrierName = t.Carrier?.CompanyName ?? "—",
                DriverName = t.DriverUser != null
                    ? $"{t.DriverUser.FirstName} {t.DriverUser.LastName}".Trim()
                    : "—",
                Status = t.CurrentStatus,
                LocationName = t.CurrentLocation?.LocationName ?? (t.CurrentStatus == "Checked Out" ? "Departed" : "—"),
                Arrival = t.ArrivalTime,
                Departure = t.DepartureTime,
                CreatedOn = t.CreatedOn
            };
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }

        public class TrailerReportRow
        {
            public string TrailerCode { get; set; } = "";
            public string CarrierName { get; set; } = "";
            public string DriverName { get; set; } = "";
            public string Status { get; set; } = "";
            public string LocationName { get; set; } = "";
            public DateTime? Arrival { get; set; }
            public DateTime? Departure { get; set; }
            public DateTime CreatedOn { get; set; }
        }
    }
}