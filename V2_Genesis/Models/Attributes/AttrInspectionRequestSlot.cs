using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Attributes;

[Table("AttrInspectionRequestSlots", Schema = "dbo")]
public sealed class AttrInspectionRequestSlot
{
    [Key]
    public long Id { get; set; }
    public long InspectionRequestId { get; set; }
    public long Attr_ID { get; set; }
    public string? Attr_No { get; set; }
    public int SlotNo { get; set; }
    public DateTime ProposedDateTime { get; set; }
    public string? SlotStatus { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}
