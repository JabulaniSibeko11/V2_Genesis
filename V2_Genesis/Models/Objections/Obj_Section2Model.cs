using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section2Model
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_S2 { get; set; }

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

        public long? Appeal_Ref_S2 { get; set; }
    }

}
