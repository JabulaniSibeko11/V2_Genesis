using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Attributes
{
    [Table("AttrValuerReviews", Schema = "dbo")]
    public sealed class AttrValuerReview
    {
        [Key]
        public long Id { get; set; }
        public long Attr_ID { get; set; }
        public string? Attr_No { get; set; }
        public string ReviewStatus { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? FinalComment { get; set; }
    }

}
