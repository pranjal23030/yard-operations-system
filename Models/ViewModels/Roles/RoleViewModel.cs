namespace YardOps.Models.ViewModels.Roles
{
    public class RoleViewModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsSystemRole { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}