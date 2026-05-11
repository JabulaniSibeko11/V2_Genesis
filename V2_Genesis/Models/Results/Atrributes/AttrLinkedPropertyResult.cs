namespace V2_Genesis.Models.Results.Atrributes
{
    public class AttrLinkedPropertyResult
    {
        public long Id { get; set; }
        public string IDProperty { get; set; } = string.Empty;
        public string PropertyFrom { get; set; } = string.Empty;
        public string? PropertyDesc { get; set; }
        public string? CatDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public string? MarketValue { get; set; }
        public string? RateableArea { get; set; }
        public string? LisStreetAddress { get; set; }
        public int Erf { get; set; }
        public int Ptn { get; set; }
        public string? SchemeName { get; set; }
        public int UnitNo { get; set; }
        public string FormType { get; set; } = "Residential";
        public bool HasSubmission { get; set; }
    }
}
