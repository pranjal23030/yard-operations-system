using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models.ViewModels.Operations;

namespace YardOps.Pages.Admin.Operations.AllOperations
{
    [Authorize(Roles = "Admin,YardManager")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AllOperationViewModel> Operations { get; set; } = [];
        public List<SelectListItem> TypeOptions { get; set; } = [];

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalOperations { get; set; }

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? TypeFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateFrom { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateTo { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            LoadTypeOptions();
            await LoadOperationsAsync();
        }

        private void LoadTypeOptions()
        {
            TypeOptions =
            [
                new SelectListItem("All Types", "all"),
                new SelectListItem("Ingate", "Ingate"),
                new SelectListItem("Outgate", "Outgate")
            ];
        }

        private async Task LoadOperationsAsync()
        {
            var ingateQuery = _context.Ingates
                .Include(i => i.Trailer)
                    .ThenInclude(t => t!.Carrier)
                .Include(i => i.Location)
                .Include(i => i.PerformedByUser)
                .Select(i => new AllOperationViewModel
                {
                    Type = "Ingate",
                    OperationId = i.IngateId,
                    TrailerId = i.TrailerId,
                    TrailerCode = i.Trailer != null ? i.Trailer.TrailerCode : "—",
                    CarrierName = i.Trailer != null && i.Trailer.Carrier != null ? i.Trailer.Carrier.CompanyName : "—",
                    GateLocationId = i.LocationId,
                    GateName = i.Location != null ? i.Location.LocationName : "—",
                    EntryGateName = i.Location != null ? i.Location.LocationName : "—",
                    ExitGateName = "—",
                    PerformedByUserId = i.PerformedByUserId ?? "",
                    PerformedByName = i.PerformedByUser != null
                        ? (i.PerformedByUser.FirstName + " " + i.PerformedByUser.LastName).Trim()
                        : "Unknown",
                    PerformedByEmail = i.PerformedByUser != null ? (i.PerformedByUser.Email ?? "Unknown") : "Unknown",
                    Timestamp = i.Timestamp,
                    Notes = i.Notes
                });

            var outgateQuery = _context.Outgates
                .Include(o => o.Trailer)
                    .ThenInclude(t => t!.Carrier)
                .Include(o => o.Location)
                .Include(o => o.PerformedByUser)
                .Select(o => new AllOperationViewModel
                {
                    Type = "Outgate",
                    OperationId = o.OutgateId,
                    TrailerId = o.TrailerId,
                    TrailerCode = o.Trailer != null ? o.Trailer.TrailerCode : "—",
                    CarrierName = o.Trailer != null && o.Trailer.Carrier != null ? o.Trailer.Carrier.CompanyName : "—",
                    GateLocationId = o.LocationId,
                    GateName = o.Location != null ? o.Location.LocationName : "—",
                    EntryGateName = "—",
                    ExitGateName = o.Location != null ? o.Location.LocationName : "—",
                    PerformedByUserId = o.PerformedByUserId ?? "",
                    PerformedByName = o.PerformedByUser != null
                        ? (o.PerformedByUser.FirstName + " " + o.PerformedByUser.LastName).Trim()
                        : "Unknown",
                    PerformedByEmail = o.PerformedByUser != null ? (o.PerformedByUser.Email ?? "Unknown") : "Unknown",
                    Timestamp = o.Timestamp,
                    Notes = o.Notes
                });

            var query = ingateQuery.Concat(outgateQuery);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();

                query = query.Where(x =>
                    x.TrailerCode.ToLower().Contains(term) ||
                    x.CarrierName.ToLower().Contains(term) ||
                    x.EntryGateName.ToLower().Contains(term) ||
                    x.ExitGateName.ToLower().Contains(term) ||
                    x.PerformedByName.ToLower().Contains(term) ||
                    x.PerformedByEmail.ToLower().Contains(term) ||
                    (x.Notes != null && x.Notes.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter != "all")
            {
                query = query.Where(x => x.Type == TypeFilter);
            }

            if (DateFrom.HasValue)
            {
                var from = DateFrom.Value.Date;
                query = query.Where(x => x.Timestamp >= from);
            }

            if (DateTo.HasValue)
            {
                var toExclusive = DateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.Timestamp < toExclusive);
            }

            query = query.OrderByDescending(x => x.Timestamp);

            TotalOperations = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalOperations / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            Operations = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}