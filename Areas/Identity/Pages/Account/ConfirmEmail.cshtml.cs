#nullable disable

using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using YardOps.Data;

namespace YardOps.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string DefaultPassword { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            // Get default password from configuration
            DefaultPassword = _configuration["DefaultUserPassword"] ?? "Password@123";

            if (userId == null || code == null)
            {
                IsSuccess = false;
                ErrorMessage = "Invalid confirmation link. Please contact your administrator.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                IsSuccess = false;
                ErrorMessage = "User not found. The account may have been deleted.";
                return Page();
            }

            if (user.EmailConfirmed)
            {
                IsSuccess = true;
                return Page();
            }

            try
            {
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

                if (result.Succeeded)
                {
                    // Activate the user account
                    user.Status = "Active";
                    await _userManager.UpdateAsync(user);
                    
                    IsSuccess = true;
                }
                else
                {
                    IsSuccess = false;
                    ErrorMessage = "The confirmation link is invalid or has expired. Please contact your administrator to resend the confirmation email.";
                }
            }
            catch
            {
                IsSuccess = false;
                ErrorMessage = "An error occurred while confirming your email. Please try again or contact your administrator.";
            }

            return Page();
        }
    }
}