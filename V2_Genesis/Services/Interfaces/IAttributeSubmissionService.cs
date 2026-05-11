using V2_Genesis.Models.ViewModels.Attributes;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributeSubmissionService
    {
        AttributeSubmissionViewModel CreateNew(string formType);

        Task<long> SubmitAsync(AttributeSubmissionViewModel model, string userId, string userName);

        Task<AttributeSubmissionViewModel?> GetForReviewAsync(long attrId);

        Task AssignToValuerAsync(long attrId, string valuerUserId, string valuerName, string assignedBy, string? comment);

        Task ValuerDecisionAsync(long attrId, string decision, string valuerUserId, string valuerName, string? comment, string? rejectionReason);

        Task WithdrawAsync(long attrId, string userId, string userName, string reason);
    }
}
