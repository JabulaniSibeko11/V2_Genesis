using V2_Genesis.Models.ViewModels.Attributes;
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
    }
}
