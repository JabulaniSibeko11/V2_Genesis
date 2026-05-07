using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section4ResModel
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_SR4 { get; set; }

        //Section 4 Res
        //Section 4.1
        [StringLength(100)]
        public string? Res4_Scheme_Name { get; set; }
        public int Res4_Scheme_No { get; set; }
        public int Res4_Flat_No { get; set; }
        public double? Res4_Unit_Size { get; set; }
        [StringLength(100)]
        public string? Res4_Managing_Agent_Name { get; set; }
        [StringLength(100)]
        public string? Res4_Managing_Agent_Tel_No { get; set; }
        public int Res4_No_of_Bedroom { get; set; }
        public int Res4_No_of_BathRoom { get; set; }
        [StringLength(10)]
        public string? Res4_Monthly_Levy_Res { get; set; }
        [StringLength(10)]
        public string? Res4_Kitchen { get; set; }
        [StringLength(10)]
        public string? Res4_Lounge { get; set; }
        [StringLength(10)]
        public string? Res4_Dinning_Room { get; set; }
        [StringLength(10)]
        public string? Res4_Lounge_Dining_Room { get; set; }
        [StringLength(10)]
        public string? Res4_Study { get; set; }
        [StringLength(10)]
        public string? Res4_Play_Room { get; set; }
        [StringLength(10)]
        public string? Res4_Television { get; set; }
        [StringLength(10)]
        public string? Res4_Laundry { get; set; }
        [StringLength(10)]
        public string? Res4_Seperate_Toilet { get; set; }
        public string? Res4_Dwell_Other1 { get; set; }
        [StringLength(100)]
        public string? Res4_Dwell_Other2 { get; set; }
        [StringLength(100)]
        public string? Res4_Dwell_Other3 { get; set; }
        [StringLength(100)]
        public string? Res4_Dwell_Other4 { get; set; }
        public string? Res4_Common_Property_Other_1 { get; set; }
        public string? Res4_Common_Property_Other_2 { get; set; }
        public string? Res4_Common_Property_Other_3 { get; set; }
        public double? Res4_Pool_Size { get; set; }
        public double? Res4_Tennis_Court_Size { get; set; }
        public double? Res4_Garage_Size { get; set; }
        public double? Res4_Carport_Size { get; set; }
        public double? Res4_Open_Parking_Size { get; set; }
        public double? Res4_Store_Room_Size { get; set; }
        public double? Res4_Garden_Size { get; set; }
        public string? Res4_Exclusive_Other { get; set; }

        public long? Appeal_Ref_SR4 { get; set; }
    }

}
