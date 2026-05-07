namespace V2_Genesis.Models.Objections
{
    public class CheckPropertyResult
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
        public string? Sector { get; set; }

        public bool IsMultiPurpose => CatDesc == "Multiple Purposes";
    }
}
