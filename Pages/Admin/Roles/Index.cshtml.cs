using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models.ViewModels.Roles;
using YardOps.Services;

namespace YardOps.Pages.Admin.Roles
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ActivityLogger _activityLogger;

        private const int PageSize = 10;

        public IndexModel(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ActivityLogger activityLogger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        public List<RoleViewModel> Roles { get; set; } = [];

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRoles { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSystemRoles { get; set; }

        public bool ShowCreateModal { get; set; }
        public bool ShowEditModal { get; set; }

        [BindProperty] public CreateRoleInput Input { get; set; } = new();
        [BindProperty] public EditRoleInput EditInput { get; set; } = new();

        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            await LoadRolesAsync();
            await LoadSummaryAsync();
        }

        // Create Role
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

            // Check if role name already exists
            if (await _roleManager.RoleExistsAsync(Input.Name))
            {
                ModelState.AddModelError("Input.Name", "Role name already exists.");
                ShowCreateModal = true;
                await ReloadPageData();
                return Page();
            }

            var role = new ApplicationRole
            {
                Name = Input.Name,
                Description = Input.Description,
                Status = Input.Status,
                IsSystemRole = false, // New roles are NOT system roles
                CreatedAt = DateTime.UtcNow
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                ShowCreateModal = true;
                await ReloadPageData();
                return Page();
            }

            // Audit: Log role creation
            await _activityLogger.LogAsync(
                action: "CreateRole",
                description: $"Created role {Input.Name}",
                extraData: new
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    Description = role.Description,
                    Status = role.Status
                }
            );

            TempData["Success"] = $"Role '{role.Name}' created successfully.";
            return RedirectToPage();
        }

        // Edit Role
        public async Task<IActionResult> OnPostEditAsync()
        {
            var role = await _roleManager.FindByIdAsync(EditInput.RoleId);
            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToPage();
            }

            // Track changed fields for audit log
            var changedFields = new List<string>();
            var originalName = role.Name;

            // For system roles, preserve the original name (ignore what was submitted)
            if (role.IsSystemRole)
            {
                EditInput.Name = role.Name!;
            }

            ModelState.Clear();
            TryValidateModel(EditInput, nameof(EditInput));

            if (!ModelState.IsValid)
            {
                ShowEditModal = true;
                await ReloadPageData();
                return Page();
            }

            // If it's NOT a system role, check if new name already exists
            if (!role.IsSystemRole)
            {
                if (role.Name != EditInput.Name && await _roleManager.RoleExistsAsync(EditInput.Name))
                {
                    ModelState.AddModelError("EditInput.Name", "Role name already exists.");
                    ShowEditModal = true;
                    await ReloadPageData();
                    return Page();
                }

                if (role.Name != EditInput.Name)
                {
                    changedFields.Add($"Name: '{role.Name}' → '{EditInput.Name}'");
                    role.Name = EditInput.Name;
                }
            }

            if (role.Description != EditInput.Description)
            {
                changedFields.Add($"Description: '{role.Description}' → '{EditInput.Description}'");
                role.Description = EditInput.Description;
            }

            if (role.Status != EditInput.Status)
            {
                changedFields.Add($"Status: '{role.Status}' → '{EditInput.Status}'");
                role.Status = EditInput.Status;
            }

            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                ShowEditModal = true;
                await ReloadPageData();
                return Page();
            }

            // Audit: Log role edit with changed fields
            await _activityLogger.LogAsync(
                action: "EditRole",
                description: $"Edited role {role.Name}",
                extraData: new
                {
                    RoleId = role.Id,
                    IsSystemRole = role.IsSystemRole,
                    ChangedFields = changedFields
                }
            );

            TempData["Success"] = $"Role '{role.Name}' updated successfully.";
            return RedirectToPage();
        }

        // Delete Role
        public async Task<IActionResult> OnPostDeleteAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);

            if (role == null)
            {
                TempData["Error"] = "Role not found.";
                return RedirectToPage();
            }

            // System roles cannot be deleted
            if (role.IsSystemRole)
            {
                TempData["Error"] = "System roles cannot be deleted.";
                return RedirectToPage();
            }

            // Check if role has assigned users
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count > 0)
            {
                TempData["Error"] = $"Cannot delete role '{role.Name}' because it has {usersInRole.Count} assigned user(s). Please reassign users first.";
                return RedirectToPage();
            }

            // Store role info before deletion
            var roleName = role.Name;

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Failed to delete role. Please try again.";
                return RedirectToPage();
            }

            // Audit: Log role deletion
            await _activityLogger.LogAsync(
                action: "DeleteRole",
                description: $"Deleted role {roleName}",
                extraData: new
                {
                    DeletedRoleId = roleId,
                    DeletedRoleName = roleName
                }
            );

            TempData["Deleted"] = $"Role '{roleName}' has been deleted.";
            return RedirectToPage();
        }

        // Helpers
        private async Task ReloadPageData()
        {
            await LoadRolesAsync();
            await LoadSummaryAsync();
        }

        private async Task LoadRolesAsync()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var list = new List<RoleViewModel>();

            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

                list.Add(new RoleViewModel
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Description = role.Description,
                    Status = role.Status,
                    IsSystemRole = role.IsSystemRole,
                    UserCount = usersInRole.Count,
                    CreatedAt = role.CreatedAt
                });
            }

            // Custom ordering: Admin first, YardManager second, Driver third, then custom roles by CreatedAt descending
            var orderedList = list
                .OrderByDescending(r => r.Name == "Admin")
                .ThenByDescending(r => r.Name == "YardManager")
                .ThenByDescending(r => r.Name == "Driver")
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            TotalRoles = orderedList.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRoles / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            Roles = orderedList.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }

        private async Task LoadSummaryAsync()
        {
            TotalUsers = await _userManager.Users.CountAsync();
            TotalSystemRoles = await _roleManager.Roles.CountAsync(r => r.IsSystemRole);
        }
    }
}