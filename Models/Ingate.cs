using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    /// <summary>
    /// Represents an ingate (entry) operation for a trailer.
    /// Records when a trailer enters the yard through a specific gate.
    /// Serves as an audit trail for gate operations.
    /// </summary>
    public class Ingate
    {
        [Key]
        public int IngateId { get; set; }

        /// <summary>
        /// Foreign key to Trailer (Required, cascade delete)
        /// </summary>
        [Required]
        public int TrailerId { get; set; }

        /// <summary>
        /// Foreign key to Location (Gate location)
        /// </summary>
        [Required]
        public int LocationId { get; set; }

        /// <summary>
        /// Foreign key to ApplicationUser who performed the ingate operation
        /// Changed to string to match ApplicationUser.Id type (from ASP.NET Identity)
        /// Made nullable to allow SET NULL delete behavior
        /// DO NOT use [Required] - nullable FK with SetNull delete behavior
        /// </summary>
        [ForeignKey(nameof(PerformedByUser))]
        public string? PerformedByUserId { get; set; }

        /// <summary>
        /// Timestamp when the ingate operation occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional notes for the ingate operation
        /// Examples: "Delayed due to paperwork", "Truck in good condition"
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }

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
        /// Navigation property: Trailer entering the yard
        /// </summary>
        public Trailer? Trailer { get; set; }

        /// <summary>
        /// Navigation property: Gate location where trailer entered
        /// </summary>
        public Location? Location { get; set; }

        /// <summary>
        /// Navigation property: ApplicationUser who performed this operation
        /// </summary>
        public ApplicationUser? PerformedByUser { get; set; }

        /// <summary>
        /// Navigation property: User who created this record (audit)
        /// </summary>
        public ApplicationUser? CreatedByUser { get; set; }

        // ==================== Computed Properties ====================

        /// <summary>
        /// Email of the user who performed the ingate operation
        /// Returns "Unknown" if PerformedByUser is null
        /// </summary>
        [NotMapped]
        public string PerformedByEmail => PerformedByUser?.Email ?? "Unknown";

        /// <summary>
        /// Display name of the user who performed the ingate operation
        /// </summary>
        [NotMapped]
        public string PerformedByName => PerformedByUser != null
            ? $"{PerformedByUser.FirstName} {PerformedByUser.LastName}".Trim()
            : "Unknown";

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
        /// Display name of the gate location
        /// </summary>
        [NotMapped]
        public string LocationName => Location?.LocationName ?? "Unknown Gate";

        /// <summary>
        /// Formatted timestamp for display
        /// Example: "Mar 18, 2026 09:30 AM"
        /// </summary>
        [NotMapped]
        public string TimestampDisplay => Timestamp.ToString("MMM dd, yyyy HH:mm tt");
    }
}