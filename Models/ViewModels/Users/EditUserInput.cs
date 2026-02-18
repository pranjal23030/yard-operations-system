using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Users
{
    public class EditUserInput
    {
        [Required]
        public string UserId { get; set; } = "";

        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Role { get; set; } = "";

        [Required]
        public string Status { get; set; } = "";
    }
}
