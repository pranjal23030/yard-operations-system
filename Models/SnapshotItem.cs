using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    /// <summary>
    /// Represents one trailer row inside a specific snapshot run.
    /// Stores both FK references and denormalized display fields for
    /// true point-in-time reporting.
    /// </summary>
    public class SnapshotItem
    {
        [Key]
        public int SnapshotItemId { get; set; }

        [Required]
        public int SnapshotRunId { get; set; }

        [Required]
        public int TrailerId { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "In-Yard";

        public DateTime? ArrivalTime { get; set; }

        [Required]
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        // ==================== Denormalized Point-In-Time Fields ====================

        [Required]
        [StringLength(20)]
        public string TrailerCode { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string CarrierName { get; set; } = "";

        [StringLength(150)]
        public string DriverName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string LocationName { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string LocationType { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string GoodsType { get; set; } = "";

        // ==================== Audit ====================

        [ForeignKey(nameof(CreatedByUser))]
        public string? CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // ==================== Navigation Properties ====================

        public SnapshotRun? SnapshotRun { get; set; }

        public Trailer? Trailer { get; set; }

        public Location? Location { get; set; }

        public ApplicationUser? CreatedByUser { get; set; }
    }
}