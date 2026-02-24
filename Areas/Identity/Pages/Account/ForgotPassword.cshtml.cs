// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using YardOps.Data;

namespace YardOps.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                
                // Security: Don't reveal whether user exists or not
                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Generate password reset token
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code, email = Input.Email },
                    protocol: Request.Scheme);

                // Send professional HTML email
                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                        <div style='background: linear-gradient(135deg, #1c3c56 0%, #2da49d 100%); padding: 30px; text-align: center;'>
                            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>YardOps</h1>
                            <p style='color: rgba(255, 255, 255, 0.9); margin: 5px 0 0 0; font-size: 14px;'>Password Reset Request</p>
                        </div>
                        <div style='padding: 40px 30px;'>
                            <h2 style='color: #1c3c56; margin-top: 0;'>Reset Your Password</h2>
                            <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                                Hello {user.FirstName},
                            </p>
                            <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                                We received a request to reset your password for your YardOps account. 
                                Click the button below to create a new password:
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
                                   style='background-color: #2da49d; color: white; padding: 14px 32px; 
                                          text-decoration: none; border-radius: 6px; display: inline-block;
                                          font-weight: 600; font-size: 16px;'>
                                    Reset Password
                                </a>
                            </div>
                            <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                                <strong>Important:</strong> This link will expire in 24 hours for security reasons.
                            </p>
                            <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                                If you didn't request a password reset, please ignore this email or contact your administrator if you have concerns.
                            </p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                            <p style='color: #999; font-size: 12px; text-align: center;'>
                                This is an automated message from YardOps. Please do not reply to this email.
                            </p>
                        </div>
                        <div style='background-color: #f5f5f5; padding: 20px; text-align: center;'>
                            <p style='color: #888; font-size: 12px; margin: 0;'>
                                © {DateTime.Now.Year} YardOps. All rights reserved.
                            </p>
                        </div>
                    </div>";

                await _emailSender.SendEmailAsync(
                    Input.Email,
                    "Reset Your YardOps Password",
                    emailBody);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
