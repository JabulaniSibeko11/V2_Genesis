using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace V2_Genesis.Models
{
    public class LinkedProperties
    {
        [Key]
        public long ID { get; set; }
        public string? IDProperty { get; set; } 
        public string? UserID { get; set; }

        public string? PropertyFrom { get; set;}
	}
}
