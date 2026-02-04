using YardOps.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace YardOps.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public List<UserViewModel> Users { get; set; } = new();
        public List<SelectListItem> RoleOptions { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RoleFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Get all users
            var allUsers = await _userManager.Users.ToListAsync();

            // Build user view models with roles
            var userViewModels = new List<UserViewModel>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    Status = user.Status,
                    LastLogin = user.LastLogin,
                    AssignedLocation = "All Locations"
                });
            }

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                userViewModels = userViewModels.Where(u =>
                    u.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(RoleFilter) && RoleFilter != "All")
            {
                userViewModels = userViewModels.Where(u => u.Role == RoleFilter).ToList();
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                userViewModels = userViewModels.Where(u => u.Status == StatusFilter).ToList();
            }

            Users = userViewModels;

            // Populate filter dropdowns
            RoleOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "All", Text = "All Roles" }
            };
            RoleOptions.AddRange(
                (await _roleManager.Roles.ToListAsync())
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
            );

            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "All", Text = "All Status" },
                new SelectListItem { Value = "Active", Text = "Active" },
                new SelectListItem { Value = "Inactive", Text = "Inactive" },
                new SelectListItem { Value = "Pending", Text = "Pending" }
            };
        }

        public class UserViewModel
        {
            public string Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
            public string AssignedLocation { get; set; }
            public string Status { get; set; }
            public DateTime? LastLogin { get; set; }
        }
    }
}
