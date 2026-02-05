// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YardOps.Data;

namespace YardOps.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Display properties
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Initials { get; set; }
        public string Role { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string PasswordStatusMessage { get; set; }

        [BindProperty]
        public ProfileInputModel ProfileInput { get; set; }

        [BindProperty]
        public PasswordInputModel PasswordInput { get; set; }

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "First name is required")]
            [Display(Name = "First Name")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last name is required")]
            [Display(Name = "Last Name")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email Address")]
            public string Email { get; set; }

            [Phone(ErrorMessage = "Invalid phone number")]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }
        }

        public class PasswordInputModel
        {
            [Required(ErrorMessage = "Current password is required")]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string OldPassword { get; set; }

            [Required(ErrorMessage = "New password is required")]
            [StringLength(100, ErrorMessage = "Password must be at least {2} characters", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; }

            [Required(ErrorMessage = "Please confirm your new password")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
            public string ConfirmPassword { get; set; }
        }

        private async Task LoadDisplayDataAsync(ApplicationUser user)
        {
            Email = user.Email;
            FullName = $"{user.FirstName} {user.LastName}";
            Initials = $"{(string.IsNullOrEmpty(user.FirstName) ? "?" : user.FirstName[0].ToString())}{(string.IsNullOrEmpty(user.LastName) ? "?" : user.LastName[0].ToString())}".ToUpper();

            var roles = await _userManager.GetRolesAsync(user);
            Role = roles.FirstOrDefault() ?? "User";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadDisplayDataAsync(user);

            ProfileInput = new ProfileInputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            PasswordInput = new PasswordInputModel();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Clear password validation - we're only saving profile
            ModelState.Remove("PasswordInput.OldPassword");
            ModelState.Remove("PasswordInput.NewPassword");
            ModelState.Remove("PasswordInput.ConfirmPassword");

            // Validate only profile fields
            var isValid = true;
            if (string.IsNullOrWhiteSpace(ProfileInput.FirstName))
            {
                ModelState.AddModelError("ProfileInput.FirstName", "First name is required");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(ProfileInput.LastName))
            {
                ModelState.AddModelError("ProfileInput.LastName", "Last name is required");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(ProfileInput.Email))
            {
                ModelState.AddModelError("ProfileInput.Email", "Email is required");
                isValid = false;
            }

            if (!isValid)
            {
                await LoadDisplayDataAsync(user);
                PasswordInput = new PasswordInputModel();
                return Page();
            }

            // Update user properties
            user.FirstName = ProfileInput.FirstName;
            user.LastName = ProfileInput.LastName;
            user.PhoneNumber = ProfileInput.PhoneNumber;

            // Handle email change
            if (user.Email != ProfileInput.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(ProfileInput.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("ProfileInput.Email", "This email is already in use.");
                    await LoadDisplayDataAsync(user);
                    PasswordInput = new PasswordInputModel();
                    return Page();
                }
                user.Email = ProfileInput.Email;
                user.UserName = ProfileInput.Email;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                await LoadDisplayDataAsync(user);
                PasswordInput = new PasswordInputModel();
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Clear profile validation - we're only changing password
            ModelState.Remove("ProfileInput.FirstName");
            ModelState.Remove("ProfileInput.LastName");
            ModelState.Remove("ProfileInput.Email");
            ModelState.Remove("ProfileInput.PhoneNumber");

            // Validate password fields
            var isValid = true;
            if (string.IsNullOrWhiteSpace(PasswordInput.OldPassword))
            {
                ModelState.AddModelError("PasswordInput.OldPassword", "Current password is required");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(PasswordInput.NewPassword))
            {
                ModelState.AddModelError("PasswordInput.NewPassword", "New password is required");
                isValid = false;
            }
            if (PasswordInput.NewPassword != PasswordInput.ConfirmPassword)
            {
                ModelState.AddModelError("PasswordInput.ConfirmPassword", "Passwords do not match");
                isValid = false;
            }

            if (!isValid)
            {
                await LoadDisplayDataAsync(user);
                ProfileInput = new ProfileInputModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, PasswordInput.OldPassword, PasswordInput.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError("PasswordInput.OldPassword", error.Description);
                }
                await LoadDisplayDataAsync(user);
                ProfileInput = new ProfileInputModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                };
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            PasswordStatusMessage = "Your password has been changed successfully.";
            return RedirectToPage();
        }
    }
}
