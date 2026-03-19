using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Trailers
{
    public class CreateOutgateInput
    {
        [Required]
        public int TrailerId { get; set; }

        [Required(ErrorMessage = "Gate location is required")]
        [Display(Name = "Gate Location")]
        public int LocationId { get; set; }

        [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}