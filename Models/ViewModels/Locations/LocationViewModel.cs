namespace YardOps.Models.ViewModels.Locations
{
    public class LocationViewModel
    {
        public int LocationId { get; set; }
        public int YardId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int? Capacity { get; set; }
        public int CurrentOccupancy { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedByEmail { get; set; }

        // Computed properties
        public int Available => Capacity.HasValue ? Math.Max(0, Capacity.Value - CurrentOccupancy) : 0;
        
        public int OccupancyPercentage => Capacity.HasValue && Capacity.Value > 0
            ? (int)Math.Round((CurrentOccupancy / (double)Capacity.Value) * 100)
            : 0;

        public string StatusBadgeClass => Status switch
        {
            "Active" => "status-active",
            "Maintenance" => "status-maintenance",
            "Inactive" => "status-inactive",
            _ => "status-inactive"
        };

        public string TypeBadgeClass => LocationType switch
        {
            "Zone" => "type-zone",
            "Slot" => "type-slot",
            "Dock" => "type-dock",
            "Gate" => "type-gate",
            _ => "type-default"
        };

        public string TypeIcon => LocationType switch
        {
            "Zone" => "fa-layer-group",
            "Slot" => "fa-border-all",
            "Dock" => "fa-warehouse",
            "Gate" => "fa-door-open",
            _ => "fa-location-dot"
        };

        public bool ShowSlotPreview => LocationType == "Zone" || LocationType == "Slot";
        public bool ShowCapacityInput => LocationType == "Zone" || LocationType == "Dock";
    }
}