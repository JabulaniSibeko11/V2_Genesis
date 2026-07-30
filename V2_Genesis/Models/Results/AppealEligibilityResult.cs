namespace V2_Genesis.Models.Results;

public sealed class AppealEligibilityResult
{
    public bool ObjectionExists { get; init; }
    public bool HasNoticeSentStatus { get; init; }
    public bool AppealPeriodExists { get; init; }
    public bool IsAppealPeriodOpen { get; init; }
    public bool ExistingAppealFound { get; init; }
    public bool UsesRevisedMvdDates { get; init; }

    public string ObjectionNumber { get; init; } = string.Empty;
    public string ObjectionStatus { get; init; } = string.Empty;
    public string PropertyDescription { get; init; } = string.Empty;

    public DateTime? AppealStartDate { get; init; }
    public DateTime? AppealCloseDate { get; init; }

    public string ExistingAppealNumber { get; init; } = string.Empty;
    public string ExistingAppealStatus { get; init; } = string.Empty;

    public bool CanLodge =>
        ObjectionExists
        && HasNoticeSentStatus
        && AppealPeriodExists
        && IsAppealPeriodOpen
        && !ExistingAppealFound;

    public string Message
    {
        get
        {
            if (!ObjectionExists)
            {
                return "The objection could not be found. An appeal cannot be lodged.";
            }

            if (!HasNoticeSentStatus)
            {
                return "An appeal cannot be lodged yet because the Municipal Valuer's Decision notice has not been issued.";
            }

            if (ExistingAppealFound)
            {
                var reference = string.IsNullOrWhiteSpace(ExistingAppealNumber)
                    ? string.Empty
                    : $" Existing appeal reference: {ExistingAppealNumber}.";

                return "An appeal has already been lodged for this objection or property."
                       + reference;
            }

            if (!AppealPeriodExists)
            {
                return "The appeal period could not be confirmed for this objection. Please visit the City of Johannesburg Valuation Services office for assistance.";
            }

            var today = DateTime.Today;

            if (AppealStartDate.HasValue
                && today < AppealStartDate.Value.Date)
            {
                return $"The appeal period opens on {AppealStartDate.Value:dd MMMM yyyy}. Please return on or after this date.";
            }

            if (AppealCloseDate.HasValue
                && today > AppealCloseDate.Value.Date)
            {
                return $"The online appeal period closed on {AppealCloseDate.Value:dd MMMM yyyy}. Please visit the City of Johannesburg Valuation Services office for assistance.";
            }

            return "The appeal cannot be lodged online. Please visit the City of Johannesburg Valuation Services office for assistance.";
        }
    }
}
