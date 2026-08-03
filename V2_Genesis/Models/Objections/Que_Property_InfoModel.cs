using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Que_Property_InfoModel
    {
        [Key]
        public long Query_ID { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [StringLength(100)]
        public string? Query_No { get; set; }

        [StringLength(100)]
        public string? Review_No { get; set; }

        [StringLength(100)]
        public string? Query_Type { get; set; }
        [StringLength(100)]
        public string? Property_Type { get; set; }
        [StringLength(100)]
        public string? Property_Desc { get; set; }
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

        [StringLength(100)]
        public string? UserID { get; set; }

        public string? Query_Status { get; set; }

        public int Sub_typ { get; set; }
        [StringLength(50)]
        public string? Capturer { get; set; }



    }

}
