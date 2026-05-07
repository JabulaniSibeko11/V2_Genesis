using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section3ResModel
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_SR3 { get; set; }

        //Section 3 Res
        //Section 3.1 Main Dwelling
        public int? Res_No_of_Bedroom { get; set; }
        public int? Res_No_of_BathRoom { get; set; }
        [StringLength(10)]
        public string? Res_Kitchen { get; set; }
        [StringLength(10)]
        public string? Res_Lounge { get; set; }
        [StringLength(10)]
        public string? Res_Dinning_Room { get; set; }
        [StringLength(10)]
        public string? Res_Lounge_Dining_Room { get; set; }
        [StringLength(10)]
        public string? Res_Study { get; set; }
        [StringLength(10)]
        public string? Res_Play_Room { get; set; }
        [StringLength(10)]
        public string? Res_Television { get; set; }
        [StringLength(10)]
        public string? Res_Laundry { get; set; }
        [StringLength(10)]
        public string? Res_Seperate_Toilet { get; set; }
        [StringLength(100)]
        public string? Res_Dwell_Other1 { get; set; }
        [StringLength(100)]
        public string? Res_Dwell_Other2 { get; set; }
        [StringLength(100)]
        public string? Res_Dwell_Other3 { get; set; }
        [StringLength(100)]
        public string? Res_Dwell_Other4 { get; set; }
        //Sectio 3.2 Outside Buildings

        public int? Res_No_of_Garages { get; set; }
        public string? Res_Granny_Room { get; set; }
        public string? Res_Outbuild_Other { get; set; }
        public double? Res_Main_Dwelling_Size { get; set; }
        public double? Res_Outside_Building_Size { get; set; }
        public double? Res_Other_Building_Size { get; set; }
        public double? Res_Total_Building_Size { get; set; }

        //Section 3.3

        [StringLength(10)]
        public string? Res_Swimming_Pool { get; set; }
        [StringLength(10)]
        public string? Res_Bore_Hole { get; set; }
        [StringLength(10)]
        public string? Res_Tennis_Court { get; set; }
        [StringLength(10)]
        public string? Res_Garden { get; set; }
        [StringLength(50)]
        public string? Res_other_dwell1 { get; set; }
        [StringLength(50)]
        public string? Res_other_dwell2 { get; set; }
        [StringLength(20)]
        public string? Res_Fence { get; set; }
        [StringLength(30)]
        public string? Res_Fence_Front { get; set; }
        [StringLength(30)]
        public string? Res_Fence_Back { get; set; }
        [StringLength(30)]
        public string? Res_Fence_Side_1 { get; set; }
        [StringLength(30)]
        public string? Res_Fence_Side_2 { get; set; }
        public double? Res_Fence_Height_Front { get; set; }
        public double? Res_Fence_Height_Back { get; set; }
        public double? Res_Fence_Height_Side1 { get; set; }
        public double? Res_Fence_Height_Side2 { get; set; }
        public string? Res_Drive_Way { get; set; }
        public String? Res_Security_Boomed_Area { get; set; }
        [StringLength(100)]
        public string? Res_Other_features { get; set; }
        [StringLength(100)]
        public string? Res_Other_features_Condition { get; set; }
        [StringLength(100)]
        public string? Res_General_Condition { get; set; }

        public long? Appeal_Ref_SR3 { get; set; }
    }

}
