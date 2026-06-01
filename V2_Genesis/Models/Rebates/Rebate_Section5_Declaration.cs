using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section5_Declaration
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Reb_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S5 { get; set; }


        //public string? Place { get; set; }
        //public string? DeclarationDate { get; set; } 
        public string? Signature { get; set; } 
        public DateTime? DateOfSubmission { get; set; }
        public string? FileName { get; set; } 
    }
}
