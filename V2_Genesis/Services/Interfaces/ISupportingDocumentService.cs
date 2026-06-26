using V2_Genesis.Models.ViewModels;

namespace V2_Genesis.Services.Interfaces
{
    public interface ISupportingDocumentService
    {
        Task<List<SupportingDocumentViewModel>> GetDocumentsAsync(
          string referenceNo,
          string? rollSource);

        Task<(bool Success, string? Error)> AddDocumentsAsync(
            string referenceNo,
            string? rollSource,
            List<IFormFile> files,
            string? uploadedBy);
    }
}
