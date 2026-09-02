using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Attributes;

[Table("AttrInspectionCalendarBlock", Schema = "dbo")]
public sealed class AttrInspectionCalendarBlock
{
    [Key]
    public long Id { get; set; }

    public int UserId { get; set; }

    public DateTime BlockedFrom { get; set; }

    public DateTime BlockedTo { get; set; }

    public bool IsWholeDay { get; set; }

    [StringLength(250)]
    public string? Reason { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(150)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    [StringLength(150)]
    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
