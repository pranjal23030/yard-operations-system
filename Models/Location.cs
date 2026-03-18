using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        [Required]
        public int YardId { get; set; }

        [Required]
        [StringLength(50)]
        public string LocationName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string LocationType { get; set; } = string.Empty;  // Zone, Slot, Dock, Gate

        [StringLength(20)]
        public string Status { get; set; } = "Active";  // Active, Inactive, Maintenance

        public int? Capacity { get; set; }  // Required for Zone & Dock, NULL for Gate

        public int CurrentOccupancy { get; set; } = 0;  // Tracks trailers (default 0)

        [StringLength(200)]
        public string? Description { get; set; }

        [ForeignKey(nameof(CreatedByUser))]
        public string? CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Yard? Yard { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        // Computed properties
        [NotMapped]
        public int Available => Capacity.HasValue ? Math.Max(0, Capacity.Value - CurrentOccupancy) : 0;

        [NotMapped]
        public int OccupancyPercentage => Capacity.HasValue && Capacity.Value > 0
            ? (int)Math.Round((CurrentOccupancy / (double)Capacity.Value) * 100)
            : 0;

        [NotMapped]
        public bool IsAvailable => Status == "Active" && CurrentOccupancy < (Capacity ?? 1);

        [NotMapped]
        public string CreatedByEmail => CreatedByUser?.Email ?? "Unknown";
    }
}