using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Files
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Obj_Property_Info")]

        public long? Ref { get; set; }
        [StringLength(100)]
		public string? Objection_Ref_files { get; set; }
        [StringLength(100)]
        public string? Files1 { get; set; }
        [StringLength(100)]
        public string? Files2 { get; set; }
        [StringLength(100)]
        public string? Files3 { get; set; }
        [StringLength(100)]
        public string? Files4 { get; set; }
        [StringLength(100)]
        public string? Files5 { get; set; }
        [StringLength(100)]
        public string? Files6 { get; set; }
        [StringLength(100)]
        public string? Files7 { get; set; }
        [StringLength(100)]
        public string? Files8 { get; set; }
        [StringLength(100)]
        public string? Files9 { get; set; }
        [StringLength(100)]
        public string? Files10 { get; set; }
        [StringLength(100)]
        public string? Rep_letter { get; set; }
        [StringLength(100)]
        public double? Evidence_count { get; set; }

        public long? Appeal_Ref_files { get; set; }
    }
}
