using Microsoft.AspNetCore.Identity;

namespace YardOps.Data
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }

        public bool IsSystemRole { get; set; }

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
