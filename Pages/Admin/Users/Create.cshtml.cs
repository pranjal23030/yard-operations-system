using ExotracYMS.Data;
using ExotracYMS.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ExotracYMS.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public CreateUserDto Input { get; set; } = new();

        public List<SelectListItem> Roles { get; set; } = [];

        public async Task OnGetAsync()
        {
            Roles = await _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Name!,
                    Text = r.Name!
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await OnGetAsync(); // reload roles if validation fails

            if (!ModelState.IsValid)
                return Page();

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
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return Page();
            }

            await _userManager.AddToRoleAsync(user, Input.Role);

            TempData["Success"] = "User created successfully";
            return RedirectToPage("./Index");
        }
    }
}
