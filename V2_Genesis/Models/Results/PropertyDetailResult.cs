namespace V2_Genesis.Models.Results
{


    /// <summary>
    /// Full property detail record — returned by IndexObjection_* stored procedures.
    /// Field names match the SP column names exactly.
    /// </summary>
    public class PropertyDetailResult
    {
        public int Id { get; set; }
        public string? TownNameDesc { get; set; }
        public string? OwnerName { get; set; }
        public int Erf { get; set; }
        public int Ptn { get; set; }
        public string? Re { get; set; }
        public string? LisStreetAddress { get; set; }
        public string? CatDesc { get; set; }
        public string? RateableArea { get; set; }
        public string? MarketValue { get; set; }
        public string? WefDate { get; set; }
        public string? Reason { get; set; }
        public string? SchemeName { get; set; }
        public string? SchemeNumber { get; set; }
        public string? SchemeYear { get; set; }
        public int UnitNo { get; set; }
        public string? PropertyDesc { get; set; }
        public string? PremiseId { get; set; }
        public string? UnitKey { get; set; }
        public string? PropertyId { get; set; }
        public string? ValuationKey { get; set; }
        public string? ValuationDate { get; set; }
        public string? LeaseDesc { get; set; }
        public string? Sector { get; set; }

        // Add these to the existing PropertyDetailResult class:
        public string? ADDR1 { get; set; }
        public string? ADDR2 { get; set; }
        public string? ADDR3 { get; set; }
        public string? ADDR4 { get; set; }
        public string? ADDR5 { get; set; }
        public string? LeaseStatus { get; set; }
        public string? AdditionalNotes { get; set; }
    }
}