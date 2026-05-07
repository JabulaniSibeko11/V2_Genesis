using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Property_InfoModel
    {
        [Key]
        public long Objection_ID { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Objection_No { get; set; }


        [StringLength(100)]
        public string? Objector_Type { get; set; }
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
        public string? Capturer { get; set; }

        [StringLength(100)]
        public string? UserID { get; set; }

        public string? objection_Status { get; set; }

        public string? PropertyFrom { get; set; }

        public DateTime Objection_Start_DateTime { get; set; } = DateTime.Now;

       
    }

}
