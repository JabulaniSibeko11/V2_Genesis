namespace V2_Genesis.Services.Attributes
{
    public class AttributeLinkedProperty
    {
        public int Id { get; set; }
        public string? PropertyDesc { get; set; }
        public string? CatDesc { get; set; }
        public string? TownNameDesc { get; set; }
        public string? MarketValue { get; set; }
        public string? WefDate { get; set; }
        public string? PropertyFrom { get; set; }
        public string? UnitKey { get; set; }
        public string? ValuationKey { get; set; }
        public string? FormType { get; set; }  // Residential / ST / Business / DRC
        public bool HasSubmission { get; set; }
    }
}
