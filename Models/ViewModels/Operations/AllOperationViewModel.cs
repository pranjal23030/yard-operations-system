namespace YardOps.Models.ViewModels.Operations
{
    public class AllOperationViewModel
    {
        public string Type { get; set; } = ""; // Ingate / Outgate

        public int OperationId { get; set; }

        public int TrailerId { get; set; }

        public string TrailerCode { get; set; } = "";

        public string CarrierName { get; set; } = "";

        public int GateLocationId { get; set; }

        // Kept for compatibility where generic gate text is needed
        public string GateName { get; set; } = "";

        public string EntryGateName { get; set; } = "—";

        public string ExitGateName { get; set; } = "—";

        public string PerformedByUserId { get; set; } = "";

        public string PerformedByName { get; set; } = "";

        public string PerformedByEmail { get; set; } = "";

        public DateTime Timestamp { get; set; }

        public string? Notes { get; set; }
    }
}