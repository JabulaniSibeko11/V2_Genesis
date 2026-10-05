namespace V2_Genesis.Models.Section51;

/// <summary>
/// Data needed to send the Section 51 notice to the property owner when a
/// Third-Party objection is submitted.
/// </summary>
public sealed class Section51NoticeRequest
{
    public string RollSource { get; set; } = string.Empty;
    public string ObjectionNo { get; set; } = string.Empty;
    public string? PremiseId { get; set; }
    public string? ValuationKey { get; set; }
    public string? UnitKey { get; set; }
    public string? PropertyDescription { get; set; }
    public string? PropertyFrom { get; set; }
    public bool IsMulti { get; set; }
    public string? Section51Pin { get; set; }
    public string? RandomPin { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    /// Objection folder (ObjectionRolls:{roll}:FileRootPath\{ObjectionNo}).
    public string ObjectionFolder { get; set; } = string.Empty;

    public Obj_Section6Model? Section6 { get; set; }
}

public sealed class Section51NoticeResult
{
    public bool Emailed { get; set; }
    public string? OwnerEmail { get; set; }
    public string? PdfPath { get; set; }
    public string? Error { get; set; }
}
