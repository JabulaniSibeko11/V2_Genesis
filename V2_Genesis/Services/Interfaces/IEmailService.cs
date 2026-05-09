namespace V2_Genesis.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task SendConfirmationEmailAsync(string toEmail, string displayName, string confirmationLink);
        Task SendPasswordResetEmailAsync(string toEmail, string displayName, string resetLink);

        Task SendObjectionAcknowledgementAsync(string objectionRef,string rollSource,bool isAppeal,byte[] acknowledgementPdf,string folderPath);
    }
}
