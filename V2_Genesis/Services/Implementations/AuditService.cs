using V2_Genesis.Data;
using V2_Genesis.Models.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(
        string adminEmail,
        string action,
        string? sapNumber = null,
        string? rollSource = null,
        string? searchValue = null,
        string? entityRef = null,
        string? details = null,
        string? ipAddress = null)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var auditEntry = new AdminAuditLog
            {
                AdminEmail = adminEmail?.Trim() ?? string.Empty,
                SapNumber = sapNumber?.Trim(),
                Action = action?.Trim() ?? string.Empty,
                RollSource = rollSource?.Trim(),
                SearchValue = searchValue?.Trim(),
                EntityRef = entityRef?.Trim(),
                Details = details,
                IpAddress = ipAddress?.Trim(),
                Timestamp = DateTime.Now
            };

            await db.AdminAuditLogs.AddAsync(auditEntry);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Audit failure must never break the main flow
            _logger.LogError(ex, "Audit log failed for action '{Action}' by '{Admin}'",
                action, adminEmail);
        }
    }
}
