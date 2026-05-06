namespace V2_Genesis.Services
{
    public class UserManagementResult
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public bool Active { get; set; }
    }

}
