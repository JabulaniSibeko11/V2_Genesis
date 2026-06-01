using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Rebate_Section4_Ownership
    {
        [Key]
        public long ID { get; set; }

        [ForeignKey("Reb_Info")]
        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_S4 { get; set; }



        public string? StandNumber { get; set; } 
        public string? PortionNumber { get; set; }
        public string? Suburb { get; set; }
        //public string? OccupyProp { get; set; }          
        public string? NameOfBodyCorporate { get; set; }
        public string? UnitNumberDoorNumber { get; set; }
        public string? DoorNumber { get; set; }

    }
}
