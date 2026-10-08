namespace V2_Genesis.Models.Notice;

/// <summary>
/// Everything the on-screen Section 49 notice (Views/Notice/Section49Display)
/// shows. Built by NoticeService.GetSection49ViewAsync with the same rules as
/// the Section 49 PDF, so the screen and the download read the same.
/// No signature on screen (only on the PDF).
/// </summary>
public sealed class Section49ViewModel
{
    public string RollSource { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string ValuationKey { get; set; } = string.Empty;

    /// e.g. "Supplementary Valuation Roll 4 (GVR2023)"
    public string RollTitle { get; set; } = string.Empty;
    public string FinancialYears { get; set; } = string.Empty;
    public string LetterDate { get; set; } = string.Empty;

    public DateTime? OpenDate { get; set; }
    public DateTime? ClosingDate { get; set; }
    public string? ExtendedPeriodText { get; set; }

    public List<string> PostalLines { get; set; } = new();
    public string PropertyDescription { get; set; } = string.Empty;
    public string PhysicalAddress { get; set; } = string.Empty;

    public List<Section49ViewRow> Rows { get; set; } = new();

    public string PortalUrl { get; set; } = "https://objections.joburg.org.za/";
}

public sealed class Section49ViewRow
{
    public string Category { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string MarketValue { get; set; } = string.Empty;
    public string EffectiveDate { get; set; } = string.Empty;
}
