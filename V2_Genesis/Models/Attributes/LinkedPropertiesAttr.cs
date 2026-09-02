using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Attributes
{
    [Table("LinkedProperties_Attr")]
    public class LinkedPropertyAttr
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ID { get; set; }

        [Required]
        [MaxLength(50)]
        public string IDProperty { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string UserID { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PropertyFrom { get; set; }

        [MaxLength(50)]
        public string? VerifiedAccountNumber { get; set; }

        public DateTime? AccountVerifiedAt { get; set; }

        [MaxLength(30)]
        public string? VerificationMethod { get; set; }
    }
}
