namespace YardOps.Models.ViewModels.Snapshots
{
    public class SnapshotRunViewModel
    {
        public int SnapshotRunId { get; set; }

        public DateTime CapturedAt { get; set; }

        public string CapturedBy { get; set; } = "Unknown";

        public int ItemCount { get; set; }
    }
}