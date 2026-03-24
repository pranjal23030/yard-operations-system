using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YardOps.Data;
using YardOps.Models;
using YardOps.Models.ViewModels.Trailers;
using YardOps.Services;

namespace YardOps.Pages.Admin.Trailers
{
    [Authorize(Roles = "Admin,YardManager")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ActivityLogger _activityLogger;

        private const int PageSize = 10;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ActivityLogger activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        public List<TrailerViewModel> Trailers { get; set; } = [];
        public List<SelectListItem> StatusOptions { get; set; } = [];
        public List<SelectListItem> CarrierOptions { get; set; } = [];
        public List<SelectListItem> LocationOptions { get; set; } = [];
        public List<SelectListItem> AssignableLocationOptions { get; set; } = [];
        public List<SelectListItem> GoodsTypeOptions { get; set; } = [];
        public List<SelectListItem> DriverOptions { get; set; } = [];

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalTrailers { get; set; }

        public bool ShowCreateModal { get; set; }
        public bool ShowEditModal { get; set; }

        [BindProperty] public CreateTrailerInput Input { get; set; } = new();
        [BindProperty] public EditTrailerInput EditInput { get; set; } = new();

        [BindProperty] public string GoodsJson { get; set; } = "[]";
        [BindProperty] public string EditGoodsJson { get; set; } = "[]";

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? CarrierFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? LocationFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            await LoadFiltersAsync();
            await LoadTrailersAsync();
        }

        // AJAX details for no-refresh View/Edit modal
        public async Task<IActionResult> OnGetTrailerDetailsAsync(int trailerId)
        {
            var trailer = await _context.Trailers
                .Include(t => t.Carrier)
                .Include(t => t.DriverUser)
                .Include(t => t.CurrentLocation)
                .Include(t => t.CreatedByUser)
                .Include(t => t.GoodsItems!)
                    .ThenInclude(g => g.CreatedByUser)
                .FirstOrDefaultAsync(t => t.TrailerId == trailerId);

            if (trailer == null)
                return NotFound();

            var history = await _context.TrailerHistories
                .Where(h => h.TrailerId == trailerId)
                .Include(h => h.Location)
                .Include(h => h.CreatedByUser)
                .OrderByDescending(h => h.StartTime)
                .ToListAsync();

            return new JsonResult(new
            {
                trailerId = trailer.TrailerId,
                trailerCode = trailer.TrailerCode,
                carrierId = trailer.CarrierId,
                carrierName = trailer.Carrier?.CompanyName ?? "—",
                driverUserId = trailer.DriverUserId,
                driverName = trailer.DriverUser != null ? $"{trailer.DriverUser.FirstName} {trailer.DriverUser.LastName}".Trim() : "—",
                driverContact = trailer.DriverUser?.PhoneNumber ?? trailer.DriverUser?.Email ?? "—",
                goodsType = trailer.GoodsType,
                currentStatus = trailer.CurrentStatus,
                locationId = trailer.CurrentLocationId,
                locationName = trailer.CurrentLocation?.LocationName ?? (trailer.CurrentStatus == "Checked Out" ? "Departed" : "—"),
                arrival = trailer.ArrivalTime?.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt"),
                departure = trailer.DepartureTime?.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt"),
                goods = (trailer.GoodsItems ?? []).Select(g => new
                {
                    description = g.Description,
                    weight = g.Weight,
                    quantity = g.Quantity,
                    handlingNotes = g.HandlingNotes,
                    totalWeight = g.Weight * g.Quantity,
                    createdByEmail = g.CreatedByUser?.Email ?? "Unknown"
                }),
                history = history.Select(h => new
                {
                    locationName = h.Location?.LocationName ?? "—",
                    startTime = h.StartTime.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt"),
                    endTime = h.EndTime.HasValue ? h.EndTime.Value.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt") : "Active",
                    dwellTimeMinutes = h.DwellTimeMinutes,
                    createdByEmail = h.CreatedByUser?.Email ?? "Unknown"
                })
            });
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Clear();
            TryValidateModel(Input, nameof(Input));

            var goodsItems = ParseGoodsJson(GoodsJson);
            if (goodsItems.Count == 0)
                ModelState.AddModelError("", "At least one goods item is required.");

            if (Input.LocationId.HasValue)
            {
                var locationType = await _context.Locations
                    .Where(l => l.LocationId == Input.LocationId.Value)
                    .Select(l => l.LocationType)
                    .FirstOrDefaultAsync();

                if (locationType == null)
                    ModelState.AddModelError("Input.LocationId", "Selected location is invalid.");
                else if (locationType == "Gate")
                    ModelState.AddModelError("Input.LocationId", "Gate cannot be assigned from Trailer master data.");
            }

            if (!ModelState.IsValid)
            {
                ShowCreateModal = true;
                await ReloadPageDataAsync();
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var maxId = await _context.Trailers.MaxAsync(t => (int?)t.TrailerId) ?? 0;
            var trailerCode = TrailerCodeGenerator.GenerateNextCode(maxId);

            var trailer = new Trailer
            {
                TrailerCode = trailerCode,
                CarrierId = Input.CarrierId,
                DriverUserId = Input.DriverUserId,
                CurrentStatus = Input.CurrentStatus,
                GoodsType = Input.GoodsType,
                CurrentLocationId = Input.LocationId,
                ArrivalTime = Input.CurrentStatus is "Incoming" or "In-Yard" ? DateTime.UtcNow : null,
                CreatedBy = currentUser?.Id,
                CreatedOn = DateTime.UtcNow
            };

            _context.Trailers.Add(trailer);
            await _context.SaveChangesAsync();

            foreach (var item in goodsItems)
            {
                _context.Goods.Add(new Goods
                {
                    TrailerId = trailer.TrailerId,
                    Description = item.Description.Trim(),
                    Weight = item.Weight,
                    Quantity = item.Quantity,
                    HandlingNotes = item.HandlingNotes?.Trim(),
                    CreatedBy = currentUser?.Id,
                    CreatedOn = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var driver = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == trailer.DriverUserId);
            var carrier = await _context.Carriers.AsNoTracking().FirstOrDefaultAsync(c => c.CarrierId == trailer.CarrierId);
            var location = trailer.CurrentLocationId.HasValue
                ? await _context.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.LocationId == trailer.CurrentLocationId.Value)
                : null;

            await _activityLogger.LogAsync(
                action: "CreateTrailer",
                description: $"Created trailer {trailer.TrailerCode}",
                extraData: new
                {
                    TrailerCode = trailer.TrailerCode,
                    Carrier = carrier?.CompanyName ?? "Unknown",
                    DriverName = driver != null ? $"{driver.FirstName} {driver.LastName}".Trim() : "Unknown",
                    DriverContact = driver?.PhoneNumber ?? driver?.Email ?? "Unknown",
                    GoodsType = trailer.GoodsType,
                    Status = trailer.CurrentStatus,
                    Location = location?.LocationName ?? "Not Assigned",
                    GoodsCount = goodsItems.Count
                });

            TempData["Success"] = $"Trailer '{trailer.TrailerCode}' created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            ModelState.Clear();
            TryValidateModel(EditInput, nameof(EditInput));

            var goodsItems = ParseGoodsJson(EditGoodsJson);
            if (goodsItems.Count == 0)
                ModelState.AddModelError("", "At least one goods item is required.");

            if (EditInput.LocationId.HasValue)
            {
                var locationType = await _context.Locations
                    .Where(l => l.LocationId == EditInput.LocationId.Value)
                    .Select(l => l.LocationType)
                    .FirstOrDefaultAsync();

                if (locationType == null)
                    ModelState.AddModelError("EditInput.LocationId", "Selected location is invalid.");
                else if (locationType == "Gate")
                    ModelState.AddModelError("EditInput.LocationId", "Gate cannot be assigned from Trailer master data.");
            }

            if (!ModelState.IsValid)
            {
                ShowEditModal = true;
                await ReloadPageDataAsync();
                return Page();
            }

            var trailer = await _context.Trailers
                .Include(t => t.GoodsItems)
                .FirstOrDefaultAsync(t => t.TrailerId == EditInput.TrailerId);

            if (trailer == null)
            {
                TempData["Error"] = "Trailer not found.";
                return RedirectToPage();
            }

            var oldCarrierId = trailer.CarrierId;
            var oldDriverUserId = trailer.DriverUserId;
            var oldGoodsType = trailer.GoodsType;
            var oldStatus = trailer.CurrentStatus;
            var oldLocationId = trailer.CurrentLocationId;

            trailer.CarrierId = EditInput.CarrierId;
            trailer.DriverUserId = EditInput.DriverUserId;
            trailer.GoodsType = EditInput.GoodsType;
            trailer.CurrentStatus = EditInput.CurrentStatus;
            trailer.CurrentLocationId = EditInput.LocationId;

            if (trailer.GoodsItems?.Any() == true)
                _context.Goods.RemoveRange(trailer.GoodsItems);

            var currentUser = await _userManager.GetUserAsync(User);

            foreach (var item in goodsItems)
            {
                _context.Goods.Add(new Goods
                {
                    TrailerId = trailer.TrailerId,
                    Description = item.Description.Trim(),
                    Weight = item.Weight,
                    Quantity = item.Quantity,
                    HandlingNotes = item.HandlingNotes?.Trim(),
                    CreatedBy = currentUser?.Id,
                    CreatedOn = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var carrierMap = await _context.Carriers
                .Where(c => c.CarrierId == oldCarrierId || c.CarrierId == trailer.CarrierId)
                .ToDictionaryAsync(c => c.CarrierId, c => c.CompanyName);

            var locationIds = new List<int>();
            if (oldLocationId.HasValue) locationIds.Add(oldLocationId.Value);
            if (trailer.CurrentLocationId.HasValue) locationIds.Add(trailer.CurrentLocationId.Value);

            var locationMap = await _context.Locations
                .Where(l => locationIds.Contains(l.LocationId))
                .ToDictionaryAsync(l => l.LocationId, l => l.LocationName);

            var userIds = new List<string>();
            if (!string.IsNullOrWhiteSpace(oldDriverUserId)) userIds.Add(oldDriverUserId);
            if (!string.IsNullOrWhiteSpace(trailer.DriverUserId)) userIds.Add(trailer.DriverUserId);

            var userMap = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

            var changedFields = new List<string>();

            if (oldCarrierId != trailer.CarrierId)
            {
                var oldName = carrierMap.TryGetValue(oldCarrierId, out var oc) ? oc : oldCarrierId.ToString();
                var newName = carrierMap.TryGetValue(trailer.CarrierId, out var nc) ? nc : trailer.CarrierId.ToString();
                changedFields.Add($"Carrier: '{oldName}' → '{newName}'");
            }

            if (oldDriverUserId != trailer.DriverUserId)
            {
                var oldDriver = !string.IsNullOrWhiteSpace(oldDriverUserId) && userMap.TryGetValue(oldDriverUserId, out var od) ? od : "Unknown";
                var newDriver = !string.IsNullOrWhiteSpace(trailer.DriverUserId) && userMap.TryGetValue(trailer.DriverUserId, out var nd) ? nd : "Unknown";
                changedFields.Add($"Driver: '{oldDriver}' → '{newDriver}'");
            }

            if (oldGoodsType != trailer.GoodsType)
                changedFields.Add($"GoodsType: '{oldGoodsType}' → '{trailer.GoodsType}'");

            if (oldStatus != trailer.CurrentStatus)
                changedFields.Add($"Status: '{oldStatus}' → '{trailer.CurrentStatus}'");

            if (oldLocationId != trailer.CurrentLocationId)
            {
                var oldLoc = oldLocationId.HasValue && locationMap.TryGetValue(oldLocationId.Value, out var ol) ? ol : "Not Assigned";
                var newLoc = trailer.CurrentLocationId.HasValue && locationMap.TryGetValue(trailer.CurrentLocationId.Value, out var nl) ? nl : "Not Assigned";
                changedFields.Add($"Location: '{oldLoc}' → '{newLoc}'");
            }

            await _activityLogger.LogAsync(
                action: "EditTrailer",
                description: $"Edited trailer {trailer.TrailerCode}",
                extraData: new
                {
                    TrailerCode = trailer.TrailerCode,
                    ChangedFields = changedFields,
                    GoodsCount = goodsItems.Count
                });

            TempData["Success"] = $"Trailer '{trailer.TrailerCode}' updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int trailerId)
        {
            var trailer = await _context.Trailers.FirstOrDefaultAsync(t => t.TrailerId == trailerId);
            if (trailer == null)
            {
                TempData["Error"] = "Trailer not found.";
                return RedirectToPage();
            }

            var trailerCode = trailer.TrailerCode;

            _context.Trailers.Remove(trailer);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(
                action: "DeleteTrailer",
                description: $"Deleted trailer {trailerCode}",
                extraData: new { TrailerCode = trailerCode });

            TempData["Success"] = $"Trailer '{trailerCode}' deleted successfully.";
            return RedirectToPage();
        }

        // kept for backward compatibility (not used in UI)
        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            var trailers = await BuildFilteredQuery().OrderByDescending(t => t.CreatedOn).ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("TrailerCode,Carrier,Driver,GoodsType,Status,Location,Arrival,Departure,GoodsCount,TotalWeight");

            foreach (var t in trailers)
            {
                var driverName = t.DriverUser != null ? $"{t.DriverUser.FirstName} {t.DriverUser.LastName}".Trim() : "—";
                var carrier = EscapeCsv(t.Carrier?.CompanyName ?? "—");
                var location = EscapeCsv(t.CurrentLocation?.LocationName ?? "—");
                var goodsCount = t.GoodsItems?.Count ?? 0;
                var totalWeight = t.GoodsItems?.Sum(g => g.Weight * g.Quantity) ?? 0m;

                sb.AppendLine($"{EscapeCsv(t.TrailerCode)},{carrier},{EscapeCsv(driverName)},{EscapeCsv(t.GoodsType)},{EscapeCsv(t.CurrentStatus)},{location},{t.ArrivalTime:yyyy-MM-dd HH:mm},{t.DepartureTime:yyyy-MM-dd HH:mm},{goodsCount},{totalWeight}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"trailers_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            var trailers = await BuildFilteredQuery().OrderByDescending(t => t.CreatedOn).ToListAsync();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("Trailers Report").FontSize(18).SemiBold();
                        column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd hh:mm tt}").FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.0f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2.4f);
                            c.RelativeColumn(1.1f);
                        });

                        void HeaderCell(string text) => table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).SemiBold();

                        HeaderCell("Trailer");
                        HeaderCell("Carrier");
                        HeaderCell("Driver");
                        HeaderCell("Type");
                        HeaderCell("Status");
                        HeaderCell("Location");
                        HeaderCell("Arrival");
                        HeaderCell("Departure");
                        HeaderCell("Goods Details");
                        HeaderCell("Weight");

                        foreach (var t in trailers)
                        {
                            var driverName = t.DriverUser != null ? $"{t.DriverUser.FirstName} {t.DriverUser.LastName}".Trim() : "—";
                            var totalWeight = t.GoodsItems?.Sum(g => g.Weight * g.Quantity) ?? 0m;

                            var goodsText = (t.GoodsItems?.Any() == true)
                                ? string.Join(", ", t.GoodsItems.Select(g => $"{g.Description} (x{g.Quantity})"))
                                : "No goods";

                            table.Cell().Padding(3).Text(t.TrailerCode);
                            table.Cell().Padding(3).Text(t.Carrier?.CompanyName ?? "—");
                            table.Cell().Padding(3).Text(driverName);
                            table.Cell().Padding(3).Text(t.GoodsType);
                            table.Cell().Padding(3).Text(t.CurrentStatus);
                            table.Cell().Padding(3).Text(t.CurrentLocation?.LocationName ?? (t.CurrentStatus == "Checked Out" ? "Departed" : "—"));
                            table.Cell().Padding(3).Text(t.ArrivalTime.HasValue ? t.ArrivalTime.Value.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt") : "—");
                            table.Cell().Padding(3).Text(t.DepartureTime.HasValue ? t.DepartureTime.Value.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt") : "—");
                            table.Cell().Padding(3).Text(goodsText);
                            table.Cell().Padding(3).Text(totalWeight.ToString("0.##"));
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

            return File(pdfBytes, "application/pdf", $"trailers_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }

        private async Task ReloadPageDataAsync()
        {
            await LoadFiltersAsync();
            await LoadTrailersAsync();
        }

        private IQueryable<Trailer> BuildFilteredQuery()
        {
            var query = _context.Trailers
                .Include(t => t.Carrier)
                .Include(t => t.DriverUser)
                .Include(t => t.CurrentLocation)
                .Include(t => t.GoodsItems)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();

                query = query.Where(t =>
                    t.TrailerCode.Contains(term) ||
                    t.GoodsType.Contains(term) ||
                    t.CurrentStatus.Contains(term) ||
                    (t.Carrier != null && t.Carrier.CompanyName.Contains(term)) ||
                    (t.DriverUser != null &&
                     ((t.DriverUser.FirstName + " " + t.DriverUser.LastName).Contains(term) ||
                      (t.DriverUser.UserName != null && t.DriverUser.UserName.Contains(term)) ||
                      (t.DriverUser.Email != null && t.DriverUser.Email.Contains(term)))));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "all")
                query = query.Where(t => t.CurrentStatus == StatusFilter);

            if (!string.IsNullOrWhiteSpace(CarrierFilter) && CarrierFilter != "all" && int.TryParse(CarrierFilter, out var carrierId))
                query = query.Where(t => t.CarrierId == carrierId);

            if (!string.IsNullOrWhiteSpace(LocationFilter) && LocationFilter != "all" && int.TryParse(LocationFilter, out var locationId))
                query = query.Where(t => t.CurrentLocationId == locationId);

            return query;
        }

        private async Task LoadTrailersAsync()
        {
            var query = BuildFilteredQuery().OrderByDescending(t => t.CreatedOn);

            TotalTrailers = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalTrailers / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            var trailers = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToListAsync();

            Trailers = trailers.Select(t => new TrailerViewModel
            {
                TrailerId = t.TrailerId,
                TrailerCode = t.TrailerCode,
                CarrierId = t.CarrierId,
                CarrierName = t.Carrier?.CompanyName ?? "—",
                DriverUserId = t.DriverUserId,
                DriverName = t.DriverUser != null ? $"{t.DriverUser.FirstName} {t.DriverUser.LastName}".Trim() : "—",
                DriverContact = t.DriverUser?.PhoneNumber,
                GoodsType = t.GoodsType,
                CurrentStatus = t.CurrentStatus,
                LocationId = t.CurrentLocationId,
                LocationName = t.CurrentLocation?.LocationName ?? (t.CurrentStatus == "Checked Out" ? "Departed" : "—"),
                ArrivalTime = t.ArrivalTime,
                DepartureTime = t.DepartureTime,
                CreatedOn = t.CreatedOn,
                CreatedByEmail = t.CreatedByUser?.Email,
                GoodsItems = (t.GoodsItems ?? []).Select(g => new GoodsViewModel
                {
                    GoodsId = g.GoodsId,
                    Description = g.Description,
                    Weight = g.Weight,
                    Quantity = g.Quantity,
                    HandlingNotes = g.HandlingNotes,
                    CreatedByEmail = g.CreatedByUser?.Email
                }).ToList()
            }).ToList();
        }

        private async Task LoadFiltersAsync()
        {
            StatusOptions =
            [
                new("All Status", "all"),
                new("Incoming", "Incoming"),
                new("In-Yard", "In-Yard"),
                new("Outgoing", "Outgoing"),
                new("Checked Out", "Checked Out")
            ];

            GoodsTypeOptions =
            [
                new("General", "General"),
                new("Hazmat", "Hazmat"),
                new("Refrigerated", "Refrigerated"),
                new("Oversized", "Oversized")
            ];

            CarrierOptions =
            [
                new("All Carriers", "all"),
                .. (await _context.Carriers
                    .OrderBy(c => c.CompanyName)
                    .Select(c => new SelectListItem(c.CompanyName, c.CarrierId.ToString()))
                    .ToListAsync())
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

            AssignableLocationOptions =
            [
                new("All Locations", "all"),
                .. (await _context.Locations
                    .Where(l => l.LocationType != "Gate")
                    .OrderBy(l => l.LocationType)
                    .ThenBy(l => l.LocationName)
                    .Select(l => new SelectListItem($"{l.LocationName} ({l.LocationType})", l.LocationId.ToString()))
                    .ToListAsync())
            ];

            var drivers = await _userManager.GetUsersInRoleAsync("Driver");
            DriverOptions = drivers
                .OrderBy(d => d.FirstName)
                .ThenBy(d => d.LastName)
                .Select(d => new SelectListItem(
                    string.IsNullOrWhiteSpace($"{d.FirstName} {d.LastName}".Trim())
                        ? (d.Email ?? d.UserName ?? "Driver")
                        : $"{d.FirstName} {d.LastName}".Trim(),
                    d.Id))
                .ToList();
        }

        private static List<CreateGoodsInput> ParseGoodsJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<List<CreateGoodsInput>>(json, options);

                return data?.Where(g =>
                    !string.IsNullOrWhiteSpace(g.Description) &&
                    g.Weight > 0 &&
                    g.Quantity > 0).ToList() ?? [];
            }
            catch
            {
                return [];
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";

            return value;
        }
    }
}