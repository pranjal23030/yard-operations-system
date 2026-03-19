using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Trailers
{
    public class CreateTrailerInput
    {
        [Required(ErrorMessage = "Carrier is required")]
        [Display(Name = "Carrier")]
        public int CarrierId { get; set; }

        [Required(ErrorMessage = "Driver is required")]
        [Display(Name = "Driver")]
        public string DriverUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Goods type is required")]
        [StringLength(50, ErrorMessage = "Goods type cannot exceed 50 characters")]
        [Display(Name = "Goods Type")]
        public string GoodsType { get; set; } = "General";

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        [Display(Name = "Status")]
        public string CurrentStatus { get; set; } = "Incoming";

        [Display(Name = "Location")]
        public int? LocationId { get; set; }
    }
}