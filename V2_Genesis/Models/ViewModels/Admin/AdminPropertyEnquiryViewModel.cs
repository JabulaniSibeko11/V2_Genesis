using V2_Genesis.Models.Notice;
using V2_Genesis.Models.Results;

namespace V2_Genesis.Models.ViewModels.Admin;

public sealed class AdminPropertyEnquiryViewModel
{
    public AdminClientAccountViewModel Client { get; set; } = new();
    public AdminClientPropertyViewModel Property { get; set; } = new();

    public List<AdminClientSubmissionViewModel> Submissions { get; set; } = new();
    public List<AdminAcknowledgementSupportItem> Acknowledgements { get; set; } = new();
    public List<AdminNoticeSupportItem> Notices { get; set; } = new();
    public List<AdminAppealWindowSupportItem> AppealWindows { get; set; } = new();

    public int AvailableAcknowledgementCount =>
        Acknowledgements.Count(x => x.CanGenerate);

    public int AvailableNoticeCount =>
        Notices.Count(x => x.FileExists);

    public int EmailCopyCount =>
        Notices.Count(x => x.IsEmailCopy && x.FileExists);
}

public sealed class AdminAcknowledgementSupportItem
{
    public string SubmissionType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string RollSource { get; set; } = string.Empty;
    public bool CanGenerate { get; set; }
    public string UnavailableReason { get; set; } = string.Empty;
}

public sealed class AdminNoticeSupportItem
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string RollName { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public DateTime? IssuedDate { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public bool FileExists { get; set; }
    public bool IsEmailCopy { get; set; }

    public string EmailFrom { get; set; } = string.Empty;
    public string EmailTo { get; set; } = string.Empty;
    public string EmailCc { get; set; } = string.Empty;
    public string EmailSubject { get; set; } = string.Empty;
    public DateTime? EmailSentAt { get; set; }

    public DateTime? AppealOpenDate { get; set; }
    public DateTime? AppealCloseDate { get; set; }
}

public sealed class AdminAppealWindowSupportItem
{
    public string ObjectionNumber { get; set; } = string.Empty;
    public string RollSource { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public bool IsOpen { get; set; }
    public DateTime? OpenDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public int? DaysRemaining { get; set; }
}
