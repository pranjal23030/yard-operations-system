using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using YardOps.Data;

namespace YardOps.Models
{
    public class Yard
    {
        [Key]
        public int YardId { get; set; }

        [Required]
        [StringLength(100)]
        public string YardName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [ForeignKey(nameof(CreatedByUser))]
        public string? CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public ApplicationUser? CreatedByUser { get; set; }

        [NotMapped]
        public string CreatedByEmail => CreatedByUser?.Email ?? "Unknown";
    }
}