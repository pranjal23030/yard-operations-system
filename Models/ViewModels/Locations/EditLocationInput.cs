using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Locations
{
    public class EditLocationInput
    {
        [Required]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Location name is required")]
        [StringLength(50, ErrorMessage = "Location name cannot exceed 50 characters")]
        public string LocationName { get; set; } = string.Empty;

        public string LocationType { get; set; } = string.Empty;  // Read-only, for display

        // Capacity is validated manually in the handler based on LocationType
        public int? Capacity { get; set; }

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Active";

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        public string? Description { get; set; }
    }
}