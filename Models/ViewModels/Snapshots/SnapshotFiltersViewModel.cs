namespace YardOps.Models.ViewModels.Snapshots
{
    public class SnapshotFiltersViewModel
    {
        public int? SelectedRunId { get; set; }

        public string? SearchTerm { get; set; }

        public string? StatusFilter { get; set; } = "all";

        public int PageNumber { get; set; } = 1;

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public int TotalItems { get; set; }

        public int PageSize { get; set; } = 10;
    }
}