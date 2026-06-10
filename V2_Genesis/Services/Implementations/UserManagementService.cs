// ═══════════════════════════════════════════════════════════════
//  Services/Implementations/UserManagementService.cs — REPLACE
// ═══════════════════════════════════════════════════════════════
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly string _connString;
        private readonly AppSettings _app;

        public UserManagementService(
            IConfiguration config,
            IOptions<AppSettings> appOpts)
        {
            _connString = config.GetConnectionString("UserManagementConnection")!;
            _app = appOpts.Value;
        }

        /// <summary>
        /// SAP number entry flow (existing SapStep form).
        /// Builds "JOBURG\{sapNumber}" then calls the Login SP.
        /// </summary>
        public async Task<UserManagementResult?> ValidateAdminAsync(string sapNumber)
        {
            var username = $@"{_app.SapDomain}\{sapNumber.Trim()}";
            return await CallLoginSpAsync(username);
        }

        /// <summary>
        /// Windows Authentication flow.
        /// Receives the full Windows identity name ("JOBURG\30092655") and
        /// passes it directly to the Login SP — no reconstruction needed.
        /// </summary>
        public async Task<UserManagementResult?> ValidateByWindowsIdentityAsync(
            string windowsIdentityName)
        {
            if (string.IsNullOrWhiteSpace(windowsIdentityName))
                return null;

            // Windows identity already in "DOMAIN\username" format
            return await CallLoginSpAsync(windowsIdentityName.Trim());
        }

        // ── Shared SP call ────────────────────────────────────────
        private async Task<UserManagementResult?> CallLoginSpAsync(string username)
        {
            await using var conn = new SqlConnection(_connString);

            var result = await conn.QueryFirstOrDefaultAsync<UserManagementResult>(
                "dbo.Login",
                new { Username = username, System = _app.UserManagementSystemId },
                commandType: CommandType.StoredProcedure);

            return result;
        }
    }
}