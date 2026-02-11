using YardOps.Data;
using System;

namespace YardOps.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;  
        public string Action { get; set; } = string.Empty;  // e.g., "Login", "CreateUser"
        public string? Description { get; set; }  // Details, e.g., "User XYZ created"
        public string? JsonData { get; set; } 
        public ApplicationUser? User { get; set; }
    }
}