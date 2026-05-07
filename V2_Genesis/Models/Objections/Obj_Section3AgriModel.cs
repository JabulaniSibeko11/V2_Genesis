using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section3AgriModel
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_SA3 { get; set; }

        //Section 3 Agric
        //Section 3.1
        public int? Agri_No_of_Bedroom { get; set; }
        public int? Agri_No_of_BathRoom { get; set; }
        public string? Agri_Kitchen { get; set; }
        [StringLength(10)]
        public string? Agri_Lounge { get; set; }
        [StringLength(10)]
        public string? Agri_Dinning_Room { get; set; }
        [StringLength(10)]
        public string? Agri_Lounge_Dining_Room { get; set; }
        [StringLength(10)]
        public string? Agri_Study { get; set; }
        [StringLength(10)]
        public string? Agri_Play_Room { get; set; }
        [StringLength(10)]
        public string? Agri_Television { get; set; }
        [StringLength(10)]
        public string? Agri_Laundry { get; set; }
        [StringLength(10)]
        public string? Agri_Seperate_Toilet { get; set; }
        [StringLength(100)]
        public string? Agri_Dwell_Other1 { get; set; }
        [StringLength(100)]
        public string? Agri_Main_Dwelling_Size { get; set; }
        public int? Agri_Building_No { get; set; }
        [StringLength(400)]
        public string? Agri_Building_Description { get; set; }
        public double? Agri_Building_Size { get; set; }

        [StringLength(100)]
        public string? Agri_Building_Condition { get; set; }
        [StringLength(100)]
        public string? Agri_Building_Functional { get; set; }
        
        public string? Agri_Another_Purpose_Not_Agriculture { get; set; }
        public string? Agri_Another_Purpose_Not_Agriculture_Desc { get; set; }
        public double? Agri_Non_Agricultural { get; set; }
        public double? Agri_Grazing { get; set; }
        public double? Agri_Under_Irrigation { get; set; }
        public double? Agri_Dry_Land { get; set; }
        public double? Agri_Permanent_Crop { get; set; }
        
        public double? Agri_Other_ha_1 { get; set; }
        public double? Agri_Other_ha_2 { get; set; }
        public double? Agri_Other_ha_3 { get; set; }

        public double? Agri_Total_ha { get; set; }
        [StringLength(100)]
        public string? Agri_Fence_Condition { get; set; }
        public double? Agri_Game_Area_Fenced { get; set; }
        public double? Agri_Num_of_Boreholes { get; set; }
        public double? Agri_Output_litres_Hours { get; set; }
        public int? Agri_Dams { get; set; }
        public int? Agri_Capacity { get; set; }
        public String? Agri_Exposed_To_River { get; set; }
        public String? Agri_Land_Claim { get; set; }
        [StringLength(100)]
        public string? Agri_Claim_Date { get; set; }
        public double? Agri_Gazette_No { get; set; }
        public String? Agri_Water_Rights { get; set; }

        public String? Agri_Water_Rights_Details { get; set; }
        public String? Agri_Rezoning_Consent_Use { get; set; }
        [StringLength(255)]
        public string? Agri_Consent_Use_Details { get; set; }
        public String? Agri_Land_Excised { get; set; }
        [StringLength(100)]
        public string? Agri_New_Farm_Desc { get; set; }
        public String? Agri_Township_Applied { get; set; }
        [StringLength(255)]
        public string? Agri_Township_Applied_Detail { get; set; }
        
        
        
        //BUSINESS
        [StringLength(100)]
        public string? Agri_Tenant_Name { get; set; }
        public double? Agri_Rental_Land_Size { get; set; }
        public double? Agri_Rental { get; set; }
        [StringLength(100)]
        public string? Agri_Escalation { get; set; }
        [StringLength(100)]
        public string? Agri_Other_contribution { get; set; }
        [StringLength(100)]
        public string? Agri_Lease_Term { get; set; }
        public string? Agri_Start_Date { get; set; }
        public string? Agri_Use { get; set; }

        public long? Appeal_Ref_SA3 { get; set; }
    }

}
