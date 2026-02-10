using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Roles
{
    public class CreateRoleInput
    {
        [Required(ErrorMessage = "Role name is required")]
        [Display(Name = "Role Name")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        public string Name { get; set; } = "";

        [Display(Name = "Description")]
        [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Active";
    }
}