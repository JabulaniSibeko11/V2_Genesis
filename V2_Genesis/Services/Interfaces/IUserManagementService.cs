namespace V2_Genesis.Services.Interfaces
{
    public interface IUserManagementService
    {
       
            /// <summary>
            /// SAP number entry flow: builds "JOBURG\{sapNumber}" then calls Login SP.
            /// </summary>
            Task<UserManagementResult?> ValidateAdminAsync(string sapNumber);

            /// <summary>
            /// Windows Authentication flow: passes the Windows identity name
            /// (e.g. "JOBURG\30092655") directly to the Login SP — no string building.
            /// </summary>
            Task<UserManagementResult?> ValidateByWindowsIdentityAsync(string windowsIdentityName);
        }
    }

