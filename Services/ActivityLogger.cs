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
            if (httpContext == null) return; // Rare: no request context

            var user = await _userManager.GetUserAsync(httpContext.User);
            if (user == null) return; // Skip anonymous actions for now

            var log = new ActivityLog
            {
                UserId = user.Id,
                Action = action,
                Description = description,
                JsonData = extraData != null ? JsonSerializer.Serialize(extraData) : null
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}