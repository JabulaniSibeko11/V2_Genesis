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

        public async Task<UserManagementResult?> ValidateAdminAsync(string sapNumber)
        {
            // Format: JOBURG\30092655  (domain from appsettings + SAP number entered)
            var username = $@"{_app.SapDomain}\{sapNumber.Trim()}";

            await using var conn = new SqlConnection(_connString);

            var result = await conn.QueryFirstOrDefaultAsync<UserManagementResult>(
                "dbo.Login",
                new { Username = username, System = _app.UserManagementSystemId },
                commandType: CommandType.StoredProcedure);

            return result;
        }
    }
}
