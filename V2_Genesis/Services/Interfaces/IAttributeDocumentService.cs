using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Models.ViewModels.Attributes;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributeDocumentService
    {
        Task<AttributeDocumentSaveResult> CreateSubmissionPackageAsync(
           AttributeSubmissionViewModel model,
           AttrPropertyInfo propertyInfo);

        Task<(byte[] Pdf, string FileName)> GenerateAcknowledgementPdfAsync(
            AttributeSubmissionViewModel model,
            AttrPropertyInfo propertyInfo);

        Task<(byte[] Pdf, string FileName, string FullPath, string FolderPath)>
            GenerateCorrectionAcknowledgementPdfAsync(
                AttributeSubmissionViewModel model,
                AttrPropertyInfo propertyInfo,
                string correctionComment,
                IReadOnlyCollection<string> correctedSections);
    }
}
