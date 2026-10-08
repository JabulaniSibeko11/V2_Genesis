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

        // Appeal: the Municipal Valuer's Decision splits (Obj_Property_Info
        // New2_* / New3_* MVD columns) — Section 6 of a multipurpose appeal.
        public string? Mvd2Category { get; set; }
        public string? Mvd2Extent { get; set; }
        public string? Mvd2MarketValue { get; set; }
        public string? Mvd3Category { get; set; }
        public string? Mvd3Extent { get; set; }
        public string? Mvd3MarketValue { get; set; }
    }
}
