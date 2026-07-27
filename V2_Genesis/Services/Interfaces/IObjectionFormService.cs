using Microsoft.AspNetCore.Http;
using V2_Genesis.Models;
using V2_Genesis.Services.Notice;

namespace V2_Genesis.Services.Interfaces;

public interface IObjectionFormService
{
    Task<ObjectionSubmitResult> SubmitAsync(
        string rollSource,
        string userId,
        string appealStat,
        string? objAppeal,
        string? PropertyFrom,
        Obj_Property_InfoModel obj,
        Obj_Section1Model obj1,
        Obj_Section2Model obj2,
        Obj_Section3ResModel objR3,
        Obj_Section3BusModel objB3,
        Obj_Section3AgriModel objA3,
        Obj_Section4BusModel objB4,
        Obj_Section4ResModel objR4,
        Obj_Section5Model obj5,
        Obj_Section6Model obj6,
        Obj_Section7Model obj7,
        Obj_Files objFile,
        List<IFormFile> files,
        List<IFormFile> fileR,
        Obj_Property_Info_AppealModel appeal);

    /// <summary>
    /// Rebuilds an objection or appeal acknowledgement from the submitted
    /// database records. No acknowledgement file is read from disk.
    /// </summary>
    Task<AcknowledgementData?> GetAcknowledgementDataAsync(
        string rollSource,
        string referenceNo);

    Task<(bool Success, string? Error)> WithdrawAsync(
    string objectionNo,
    string withdrawType,
    string rollSource,
    string userId);

    // Unlink a saved property from the user's dashboard.
    Task<(bool Success, string? Error)> UnlinkPropertyAsync(
        long linkedId,
        string userId);
}

public class ObjectionSubmitResult
{
    public bool Success { get; set; }
    public string ObjectionNo { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool IsMulti { get; set; }
    public bool IsAppeal { get; set; }
}