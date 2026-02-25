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
        
        // JSON serializer options for human-readable output
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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
                JsonData = extraData != null ? JsonSerializer.Serialize(extraData, _jsonOptions) : null
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}