using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models
{
    public class Que_WithdrawalsModel
    {
        [Key]
        public long ID_Withdrawal { get; set; }
        [StringLength(100)]
        public string? Query_Withdrawn { get; set; }

        [StringLength(100)]
        public string? User { get; set; }
        
    }

}
