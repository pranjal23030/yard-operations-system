using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YardOps.Data;
using YardOps.Models;
using YardOps.Models.ViewModels.Locations;
using YardOps.Services;

namespace YardOps.Pages.Admin.Locations
{
    [Authorize(Roles = "Admin,YardManager")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ActivityLogger _activityLogger;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ActivityLogger activityLogger)
        {
            _context = context;
            _userManager = userManager;
            _activityLogger = activityLogger;
        }

        // Yard Overview
        public Yard? CurrentYard { get; set; }
        public int ZoneCount { get; set; }
        public int SlotCount { get; set; }
        public int DockCount { get; set; }
        public int GateCount { get; set; }
        public int TotalCapacity { get; set; }
        public int TotalOccupancy { get; set; }
        public int OverallOccupancyPercentage { get; set; }

        // Location Lists (grouped by type)
        public List<LocationViewModel> Zones { get; set; } = new();
        public List<LocationViewModel> Slots { get; set; } = new();
        public List<LocationViewModel> Docks { get; set; } = new();
        public List<LocationViewModel> Gates { get; set; } = new();

        // All locations for search/filter
        public List<LocationViewModel> AllLocations { get; set; } = new();

        // Selected location (for detail view)
        public LocationViewModel? SelectedLocation { get; set; }
        public int? SelectedLocationId { get; set; }

        // Filter parameters
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        // Form inputs - Manually bind in handlers to avoid conflicts
        public CreateLocationInput Input { get; set; } = new();
        public EditLocationInput EditInput { get; set; } = new();
        public int DeleteLocationId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? selectedId = null)
        {
            // Get the main yard (we only have one yard)
            CurrentYard = await _context.Yards.FirstOrDefaultAsync();
            
            if (CurrentYard == null)
            {
                TempData["Error"] = "No yard found. Please contact administrator.";
                return Page();
            }

            // Query locations
            var query = _context.Locations
                .Where(l => l.YardId == CurrentYard.YardId)
                .Include(l => l.CreatedByUser)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(l => 
                    l.LocationName.ToLower().Contains(searchLower) ||
                    (l.Description != null && l.Description.ToLower().Contains(searchLower)));
            }

            // Apply type filter
            if (!string.IsNullOrWhiteSpace(TypeFilter) && TypeFilter != "all")
            {
                query = query.Where(l => l.LocationType == TypeFilter);
            }

            // Get all locations
            var locations = await query
                .OrderBy(l => l.LocationType)
                .ThenBy(l => l.LocationName)
                .ToListAsync();

            // Map to ViewModels
            AllLocations = locations.Select(l => new LocationViewModel
            {
                LocationId = l.LocationId,
                YardId = l.YardId,
                LocationName = l.LocationName,
                LocationType = l.LocationType,
                Status = l.Status,
                Capacity = l.Capacity,
                CurrentOccupancy = l.CurrentOccupancy,
                Description = l.Description,
                CreatedOn = l.CreatedOn,
                CreatedByEmail = l.CreatedByUser?.Email
            }).ToList();

            // Group by type
            Zones = AllLocations.Where(l => l.LocationType == "Zone").ToList();
            Slots = AllLocations.Where(l => l.LocationType == "Slot").ToList();
            Docks = AllLocations.Where(l => l.LocationType == "Dock").ToList();
            Gates = AllLocations.Where(l => l.LocationType == "Gate").ToList();

            // Calculate counts
            ZoneCount = Zones.Count;
            SlotCount = Slots.Count;
            DockCount = Docks.Count;
            GateCount = Gates.Count;

            // Calculate total capacity and occupancy
            TotalCapacity = AllLocations
                .Where(l => l.Capacity.HasValue)
                .Sum(l => l.Capacity!.Value);
            TotalOccupancy = AllLocations.Sum(l => l.CurrentOccupancy);
            OverallOccupancyPercentage = TotalCapacity > 0 
                ? (int)Math.Round((TotalOccupancy / (double)TotalCapacity) * 100) 
                : 0;

            // Set selected location (default to first if none selected)
            SelectedLocationId = selectedId;
            if (SelectedLocationId.HasValue)
            {
                SelectedLocation = AllLocations.FirstOrDefault(l => l.LocationId == SelectedLocationId);
            }
            else if (AllLocations.Any())
            {
                SelectedLocation = AllLocations.First();
                SelectedLocationId = SelectedLocation.LocationId;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync([FromForm] CreateLocationInput Input)
        {
            // Manually bind the Input model
            this.Input = Input;

            // Validate capacity for Zone and Dock types
            if (Input.LocationType == "Zone" || Input.LocationType == "Dock")
            {
                if (!Input.Capacity.HasValue || Input.Capacity < 1 || Input.Capacity > 1000)
                {
                    TempData["Error"] = "Capacity is required for Zone and Dock types (1-1000).";
                    return RedirectToPage();
                }
            }

            // Validate the model
            if (!TryValidateModel(Input))
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();
                TempData["Error"] = $"Validation errors: {string.Join(" | ", errors)}";
                return RedirectToPage();
            }

            // Get the main yard
            var yard = await _context.Yards.FirstOrDefaultAsync();
            if (yard == null)
            {
                TempData["Error"] = "No yard found.";
                return RedirectToPage();
            }

            // Check for duplicate name
            var exists = await _context.Locations
                .AnyAsync(l => l.YardId == yard.YardId &&
                              l.LocationName.ToLower() == Input.LocationName.ToLower());

            if (exists)
            {
                TempData["Error"] = $"A location named '{Input.LocationName}' already exists.";
                return RedirectToPage();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Set capacity based on type
            int? capacity = Input.LocationType switch
            {
                "Zone" => Input.Capacity ?? 10,
                "Dock" => Input.Capacity ?? 1,
                "Slot" => 1,  // Slots always have capacity 1
                "Gate" => null,  // Gates don't have capacity
                _ => null
            };

            var location = new Location
            {
                YardId = yard.YardId,
                LocationName = Input.LocationName.Trim(),
                LocationType = Input.LocationType,
                Status = Input.Status,
                Capacity = capacity,
                CurrentOccupancy = 0,
                Description = Input.Description?.Trim(),
                CreatedBy = currentUser?.Id,
                CreatedOn = DateTime.UtcNow
            };

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            // Log activity
            await _activityLogger.LogAsync(
                "CreateLocation",
                $"Created {Input.LocationType.ToLower()} '{location.LocationName}'",
                new
                {
                    LocationId = location.LocationId,
                    LocationName = location.LocationName,
                    LocationType = location.LocationType,
                    Capacity = capacity,
                    Status = location.Status,
                    YardId = yard.YardId
                }
            );

            TempData["Success"] = $"{Input.LocationType} '{location.LocationName}' created successfully.";
            return RedirectToPage(new { selectedId = location.LocationId });
        }

        public async Task<IActionResult> OnPostEditAsync([FromForm] EditLocationInput EditInput)
        {
            this.EditInput = EditInput;

            // Clear ModelState for Input since we're only editing
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Input.")).ToList())
            {
                ModelState.Remove(key);
            }

            // Also clear DeleteLocationId validation
            ModelState.Remove("DeleteLocationId");

            // Get the existing location
            var location = await _context.Locations.FindAsync(EditInput.LocationId);
            if (location == null)
            {
                TempData["Error"] = "Location not found.";
                return RedirectToPage();
            }

            // Remove validation for Capacity if not Zone or Dock
            if (location.LocationType != "Zone" && location.LocationType != "Dock")
            {
                ModelState.Remove("EditInput.Capacity");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();
                TempData["Error"] = errors.Any() ? string.Join(" | ", errors) : "Please correct the errors and try again.";
                return RedirectToPage(new { selectedId = EditInput.LocationId });
            }

            // Check for duplicate name (excluding current location)
            var exists = await _context.Locations
                .AnyAsync(l => l.YardId == location.YardId && 
                              l.LocationId != location.LocationId &&
                              l.LocationName.ToLower() == EditInput.LocationName.ToLower());
            
            if (exists)
            {
                TempData["Error"] = $"A location named '{EditInput.LocationName}' already exists.";
                return RedirectToPage(new { selectedId = EditInput.LocationId });
            }

            // Track changes
            var changes = new List<string>();

            if (location.LocationName != EditInput.LocationName.Trim())
            {
                changes.Add($"Name: '{location.LocationName}' → '{EditInput.LocationName.Trim()}'");
                location.LocationName = EditInput.LocationName.Trim();
            }

            // Only update capacity for Zone and Dock
            if ((location.LocationType == "Zone" || location.LocationType == "Dock") && 
                location.Capacity != EditInput.Capacity)
            {
                changes.Add($"Capacity: '{location.Capacity}' → '{EditInput.Capacity}'");
                location.Capacity = EditInput.Capacity;
            }

            if (location.Status != EditInput.Status)
            {
                changes.Add($"Status: '{location.Status}' → '{EditInput.Status}'");
                location.Status = EditInput.Status;
            }

            if (location.Description != EditInput.Description?.Trim())
            {
                var oldDesc = string.IsNullOrEmpty(location.Description) ? "None" : location.Description;
                var newDesc = string.IsNullOrEmpty(EditInput.Description?.Trim()) ? "None" : EditInput.Description.Trim();
                if (oldDesc != newDesc)
                {
                    changes.Add($"Description updated");
                    location.Description = EditInput.Description?.Trim();
                }
            }

            if (changes.Any())
            {
                await _context.SaveChangesAsync();

                // Log activity
                await _activityLogger.LogAsync(
                    "EditLocation",
                    $"Edited {location.LocationType.ToLower()} '{location.LocationName}'",
                    new
                    {
                        LocationId = location.LocationId,
                        LocationName = location.LocationName,
                        LocationType = location.LocationType,
                        ChangedFields = changes
                    }
                );

                TempData["Success"] = $"{location.LocationType} '{location.LocationName}' updated successfully.";
            }
            else
            {
                TempData["Info"] = "No changes were made.";
            }

            return RedirectToPage(new { selectedId = EditInput.LocationId });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromForm] int DeleteLocationId)
        {
            this.DeleteLocationId = DeleteLocationId;

            var location = await _context.Locations.FindAsync(DeleteLocationId);
            if (location == null)
            {
                TempData["Error"] = "Location not found.";
                return RedirectToPage();
            }

            // Check if location is occupied
            if (location.CurrentOccupancy > 0)
            {
                TempData["Error"] = $"Cannot delete '{location.LocationName}'. It currently has {location.CurrentOccupancy} trailer(s). Please relocate them first or mark the location as Inactive.";
                return RedirectToPage(new { selectedId = DeleteLocationId });
            }

            var locationName = location.LocationName;
            var locationType = location.LocationType;

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();

            // Log activity
            await _activityLogger.LogAsync(
                "DeleteLocation",
                $"Deleted {locationType.ToLower()} '{locationName}'",
                new
                {
                    DeletedLocationId = location.LocationId,
                    DeletedLocationName = locationName,
                    LocationType = locationType
                }
            );

            TempData["Success"] = $"{locationType} '{locationName}' deleted successfully.";
            return RedirectToPage();
        }
    }
}