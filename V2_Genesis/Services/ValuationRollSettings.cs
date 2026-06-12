using Microsoft.Extensions.Options;

namespace V2_Genesis.Services;

public class ValuationRollSettings
{
    public string RollName { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public int WarningDaysBeforeClose { get; set; } = 5;
    public string CustomAnnouncementOverride { get; set; } = string.Empty;
}

public class DisclaimerSettings
{
    public string CookieName { get; set; } = "genesis_disclaimer_accepted";
    public int CookieExpiryHours { get; set; } = 8;
    public string Title { get; set; } = string.Empty;
    public string Text1 { get; set; } = string.Empty;
    public string Text2 { get; set; } = string.Empty;
    public string Text3 { get; set; } = string.Empty;
    public string Text4 { get; set; } = string.Empty;
    public string Text5 { get; set; } = string.Empty;
}

public enum RollStatus { BeforeOpen, Open, WarningClose, Closed }

public class AnnouncementResult
{
    public string Message { get; set; } = string.Empty;
    public RollStatus Status { get; set; }
    public int DaysLeft { get; set; }
    public string BadgeText { get; set; } = string.Empty;
    public string BadgeCss { get; set; } = string.Empty;
    public string DaysCss { get; set; } = string.Empty;
}

public interface IAnnouncementService
{
    AnnouncementResult GetAnnouncement();
}

public class AnnouncementService : IAnnouncementService
{
    private readonly ValuationRollSettings _cfg;

    public AnnouncementService(IOptions<ValuationRollSettings> opts)
        => _cfg = opts.Value;

    public AnnouncementResult GetAnnouncement()
    {
        // Admin override takes priority
        if (!string.IsNullOrWhiteSpace(_cfg.CustomAnnouncementOverride))
        {
            return new AnnouncementResult
            {
                Message = _cfg.CustomAnnouncementOverride,
                Status = RollStatus.Open,
                DaysLeft = 0,
                BadgeText = "NOTICE",
                BadgeCss = "badge-info",
                DaysCss = "days-info"
            };
        }

        var now = DateTime.Now;

        // IMPORTANT:
        // Coming soon means the roll has NOT opened yet.
        if (now < _cfg.OpenDate)
        {
            var daysToOpen = (int)Math.Ceiling((_cfg.OpenDate - now).TotalDays);

            return new AnnouncementResult
            {
                Status = RollStatus.BeforeOpen,
                DaysLeft = daysToOpen,
                Message = $"{_cfg.RollName} {_cfg.RollNumber} is coming soon — opens on {_cfg.OpenDate:dd MMMM yyyy} at 08:00.",
                BadgeText = "COMING SOON",
                BadgeCss = "badge-upcoming",
                DaysCss = "days-upcoming"
            };
        }

        // Closed means today is after the closing date.
        if (now > _cfg.CloseDate)
        {
            return new AnnouncementResult
            {
                Status = RollStatus.Closed,
                DaysLeft = 0,
                Message = $"{_cfg.RollName} {_cfg.RollNumber} objection period is now closed.",
                BadgeText = "CLOSED",
                BadgeCss = "badge-closed",
                DaysCss = "days-closed"
            };
        }

        var daysLeft = (int)Math.Ceiling((_cfg.CloseDate - now).TotalDays);

        // Warning before close
        if (daysLeft <= _cfg.WarningDaysBeforeClose)
        {
            return new AnnouncementResult
            {
                Status = RollStatus.WarningClose,
                DaysLeft = daysLeft,
                Message = $"⚠️ {_cfg.RollName} {_cfg.RollNumber} closes in {daysLeft} {(daysLeft == 1 ? "day" : "days")} — {_cfg.CloseDate:dd MMMM yyyy} at 15:00.",
                BadgeText = "CLOSING SOON",
                BadgeCss = "badge-warning",
                DaysCss = "days-warning"
            };
        }

        // Normal open period
        return new AnnouncementResult
        {
            Status = RollStatus.Open,
            DaysLeft = daysLeft,
            Message = $"{_cfg.RollName} {_cfg.RollNumber} objection period is open — closes {_cfg.CloseDate:dd MMMM yyyy} at 15:00. {daysLeft} days remaining.",
            BadgeText = "OPEN NOW",
            BadgeCss = "badge-open",
            DaysCss = "days-open"
        };
    }
}