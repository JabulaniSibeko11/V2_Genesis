using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Attributes;

[Table("AttrValuerInspectionDetails", Schema = "dbo")]
public sealed class AttrValuerInspectionDetail
{
    [Key]
    public int Id { get; set; }
    public string SapNumber { get; set; } = string.Empty;
    public string ValuerName { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public string? CellNumber { get; set; }
    public string? VehicleRegistration { get; set; }
    public string? VehicleMake { get; set; }
    public string? VehicleColour { get; set; }
    public string? PhotoFileName { get; set; }
    public string? PhotoPath { get; set; }
    public bool IsActive { get; set; }
}
