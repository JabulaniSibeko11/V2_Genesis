using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section2QueryModel
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_SQ { get; set; }

        
        //Details
        

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

    }

}
