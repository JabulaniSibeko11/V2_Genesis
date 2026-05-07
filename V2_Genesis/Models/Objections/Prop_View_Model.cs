using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace V2_Genesis.Models
{
    public class Prop_View_Model
    {
        //Objection model/table 
        [Key]
        public long Objection_ID { get; set; }
		public long Id { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Objection_No { get; set; }

        [StringLength(100)]
        public string? Objector_Type { get; set; }
        [StringLength(100)]
        public string? Property_Type { get; set; }
        [StringLength(100)]
        public string? Property_Desc { get; set; }
		public string? PropertyFrom { get; set; }
		[StringLength(100)]
        public string? Premise_id { get; set; }
        [StringLength(100)]
        public string? Unit_key { get; set; }
        [StringLength(100)]
        public string? Property_id { get; set; }
        [StringLength(100)]
        public string? Valuation_Key { get; set; }
        [StringLength(100)]
        public string? Sector { get; set; }
        public string? objection_Status { get; set; }

		//Apeal
		public long Appeal_ID { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Appeal_No { get; set; }
        public string? Obj_Ref { get; set; }


        [StringLength(100)]
        public string? Appeal_Type { get; set; }
        [StringLength(100)]
        public string? A_Property_Type { get; set; }
        [StringLength(100)]
        public string? A_Property_Desc { get; set; }
        [StringLength(100)]
        public string? A_Premise_id { get; set; }
        [StringLength(100)]
        public string? A_Unit_key { get; set; }
        [StringLength(100)]
        public string? A_Property_id { get; set; }
        [StringLength(100)]
        public string? A_Valuation_Key { get; set; }
        [StringLength(100)]
        public string? A_Sector { get; set; }
        [StringLength(100)]
        public string? A_UserID { get; set; }
        [StringLength(50)]
        public string? Capturer { get; set; }
		public string? New_Market_Value_MVD { get; set; }
		public string? New_Category_MVD { get; set; }

		//Section 1
		//Owner
		//Datails
		[StringLength(100)]
        public string? Owner_Name { get; set; }
        [StringLength(100)]
        public string? Owner_Identity { get; set; }
        [StringLength(100)]
        public string? Owner_Company { get; set; }
        [StringLength(100)]
        //Address
        public string? Owner_Address_1 { get; set; }

        [StringLength(100)]
        public string? Owner_Address_2 { get; set; }

        [StringLength(100)]
        public string? Owner_Address_3 { get; set; }

        [StringLength(100)]
        public string? Owner_Address_4 { get; set; }
        [StringLength(10)]
        public string? Owner_Address_5 { get; set; }
        //Postal
        [StringLength(100)]
        public string? Owner_Postal_1 { get; set; }

        [StringLength(100)]
        public string? Owner_Postal_2 { get; set; }

        [StringLength(100)]
        public string? Owner_Postal_3 { get; set; }

        [StringLength(100)]
        public string? Owner_Postal_4 { get; set; }
        [StringLength(10)]
        public string? Owner_Postal_5 { get; set; }
        [StringLength(15)]
        //Contact Details
        public string? Owner_Home_Phone { get; set; }
        [StringLength(15)]
        public string? Owner_Cell_Phone { get; set; }
        [StringLength(15)]
        public string? Owner_Work_Phone { get; set; }
        [StringLength(15)]
        public string? Owner_Fax_Phone { get; set; }
        [StringLength(100)]
        public string? Owner_Email { get; set; }


        //Objector
        //Datails
        [StringLength(100)]
        public string? Objector_Name { get; set; }

        [StringLength(100)]
        public string? Objector_Identity { get; set; }
        [StringLength(100)]
        public string? Objector_Company { get; set; }
        [StringLength(100)]
        //Address 
        public string? Objector_Postal_1 { get; set; }

        [StringLength(100)]
        public string? Objector_Postal_2 { get; set; }

        [StringLength(100)]
        public string? Objector_Postal_3 { get; set; }

        [StringLength(100)]
        public string? Objector_Postal_4 { get; set; }
        [StringLength(10)]
        public string? Objector_Postal_5 { get; set; }
        [StringLength(15)]
        //contact Details
        public string? Objector_Home { get; set; }
        [StringLength(15)]
        public string? Objector_Cell { get; set; }
        [StringLength(15)]
        public string? Objector_Work { get; set; }
        [StringLength(15)]
        public string? Objector_Fax { get; set; }
        [StringLength(100)]
        public string? Objector_Email { get; set; }
        [StringLength(100)]
        public string? Objector_Status { get; set; }
        [StringLength(100)]


        //Representative
        //Details
        public string? Representative_name { get; set; }
        [StringLength(100)]
        //Postal
        public string? Rep_Postal_1 { get; set; }

        [StringLength(100)]
        public string? Rep_Postal_2 { get; set; }

        [StringLength(100)]
        public string? Rep_Postal_3 { get; set; }

        [StringLength(100)]
        public string? Rep_Postal_4 { get; set; }
        [StringLength(10)]
        public string? Rep_Postal_5 { get; set; }
        [StringLength(15)]
        //Contact Details
        public string? Rep_Home_Phone { get; set; }
        [StringLength(15)]
        public string? Rep_Cell_Phone { get; set; }
        [StringLength(15)]
        public string? Rep_Work_Phone { get; set; }
        [StringLength(15)]
        public string? Rep_Fax_Phone { get; set; }
        [StringLength(100)]
        public string? Rep_Email { get; set; }

        //Section 2
        [StringLength(100)]
        public string? physical_address { get; set; }
        [StringLength(100)]
        public string? Town_Name { get; set; }
        [StringLength(10)]
        public string? Code { get; set; }
        public double? Extent { get; set; }
        public int? Municipal_Account_No { get; set; }
        [StringLength(100)]
        public string? BondHolder_Name { get; set; }

        public double? Registered_Amount { get; set; }
        [StringLength(550)]
        public string? Full_Details { get; set; }

        [StringLength(50)]
        public string? Servitude_No { get; set; }
        public double? Affected_Area { get; set; }
        [StringLength(100)]
        public string? Property_Favour_Of { get; set; }
        [StringLength(250)]
        public string? Property_Purpose { get; set; }
        public string? Compensation_Paid { get; set; }
        [StringLength(50)]
        public string? Payment_Date { get; set; }
        public double? Compensation_Amount { get; set; }

        //Section 2 Query
        [StringLength(100)]
        public string? Objection_Ref_SQ { get; set; }

        [StringLength(5)]
        public string? Option_A { get; set; }

        [StringLength(5)]
        public string? Option_B { get; set; }

        [StringLength(5)]
        public string? Option_C { get; set; }
        [StringLength(5)]
        public string? Option_D { get; set; }
        //Postal
        [StringLength(5)]
        public string? Option_E { get; set; }

        [StringLength(5)]
        public string? Option_F { get; set; }

        [StringLength(5)]
        public string? Option_G { get; set; }

        [StringLength(5)]
        public string? Option_H { get; set; }
        [StringLength(1100)]
        public string? Motivation_for_Supp_Request { get; set; }



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
        [StringLength(10)]
        public string? Res_Other_features { get; set; }
        [StringLength(100)]
        public string? Res_Other_features_Condition { get; set; }
        [StringLength(100)]
        public string? Res_General_Condition { get; set; }

        //Section 4 Business
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

        //Section 4 Res
        //Section 4.1
        [StringLength(100)]
        public string? Res4_Scheme_Name { get; set; }
        public int? Res4_Scheme_No { get; set; }
        public int? Res4_Flat_No { get; set; }
        public double? Res4_Unit_Size { get; set; }
        [StringLength(100)]
        public string? Res4_Managing_Agent_Name { get; set; }
        [StringLength(100)]
        public string? Res4_Managing_Agent_Tel_No { get; set; }
        public int? Res4_No_of_Bedroom { get; set; }
        public int? Res4_No_of_BathRoom { get; set; }
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

        //Section 5
        public double? Current_Asking_price { get; set; }
        public double? Previous_Asking_price { get; set; }
        [StringLength(100)]
        public string? Agent_Name { get; set; }
        public int? Unit_No { get; set; }
        [StringLength(100)]
        public string? Other_Nearby_Sales { get; set; }
        [StringLength(100)]
        public string? Sale_Date { get; set; }
        public double? Current_Recieved_Offer { get; set; }
        public double? Previous_Recieved_Offer { get; set; }
        [StringLength(15)]
        public string? Agent_Tel_No { get; set; }
        [StringLength(100)]
        public string? Suburb_Name { get; set; }
        [StringLength(100)]
        public string? Selling_Price { get; set; }

        //Section 6

        [StringLength(100)]
        public string? Old_Property_Description { get; set; }
        [StringLength(100)]
        public string? Old_Category { get; set; }
        [StringLength(250)]
        public string? Old_Address { get; set; }
        public string? Old_Extent { get; set; }
        public string? Old_Market_Value { get; set; }
        [StringLength(100)]
        public string? Old_Owner { get; set; }

        [StringLength(100)]
        public string? New_Property_Description { get; set; }
        [StringLength(100)]
        public string? New_Category { get; set; }
        [StringLength(250)]
        public string? New_Address { get; set; }
        public string? New_Extent { get; set; }
        public string? New_Market_Value { get; set; }
        [StringLength(100)]
        public string? New_Owner { get; set; }
        [StringLength(500)]
        public string? Objection_Reasons { get; set; }

        //Second
        [StringLength(100)]
        public string? Old2_Category { get; set; }
        public string? Old2_Extent { get; set; }
        public string? Old2_Market_Value { get; set; }

        [StringLength(100)]
        public string? New2_Category { get; set; }
        public string? New2_Extent { get; set; }
        public string? New2_Market_Value { get; set; }

        //Third

        [StringLength(100)]
        public string? Old3_Category { get; set; }
        public string? Old3_Extent { get; set; }
        public string? Old3_Market_Value { get; set; }

        [StringLength(100)]
        public string? New3_Category { get; set; }
        public string? New3_Extent { get; set; }
        public string? New3_Market_Value { get; set; }

        //Section 7
        [StringLength(100)]
        public string? Declaration_Date { get; }
        [StringLength(5000)]
        public string? Signature_Picture { get; set; }
        [StringLength(100)]
        public string? Signature_Name { get; set; }

        public string? RandomPin { get; set; }

        public string? File_Name { get; set; }

        public string? File_Type { get; set; }

        public string? File_Path { get; set; }

    }
}
