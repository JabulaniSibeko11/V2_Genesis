namespace V2_Genesis.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendConfirmationEmailAsync(string toEmail, string displayName, string confirmationLink);
        Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetLink);
    }
}
