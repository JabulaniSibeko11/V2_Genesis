namespace V2_Genesis.Services.Interfaces
{
    public interface IOmissionService
    {
        Task<List<string>> GetTownsAsync(string rollSource);
        Task<List<string>> GetSchemesAsync(string rollSource);
    }
}
