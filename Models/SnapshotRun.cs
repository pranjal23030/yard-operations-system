using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    /// <summary>
    /// Represents a single point-in-time snapshot capture run.
    /// One run can contain multiple SnapshotItems (one per trailer captured).
    /// </summary>
    public class SnapshotRun
    {
        [Key]
        public int SnapshotRunId { get; set; }

        /// <summary>
        /// UTC timestamp when this snapshot was captured.
        /// </summary>
        [Required]
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who triggered the snapshot capture.
        /// </summary>
        [ForeignKey(nameof(CapturedByUser))]
        public string? CapturedBy { get; set; }

        /// <summary>
        /// Total trailers captured as currently in-yard for this run.
        /// </summary>
        public int TotalInYard { get; set; }

        /// <summary>
        /// Audit: when this run record was created.
        /// </summary>
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // ==================== Navigation Properties ====================

        public ApplicationUser? CapturedByUser { get; set; }

        public ICollection<SnapshotItem>? SnapshotItems { get; set; }
    }
}