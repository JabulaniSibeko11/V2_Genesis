using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Attributes;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributesDashboardService
    {
        Task<AttributesDashboardData> GetDashboardDataAsync(string userId);
        Task RespondToInspectionAppointmentAsync(
         InspectionAppointmentResponseVm vm,
         string userId,
         string userEmail);

 

        Task<VerifiedValuerPhotoVm?> GetVerifiedValuerPhotoAsync(
    long inspectionRequestId,
    string userId);

        Task<AppointmentValuerDetailsVm> VerifyInspectionPinAsync(
    VerifyInspectionPinVm vm,
    string userId,
    string userEmail,
    string? ipAddress,
    string? userAgent);

        Task ResubmitReturnedAttributeAsync(
    ResubmitReturnedAttributeVm vm,
    string userId,
    string userEmail);
    }
}
