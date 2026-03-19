namespace YardOps.Models.ViewModels.Trailers
{
    public class GoodsViewModel
    {
        public int GoodsId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public int Quantity { get; set; }
        public string? HandlingNotes { get; set; }
        public string? CreatedByEmail { get; set; }

        // Computed properties
        public decimal TotalWeight => Weight * Quantity;

        public string Summary => $"{Description} ({Quantity} x {Weight}kg = {TotalWeight}kg)";
    }
}