// ════════════════════════════════════════════════════════
//  Models/Lis/LisProperty.cs
// ════════════════════════════════════════════════════════
namespace V2_Genesis.Models.Lis;

public class LisProperty
{
    public string? TownNameDescription { get; set; }
    public string? PropertyDescription { get; set; }
    public string? OwnerName { get; set; }

    public int Erf { get; set; }
    public int Ptn { get; set; }
    public string? Re { get; set; }

    public string? LisStreetAddress { get; set; }
    public string? CATDescription { get; set; }
    public string? RateableArea { get; set; }
    public string? MarketValue { get; set; }

    public string? ValuationEffectiveDateWefDate { get; set; }
    public string? Reason { get; set; }

    public string? SchemeName { get; set; }
    public string? SchemeNumber { get; set; }
    public string? SchemeYear { get; set; }
    public string? UnitNo { get; set; }

    public string? PremiseId { get; set; }
    public string? UnitKey { get; set; }
    public string? PropertyId { get; set; }
    public string? ValuationKey { get; set; }
    public string? ValuationEndDate { get; set; }

    public string? AdditionalNotes { get; set; }
    public string? Lease { get; set; }
    public string? Sector { get; set; }
    public DateTime? ReviewCloseDate { get; set; }
}




