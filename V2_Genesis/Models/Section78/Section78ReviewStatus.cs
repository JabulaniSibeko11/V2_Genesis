namespace V2_Genesis.Models.Section78
{
    public class Section78ReviewStatus
    {
        // Query  = property is NOT on the Section 78 weekly extract (default)
        // Open   = property IS on the extract and the review period is open
        // Closed = property IS on the extract but the review period has closed
        public const string Query = "Query";
        public const string Open = "Open";
        public const string Closed = "Closed";

        public static bool IsQuery(string? status)
        {
            return !IsOpen(status) && !IsClosed(status);
        }

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