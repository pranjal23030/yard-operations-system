using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Operations
{
    public class CreateOutgateInput
    {
        [Required]
        public int TrailerId { get; set; }

        [Required(ErrorMessage = "Exit gate is required")]
        [Display(Name = "Exit Gate")]
        public int GateLocationId { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}