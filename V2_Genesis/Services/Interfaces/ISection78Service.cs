using V2_Genesis.Models;
using V2_Genesis.Models.Results.Acknowledgement;
using V2_Genesis.Models.Results.Section78;
using V2_Genesis.Models.ViewModels.Section78;

namespace V2_Genesis.Services.Interfaces
{
    public interface ISection78Service
    {
        // ── Property detail for the query form ───────────────────────────
        Task<Section78PropertyDetail?> GetPropertyDetailAsync(
            string unitKey, string? valuationKey);

        Task<List<Section78PropertyDetail>> GetPropertyDetailsAsync(
            string unitKey, string? valuationKey);

        // ── Submit a query/review ────────────────────────────────────────


        // ── Dashboard data ───────────────────────────────────────────────
        Task<List<Section78LinkedResult>> GetLinkedAsync(string userId);
        Task<List<Section78SubmittedResult>> GetSubmittedAsync(string userId);


        Task<Section78SubmitResult> SubmitQueryAsync(
    Que_Property_InfoModel que,
    Obj_Section1Model obj1,
    Obj_Section2Model obj2,
    Obj_Section2QueryModel que1,
    Obj_Section3ResModel objR3,
    Obj_Section3BusModel objB3,
    Obj_Section3AgriModel objA3,
    Obj_Section4BusModel objB4,
    Obj_Section4ResModel objR4,
    Obj_Section5Model obj5,
    Obj_Section6Model obj6,
    Obj_Section7Model obj7,
    Obj_Files obj_file,
    List<IFormFile> files,
    List<IFormFile> fileR,
    string reviewStat,
    string uploadRootPath,
    string propertyType,
    string userId);


        Task<GeneratedAcknowledgementResult> GenerateAcknowledgementFromDatabaseAsync(
            string queryReference,
            string userId,
            bool allowAdministrativeAccess = false,
            CancellationToken cancellationToken = default);
    }


}
