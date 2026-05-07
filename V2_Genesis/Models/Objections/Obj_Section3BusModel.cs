using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section3BusModel
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_SB3 { get; set; }
        
        //Section 3
        //Business
        [StringLength(100)]
        public string? Bus_Tenant_Name { get; set; }
        public double? Bus_Rental_Land_Size { get; set; }
        public double? Bus_Rental { get; set; }
        [StringLength(100)]
        public string? Bus_Escalation { get; set; }
        [StringLength(100)]
        public string? Bus_Other_contribution { get; set; }
        [StringLength(100)]
        public string? Bus_Lease_Term { get; set; }
        [StringLength(100)]
        public string? Bus_Start_Date { get; set; }
        public int? Bus_Building_No { get; set; }
        public double? Bus_Building_Size { get; set; }
        [StringLength(100)]
        public string? Bus_Shops { get; set; }
        [StringLength(100)]
        public string? Bus_Building_Condition { get; set; }
        [StringLength(100)]
        public string? Bus_Extent_Land_further_Dev { get; set; }


        [StringLength(100)]
        public string? Bus_Other_features_Condition { get; set; }

        public long Appeal_Ref_SB3 { get; set; }

    }

}
