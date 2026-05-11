

using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Models.ViewModels.Dashboard;

namespace V2_Genesis.Services.Interfaces;

public interface IDashboardService
{
    Task<RollData> GetRollDataAsync(string rollSource, string userId, string userEmail);
    Task<List<AttrLinkedPropertyResult>> GetAttributesLinkedAsync(string userId);
}