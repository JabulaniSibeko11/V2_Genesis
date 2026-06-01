using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section8_ACS
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Rebate_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S8 { get; set; }


        public string? AccountNo_MultipleUnits { get; set; }
        public string? Contact_CellNo { get; set; } 
        public string? SchemeBuilding { get; set; }
    }
}
