
using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Services;

namespace V2_Genesis.Models.ViewModels.Dashboard;

public class ClientDashboardViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public bool IsCompany { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AnnouncementResult Announcement { get; set; } = new();

    /// <summary>Ordered list of rolls from GV_LIST — drives all sections.</summary>
    public List<GvList> Rolls { get; set; } = new();

    /// <summary>
    /// Per-roll data keyed by GvList.Source.
    /// DashboardController stubs these out as empty.
    /// Replace with real queries when data methods are ready.
    /// </summary>
    public Dictionary<string, RollData> RollData { get; set; } = new();
}

/// <summary>Data bucket for one roll's three tables.</summary>
public class RollData
{
    public List<LinkedPropertyResult> LinkedProperties { get; set; } = new();
    public List<ObjectedPropertyResult> ObjectedProperties { get; set; } = new();
    public List<AppealResult> Appeals { get; set; } = new();
    public List<NotificationResult> Notifications { get; set; } = new();

    public int LinkedCount => LinkedProperties.Count;
    public int ObjectedCount => ObjectedProperties.Count;
    public int AppealsCount => Appeals.Count;
}