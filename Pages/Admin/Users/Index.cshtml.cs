using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using YardOps.Data;
using YardOps.Models.ViewModels.Users;
using YardOps.Services;
namespace YardOps.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ActivityLogger _activityLogger;
        private const int PageSize = 10;
        public IndexModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailSender emailSender,
            IConfiguration configuration,
            ActivityLogger activityLogger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _configuration = configuration;
            _activityLogger = activityLogger;
        }
        public List<UserViewModel> Users { get; set; } = [];
        public List<SelectListItem> RoleOptions { get; set; } = [];
        public List<SelectListItem> StatusOptions { get; set; } = [];
        public List<SelectListItem> Roles { get; set; } = [];
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
        // Create User
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
            
            // Get the current admin user (creator)
            var currentAdmin = await _userManager.GetUserAsync(User);
            
            // Get default password from configuration
            var defaultPassword = _configuration["DefaultUserPassword"] ?? "Password@123";
            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                LastName = Input.LastName,
                Status = "Inactive",
                CreatedOn = DateTime.UtcNow, 
                CreatedBy = currentAdmin?.Id,
                EmailConfirmed = false
            };
            var result = await _userManager.CreateAsync(user, defaultPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                ShowCreateModal = true;
                await ReloadPageData();
                return Page();
            }
            await _userManager.AddToRoleAsync(user, Input.Role);
            // Audit: Log user creation
            await _activityLogger.LogAsync(
                action: "CreateUser",
                description: $"Created user {Input.Email}",
                extraData: new
                {
                    NewRole = Input.Role,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName
                }
            );
            // Generate email confirmation token and send email
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var confirmationLink = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = encodedToken },
                    protocol: Request.Scheme);
                var emailBody = $@"
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
        <div style='background: linear-gradient(135deg, #1c3c56 0%, #2da49d 100%); padding: 30px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>YardOps</h1>
            <p style='color: rgba(255, 255, 255, 0.9); margin: 5px 0 0 0; font-size: 14px;'>Welcome to Your Account</p>
        </div>
        <div style='padding: 40px 30px;'>
            <h2 style='color: #1c3c56; margin-top: 0;'>Welcome to YardOps!</h2>
            <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                Hello <strong>{user.FirstName}</strong>,
            </p>
            <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                Your YardOps account has been created by an administrator. 
                Please confirm your email address to activate your account and start using the system.
            </p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{HtmlEncoder.Default.Encode(confirmationLink!)}' 
                   style='background-color: #2da49d; color: white; padding: 14px 32px; 
                          text-decoration: none; border-radius: 6px; display: inline-block;
                          font-weight: 600; font-size: 16px;'>
                    Confirm Email Address
                </a>
            </div>
            <div style='background-color: #f0f9ff; border-left: 4px solid #2da49d; padding: 16px; margin: 24px 0;'>
                <p style='margin: 0 0 8px 0; color: #1c3c56; font-weight: 600;'>Your Temporary Password:</p>
                <p style='background-color: #ffffff; padding: 12px; border-radius: 4px; font-family: monospace; font-size: 16px; color: #1c3c56; margin: 0;'>
                    <strong>{defaultPassword}</strong>
                </p>
            </div>
            <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                <strong>Next Steps:</strong>
            </p>
            <ol style='color: #555; font-size: 14px; line-height: 1.8; padding-left: 20px;'>
                <li>Click the confirmation button above to verify your email</li>
                <li>Sign in using your email and temporary password</li>
                <li>Change your password immediately after first login</li>
            </ol>
            <p style='color: #777; font-size: 14px; line-height: 1.6; margin-top: 24px;'>
                <strong>Security Reminder:</strong> For your account security, please change this temporary password after your first login.
            </p>
            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
            <p style='color: #999; font-size: 12px; text-align: center;'>
                If you did not expect this email, please contact your administrator immediately.
            </p>
        </div>
        <div style='background-color: #f5f5f5; padding: 20px; text-align: center;'>
            <p style='color: #888; font-size: 12px; margin: 0;'>
                © {DateTime.Now.Year} YardOps. All rights reserved.
            </p>
        </div>
    </div>";
                await _emailSender.SendEmailAsync(user.Email, "Confirm your YardOps account", emailBody);
                TempData["Success"] = $"User {user.FirstName} {user.LastName} created successfully. A confirmation email has been sent.";
            }
            catch (Exception)
            {
                // User created but email failed
                TempData["Success"] = $"User {user.FirstName} {user.LastName} created. Email sending failed - please resend confirmation manually.";
            }
            return RedirectToPage();
        }
        // Edit User
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
            // Track changed fields for audit log
            var changedFields = new List<string>();
            if (user.FirstName != EditInput.FirstName)
            {
                changedFields.Add($"FirstName: '{user.FirstName}' → '{EditInput.FirstName}'");
                user.FirstName = EditInput.FirstName;
            }
            if (user.LastName != EditInput.LastName)
            {
                changedFields.Add($"LastName: '{user.LastName}' → '{EditInput.LastName}'");
                user.LastName = EditInput.LastName;
            }
            if (user.Status != EditInput.Status)
            {
                changedFields.Add($"Status: '{user.Status}' → '{EditInput.Status}'");
                user.Status = EditInput.Status;
            }
            if (user.Email != EditInput.Email)
            {
                var exists = await _userManager.FindByEmailAsync(EditInput.Email);
                if (exists != null && exists.Id != user.Id)
                {
                    ModelState.AddModelError("EditInput.Email", "Email already exists.");
                    ShowEditModal = true;
                    await ReloadPageData();
                    return Page();
                }
                changedFields.Add($"Email: '{user.Email}' → '{EditInput.Email}'");
                user.Email = EditInput.Email;
                user.UserName = EditInput.Email;
            }
            await _userManager.UpdateAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault();
            if (currentRole != EditInput.Role)
            {
                changedFields.Add($"Role: '{currentRole}' → '{EditInput.Role}'");
                if (currentRole != null)
                    await _userManager.RemoveFromRoleAsync(user, currentRole);
                await _userManager.AddToRoleAsync(user, EditInput.Role);
            }
            // Audit: Log user edit with changed fields
            await _activityLogger.LogAsync(
                action: "EditUser",
                description: $"Edited user {user.Email}",
                extraData: new
                {
                    ChangedFields = changedFields
                }
            );
            TempData["Success"] = $"{user.FirstName} {user.LastName} updated successfully.";
            return RedirectToPage();
        }
        // Delete User
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
            // Store user info before deletion
            var userEmail = user.Email;
            var userFullName = $"{user.FirstName} {user.LastName}";
            await _userManager.DeleteAsync(user);
            // Audit: Log user deletion
            await _activityLogger.LogAsync(
                action: "DeleteUser",
                description: $"Deleted user {userEmail}",
                extraData: new
                {
                    DeletedUserEmail = userEmail,
                    DeletedUserName = userFullName
                }
            );
            TempData["Deleted"] = $"{userFullName} has been removed from the system.";
            return RedirectToPage();
        }
        // Resend Confirmation Email
        public async Task<IActionResult> OnPostResendConfirmationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToPage();
            }
            if (user.EmailConfirmed)
            {
                TempData["Error"] = "User email is already confirmed.";
                return RedirectToPage();
            }
            var defaultPassword = _configuration["DefaultUserPassword"] ?? "Password@123";
            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var confirmationLink = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = encodedToken },
                    protocol: Request.Scheme);
                var emailBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
            <div style='background: linear-gradient(135deg, #1c3c56 0%, #2da49d 100%); padding: 30px; text-align: center;'>
                <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>YardOps</h1>
                <p style='color: rgba(255, 255, 255, 0.9); margin: 5px 0 0 0; font-size: 14px;'>Email Confirmation Reminder</p>
            </div>
            <div style='padding: 40px 30px;'>
                <h2 style='color: #1c3c56; margin-top: 0;'>Confirm Your Email Address</h2>
                <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                    Hello <strong>{user.FirstName}</strong>,
                </p>
                <p style='color: #555; font-size: 16px; line-height: 1.6;'>
                    This is a reminder to confirm your email address for your YardOps account. 
                    Your account is currently inactive until you verify your email.
                </p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{HtmlEncoder.Default.Encode(confirmationLink!)}' 
                       style='background-color: #2da49d; color: white; padding: 14px 32px; 
                              text-decoration: none; border-radius: 6px; display: inline-block;
                              font-weight: 600; font-size: 16px;'>
                        Confirm Email Address
                    </a>
                </div>
                <div style='background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 16px; margin: 24px 0;'>
                    <p style='margin: 0; color: #856404; font-size: 14px;'>
                        <strong>⚠️ Action Required:</strong> You must confirm your email before you can sign in to YardOps.
                    </p>
                </div>
                <div style='background-color: #f0f9ff; border-left: 4px solid #2da49d; padding: 16px; margin: 24px 0;'>
                    <p style='margin: 0 0 8px 0; color: #1c3c56; font-weight: 600;'>Your Temporary Password:</p>
                    <p style='background-color: #ffffff; padding: 12px; border-radius: 4px; font-family: monospace; font-size: 16px; color: #1c3c56; margin: 0;'>
                        <strong>{defaultPassword}</strong>
                    </p>
                </div>
                <p style='color: #777; font-size: 14px; line-height: 1.6;'>
                    Use this password to sign in after confirming your email. For security reasons, please change it after your first login.
                </p>
                <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
                <p style='color: #999; font-size: 12px; text-align: center;'>
                    If you did not request this email, please contact your administrator.
                </p>
            </div>
            <div style='background-color: #f5f5f5; padding: 20px; text-align: center;'>
                <p style='color: #888; font-size: 12px; margin: 0;'>
                    © {DateTime.Now.Year} YardOps. All rights reserved.
                </p>
            </div>
        </div>";
                await _emailSender.SendEmailAsync(user.Email!, "Confirm your YardOps account", emailBody);
                TempData["Success"] = $"Confirmation email resent to {user.Email}.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Failed to send confirmation email. Please try again.";
            }
            return RedirectToPage();
        }
        // Helpers
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
            // Order by CreatedAt
            usersQuery = usersQuery.OrderBy(u => u.CreatedOn);
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
                    CreatedOn = user.CreatedOn,
                    EmailConfirmed = user.EmailConfirmed
                });
            }
            TotalUsers = list.Count;
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalUsers / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);
            Users = list.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
        private async Task LoadFiltersAsync()
        {
            RoleOptions = [new("All Roles", "all")];
            RoleOptions.AddRange(await _roleManager.Roles
                .Select(r => new SelectListItem(r.Name!, r.Name!))
                .ToListAsync());
            StatusOptions =
            [
                new("All Statuses", "all"),
                new("Active", "Active"),
                new("Inactive", "Inactive"),
                new("Pending", "Pending")
            ];
        }
        private async Task LoadRolesForCreate()
        {
            Roles = await _roleManager.Roles
                .Select(r => new SelectListItem(r.Name!, r.Name!))
                .ToListAsync();
        }
    }
}