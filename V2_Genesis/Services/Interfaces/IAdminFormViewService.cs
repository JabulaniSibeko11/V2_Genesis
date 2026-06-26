using V2_Genesis.Models.Results;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAdminFormViewService
    {
        Task<AdminFormViewResult> GetFormViewAsync(
            string referenceNo,
            string rollSource,
            string? propertyType,
            bool isAppeal,
            bool isQuery);
    }

}
