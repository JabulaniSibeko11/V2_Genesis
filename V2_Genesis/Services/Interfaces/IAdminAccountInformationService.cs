using V2_Genesis.Models.Results.Admin;

namespace V2_Genesis.Services.Interfaces;

public interface IAdminAccountInformationService
{
    Task<AdminAccountInformation> GetAsync(
        AdminEnquiryFoundation foundation,
        CancellationToken cancellationToken = default);
}
