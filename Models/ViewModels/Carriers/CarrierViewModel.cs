namespace YardOps.Models.ViewModels.Carriers
{
    public class CarrierViewModel
    {
        public int CarrierId { get; set; }
        public string CarrierCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = "Active";
        public int TrailerCount { get; set; } = 0; // Static for now
        public DateTime CreatedOn { get; set; }
        public string? CreatedByEmail { get; set; }
    }
}