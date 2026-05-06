namespace V2_Genesis.Services
{
    public class LinkResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public bool IsDuplicate { get; private set; }

        public static LinkResult Ok()
            => new() { Success = true };

        public static LinkResult Duplicate()
            => new()
            {
                Success = false,
                IsDuplicate = true,
                ErrorMessage = "This property is already linked to your profile."
            };

        public static LinkResult Fail(string message)
            => new() { Success = false, ErrorMessage = message };
    }
}
