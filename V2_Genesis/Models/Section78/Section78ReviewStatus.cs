namespace V2_Genesis.Models.Section78
{
    public class Section78ReviewStatus
    {
        public const string Open = "Open";
        public const string Closed = "Closed";

        public static bool IsOpen(string? status)
        {
            return string.Equals(
                status,
                Open,
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsClosed(string? status)
        {
            return string.Equals(
                status,
                Closed,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}