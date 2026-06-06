// ═══════════════════════════════════════════════════════════════
//  Models/Notices/NoticeModels.cs
// ═══════════════════════════════════════════════════════════════
namespace V2_Genesis.Models.Notice;

public enum NoticeType
{
    Section49,
    Section51,
    Section53,
    Section52Review,
    AppealDecision,
    InvalidObjection,
    InvalidOmission,
    DearJohnny,
    Section78Outcome
}

public class NoticeItem
{
    public string ReferenceNo { get; set; } = "";   // Objection_No / Appeal_No / Query_No
    public string PropertyDesc { get; set; } = "";
    public string RollName { get; set; } = "";   // "GV 2023", "Supp 3" etc.
    public NoticeType Type { get; set; }
    public string TypeLabel { get; set; } = "";   // "Section 53 – MVD Decision"
    public DateTime? IssuedDate { get; set; }
    public string FilePath { get; set; } = "";   // Full server path
    public string FileExt { get; set; } = "";   // ".pdf" or ".eml"
    public bool FileExists { get; set; }
    // Appeal schedule (Section 53 only)
    public DateTime? AppealOpenDate { get; set; }
    public DateTime? AppealCloseDate { get; set; }
    public bool AppealExpired => AppealCloseDate.HasValue
                                             && AppealCloseDate.Value.Date < DateTime.Today;
}

public class AppealCalendarEvent
{
    public string ObjectionNo { get; set; } = "";
    public string PropertyDesc { get; set; } = "";
    public string RollName { get; set; } = "";
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public bool IsExpired => CloseDate.Date < DateTime.Today;
    public bool IsUrgent => !IsExpired && CloseDate.Date <= DateTime.Today.AddDays(7);
    public int DaysLeft => IsExpired ? 0
                                     : Math.Max(0, (CloseDate.Date - DateTime.Today).Days);
}

public class NoticesDashboardViewModel
{
    public List<NoticeItem> ObjectionNotices { get; set; } = new();
    public List<NoticeItem> AppealNotices { get; set; } = new();
    public List<NoticeItem> QueryNotices { get; set; } = new();
    public List<AppealCalendarEvent> CalendarEvents { get; set; } = new();
    public string DisplayName { get; set; } = "";
}