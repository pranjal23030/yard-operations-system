using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Operations
{
    public class CreateIngateInput
    {
        [Required]
        public int TrailerId { get; set; }

        [Required(ErrorMessage = "Entry gate is required")]
        [Display(Name = "Entry Gate")]
        public int GateLocationId { get; set; }

        [Required(ErrorMessage = "Assigned location is required")]
        [Display(Name = "Assign Location")]
        public int AssignedLocationId { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}