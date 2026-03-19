using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;

namespace YardOps.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int PendingEmailVerifications { get; set; }
        public int TotalCarriers { get; set; }
        public int TotalLocations { get; set; }
        public int TotalTrailers { get; set; }
        public int InYardTrailers { get; set; }
        public int CheckedOutToday { get; set; }
        public int TodayIngates { get; set; }
        public int TodayOutgates { get; set; }

        public List<StatusSlice> StatusBreakdown { get; set; } = [];
        public List<ActivityItem> RecentActivities { get; set; } = [];

        public string StatusChartLabelsJson { get; set; } = "[]";
        public string StatusChartDataJson { get; set; } = "[]";
        public string TrendLabelsJson { get; set; } = "[]";
        public string TrendIngatesJson { get; set; } = "[]";
        public string TrendOutgatesJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            ViewData["Title"] = "Admin Dashboard";
            ViewData["PageHeader"] = "Admin Dashboard";

            var utcToday = DateTime.UtcNow.Date;
            var utcTomorrow = utcToday.AddDays(1);

            TotalUsers = await _context.Users.CountAsync();
            PendingEmailVerifications = await _context.Users.CountAsync(u => !u.EmailConfirmed);
            TotalCarriers = await _context.Carriers.CountAsync();
            TotalLocations = await _context.Locations.CountAsync();
            TotalTrailers = await _context.Trailers.CountAsync();
            InYardTrailers = await _context.Trailers.CountAsync(t => t.CurrentStatus == "In-Yard");
            CheckedOutToday = await _context.Trailers.CountAsync(t =>
                t.CurrentStatus == "Checked Out" &&
                t.DepartureTime.HasValue &&
                t.DepartureTime.Value >= utcToday &&
                t.DepartureTime.Value < utcTomorrow);

            TodayIngates = await _context.Ingates.CountAsync(i => i.Timestamp >= utcToday && i.Timestamp < utcTomorrow);
            TodayOutgates = await _context.Outgates.CountAsync(o => o.Timestamp >= utcToday && o.Timestamp < utcTomorrow);

            var statusRaw = await _context.Trailers
                .GroupBy(t => t.CurrentStatus)
                .Select(g => new StatusSlice
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            var knownStatusOrder = new[] { "Incoming", "In-Yard", "Outgoing", "Checked Out" };
            StatusBreakdown = knownStatusOrder
                .Select(s => new StatusSlice
                {
                    Status = s,
                    Count = statusRaw.FirstOrDefault(x => x.Status == s)?.Count ?? 0
                })
                .ToList();

            var trendStart = utcToday.AddDays(-6);
            var dayList = Enumerable.Range(0, 7).Select(i => trendStart.AddDays(i)).ToList();

            var ingateRaw = await _context.Ingates
                .Where(i => i.Timestamp >= trendStart && i.Timestamp < utcTomorrow)
                .GroupBy(i => i.Timestamp.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            var outgateRaw = await _context.Outgates
                .Where(o => o.Timestamp >= trendStart && o.Timestamp < utcTomorrow)
                .GroupBy(o => o.Timestamp.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count);

            var trendLabels = dayList.Select(d => d.ToString("MMM dd")).ToList();
            var ingateTrend = dayList.Select(d => ingateRaw.TryGetValue(d, out var c) ? c : 0).ToList();
            var outgateTrend = dayList.Select(d => outgateRaw.TryGetValue(d, out var c) ? c : 0).ToList();

            StatusChartLabelsJson = JsonSerializer.Serialize(StatusBreakdown.Select(x => x.Status));
            StatusChartDataJson = JsonSerializer.Serialize(StatusBreakdown.Select(x => x.Count));
            TrendLabelsJson = JsonSerializer.Serialize(trendLabels);
            TrendIngatesJson = JsonSerializer.Serialize(ingateTrend);
            TrendOutgatesJson = JsonSerializer.Serialize(outgateTrend);

            RecentActivities = await _context.ActivityLogs
                .Include(a => a.CreatedByUser)
                .OrderByDescending(a => a.CreatedOn)
                .Take(3)
                .Select(a => new ActivityItem
                {
                    Action = a.Action,
                    Description = a.Description ?? "",
                    Actor = a.CreatedByUser != null
                        ? $"{a.CreatedByUser.FirstName} {a.CreatedByUser.LastName}".Trim()
                        : "Unknown User",
                    OccurredOn = a.CreatedOn
                })
                .ToListAsync();
        }

        public class StatusSlice
        {
            public string Status { get; set; } = "";
            public int Count { get; set; }
        }

        public class ActivityItem
        {
            public string Action { get; set; } = "";
            public string Description { get; set; } = "";
            public string Actor { get; set; } = "";
            public DateTime OccurredOn { get; set; }
        }
    }
}