namespace V2_Genesis.Models
{
    /// <summary>
    /// Encapsulates textual differences between objection and appeal forms.
    /// </summary>
    public record Wording(string InquiryLabel, string PartyLabel, string NumberLabel, string secHeader)
    {
        public static Wording ForType(string inquiryType) =>
            inquiryType?.Trim().ToLowerInvariant() switch
            {
                "appeal" => new("APPEAL", "APPELLANT", "APPEAL NO", "MVD"),
                "query" => new("QUERY", "OWNER", "QUERY NO", "SECTION 78 QUERY FORM"),
                _ => new("OBJECTION", "OBJECTOR", "OBJECTION NO", "VALUATION ROLL")
            };
    }
}