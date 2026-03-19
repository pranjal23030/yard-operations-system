using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    /// <summary>
    /// Represents a historical record of a trailer's stay at a specific location.
    /// Used for tracking dwell times, movements, and analytics/ML training data.
    /// </summary>
    public class TrailerHistory
    {
        [Key]
        public int HistoryId { get; set; }

        /// <summary>
        /// Foreign key to Trailer (Required, cascade delete)
        /// </summary>
        [Required]
        public int TrailerId { get; set; }

        /// <summary>
        /// Foreign key to Location where trailer was stationed
        /// </summary>
        [Required]
        public int LocationId { get; set; }

        /// <summary>
        /// Timestamp when trailer arrived at this location
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Timestamp when trailer departed from this location
        /// Null if trailer is still at this location
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Foreign key to AspNetUsers for audit trail
        /// </summary>
        [ForeignKey(nameof(CreatedByUser))]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Timestamp when this record was created
        /// </summary>
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // ==================== Navigation Properties ====================

        /// <summary>
        /// Navigation property: Trailer being tracked
        /// </summary>
        public Trailer? Trailer { get; set; }

        /// <summary>
        /// Navigation property: Location where trailer was stationed
        /// </summary>
        public Location? Location { get; set; }

        /// <summary>
        /// Navigation property: User who created this record
        /// </summary>
        public ApplicationUser? CreatedByUser { get; set; }

        // ==================== Computed Properties ====================

        /// <summary>
        /// Calculates dwell time in minutes (duration of stay at location)
        /// Returns 0 if EndTime is null (still at location)
        /// This is a computed column and should be indexed for analytics
        /// </summary>
        [NotMapped]
        public int DwellTimeMinutes
        {
            get
            {
                if (EndTime == null) return 0;
                return (int)(EndTime.Value - StartTime).TotalMinutes;
            }
        }

        /// <summary>
        /// Email of the user who created this record
        /// Returns "Unknown" if CreatedByUser is null
        /// </summary>
        [NotMapped]
        public string CreatedByEmail => CreatedByUser?.Email ?? "Unknown";

        /// <summary>
        /// Display name of the user who created this record
        /// </summary>
        [NotMapped]
        public string CreatedByName => CreatedByUser != null
            ? $"{CreatedByUser.FirstName} {CreatedByUser.LastName}".Trim()
            : "Unknown";

        /// <summary>
        /// Display name of the location
        /// </summary>
        [NotMapped]
        public string LocationName => Location?.LocationName ?? "Unknown";

        /// <summary>
        /// Whether the trailer is still at this location (EndTime not set)
        /// </summary>
        [NotMapped]
        public bool IsActive => !EndTime.HasValue;

        /// <summary>
        /// Duration display string
        /// Example: "120 minutes" or "2 hours 30 minutes"
        /// </summary>
        [NotMapped]
        public string DurationDisplay
        {
            get
            {
                int minutes = DwellTimeMinutes;
                if (minutes < 60)
                    return $"{minutes} minute{(minutes != 1 ? "s" : "")}";

                int hours = minutes / 60;
                int remainingMinutes = minutes % 60;

                if (remainingMinutes == 0)
                    return $"{hours} hour{(hours != 1 ? "s" : "")}";

                return $"{hours}h {remainingMinutes}m";
            }
        }
    }
}