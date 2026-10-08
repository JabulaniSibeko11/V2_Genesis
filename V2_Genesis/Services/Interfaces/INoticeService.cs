using V2_Genesis.Models.Notice;
using V2_Genesis.Services.Notice;

namespace V2_Genesis.Services.Interfaces
{
    public interface INoticeService
    {
        
        Task<(byte[] Pdf, string FileName)> GenerateSection49Async(string rollSource,string unitKey,string valuationKey);
        /// On-screen Section 49 notice (same content as the PDF). Null = property not found.
        Task<Section49ViewModel?> GetSection49ViewAsync(string rollSource, string unitKey, string valuationKey);

        Task<(byte[] Pdf, string FileName)> GenerateSection49ForObjectionAsync(string rollSource,string unitKey,string valuationKey,string objectionNo,string propertyDescription,string? premiseId = null);

        Task<(byte[] Pdf, string FileName)> GenerateAcknowledgementAsync(AcknowledgementData data);
        Task<(byte[] Pdf, string FileName)> GenerateAttachmentConfirmationAsync(string objectionNo,string rollSource,int fileCount,List<string> fileNames);

        Task<(byte[] Pdf, string FileName)> GenerateSection51AcknowledgementAsync(string objectionNo,string rollSource,int fileCount,List<string> fileNames);

        Task<NoticesDashboardViewModel> GetNoticesDashboardAsync(string userId, string displayName);

        (bool exists, string path, string ext) FindNoticeFile(string folder, string subFolder);
    }




}
