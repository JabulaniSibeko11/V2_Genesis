using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Attributes;

[Table("AttrInspectionRequests", Schema = "dbo")]
public sealed class AttrInspectionRequest
{
    [Key]
    public long Id { get; set; }
    public long Attr_ID { get; set; }
    public string? Attr_No { get; set; }
    public string? ValuerSapNumber { get; set; }
    public DateTime? ConfirmedDateTime { get; set; }
    public string? Status { get; set; }
}
