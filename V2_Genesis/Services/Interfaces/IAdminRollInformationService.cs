using V2_Genesis.Models.Results.Admin;

namespace V2_Genesis.Services.Interfaces;

public interface IAdminRollInformationService
{
    Task<AdminRollInformation> GetAsync(
        AdminEnquiryFoundation foundation,
        string? selectedRollSource,
        CancellationToken cancellationToken = default);
}
