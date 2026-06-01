using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section3_ContactDetails
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Reb_Info")]
        public long? Ref { get; set; } 

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S3 { get; set; }



        public string? HomeTel { get; set; }
        public string? CellNo { get; set; }
        public string? WorkTel { get; set; } 
        public string? FaxNo { get; set; }
        public string? Email { get; set; }
    }
}
