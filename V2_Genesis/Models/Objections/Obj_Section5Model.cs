using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section5Model
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_S5 { get; set; }

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

        public long? Appeal_Ref_S5 { get; set; }


    }

}
