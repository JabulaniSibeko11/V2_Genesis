using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class AuditService : IAuditService
{
    private readonly string _connString;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IConfiguration config, ILogger<AuditService> logger)
    {
        _connString = config.GetConnectionString("DefaultConnection")!;
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
            await using var conn = new SqlConnection(_connString);
            await conn.ExecuteAsync(
                @"INSERT INTO [dbo].[AdminAuditLog]
                    (AdminEmail, SapNumber, Action, RollSource,
                     SearchValue, EntityRef, Details, IpAddress)
                  VALUES
                    (@AdminEmail, @SapNumber, @Action, @RollSource,
                     @SearchValue, @EntityRef, @Details, @IpAddress)",
                new
                {
                    AdminEmail = adminEmail,
                    SapNumber = sapNumber,
                    Action = action,
                    RollSource = rollSource,
                    SearchValue = searchValue,
                    EntityRef = entityRef,
                    Details = details,
                    IpAddress = ipAddress
                });
        }
        catch (Exception ex)
        {
            // Audit failure must never break the main flow
            _logger.LogError(ex, "Audit log failed for action '{Action}' by '{Admin}'",
                action, adminEmail);
        }
    }
}