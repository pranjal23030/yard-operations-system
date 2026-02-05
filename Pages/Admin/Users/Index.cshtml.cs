using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models.ViewModels.Users;

namespace YardOps.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private const int PageSize = 10;

        public IndexModel(UserManager<ApplicationUser> userManager,
                          RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> Roles { get; set; } = new();

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalUsers { get; set; }

        public bool ShowCreateModal { get; set; }
        public bool ShowEditModal { get; set; }

        [BindProperty] public CreateUserInput Input { get; set; } = new();
        [BindProperty] public EditUserInput EditInput { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? RoleFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string? StatusFilter { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            await LoadFiltersAsync();
            await LoadUsersAsync();
            await LoadRolesForCreate();
        }

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

            if (await _userManager.FindByEmailAsync(Input.Email) != null)
            {
                ModelState.AddModelError("Input.Email", "Email already exists.");
                ShowCreateModal = true;
                await ReloadPageData();
                return Page();
            }

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                ShowCreateModal = true;
                await ReloadPageData();
                return Page();
            }

            await _userManager.AddToRoleAsync(user, Input.Role);
            TempData["Success"] = $"{user.FirstName} {user.LastName} created successfully.";
            return RedirectToPage();
        }

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

            var user = await _userManager.FindByIdAsync(EditInput.UserId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            user.FirstName = EditInput.FirstName;
            user.LastName = EditInput.LastName;
            user.Status = EditInput.Status;

            if (user.Email != EditInput.Email)
            {
                user.Email = EditInput.Email;
                user.UserName = EditInput.Email;
            }

            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault();

            if (currentRole != EditInput.Role)
            {
                if (currentRole != null)
                    await _userManager.RemoveFromRoleAsync(user, currentRole);

                await _userManager.AddToRoleAsync(user, EditInput.Role);
            }

            TempData["Success"] = $"{user.FirstName} {user.LastName} updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var currentUser = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }

            if (user.Id == currentUser?.Id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToPage();
            }

            await _userManager.DeleteAsync(user);
            TempData["Deleted"] = $"{user.FirstName} {user.LastName} has been removed from the system.";
            return RedirectToPage();
        }

        private async Task ReloadPageData()
        {
            await LoadFiltersAsync();
            await LoadUsersAsync();
            await LoadRolesForCreate();
        }

        private async Task LoadUsersAsync()
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.Contains(SearchTerm) ||
                    u.LastName.Contains(SearchTerm) ||
                    u.Email!.Contains(SearchTerm));

            var users = await usersQuery.ToListAsync();
            var list = new List<UserViewModel>();

            foreach (var user in users)
            {
                var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "No Role";

                if (RoleFilter != null && RoleFilter != "all" && role != RoleFilter) continue;
                if (StatusFilter != null && StatusFilter != "all" && user.Status != StatusFilter) continue;

                list.Add(new UserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    Role = role,
                    Status = user.Status,
                    LastLogin = user.LastLogin,
                    AssignedLocation = "Not Assigned",
                    CreatedAt = user.CreatedAt
                });
            }

            TotalUsers = list.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalUsers / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            Users = list.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }

        private async Task LoadFiltersAsync()
        {
            RoleOptions = new() { new("All Roles", "all") };
            RoleOptions.AddRange(await _roleManager.Roles
                .Select(r => new SelectListItem(r.Name!, r.Name!))
                .ToListAsync());

            StatusOptions = new()
            {
                new("All Statuses", "all"),
                new("Active", "Active"),
                new("Inactive", "Inactive"),
                new("Pending", "Pending")
            };
        }

        private async Task LoadRolesForCreate()
        {
            Roles = await _roleManager.Roles
                .Select(r => new SelectListItem(r.Name!, r.Name!))
                .ToListAsync();
        }
    }
}
