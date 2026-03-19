namespace YardOps.Models.ViewModels.Trailers
{
    public class TrailerViewModel
    {
        public int TrailerId { get; set; }
        public string TrailerCode { get; set; } = string.Empty;

        public int CarrierId { get; set; }
        public string CarrierName { get; set; } = string.Empty;

        public string? DriverUserId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverContact { get; set; }

        public string GoodsType { get; set; } = "General";
        public string CurrentStatus { get; set; } = "Incoming";

        public int? LocationId { get; set; }
        public string? LocationName { get; set; }

        public DateTime? ArrivalTime { get; set; }
        public DateTime? DepartureTime { get; set; }

        public string? CreatedByEmail { get; set; }
        public DateTime CreatedOn { get; set; }

        public List<GoodsViewModel> GoodsItems { get; set; } = [];

        // Computed properties
        public int GoodsCount => GoodsItems.Count;

        public decimal TotalWeight => GoodsItems.Sum(g => g.TotalWeight);

        public string GoodsSummary => GoodsCount == 0
            ? "No goods"
            : $"{GoodsCount} item{(GoodsCount == 1 ? "" : "s")} • {TotalWeight} kg";

        public string StatusBadgeClass => CurrentStatus switch
        {
            "Incoming" => "status-incoming",
            "In-Yard" => "status-inyard",
            "Outgoing" => "status-outgoing",
            "Checked Out" => "status-checkedout",
            _ => "status-default"
        };

        public bool CanIngate => CurrentStatus == "Incoming";
        public bool CanOutgate => CurrentStatus == "In-Yard";
    }
}