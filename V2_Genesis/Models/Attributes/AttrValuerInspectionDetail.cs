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
    public bool IsActive { get; set; }
}
