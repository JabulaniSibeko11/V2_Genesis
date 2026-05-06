namespace V2_Genesis.Services.Interfaces
{
    public interface IUserManagementService
    {
        /// <summary>
        /// Calls [dbo].[Login] SP in UserManagement DB.
        /// Username is formatted as "{SapDomain}\{sapNumber}" from config.
        /// Returns null if not found / not authorised.
        /// </summary>
        Task<UserManagementResult?> ValidateAdminAsync(string sapNumber);
    }
}
