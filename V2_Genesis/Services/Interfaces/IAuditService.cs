namespace V2_Genesis.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(
            string adminEmail,
            string action,
            string? sapNumber = null,
            string? rollSource = null,
            string? searchValue = null,
            string? entityRef = null,
            string? details = null,
            string? ipAddress = null);
    }
}
