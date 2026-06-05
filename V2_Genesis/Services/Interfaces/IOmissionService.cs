namespace V2_Genesis.Services.Interfaces
{
    public interface IOmissionService
    {
        Task<List<string>> GetTownsAsync(string rollSource);
        Task<List<string>> GetSchemesAsync(string rollSource);

        (string PropertyDesc, string SourceTable, string ControllerName)
    BuildOmissionDescription(
        string rollSource,
        string propType,      // "FH" | "ST"
        string? town,
        string? erf,
        string? portion,
        string? re,
        string? right,
        string? scheme,
        string? schemeNumber,
        string? schemeYear,
        string? unit,
        string? stRight);
    }
}
