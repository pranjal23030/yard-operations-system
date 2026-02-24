using YardOps.Data;
using System;

namespace YardOps.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? JsonData { get; set; }
        
        // Navigation property
        public ApplicationUser? CreatedByUser { get; set; }
    }
}