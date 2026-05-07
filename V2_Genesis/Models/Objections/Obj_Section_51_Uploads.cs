using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Section_51_Uploads
    {
        [Key]
        public long ID { get; set; }
        public string? Objection_Ref_51 { get; set; }
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
        public double? Evidence_count { get; set; }

        public string? Appeal_Ref_51 { get; set; }
    }

}