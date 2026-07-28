using V2_Genesis.Models.Section78;

namespace V2_Genesis.Models.Results
{
    public class LinkedPropertyResult
    {
        
            public int Id { get; set; }

            public string? TownNameDesc { get; set; }

            public string? PremiseId { get; set; }

            public int Erf { get; set; }

            public int Ptn { get; set; }

            public string? LisStreetAddress { get; set; }

            public string? Reason { get; set; }

            public string? SchemeName { get; set; }

            public string? SchemeNumber { get; set; }

            public int UnitNo { get; set; }

            public string? SchemeYear { get; set; }

            public string? UnitKey { get; set; }

            public string? MarketValue { get; set; }

            public string? CatDesc { get; set; }

            public string? RateableArea { get; set; }

            public string? WefDate { get; set; }

            public string? Re { get; set; }

            public string? PropertyDesc { get; set; }

            public string? ValuationDate { get; set; }

            public string? ValuationKey { get; set; }

            public string? UnitType { get; set; }

            public string? PropertyFrom { get; set; }

            // Section 78 Review information returned by DashboardLinkedQ.
            public DateTime? Review_Close_Date { get; set; }

            public string? Review_Status { get; set; }

            // Returned by the updated DashboardLinkedQ procedure.
            public bool HasCompletedQuery { get; set; }

            // Expected values: Query, Review or Closed.
            public string? AvailableAction { get; set; }

            public bool IsReviewOpen =>
                Section78ReviewStatus.IsOpen(Review_Status);

            public bool IsReviewClosed =>
                Section78ReviewStatus.IsClosed(Review_Status);

            public bool CanLodgeQuery =>
                string.Equals(
                    AvailableAction,
                    "Query",
                    StringComparison.OrdinalIgnoreCase);

            public bool CanLodgeReview =>
                string.Equals(
                    AvailableAction,
                    "Review",
                    StringComparison.OrdinalIgnoreCase);

            public bool IsActionClosed =>
                string.Equals(
                    AvailableAction,
                    "Closed",
                    StringComparison.OrdinalIgnoreCase);
        }
    }

