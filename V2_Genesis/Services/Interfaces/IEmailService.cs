using V2_Genesis.Models.Emails;

namespace V2_Genesis.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string htmlBody);

        Task SendConfirmationEmailAsync(
            string toEmail,
            string displayName,
            string confirmationLink);

        Task SendPasswordResetEmailAsync(
            string toEmail,
            string displayName,
            string resetLink);

        Task SendAccountDetailsChangedAsync(
            string toEmail,
            string displayName,
            IReadOnlyCollection<string> changedFields,
            DateTime changedAt,
            string profileUrl);

        Task SendObjectionAcknowledgementAsync(
            string objectionRef,
            string rollSource,
            bool isAppeal,
            byte[] acknowledgementPdf,
            string folderPath,
            List<EmailAttachment>? extraAttachments = null);

        Task SendSection78AcknowledgementAsync(
            string queryRef,
            bool isReview,
            string propertyDescription,
            byte[] acknowledgementPdf,
            string folderPath,
            List<EmailAttachment>? extraAttachments = null);

        // Single attachment method
        Task SendEmailWithAttachmentAsync(
            string toEmail,
            string subject,
            string htmlBody,
            byte[] attachmentBytes,
            string attachmentFileName);

        // Multiple attachments method
        Task SendEmailWithAttachmentsAsync(
            string toEmail,
            string subject,
            string body,
            List<EmailAttachment> attachments,
            bool isHtml = true);

        Task SendEvidenceUploadConfirmationAsync(
            string referenceNo,
            string rollSource,
            bool isAppeal,
            IReadOnlyCollection<string> uploadedFileNames,
            DateTime uploadedAt,
            int remainingSlots);

        Task SendAttributeEvidenceUploadConfirmationAsync(
            string attributeNo,
            IReadOnlyCollection<string> uploadedFileNames,
            DateTime uploadedAt,
            int remainingSlots);

        Task SendAttributeAcknowledgementAsync(
            string recipientEmail,
            string clientName,
            string attributeNumber,
            string propertyDescription,
            string evidencePin,
            DateTime evidenceDeadline,
            byte[] acknowledgementPdf,
            byte[] submittedFormPdf,
            string acknowledgementFileName,
            string submittedFormFileName,
            string folderPath);
    }
}
