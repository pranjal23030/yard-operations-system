// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using YardOps.Data;
using YardOps.Services;

namespace YardOps.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ActivityLogger _activityLogger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger,
            ActivityLogger activityLogger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _activityLogger = activityLogger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public string LoginErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Check if user exists
                var user = await _userManager.FindByEmailAsync(Input.Email);
                
                if (user == null)
                {
                    LoginErrorMessage = "Invalid email or password. Please try again.";
                    return Page();
                }

                // Email must be confirmed
                if (!user.EmailConfirmed)
                {
                    LoginErrorMessage = "Please confirm your email before logging in. Check your inbox for the confirmation link.";
                    return Page();
                }

                // Account must be Active
                if (user.Status != "Active")
                {
                    _logger.LogWarning($"Login blocked for {user.Email} - Status: {user.Status}");
                    
                    // Provide specific message based on status
                    LoginErrorMessage = user.Status switch
                    {
                        "Inactive" => "Your account has been deactivated. Please contact an administrator for assistance.",
                        "Pending" => "Your account is pending activation. Please contact an administrator.",
                        _ => "Your account is not active. Please contact an administrator."
                    };
                    
                    return Page();
                }

                // Attempt password sign-in
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    // Update last login timestamp
                    user.LastLogin = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("User logged in.");

                    // Audit: Log successful login
                    await _activityLogger.LogAsync(
                        action: "Login",
                        description: $"User {Input.Email} logged in successfully"
                    );

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    LoginErrorMessage = "Invalid email or password. Please try again.";
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
