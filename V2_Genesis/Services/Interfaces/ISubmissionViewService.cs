using V2_Genesis.Models.ViewModels.Submissions;

namespace V2_Genesis.Services.Interfaces
{
    public interface ISubmissionViewService
    {
        Task<SubmissionViewResult> GetSubmissionAsync(
        string submissionType,
        string referenceNumber,
        string rollSource,
        string userId,
        CancellationToken cancellationToken = default);
    }
}
