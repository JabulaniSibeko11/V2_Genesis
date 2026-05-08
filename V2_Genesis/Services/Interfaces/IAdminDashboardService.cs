using V2_Genesis.Models.Results.Admin;

namespace V2_Genesis.Services.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminRollStats> GetStatsAsync(string rollSource);
        Task<List<AdminObjectionResult>> SearchObjectionsAsync(string rollSource, string searchValue);
        Task<List<AdminAppealResult>> SearchAppealsAsync(string rollSource, string searchValue);
    }
}
