using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section7_MinorOccupants
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Rebate_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S7 { get; set; }

        
        public string? NameSurname1 { get; set; }
        public string? NameSurname2 { get; set; }
        public string? NameSurname3 { get; set; }
        public string? NameSurname4 { get; set; }

        public string? IDNo1 { get; set; }
        public string? IDNo2 { get; set; } 
        public string? IDNo3 { get; set; }
        public string? IDNo4 { get; set; }

        public string? HouseUnits { get; set; }
    }
}
