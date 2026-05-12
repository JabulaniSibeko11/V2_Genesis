namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class CheckAttributesViewModel
    {

        // Passed from dashboard
        public string IDProperty { get; set; } = string.Empty;
        public string FormType { get; set; } = "Residential";

        // Property details — populated from Attributes DB
        public string? PropertyDesc { get; set; }
        public string? CatDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public string? LisStreetAddress { get; set; }
        public string? MarketValue { get; set; }
        public string? RateableArea { get; set; }
        public int Erf { get; set; }
        public string? Ptn { get; set; }
        public string? Re { get; set; }
        public string? SchemeName { get; set; }
        public string? SchemeNumber { get; set; }
        public string? SchemeYear { get; set; }
        public int UnitNo { get; set; }
        public string? LeaseDesc { get; set; }
        public string? OwnerName { get; set; }
        public string? ValuationDate { get; set; }

        public string? Reason { get; set; } 
              public string? Zoning { get;set; }
        // Declaration — set by client on this page
        public string DeclarationType { get; set; } = string.Empty; // "Owner" or "Representative"
    }
}
