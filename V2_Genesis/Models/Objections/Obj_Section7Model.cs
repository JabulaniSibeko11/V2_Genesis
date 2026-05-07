using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section7Model
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]
        
        public long? Ref { get; set; }
		[StringLength(100)]
		public string? Objection_Ref_S7 { get; set; }


        //Section 7
        [StringLength(100)]
        public string? Declaration_Date { get; set; }
        [StringLength(5000)]
        public string? Signature_Picture { get; set; }
        [StringLength(100)]
        public string? Signature_Name { get; set; }

        public string? RandomPin { get; set; }

        public string?File_Name { get; set; }

        public string? File_Type { get; set; }

        public string? File_Path { get; set; }

        public long? Appeal_Ref_S7 { get; set; }
        [StringLength(50)]
        public string? Section51Pin { get; set; }

    }

}
