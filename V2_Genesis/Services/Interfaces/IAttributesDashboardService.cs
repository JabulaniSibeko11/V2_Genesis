using V2_Genesis.Services.Attributes;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAttributesDashboardService
    {
        Task<AttributesDashboardData> GetDashboardDataAsync(string userId);
    }
}
