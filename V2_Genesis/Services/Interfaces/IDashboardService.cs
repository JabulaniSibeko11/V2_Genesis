

using V2_Genesis.Models.ViewModels.Dashboard;

namespace V2_Genesis.Services.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Fetches all dashboard data for one roll —
    /// linked properties, objections, appeals and notifications.
    /// Uses the roll's own database connection.
    /// </summary>
    Task<RollData> GetRollDataAsync(
        string rollSource,
        string userId,
        string userEmail);
}