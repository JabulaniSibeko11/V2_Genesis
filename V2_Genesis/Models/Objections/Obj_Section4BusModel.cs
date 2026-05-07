using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section4BusModel
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_SB4 { get; set; }

        //Section 4 Res
        //Section 4.1
        [StringLength(100)]
        public string? Bus4_Scheme_Name { get; set; }
        public int? Bus4_Scheme_No { get; set; }
        public int? Bus4_Flat_No { get; set; }
        public double? Bus4_Unit_Size { get; set; }
        [StringLength(100)]
        public string? Bus4_Managing_Agent_Name { get; set; }
        [StringLength(100)]
        public string? Bus4_Managing_Agent_Tel_No { get; set; }
        [StringLength(100)]
        public string? Bus4_Shops { get; set; }
        public double? Bus4_Offices { get; set; }
        public double? Bus4_Factories { get; set; }
        [StringLength(100)]
        public string? Bus4_Bus_Sect_Title_Other1_name { get; set; }
        [StringLength(100)]
        public string? Bus4_Bus_Sect_Title_Other2_name { get; set; }
        [StringLength(100)]
        public string? Bus4_Bus_Sect_Title_Other3_name { get; set; }
        [StringLength(100)]
        public string? Bus4_Bus_Sect_Title_Other1 { get; set; }
        [StringLength(100)]
        public string? Bus4_Bus_Sect_Title_Other2 { get; set; }
        [StringLength(100)]
        public string? Bus4_Bus_Sect_Title_Other3 { get; set; }
        [StringLength(100)]
        public string? Bus4_Tenant_Name { get; set; }
        public double? Bus4_Rental { get; set; }
        [StringLength(100)]
        public string? Bus4_Other_contribution { get; set; }
        public double? Bus4_Monthly_Levy { get; set; }
        public double? Bus4_Rental_Land_Size { get; set; }
        [StringLength(100)]
        public string? Bus4_Escalation { get; set; }
        [StringLength(100)]
        public string? Bus4_Lease_Term { get; set; }
        public string? Bus4_Start_Date { get; set; }
        public double? Bus4_Pool_Size { get; set; }
        public double? Bus4_Tennis_Court_Size { get; set; }
        public string? Bus4_Common_Property_Other_1 { get; set; }
        public string? Bus4_Common_Property_Other_2 { get; set; }
        public string? Bus4_Common_Property_Other_3 { get; set; }
        public double? Bus4_Garage_Size { get; set; }
        public double? Bus4_Carport_Size { get; set; }
        public double? Bus4_Open_Parking_Size { get; set; }
        public double? Bus4_Store_Room_Size { get; set; }
        public double? Bus4_Garden_Size { get; set; }
        public string? Bus4_Exclusive_Other { get; set; }

        public long? Appeal_Ref_SB4 { get; set; }
    }

}
