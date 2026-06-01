using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Serialization;

namespace V2_Genesis.Models
{
    public class Rebate_Info
    {
        [Key]
        public long Rebate_ID { get; set; }

        [BindNever]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string Rebate_No { get; set; }

        [StringLength(100)]
        public string? Rebate_Type { get; set; }

        [StringLength(100)]
        public string? UserID { get; set; }

        public string? Status { get; set; }

        //public string? StatusReason { get; set; } 
    }
}
