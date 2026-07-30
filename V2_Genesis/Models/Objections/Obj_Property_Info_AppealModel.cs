using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Obj_Property_Info_AppealModel
    {
        [Key]
        public long Appeal_ID { get; set; }
        public string? Appeal_No { get; set; }
        public string? Obj_Ref { get; set; }
        public string? Capturer { get; set; }


        [StringLength(100)]
        public string? Appeal_Type { get; set; }
        [StringLength(100)]
        public string? A_Property_Type { get; set; }
        [StringLength(100)]
        public string? A_Property_Desc { get; set; }
        [StringLength(100)]
        public string? A_Premise_id { get; set; }
        [StringLength(100)]
        public string? A_Unit_key { get; set; }
        [StringLength(100)]
        public string? A_Property_id { get; set; }
        [StringLength(100)]
        public string? A_Valuation_Key { get; set; }
        [StringLength(100)]
        public string? A_Sector { get; set; }

        [StringLength(100)]
        public string? A_UserID { get; set; }

        [StringLength(25)]
        public string? Appeal_Status { get; set; }
        public DateTime? Appeal_Start_DateTime { get; set; }
        

    }

}
    