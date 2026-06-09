// ═══════════════════════════════════════════════════════════════
//  Models/ViewModels/Dashboard/AdminDashboardViewModel.cs
//  REPLACE full file — adds UserId, Announcement, Rebates
// ═══════════════════════════════════════════════════════════════
using V2_Genesis.Models;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Rebates;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Services;
using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.PropertySearch;

namespace V2_Genesis.Models.ViewModels.Dashboard;

public class AdminDashboardViewModel
{
    // ── Identity ──────────────────────────────────────────────────────
    public string AdminEmail { get; set; } = string.Empty;
    public string SapNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    // ── Announcement (same as client dashboard) ───────────────────────
    public AnnouncementResult Announcement { get; set; } = new();

    // ── Rolls + roll data ─────────────────────────────────────────────
    public List<GvList> Rolls { get; set; } = new();
    public Dictionary<string, RollData> RollData { get; set; } = new();
    public Dictionary<string, RollDateEntry> RollDates { get; set; } = new();

    // ── Rebates ───────────────────────────────────────────────────────
    public List<Rebate_View_Model> Rebates { get; set; } = new();

    // ── Attributes ────────────────────────────────────────────────────
    public AttributesDashboardData AttributesData { get; set; } = new();
    public List<AttrLinkedPropertyResult> AttributesLinked { get; set; } = new();

    public string AdminFullName { get; set; } = string.Empty;
    public string AdminPosition { get; set; } = string.Empty;
    
    public string SapNumeric { get; set; } = string.Empty;

    // ── Admin search state ────────────────────────────────────────────
    public string? SearchValue { get; set; }
    public string? FilterRoll { get; set; }
    public string? FilterStatus { get; set; }
    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchValue);

    // ── Admin search results ──────────────────────────────────────────
    public Dictionary<string, List<AdminObjectionResult>> SearchObjections { get; set; } = new();
    public Dictionary<string, List<AdminAppealResult>> SearchAppeals { get; set; } = new();

    // ── Helpers ───────────────────────────────────────────────────────
    public RollData DataFor(string rollSource) =>
        RollData.TryGetValue(rollSource, out var d) ? d : new RollData();

    public RollDateEntry? DatesFor(string rollSource) =>
        RollDates.TryGetValue(rollSource, out var d) ? d : null;

    public List<AdminObjectionResult> ObjectionsFor(string rollSource) =>
        SearchObjections.TryGetValue(rollSource, out var o) ? o : new();

    public List<AdminAppealResult> AppealsFor(string rollSource) =>
        SearchAppeals.TryGetValue(rollSource, out var a) ? a : new();

    // Period always OPEN for admin — no date validation
    public string GetPeriodStatus(string rollSource) => "active";
}