using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Attributes
{
    [Table("AttrValuerReviewSections", Schema = "dbo")]
    public sealed class AttrValuerReviewSection
    {
        [Key]
        public long Id { get; set; }
        public long ReviewId { get; set; }
        public long Attr_ID { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string? SectionDecision { get; set; }
        public string? SectionComment { get; set; }
        public bool RequiresCorrection { get; set; }
    }

}
