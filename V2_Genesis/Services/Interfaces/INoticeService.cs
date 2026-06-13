using V2_Genesis.Models.Notice;
using V2_Genesis.Services.Notice;

namespace V2_Genesis.Services.Interfaces
{
    public interface INoticeService
    {
        /// <summary>
        /// Generates Section 49 PDF for a property.
        /// Returns (pdfBytes, fileName).
        /// </summary>
        Task<(byte[] Pdf, string FileName)> GenerateSection49Async(
            string rollSource,
            string unitKey,
            string valuationKey);

        Task<(byte[] Pdf, string FileName)> GenerateAcknowledgementAsync(
       AcknowledgementData data);
        Task<(byte[] Pdf, string FileName)> GenerateAttachmentConfirmationAsync(
    string objectionNo,
    string rollSource,
    int fileCount,
    List<string> fileNames);

        Task<(byte[] Pdf, string FileName)> GenerateSection51AcknowledgementAsync(
    string objectionNo,
    string rollSource,
    int fileCount,
    List<string> fileNames);


        Task<NoticesDashboardViewModel> GetNoticesDashboardAsync(string userId, string displayName);
        (bool exists, string path, string ext) FindNoticeFile(string folder, string subFolder);
        Task<(bool Success, byte[]? FileBytes, string? FileName, string? Error)> GetSavedAcknowledgementAsync(
    string referenceNo,
    string rollSource);
    }



}
