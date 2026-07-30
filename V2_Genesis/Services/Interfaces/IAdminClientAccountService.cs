using V2_Genesis.Models.ViewModels.Admin;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAdminClientAccountService
    {
        Task<AdminClientAccountViewModel?> GetClientAccountAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }

}
