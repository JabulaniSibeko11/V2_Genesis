using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace V2_Genesis.Models.Notifications
{
    [Table("Notifications")]
    public class Notifications
    {
        [Key]
        public long IDNotifications { get; set; }

        public string? UserID { get; set; }
        public string? UserEmail { get; set; }
        public string? TargetRole { get; set; }

        public string? Title { get; set; }
        public string Message { get; set; } = "";

        public string? NotificationType { get; set; }

        public string? ReferenceNumber { get; set; }
        public string? PremiseID { get; set; }
        public string? RollSource { get; set; }
        public string? SourceTable { get; set; }

        public string? Url { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
    }
}