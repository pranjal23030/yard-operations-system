namespace YardOps.Models.ViewModels.Operations
{
    public class OutgateViewModel
    {
        public int OutgateId { get; set; }

        public int TrailerId { get; set; }

        public string TrailerCode { get; set; } = "";

        public string CarrierName { get; set; } = "";

        public int GateLocationId { get; set; }

        public string GateName { get; set; } = "";

        public string PerformedByUserId { get; set; } = "";

        public string PerformedByName { get; set; } = "";

        public string PerformedByEmail { get; set; } = "";

        public DateTime Timestamp { get; set; }

        public string? Notes { get; set; }
    }
}