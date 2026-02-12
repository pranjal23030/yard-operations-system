using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models.ViewModels.Activities;

namespace YardOps.Pages.Admin.Activities
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private const int PageSize = 10;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Data properties
        public List<ActivityLogViewModel> Activities { get; set; } = [];
        public List<SelectListItem> ActionOptions { get; set; } = [];

        // Pagination properties
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalActivities { get; set; }

        // Filter properties (bound from query string)
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? ActionFilter { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateFrom { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateTo { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public async Task OnGetAsync()
        {
            CurrentPage = PageNumber < 1 ? 1 : PageNumber;
            
            // Load filter options
            await LoadActionOptionsAsync();
            
            // Load activity logs with filters and pagination
            await LoadActivitiesAsync();
        }

        /// <summary>
        /// Loads distinct actions from the database for the filter dropdown.
        /// </summary>
        private async Task LoadActionOptionsAsync()
        {
            // Start with "All Actions" option
            ActionOptions = [new SelectListItem("All Actions", "all")];

            // Get distinct actions from the database
            var distinctActions = await _context.ActivityLogs
                .Select(l => l.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // Add each action as an option
            foreach (var action in distinctActions)
            {
                ActionOptions.Add(new SelectListItem(action, action));
            }
        }

        /// <summary>
        /// Loads activity logs with applied filters and pagination.
        /// Orders by Timestamp descending (most recent first).
        /// </summary>
        private async Task LoadActivitiesAsync()
        {
            // Start with base query including User navigation property
            var query = _context.ActivityLogs
                .Include(l => l.User)
                .AsQueryable();

            // Apply search filter (searches user name, email, action, description)
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(l =>
                    (l.User != null && (
                        l.User.FirstName.ToLower().Contains(searchLower) ||
                        l.User.LastName.ToLower().Contains(searchLower) ||
                        l.User.Email!.ToLower().Contains(searchLower)
                    )) ||
                    l.Action.ToLower().Contains(searchLower) ||
                    (l.Description != null && l.Description.ToLower().Contains(searchLower))
                );
            }

            // Apply action filter
            if (!string.IsNullOrWhiteSpace(ActionFilter) && ActionFilter != "all")
            {
                query = query.Where(l => l.Action == ActionFilter);
            }

            // Apply date range filter
            if (DateFrom.HasValue)
            {
                var fromDate = DateFrom.Value.Date;
                query = query.Where(l => l.Timestamp >= fromDate);
            }

            if (DateTo.HasValue)
            {
                var toDate = DateTo.Value.Date.AddDays(1); // Include the entire "to" day
                query = query.Where(l => l.Timestamp < toDate);
            }

            // Order by Timestamp descending (most recent first)
            query = query.OrderByDescending(l => l.Timestamp);

            // Get total count for pagination
            TotalActivities = await query.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalActivities / (double)PageSize));
            CurrentPage = Math.Clamp(PageNumber, 1, TotalPages);

            // Apply pagination and project to ViewModel
            var logs = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Map to ViewModel
            Activities = logs.Select(l => new ActivityLogViewModel
            {
                Id = l.Id,
                UserFullName = l.User != null 
                    ? $"{l.User.FirstName} {l.User.LastName}" 
                    : "Unknown User",
                UserEmail = l.User?.Email ?? "N/A",
                Timestamp = l.Timestamp.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt"),
                RawTimestamp = l.Timestamp,
                Action = l.Action,
                Description = l.Description,
                JsonData = l.JsonData
            }).ToList();
        }
    }
}