using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models { 
    public class Rebates_Files
    {
        [Key]
        public long ID { get; set; }
        [ForeignKey("Rebate_Info")]

        public long? Ref { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? Rebate_Ref_files { get; set; }


        //[StringLength(100)]
        public string? Files1 { get; set; }
        //[StringLength(100)]
        public string? Files2 { get; set; }

        //[StringLength(100)]
        public string? Files3 { get; set; }
        //[StringLength(100)]
        public string? Files4 { get; set; }
        //[StringLength(100)]
        public string? Files5 { get; set; }
        //[StringLength(100)]
        public string? Files6 { get; set; }
        //[StringLength(100)]
        public string? Files7 { get; set; }
        //[StringLength(100)]
        public string? Files8 { get; set; }
        //[StringLength(100)]
        public string? Files9 { get; set; }
        //[StringLength(100)]
        public string? Files10 { get; set; }
        //[StringLength(100)]
        public string? Rep_letter { get; set; }
        //[StringLength(100)]
        public double? Evidence_count { get; set; }
    }
}
