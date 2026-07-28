namespace V2_Genesis.Services
{
    public class LinkResult
    {
        public bool Success { get; private set; }

        public string? ErrorMessage { get; private set; }

        public bool IsDuplicate { get; private set; }

        public long? LinkedPropertyId { get; private set; }

        public string? ReviewStatus { get; private set; }

        public DateTime? ReviewCloseDate { get; private set; }

        public static LinkResult Ok(
            long? linkedPropertyId = null,
            string? reviewStatus = null,
            DateTime? reviewCloseDate = null)
        {
            return new LinkResult
            {
                Success = true,
                LinkedPropertyId = linkedPropertyId,
                ReviewStatus = reviewStatus,
                ReviewCloseDate = reviewCloseDate
            };
        }

        public static LinkResult Duplicate()
        {
            return new LinkResult
            {
                Success = false,
                IsDuplicate = true,
                ErrorMessage =
                    "This property is already linked to your profile."
            };
        }

        public static LinkResult Fail(string message)
        {
            return new LinkResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}