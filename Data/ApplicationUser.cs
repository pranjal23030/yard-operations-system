using Microsoft.AspNetCore.Identity;

namespace YardOps.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        
        // Nullable for seeded admin user (self-created), required for others
        public string? CreatedBy { get; set; }
        
        // Navigation property
        public ApplicationUser? CreatedByUser { get; set; }
    }
}
