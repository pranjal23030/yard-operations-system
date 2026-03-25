using System.ComponentModel.DataAnnotations;

namespace YardOps.Models.ViewModels.Snapshots
{
    public class CaptureSnapshotInput
    {
        [Display(Name = "Capture Time (UTC)")]
        public DateTime? CapturedAtUtc { get; set; }

        [Display(Name = "Include Only In-Yard")]
        public bool IncludeOnlyInYard { get; set; } = true;
    }
}