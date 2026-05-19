namespace V2_Genesis.Models.Results.Section78
{
    public class Section78PropertyDetail
    {
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public string? LisStreetAddress { get; set; }
        public string? CatDesc { get; set; }
        public string? MarketValue { get; set; }
        public string? RateableArea { get; set; }
        public string? WefDate { get; set; }
        public string? OwnerName { get; set; }
        public string? PremiseId { get; set; }
        public string? PropertyId { get; set; }
        public string? Sector { get; set; }
    }
    public class Section78LinkedResult
    {
        public int Id { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? PropertyDesc { get; set; }
        public string? CatDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public string? MarketValue { get; set; }
        public string? WefDate { get; set; }
        public string? PropertyFrom { get; set; }
    }

    public class Section78SubmittedResult
    {
        public string? Objection_No { get; set; }
        public string? Property_Desc { get; set; }
        public string? Old_Category { get; set; }
        public string? Town_Name { get; set; }
        public string? Old_Market_Value { get; set; }
        public string? objection_Status { get; set; }
        public string? Property_Type { get; set; }
        public string? Unit_key { get; set; }
        public string? Valuation_Key { get; set; }
    }

}
