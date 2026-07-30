using System.Globalization;
using System.Text;
using V2_Genesis.Models.Notice;
using V2_Genesis.Models.ViewModels.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class AdminPropertyEnquiryService : IAdminPropertyEnquiryService
{
    private readonly IAdminClientAccountService _clientAccountService;
    private readonly INoticeService _noticeService;
    private readonly IObjectionService _objectionService;
    private readonly ILogger<AdminPropertyEnquiryService> _logger;

    public AdminPropertyEnquiryService(
        IAdminClientAccountService clientAccountService,
        INoticeService noticeService,
        IObjectionService objectionService,
        ILogger<AdminPropertyEnquiryService> logger)
    {
        _clientAccountService = clientAccountService;
        _noticeService = noticeService;
        _objectionService = objectionService;
        _logger = logger;
    }

    public async Task<AdminPropertyEnquiryViewModel?> GetAsync(
        string userId,
        string propertyKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(propertyKey))
        {
            return null;
        }

        var account =
            await _clientAccountService.GetClientAccountAsync(
                userId.Trim(),
                cancellationToken);

        if (account is null)
            return null;

        var property = account.Properties.FirstOrDefault(x =>
            x.PropertyKey.Equals(
                propertyKey.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (property is null)
            return null;

        var model = new AdminPropertyEnquiryViewModel
        {
            Client = account,
            Property = property,
            Submissions = property.Submissions
                .OrderBy(x => x.SubmissionType)
                .ThenBy(x => x.ReferenceNumber)
                .ToList()
        };

        BuildAcknowledgements(model);

        var noticeDashboard =
            await _noticeService.GetNoticesDashboardAsync(
                account.UserId,
                account.DisplayName);

        var allNotices =
            noticeDashboard.ObjectionNotices
                .Concat(noticeDashboard.AppealNotices)
                .Concat(noticeDashboard.QueryNotices)
                .ToList();

        var referenceNumbers = property.Submissions
            .Select(x => x.ReferenceNumber)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var normalisedProperty =
            Normalise(property.PropertyDescription);

        foreach (var notice in allNotices)
        {
            var matchesReference =
                referenceNumbers.Contains(notice.ReferenceNo);

            var matchesProperty =
                !string.IsNullOrWhiteSpace(normalisedProperty)
                && Normalise(notice.PropertyDesc) == normalisedProperty;

            if (!matchesReference && !matchesProperty)
                continue;

            var item = BuildNoticeItem(notice);
            model.Notices.Add(item);
        }

        model.Notices = model.Notices
            .GroupBy(
                x => x.FilePath,
                StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderByDescending(x => x.IssuedDate)
            .ThenBy(x => x.TypeLabel)
            .ToList();

        foreach (var objection in property.Submissions.Where(x =>
                     x.SubmissionType.Equals(
                         "Objection",
                         StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var result =
                    await _objectionService.CheckAppealWindowAsync(
                        objection.RollSource,
                        objection.ReferenceNumber,
                        property.UnitKey,
                        property.ValuationKey,
                        property.PropertyDescription);

                int? daysRemaining = null;

                if (result.CloseDate.HasValue)
                {
                    daysRemaining = Math.Max(
                        0,
                        (result.CloseDate.Value.Date
                         - DateTime.Today).Days);
                }

                model.AppealWindows.Add(
                    new AdminAppealWindowSupportItem
                    {
                        ObjectionNumber =
                            objection.ReferenceNumber,

                        RollSource =
                            objection.RollSource,

                        Exists =
                            result.Exists,

                        IsOpen =
                            result.IsOpen,

                        OpenDate =
                            result.StartDate,

                        CloseDate =
                            result.CloseDate,

                        DaysRemaining =
                            daysRemaining
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[AdminPropertyEnquiry] Appeal window lookup failed for {ReferenceNumber}.",
                    objection.ReferenceNumber);
            }
        }

        return model;
    }

    public async Task<bool> NoticeBelongsToClientAsync(
        string userId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        var account =
            await _clientAccountService.GetClientAccountAsync(
                userId.Trim(),
                cancellationToken);

        if (account is null)
            return false;

        var dashboard =
            await _noticeService.GetNoticesDashboardAsync(
                account.UserId,
                account.DisplayName);

        var requested =
            Path.GetFullPath(filePath);

        return dashboard.ObjectionNotices
            .Concat(dashboard.AppealNotices)
            .Concat(dashboard.QueryNotices)
            .Where(x => x.FileExists)
            .Any(x =>
                Path.GetFullPath(x.FilePath).Equals(
                    requested,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static void BuildAcknowledgements(
        AdminPropertyEnquiryViewModel model)
    {
        foreach (var submission in model.Submissions)
        {
            var type =
                submission.SubmissionType.Trim();

            var supported =
                type.Equals(
                    "Objection",
                    StringComparison.OrdinalIgnoreCase)
                ||
                type.Equals(
                    "Appeal",
                    StringComparison.OrdinalIgnoreCase)
                ||
                type.Equals(
                    "Query",
                    StringComparison.OrdinalIgnoreCase)
                ||
                type.Equals(
                    "Review",
                    StringComparison.OrdinalIgnoreCase);

            model.Acknowledgements.Add(
                new AdminAcknowledgementSupportItem
                {
                    SubmissionType = type,
                    ReferenceNumber =
                        submission.ReferenceNumber,

                    RollSource =
                        submission.RollSource,

                    CanGenerate =
                        supported,

                    UnavailableReason =
                        supported
                            ? string.Empty
                            : "Acknowledgement generation is not configured for this submission type."
                });
        }
    }

    private static AdminNoticeSupportItem BuildNoticeItem(
        NoticeItem notice)
    {
        var item = new AdminNoticeSupportItem
        {
            ReferenceNumber =
                notice.ReferenceNo,

            PropertyDescription =
                notice.PropertyDesc,

            RollName =
                notice.RollName,

            TypeLabel =
                notice.TypeLabel,

            IssuedDate =
                notice.IssuedDate,

            FilePath =
                notice.FilePath,

            FileName =
                string.IsNullOrWhiteSpace(notice.FilePath)
                    ? string.Empty
                    : Path.GetFileName(notice.FilePath),

            FileExtension =
                notice.FileExt,

            FileExists =
                notice.FileExists,

            IsEmailCopy =
                notice.FileExt.Equals(
                    ".eml",
                    StringComparison.OrdinalIgnoreCase),

            AppealOpenDate =
                notice.AppealOpenDate,

            AppealCloseDate =
                notice.AppealCloseDate
        };

        if (item.IsEmailCopy && item.FileExists)
        {
            ReadEmailHeaders(item);
        }

        return item;
    }

    private static void ReadEmailHeaders(
        AdminNoticeSupportItem item)
    {
        try
        {
            var headers =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            string? currentName = null;
            var currentValue = new StringBuilder();

            foreach (var line in File.ReadLines(item.FilePath))
            {
                if (line.Length == 0)
                    break;

                if ((line.StartsWith(" ")
                     || line.StartsWith("\t"))
                    && currentName is not null)
                {
                    currentValue.Append(' ');
                    currentValue.Append(line.Trim());
                    continue;
                }

                if (currentName is not null)
                {
                    headers[currentName] =
                        currentValue.ToString().Trim();
                }

                var colon = line.IndexOf(':');

                if (colon <= 0)
                {
                    currentName = null;
                    currentValue.Clear();
                    continue;
                }

                currentName =
                    line[..colon].Trim();

                currentValue.Clear();
                currentValue.Append(
                    line[(colon + 1)..].Trim());
            }

            if (currentName is not null)
            {
                headers[currentName] =
                    currentValue.ToString().Trim();
            }

            item.EmailFrom =
                GetHeader(headers, "From");

            item.EmailTo =
                GetHeader(headers, "To");

            item.EmailCc =
                GetHeader(headers, "Cc");

            item.EmailSubject =
                GetHeader(headers, "Subject");

            var dateText =
                GetHeader(headers, "Date");

            if (DateTimeOffset.TryParse(
                    dateText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var sentAt))
            {
                item.EmailSentAt =
                    sentAt.LocalDateTime;
            }
        }
        catch
        {
            // The email file remains downloadable even where its headers
            // cannot be parsed.
        }
    }

    private static string GetHeader(
        IReadOnlyDictionary<string, string> headers,
        string name)
    {
        return headers.TryGetValue(
            name,
            out var value)
                ? value
                : string.Empty;
    }

    private static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return new string(
            value.Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
    }
}
