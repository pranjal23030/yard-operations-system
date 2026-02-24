using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using YardOps.Data;
using YardOps.Models;

namespace YardOps.Services
{
    public class ActivityLogger
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogger(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string? description = null, object? extraData = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var user = await _userManager.GetUserAsync(httpContext.User);
            if (user == null) return;

            var log = new ActivityLog
            {
                CreatedBy = user.Id,
                CreatedOn = DateTime.UtcNow,
                Action = action,
                Description = description,
                JsonData = extraData != null ? JsonSerializer.Serialize(extraData) : null
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}