// ═══════════════════════════════════════════════════════════════
//  Services/Interfaces/IAdminDashboardService.cs  — replace file
// ═══════════════════════════════════════════════════════════════
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Models.ViewModels.Dashboard;

namespace V2_Genesis.Services.Interfaces;

public interface IAdminDashboardService
{
    // ── Existing ──────────────────────────────────────────────────────
    Task<AdminRollStats> GetStatsAsync(string rollSource);
    Task<List<AdminObjectionResult>> SearchObjectionsAsync(string rollSource, string searchValue);
    Task<List<AdminAppealResult>> SearchAppealsAsync(string rollSource, string searchValue);

    // ── New: all-users roll data for admin dashboard ─────────────────
    Task<RollData> GetAllRollDataAsync(string rollSource);

    // ── New: unified search ───────────────────────────────────────────
    /// <summary>Search by Objection, Appeal, Query, Review or Attribute reference.</summary>
    Task<AdminSearchResult> SearchByReferenceAsync(
        string refNo,
        string? rollSource,
        CancellationToken cancellationToken = default);

    /// <summary>Search all rolls by property attributes (like home search).</summary>
    Task<AdminSearchResult> SearchByPropertyAsync(
        string? town, string? stand, string? address,
        string? scheme, string? unit,
        string? rollSource,
        CancellationToken cancellationToken = default);

    Task<AdminSearchResult> OpenPropertyAsync(
        string rollSource,
        string propertyFrom,
        string propertyDescription,
        string unitKey,
        string valuationKey,
        CancellationToken cancellationToken = default);
}
