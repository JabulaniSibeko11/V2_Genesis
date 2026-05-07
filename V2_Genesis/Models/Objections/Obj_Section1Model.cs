using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section1Model
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_S1 { get; set; }

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

        public long? Appeal_Ref_S1 { get; set; }

    }

}
