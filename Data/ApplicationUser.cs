using Microsoft.AspNetCore.Identity;

namespace ExotracYMS.Data
{
    public class ApplicationUser : IdentityUser
    {
        // ERD-aligned fields
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Business-level status (not Identity lockout)
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
    }
}
