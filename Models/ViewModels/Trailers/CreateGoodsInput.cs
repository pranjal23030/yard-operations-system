using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Trailers
{
    public class CreateGoodsInput
    {
        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Weight is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Weight must be greater than 0")]
        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [StringLength(500, ErrorMessage = "Handling notes cannot exceed 500 characters")]
        [Display(Name = "Handling Notes")]
        public string? HandlingNotes { get; set; }
    }
}