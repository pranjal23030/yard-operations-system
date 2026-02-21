using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    public class Carrier
    {
        [Key]
        public int CarrierId { get; set; }

        [Required]
        [StringLength(20)]
        public string CarrierCode { get; set; } = string.Empty;  // CAR-001, CAR-002

        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        // Foreign Key to AspNetUsers.Id
        [ForeignKey(nameof(CreatedByUser))]
        public string? CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ApplicationUser? CreatedByUser { get; set; }

        // Computed property for display
        [NotMapped]
        public string CreatedByEmail => CreatedByUser?.Email ?? "Unknown";
    }
}