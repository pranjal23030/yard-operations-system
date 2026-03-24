using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models.ViewModels.Operations;
using YardOps.Services;

namespace YardOps.Pages.Admin.Operations.Outgates
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

        public List<OutgateViewModel> Outgates { get; set; } = [];
        public List<SelectListItem> TrailerOptions { get; set; } = [];
        public List<SelectListItem> GateOptions { get; set; } = [];

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalOutgates { get; set; }

        public bool ShowLogModal { get; set; }

        [BindProperty] public CreateOutgateInput Input { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateFrom { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateTo { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            await LoadOptionsAsync();
            await LoadOutgatesAsync();
        }

        public async Task<IActionResult> OnPostLogOutgateAsync()
        {
            ModelState.Clear();
            TryValidateModel(Input, nameof(Input));

            if (!ModelState.IsValid)
            {
                ShowLogModal = true;
                CurrentPage = PageNumber < 1 ? 1 : PageNumber;
                await LoadOptionsAsync();
                await LoadOutgatesAsync();
                TempData["Error"] = "Please complete all required fields.";
                return Page();
            }

            var result = await _gateOperationService.LogOutgateAsync(Input, User);
            TempData[result.IsSuccess ? "Success" : "Error"] = result.Message;

            return RedirectToPage();
        }

        private async Task LoadOptionsAsync()
        {
            TrailerOptions =
            [
                new SelectListItem("Select in-yard trailer", ""),
                .. (await _context.Trailers
                    .Where(t => t.CurrentStatus == "In-Yard")
                    .Include(t => t.Carrier)
                    .OrderBy(t => t.TrailerCode)
                    .Select(t => new SelectListItem(
                        $"{t.TrailerCode} - {(t.Carrier != null ? t.Carrier.CompanyName : "Unknown Carrier")}",
                        t.TrailerId.ToString()))
                    .ToListAsync())
            ];

            GateOptions =
            [
                new SelectListItem("Select exit gate", ""),
                .. (await _context.Locations
                    .Where(l => l.LocationType == "Gate")
                    .OrderBy(l => l.LocationName)
                    .Select(l => new SelectListItem(l.LocationName, l.LocationId.ToString()))
                    .ToListAsync())
            ];
        }

        private async Task LoadOutgatesAsync()
        {
            var query = _context.Outgates
                .Include(o => o.Trailer)
                    .ThenInclude(t => t!.Carrier)
                .Include(o => o.Location)
                .Include(o => o.PerformedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();

                query = query.Where(o =>
                    (o.Trailer != null && o.Trailer.TrailerCode.ToLower().Contains(term)) ||
                    (o.Trailer != null && o.Trailer.Carrier != null && o.Trailer.Carrier.CompanyName.ToLower().Contains(term)) ||
                    (o.Location != null && o.Location.LocationName.ToLower().Contains(term)) ||
                    (o.PerformedByUser != null && (
                        ((o.PerformedByUser.FirstName + " " + o.PerformedByUser.LastName).ToLower().Contains(term)) ||
                        (o.PerformedByUser.Email != null && o.PerformedByUser.Email.ToLower().Contains(term))
                    )) ||
                    (o.Notes != null && o.Notes.ToLower().Contains(term))
                );
            }

            if (DateFrom.HasValue)
            {
                var from = DateFrom.Value.Date;
                query = query.Where(o => o.Timestamp >= from);
            }

            if (DateTo.HasValue)
            {
                var toExclusive = DateTo.Value.Date.AddDays(1);
                query = query.Where(o => o.Timestamp < toExclusive);
            }

            query = query.OrderByDescending(o => o.Timestamp);

            TotalOutgates = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalOutgates / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            var rows = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Outgates = rows.Select(o => new OutgateViewModel
            {
                OutgateId = o.OutgateId,
                TrailerId = o.TrailerId,
                TrailerCode = o.Trailer?.TrailerCode ?? "—",
                CarrierName = o.Trailer?.Carrier?.CompanyName ?? "—",
                GateLocationId = o.LocationId,
                GateName = o.Location?.LocationName ?? "—",
                PerformedByUserId = o.PerformedByUserId ?? "",
                PerformedByName = o.PerformedByUser != null
                    ? $"{o.PerformedByUser.FirstName} {o.PerformedByUser.LastName}".Trim()
                    : "Unknown",
                PerformedByEmail = o.PerformedByUser?.Email ?? "Unknown",
                Timestamp = o.Timestamp,
                Notes = o.Notes
            }).ToList();
        }
    }
}