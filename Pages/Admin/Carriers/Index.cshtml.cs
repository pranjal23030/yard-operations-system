using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models;
using YardOps.Models.ViewModels.Carriers;
using YardOps.Services;

namespace YardOps.Pages.Admin.Carriers
{
    [Authorize(Roles = "Admin")]
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

        public List<CarrierViewModel> Carriers { get; set; } = [];
        public List<SelectListItem> StatusOptions { get; set; } = [];

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCarriers { get; set; }

        public bool ShowCreateModal { get; set; }
        public bool ShowEditModal { get; set; }

        [BindProperty] public CreateCarrierInput Input { get; set; } = new();
        [BindProperty] public EditCarrierInput EditInput { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            await LoadFiltersAsync();
            await LoadCarriersAsync();
        }

        // Create Carrier
        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Clear();
            TryValidateModel(Input, nameof(Input));

            if (!ModelState.IsValid)
            {
                ShowCreateModal = true;
                await ReloadPageData();
                return Page();
            }

            // Get current user
            var currentUser = await _userManager.GetUserAsync(User);

            // Generate CarrierCode
            var maxId = await _context.Carriers.MaxAsync(c => (int?)c.CarrierId) ?? 0;
            var carrierCode = CarrierCodeGenerator.GenerateNextCode(maxId);

            var carrier = new Carrier
            {
                CarrierCode = carrierCode,
                CompanyName = Input.CompanyName,
                ContactPerson = Input.ContactPerson,
                Phone = Input.Phone,
                Email = Input.Email,
                Address = Input.Address,
                Status = Input.Status,
                CreatedBy = currentUser?.Id,
                CreatedOn = DateTime.UtcNow
            };

            _context.Carriers.Add(carrier);
            await _context.SaveChangesAsync();

            // Audit: Log carrier creation
            await _activityLogger.LogAsync(
                action: "CreateCarrier",
                description: $"Created carrier {carrier.CompanyName} ({carrier.CarrierCode})",
                extraData: new
                {
                    CarrierCode = carrier.CarrierCode,
                    CompanyName = carrier.CompanyName
                }
            );

            TempData["Success"] = $"Carrier '{carrier.CompanyName}' ({carrier.CarrierCode}) created successfully.";
            return RedirectToPage();
        }

        // Edit Carrier
        public async Task<IActionResult> OnPostEditAsync()
        {
            ModelState.Clear();
            TryValidateModel(EditInput, nameof(EditInput));

            if (!ModelState.IsValid)
            {
                ShowEditModal = true;
                await ReloadPageData();
                return Page();
            }

            var carrier = await _context.Carriers.FindAsync(EditInput.CarrierId);
            if (carrier == null)
            {
                TempData["Error"] = "Carrier not found.";
                return RedirectToPage();
            }

            // Track changed fields for audit log
            var changedFields = new List<string>();

            if (carrier.CompanyName != EditInput.CompanyName)
            {
                changedFields.Add($"CompanyName: '{carrier.CompanyName}' → '{EditInput.CompanyName}'");
                carrier.CompanyName = EditInput.CompanyName;
            }

            if (carrier.ContactPerson != EditInput.ContactPerson)
            {
                changedFields.Add($"ContactPerson: '{carrier.ContactPerson}' → '{EditInput.ContactPerson}'");
                carrier.ContactPerson = EditInput.ContactPerson;
            }

            if (carrier.Phone != EditInput.Phone)
            {
                changedFields.Add($"Phone: '{carrier.Phone}' → '{EditInput.Phone}'");
                carrier.Phone = EditInput.Phone;
            }

            if (carrier.Email != EditInput.Email)
            {
                changedFields.Add($"Email: '{carrier.Email}' → '{EditInput.Email}'");
                carrier.Email = EditInput.Email;
            }

            if (carrier.Address != EditInput.Address)
            {
                changedFields.Add($"Address: '{carrier.Address}' → '{EditInput.Address}'");
                carrier.Address = EditInput.Address;
            }

            if (carrier.Status != EditInput.Status)
            {
                changedFields.Add($"Status: '{carrier.Status}' → '{EditInput.Status}'");
                carrier.Status = EditInput.Status;
            }

            await _context.SaveChangesAsync();

            // Audit: Log carrier edit
            await _activityLogger.LogAsync(
                action: "EditCarrier",
                description: $"Edited carrier {carrier.CompanyName} ({carrier.CarrierCode})",
                extraData: new
                {
                    CarrierCode = carrier.CarrierCode,
                    ChangedFields = changedFields
                }
            );

            TempData["Success"] = $"Carrier '{carrier.CompanyName}' updated successfully.";
            return RedirectToPage();
        }

        // Delete Carrier
        public async Task<IActionResult> OnPostDeleteAsync(int carrierId)
        {
            var carrier = await _context.Carriers.FindAsync(carrierId);

            if (carrier == null)
            {
                TempData["Error"] = "Carrier not found.";
                return RedirectToPage();
            }

            // TODO: Check if carrier has trailers assigned (when trailers feature is implemented)
            // For now, allow deletion

            var carrierName = carrier.CompanyName;
            var carrierCode = carrier.CarrierCode;

            _context.Carriers.Remove(carrier);
            await _context.SaveChangesAsync();

            // Audit: Log carrier deletion
            await _activityLogger.LogAsync(
                action: "DeleteCarrier",
                description: $"Deleted carrier {carrierName} ({carrierCode})",
                extraData: new
                {
                    DeletedCarrierCode = carrierCode,
                    DeletedCompanyName = carrierName
                }
            );

            TempData["Deleted"] = $"Carrier '{carrierName}' has been deleted.";
            return RedirectToPage();
        }

        // Helpers
        private async Task ReloadPageData()
        {
            await LoadFiltersAsync();
            await LoadCarriersAsync();
        }

        private async Task LoadCarriersAsync()
        {
            var query = _context.Carriers
                .Include(c => c.CreatedByUser)
                .AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(c =>
                    c.CompanyName.Contains(SearchTerm) ||
                    c.CarrierCode.Contains(SearchTerm) ||
                    (c.ContactPerson != null && c.ContactPerson.Contains(SearchTerm)));
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "all")
            {
                query = query.Where(c => c.Status == StatusFilter);
            }

            // Order by CreatedOn descending
            query = query.OrderByDescending(c => c.CreatedOn);

            TotalCarriers = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCarriers / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            var carriers = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Carriers = carriers.Select(c => new CarrierViewModel
            {
                CarrierId = c.CarrierId,
                CarrierCode = c.CarrierCode,
                CompanyName = c.CompanyName,
                ContactPerson = c.ContactPerson,
                Phone = c.Phone,
                Email = c.Email,
                Address = c.Address,
                Status = c.Status,
                TrailerCount = 0, // Static for now
                CreatedOn = c.CreatedOn,
                CreatedByEmail = c.CreatedByUser?.Email
            }).ToList();
        }

        private Task LoadFiltersAsync()
        {
            StatusOptions =
            [
                new("All Status", "all"),
                new("Active", "Active"),
                new("Inactive", "Inactive")
            ];
            return Task.CompletedTask;
        }
    }
}