using V2_Genesis.Models.Results.Evidence;

namespace V2_Genesis.Services.Interfaces
{
    public interface IEvidenceService
    {
        Task<EvidenceValidateResult> ValidateAsync(
            string rollSource, string objectionNo, string pin);

        Task<(bool Success, string? Error, int NewCount, List<string> FileNames)>
            UploadAsync(
                string rollSource,
                string objectionNo,
                bool isAppeal,
                int currentCount,
                List<IFormFile> files);
    }
}
