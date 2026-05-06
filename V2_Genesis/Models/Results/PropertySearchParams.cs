using System.ComponentModel.DataAnnotations;

namespace V2_Genesis.Models;

public class PropertySearchParams
{
    [Required(ErrorMessage = "Please select a Township.")]
    public string TownName { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? Stand { get; set; }
    public string? Scheme { get; set; }
    public string? Unit { get; set; }

    // ── Determines which combination of fields is filled ─────────────
    public bool HasStand => !string.IsNullOrWhiteSpace(Stand);
    public bool HasAddress => !string.IsNullOrWhiteSpace(Address);
    public bool HasScheme => !string.IsNullOrWhiteSpace(Scheme);
    public bool HasUnit => !string.IsNullOrWhiteSpace(Unit);
}