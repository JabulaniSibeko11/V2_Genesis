using V2_Genesis.Models.ViewModels.Admin;

namespace V2_Genesis.Services.Interfaces;

public interface IAdminPropertyEnquiryService
{
    Task<AdminPropertyEnquiryViewModel?> GetAsync(
        string userId,
        string propertyKey,
        CancellationToken cancellationToken = default);

    Task<bool> NoticeBelongsToClientAsync(
        string userId,
        string filePath,
        CancellationToken cancellationToken = default);
}
