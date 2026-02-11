// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YardOps.Data;
using YardOps.Models.ViewModels.Profile;
using YardOps.Services;

namespace YardOps.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ActivityLogger _activityLogger;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ActivityLogger activityLogger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _activityLogger = activityLogger;
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
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

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
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

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

            // Track changed fields for audit log
            var changedFields = new List<string>();

            // Update user properties
            if (user.FirstName != ProfileInput.FirstName)
            {
                changedFields.Add($"FirstName: '{user.FirstName}' → '{ProfileInput.FirstName}'");
                user.FirstName = ProfileInput.FirstName;
            }

            if (user.LastName != ProfileInput.LastName)
            {
                changedFields.Add($"LastName: '{user.LastName}' → '{ProfileInput.LastName}'");
                user.LastName = ProfileInput.LastName;
            }

            if (user.PhoneNumber != ProfileInput.PhoneNumber)
            {
                changedFields.Add($"PhoneNumber: '{user.PhoneNumber ?? "None"}' → '{ProfileInput.PhoneNumber ?? "None"}'");
                user.PhoneNumber = ProfileInput.PhoneNumber;
            }

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
                changedFields.Add($"Email: '{user.Email}' → '{ProfileInput.Email}'");
                user.Email = ProfileInput.Email;
                user.UserName = ProfileInput.Email;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await LoadDisplayDataAsync(user);
                PasswordInput = new PasswordInputModel();
                return Page();
            }

            // Audit: Log profile update (only if changes were made)
            if (changedFields.Count > 0)
            {
                await _activityLogger.LogAsync(
                    action: "UpdateProfile",
                    description: $"Updated profile for {user.Email}",
                    extraData: new
                    {
                        UserId = user.Id,
                        ChangedFields = changedFields
                    }
                );
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

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
                    ModelState.AddModelError("PasswordInput.OldPassword", error.Description);

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

            // Audit: Log password change (do NOT log passwords)
            await _activityLogger.LogAsync(
                action: "ChangePassword",
                description: $"Changed password for {user.Email}",
                extraData: new
                {
                    UserId = user.Id
                }
            );

            await _signInManager.RefreshSignInAsync(user);
            PasswordStatusMessage = "Your password has been changed successfully.";
            return RedirectToPage();
        }
    }
}
