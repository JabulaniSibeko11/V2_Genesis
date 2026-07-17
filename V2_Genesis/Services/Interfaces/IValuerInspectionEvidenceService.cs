using V2_Genesis.Models.ViewModels.ValuerInspectionEvidence;

namespace V2_Genesis.Services.Interfaces
{
    public interface IValuerInspectionEvidenceService
    {
        Task<ValuerInspectionTodayVm> GetTodayInspectionsAsync(string sapNumber);

        Task UploadEvidenceAsync(
            UploadValuerInspectionEvidenceVm vm,
            string? uploadedByUserId,
            string? uploadedByName);
    }
}
