namespace YardOps.Models.ViewModels.Snapshots
{
    public class SnapshotItemViewModel
    {
        public int SnapshotItemId { get; set; }

        public int SnapshotRunId { get; set; }

        public int TrailerId { get; set; }

        public string TrailerCode { get; set; } = string.Empty;

        public int LocationId { get; set; }

        public string LocationName { get; set; } = string.Empty;

        public string LocationType { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime? ArrivalTime { get; set; }

        public DateTime CapturedAt { get; set; }

        public string CarrierName { get; set; } = string.Empty;

        public string DriverName { get; set; } = string.Empty;

        public string GoodsType { get; set; } = string.Empty;
    }
}