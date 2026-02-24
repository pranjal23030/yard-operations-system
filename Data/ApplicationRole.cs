using Microsoft.AspNetCore.Identity;

namespace YardOps.Data
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }

        public bool IsSystemRole { get; set; }

        public string Status { get; set; } = "Active";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        
        // Nullable for seeded roles, will be admin for manually created roles
        public string? CreatedBy { get; set; }
        
        // Navigation property
        public ApplicationUser? CreatedByUser { get; set; }
    }
}
