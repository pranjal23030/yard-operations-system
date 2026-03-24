using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models.ViewModels.Operations;
using YardOps.Services;

namespace YardOps.Pages.Admin.Operations.Ingates
{
    [Authorize(Roles = "Admin,YardManager")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly GateOperationService _gateOperationService;

        private const int PageSize = 10;

        public IndexModel(
            ApplicationDbContext context,
            GateOperationService gateOperationService)
        {
            _context = context;
            _gateOperationService = gateOperationService;
        }

        public List<IngateViewModel> Ingates { get; set; } = [];
        public List<SelectListItem> TrailerOptions { get; set; } = [];
        public List<SelectListItem> GateOptions { get; set; } = [];
        public List<SelectListItem> AssignableLocationOptions { get; set; } = [];

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalIngates { get; set; }

        public bool ShowLogModal { get; set; }

        [BindProperty] public CreateIngateInput Input { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateFrom { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateTo { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            await LoadOptionsAsync();
            await LoadIngatesAsync();
        }

        public async Task<IActionResult> OnPostLogIngateAsync()
        {
            ModelState.Clear();
            TryValidateModel(Input, nameof(Input));

            if (!ModelState.IsValid)
            {
                ShowLogModal = true;
                CurrentPage = PageNumber < 1 ? 1 : PageNumber;
                await LoadOptionsAsync();
                await LoadIngatesAsync();
                TempData["Error"] = "Please complete all required fields.";
                return Page();
            }

            var result = await _gateOperationService.LogIngateAsync(Input, User);
            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;

            return RedirectToPage();
        }

        private async Task LoadOptionsAsync()
        {
            TrailerOptions =
            [
                new SelectListItem("Select incoming trailer", ""),
                .. (await _context.Trailers
                    .Where(t => t.CurrentStatus == "Incoming")
                    .Include(t => t.Carrier)
                    .OrderBy(t => t.TrailerCode)
                    .Select(t => new SelectListItem(
                        $"{t.TrailerCode} - {(t.Carrier != null ? t.Carrier.CompanyName : "Unknown Carrier")}",
                        t.TrailerId.ToString()))
                    .ToListAsync())
            ];

            GateOptions =
            [
                new SelectListItem("Select entry gate", ""),
                .. (await _context.Locations
                    .Where(l => l.LocationType == "Gate")
                    .OrderBy(l => l.LocationName)
                    .Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString()))
                    .ToListAsync())
            ];

            AssignableLocationOptions =
            [
                new SelectListItem("Select assign location", ""),
                .. (await _context.Locations
                    .Where(l => l.LocationType != "Gate")
                    .OrderBy(l => l.LocationType)
                    .ThenBy(l => l.LocationName)
                    .Select(l => new SelectListItem($"{l.LocationName} ({l.LocationType})", l.LocationId.ToString()))
                    .ToListAsync())
            ];
        }

        private async Task LoadIngatesAsync()
        {
            var query = _context.Ingates
                .Include(i => i.Trailer)
                    .ThenInclude(t => t!.Carrier)
                .Include(i => i.Location)
                .Include(i => i.PerformedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();

                query = query.Where(i =>
                    (i.Trailer != null && i.Trailer.TrailerCode.ToLower().Contains(term)) ||
                    (i.Trailer != null && i.Trailer.Carrier != null && i.Trailer.Carrier.CompanyName.ToLower().Contains(term)) ||
                    (i.Location != null && i.Location.LocationName.ToLower().Contains(term)) ||
                    (i.PerformedByUser != null && (
                        ((i.PerformedByUser.FirstName + " " + i.PerformedByUser.LastName).ToLower().Contains(term)) ||
                        (i.PerformedByUser.Email != null && i.PerformedByUser.Email.ToLower().Contains(term))
                    )) ||
                    (i.Notes != null && i.Notes.ToLower().Contains(term))
                );
            }

            if (DateFrom.HasValue)
            {
                var from = DateFrom.Value.Date;
                query = query.Where(i => i.Timestamp >= from);
            }

            if (DateTo.HasValue)
            {
                var toExclusive = DateTo.Value.Date.AddDays(1);
                query = query.Where(i => i.Timestamp < toExclusive);
            }

            query = query.OrderByDescending(i => i.Timestamp);

            TotalIngates = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalIngates / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            var rows = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Resolve assigned location from TrailerHistory record created at ingate time
            var trailerIds = rows.Select(r => r.TrailerId).Distinct().ToList();
            var minTs = rows.Count > 0 ? rows.Min(r => r.Timestamp).AddMinutes(-1) : DateTime.UtcNow.AddMinutes(-1);
            var maxTs = rows.Count > 0 ? rows.Max(r => r.Timestamp).AddMinutes(1) : DateTime.UtcNow.AddMinutes(1);

            var historyRows = await _context.TrailerHistories
                .Where(h => trailerIds.Contains(h.TrailerId) && h.StartTime >= minTs && h.StartTime <= maxTs)
                .Include(h => h.Location)
                .ToListAsync();

            Ingates = rows.Select(i =>
            {
                var matchedHistory = historyRows
                    .Where(h => h.TrailerId == i.TrailerId)
                    .OrderBy(h => Math.Abs((h.StartTime - i.Timestamp).Ticks))
                    .FirstOrDefault();

                return new IngateViewModel
                {
                    IngateId = i.IngateId,
                    TrailerId = i.TrailerId,
                    TrailerCode = i.Trailer?.TrailerCode ?? "—",
                    CarrierName = i.Trailer?.Carrier?.CompanyName ?? "—",
                    GateLocationId = i.LocationId,
                    GateName = i.Location?.LocationName ?? "—",
                    AssignedLocationId = matchedHistory?.LocationId,
                    AssignedLocationName = matchedHistory?.Location?.LocationName ?? "—",
                    PerformedByUserId = i.PerformedByUserId ?? "",
                    PerformedByName = i.PerformedByUser != null
                        ? $"{i.PerformedByUser.FirstName} {i.PerformedByUser.LastName}".Trim()
                        : "Unknown",
                    PerformedByEmail = i.PerformedByUser?.Email ?? "Unknown",
                    Timestamp = i.Timestamp,
                    Notes = i.Notes
                };
            }).ToList();
        }
    }
}