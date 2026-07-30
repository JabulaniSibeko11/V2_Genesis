using V2_Genesis.Models.Objections;
using V2_Genesis.Models.Results;

namespace V2_Genesis.Services.Interfaces
{
    public interface IObjectionService
    {
        /// <summary>
        /// Fetches full property details for the objection flow.
        /// Uses sourceTable to determine which SP + DB to call.
        /// </summary>
        Task<List<CheckPropertyResult>> GetPropertyForObjectionAsync(
            string sourceTable,
            string unitKey,
            string valuationKey);

        /// <summary>
        /// Fetches property details for the appeal flow using IndexAppeal SP.
        /// Uses rollSource to determine which DB to call.
        /// </summary>
        Task<List<CheckPropertyResult>> GetPropertyForAppealAsync(
            string rollSource,
            string objectionNo);

        Task<List<CheckPropertyResult>> GetPropertyForLisAsync(
    string rollSource,
    string? unitKey,
    string? valuationKey);

        Task<DuplicateLodgementResult> CheckDuplicateLodgementAsync(
    string rollSource,
    string sourceTable,
    string? unitKey,
    string? valuationKey,
    string? propertyDesc,
    bool isAppeal);

        Task<LodgementWindowResult> CheckObjectionWindowAsync(
      string rollSource,
      string sourceTable);

        Task<LodgementWindowResult> CheckAppealWindowAsync(
            string rollSource,
            string? objectionNo,
            string? unitKey,
            string? valuationKey,
            string? propertyDesc);

        Task<AppealEligibilityResult> CheckAppealEligibilityAsync(
            string rollSource,
            string objectionNo,
            string? unitKey,
            string? valuationKey,
            string? propertyDesc);
    }
}