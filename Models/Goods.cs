using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    /// <summary>
    /// Represents a goods item (manifest line) in a trailer.
    /// Allows trailers to contain multiple items with different characteristics.
    /// </summary>
    public class Goods
    {
        [Key]
        public int GoodsId { get; set; }

        /// <summary>
        /// Foreign key to Trailer (Required, cascade delete)
        /// </summary>
        [Required]
        public int TrailerId { get; set; }

        /// <summary>
        /// Description of the goods
        /// Examples: "Electronics", "Furniture", "Raw Materials"
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Weight of goods in kilograms
        /// Must be greater than 0
        /// </summary>
        [Required]
        [Range(0.01, 999999.99)]
        public decimal Weight { get; set; }

        /// <summary>
        /// Quantity of items
        /// Must be greater than 0
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Special handling notes
        /// Examples: "Fragile", "Keep upright", "Temperature controlled"
        /// </summary>
        [StringLength(500)]
        public string? HandlingNotes { get; set; }

        /// <summary>
        /// Foreign key to AspNetUsers for audit trail
        /// </summary>
        [ForeignKey(nameof(CreatedByUser))]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Timestamp when this goods item was added
        /// </summary>
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // ==================== Navigation Properties ====================

        /// <summary>
        /// Navigation property: Trailer containing this goods
        /// </summary>
        public Trailer? Trailer { get; set; }

        /// <summary>
        /// Navigation property: User who created this record
        /// </summary>
        public ApplicationUser? CreatedByUser { get; set; }

        // ==================== Computed Properties ====================

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
        /// Total weight (Weight * Quantity) for easy calculation
        /// </summary>
        [NotMapped]
        public decimal TotalWeight => Weight * Quantity;

        /// <summary>
        /// Display summary of the goods
        /// Example: "Electronics (100 x 5kg = 500kg)"
        /// </summary>
        [NotMapped]
        public string Summary => $"{Description} ({Quantity} x {Weight}kg = {TotalWeight}kg)";
    }
}