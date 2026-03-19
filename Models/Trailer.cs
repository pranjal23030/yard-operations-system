using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    /// <summary>
    /// Represents a trailer in the yard operations system.
    /// Tracks trailer movements, status, and associated goods.
    /// </summary>
    public class Trailer
    {
        [Key]
        public int TrailerId { get; set; }

        /// <summary>
        /// Auto-generated unique trailer code (e.g., TRL-001, TRL-002)
        /// </summary>
        [Required]
        [StringLength(20)]
        public string TrailerCode { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to Carrier (Required)
        /// </summary>
        [Required]
        public int CarrierId { get; set; }

        /// <summary>
        /// Optional: UserId of the driver assigned to this trailer
        /// </summary>
        [ForeignKey(nameof(DriverUser))]
        public string? DriverUserId { get; set; }

        /// <summary>
        /// Current status of the trailer
        /// Valid values: Incoming, In-Yard, Outgoing, Checked Out
        /// </summary>
        [Required]
        [StringLength(50)]
        public string CurrentStatus { get; set; } = "Incoming";

        /// <summary>
        /// Type of goods in the trailer
        /// Valid values: General, Hazmat, Refrigerated, Oversized
        /// </summary>
        [Required]
        [StringLength(50)]
        public string GoodsType { get; set; } = "General";

        /// <summary>
        /// Current location of the trailer (Zone, Slot, Dock, or Gate)
        /// Optional - null if trailer is in transit or not yet assigned
        /// </summary>
        public int? CurrentLocationId { get; set; }

        /// <summary>
        /// Timestamp when the trailer arrived at the yard
        /// </summary>
        public DateTime? ArrivalTime { get; set; }

        /// <summary>
        /// Timestamp when the trailer departed from the yard
        /// Null if trailer is still in yard
        /// </summary>
        public DateTime? DepartureTime { get; set; }

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
        /// Navigation property: Carrier associated with this trailer
        /// </summary>
        public Carrier? Carrier { get; set; }

        /// <summary>
        /// Navigation property: Current location of the trailer
        /// </summary>
        public Location? CurrentLocation { get; set; }

        /// <summary>
        /// Navigation property: User who created this record
        /// </summary>
        public ApplicationUser? CreatedByUser { get; set; }

        /// <summary>
        /// Navigation property: All goods items in this trailer
        /// </summary>
        public ICollection<Goods>? GoodsItems { get; set; }

        /// <summary>
        /// Navigation property: History of trailer movements
        /// </summary>
        public ICollection<TrailerHistory>? HistoryRecords { get; set; }

        /// <summary>
        /// Navigation property: Ingate records for this trailer
        /// </summary>
        public ICollection<Ingate>? IngateRecords { get; set; }

        /// <summary>
        /// Navigation property: Outgate records for this trailer
        /// </summary>
        public ICollection<Outgate>? OutgateRecords { get; set; }

        /// <summary>
        /// Navigation property: User assigned as driver for this trailer
        /// </summary>
        public ApplicationUser? DriverUser { get; set; }

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
        /// Display name of the carrier
        /// </summary>
        [NotMapped]
        public string CarrierName => Carrier?.CompanyName ?? "Unknown";

        /// <summary>
        /// Display name of the current location
        /// </summary>
        [NotMapped]
        public string LocationName => CurrentLocation?.LocationName ?? "Not Assigned";

        /// <summary>
        /// Calculates dwell time in minutes from arrival to now (or departure if departed)
        /// Returns 0 if ArrivalTime is null
        /// </summary>
        [NotMapped]
        public int DwellTimeMinutes
        {
            get
            {
                if (ArrivalTime == null) return 0;

                var endTime = DepartureTime ?? DateTime.UtcNow;
                return (int)(endTime - ArrivalTime.Value).TotalMinutes;
            }
        }

        /// <summary>
        /// Total number of goods items in this trailer
        /// </summary>
        [NotMapped]
        public int GoodsCount => GoodsItems?.Count ?? 0;

        /// <summary>
        /// Total weight of all goods in this trailer (kg)
        /// </summary>
        [NotMapped]
        public decimal TotalWeight => GoodsItems?.Sum(g => g.Weight) ?? 0;

        /// <summary>
        /// Summary of goods (e.g., "3 items - 500kg")
        /// </summary>
        [NotMapped]
        public string GoodsSummary
        {
            get
            {
                if (GoodsCount == 0) return "No goods";
                return $"{GoodsCount} item{(GoodsCount != 1 ? "s" : "")} - {TotalWeight}kg";
            }
        }

        /// <summary>
        /// Whether the trailer has departed
        /// </summary>
        [NotMapped]
        public bool HasDeparted => DepartureTime.HasValue;

        /// <summary>
        /// CSS class for status badge styling
        /// </summary>
        [NotMapped]
        public string StatusBadgeClass => CurrentStatus switch
        {
            "Incoming" => "badge bg-warning",
            "In-Yard" => "badge bg-info",
            "Outgoing" => "badge bg-primary",
            "Checked Out" => "badge bg-success",
            _ => "badge bg-secondary"
        };
    }
}