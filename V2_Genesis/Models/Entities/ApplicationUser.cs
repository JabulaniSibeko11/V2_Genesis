using Microsoft.AspNetCore.Identity;

namespace V2_Genesis.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? IDNumber { get; set; }
        public string? PassportNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyRegistration { get; set; }
        public string? SAPNumber { get; set; }
        public bool UserNotification { get; set; } = false;
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        // ── Helpers ────────────────────────────────────────────────────────────
        public string DisplayName =>
            !string.IsNullOrWhiteSpace(CompanyName)
                ? CompanyName
                : $"{FirstName} {LastName}".Trim();

        public bool IsCompany => !string.IsNullOrWhiteSpace(CompanyRegistration);
    }
}
