namespace YardOps.Models.ViewModels.Users
{
    public class UserViewModel
    {
        public string Id { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? LastLogin { get; set; }
        public string AssignedLocation { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool EmailConfirmed { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Initials => $"{(FirstName.Length > 0 ? FirstName[0] : '?')}{(LastName.Length > 0 ? LastName[0] : '?')}".ToUpper();
    }
}
