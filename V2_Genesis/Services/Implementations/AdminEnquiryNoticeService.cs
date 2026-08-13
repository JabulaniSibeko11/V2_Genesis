using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

/// <summary>
/// Applies the same notice-by-status rules used by the dashboards to the
/// property-centred Admin enquiry workspace.
/// </summary>
public sealed class AdminEnquiryNoticeService : IAdminEnquiryNoticeService
{
    public AdminEnquiryNotices Build(AdminEnquiryFoundation foundation)
    {
        ArgumentNullException.ThrowIfNull(foundation);

        var items = new List<AdminEnquiryNoticeItem>();

        AddSection49Notices(items, foundation.RollInformation.Properties);

        foreach (var item in foundation.CaseHistory.Cases)
        {
            AddAcknowledgement(items, item);

            switch (item.CaseType)
            {
                case "Objection":
                    AddObjectionNotices(items, item);
                    break;
                case "Appeal":
                    AddAppealNotice(items, item);
                    break;
                case "Query":
                case "Review":
                    AddSection78Notice(items, item);
                    break;
            }
        }

        return new AdminEnquiryNotices
        {
            Items = items
                .GroupBy(
                    x => $"{x.NoticeName}|{x.ReferenceNumber}|{x.RollSource}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => GroupOrder(x.Group))
                .ThenBy(x => x.RollSource)
                .ThenByDescending(x => x.IsAvailable)
                .ThenBy(x => x.ReferenceNumber)
                .ToList()
        };
    }

    private static void AddSection49Notices(
        ICollection<AdminEnquiryNoticeItem> notices,
        IEnumerable<AdminRollPropertyItem> properties)
    {
        foreach (var property in properties.Where(x =>
                     !x.IsLis
                     && !x.PropertyFrom.Equals("LIS", StringComparison.OrdinalIgnoreCase)
                     && !x.PropertyFrom.Equals("Omission", StringComparison.OrdinalIgnoreCase)))
        {
            notices.Add(new AdminEnquiryNoticeItem
            {
                Group = "Property notices",
                NoticeName = "Section 49 Notice",
                ReferenceNumber = property.PropertyDescription,
                CaseType = "Property",
                RollSource = property.RollSource,
                Status = "Available",
                Url = $"/notice/section49/download?rollSource=" +
                      $"{Uri.EscapeDataString(property.RollSource)}&unitKey=" +
                      $"{Uri.EscapeDataString(property.UnitKey)}&valuationKey=" +
                      Uri.EscapeDataString(property.ValuationKey),
                Icon = "fa-envelope-open-text",
                IsAvailable = true
            });
        }
    }

    private static void AddAcknowledgement(
        ICollection<AdminEnquiryNoticeItem> notices,
        AdminCaseHistoryItem item)
    {
        var isAttribute = item.CaseType == "Attributes";
        notices.Add(new AdminEnquiryNoticeItem
        {
            Group = "Acknowledgements",
            NoticeName = $"{item.CaseType} Acknowledgement",
            ReferenceNumber = item.ReferenceNumber,
            CaseType = item.CaseType,
            RollSource = item.RollSource,
            Status = item.Status,
            Url = isAttribute
                ? $"/attributes/acknowledgement/download?attrNo=" +
                  $"{Uri.EscapeDataString(item.ReferenceNumber)}&returnUrl=%2Fadmin%2Fsearch"
                : $"/notice/acknowledgement/download?objectionNo=" +
                  $"{Uri.EscapeDataString(item.ReferenceNumber)}&rollSource=" +
                  $"{Uri.EscapeDataString(item.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
            Icon = "fa-file-circle-check",
            IsAvailable = true
        });
    }

    private static void AddObjectionNotices(
        ICollection<AdminEnquiryNoticeItem> notices,
        AdminCaseHistoryItem item)
    {
        var thirdParty = item.ObjectorType.Equals(
            "Third-Party",
            StringComparison.OrdinalIgnoreCase)
            || item.ObjectorType.Equals(
                "Third Party",
                StringComparison.OrdinalIgnoreCase);

        if (thirdParty)
        {
            notices.Add(new AdminEnquiryNoticeItem
            {
                Group = "Objection notices",
                NoticeName = "Section 51 Notice",
                ReferenceNumber = item.ReferenceNumber,
                CaseType = item.CaseType,
                RollSource = item.RollSource,
                Status = item.Status,
                Url = AvailableNoticeUrl(item, "Section51"),
                Icon = "fa-users",
                IsAvailable = true
            });
        }

        var section53 = item.Status.Equals(
            "Notice-Sent",
            StringComparison.OrdinalIgnoreCase);
        if (section53)
        {
            notices.Add(new AdminEnquiryNoticeItem
            {
                Group = "Section 53 notices",
                NoticeName = "Section 53 – Valuer Decision",
                ReferenceNumber = item.ReferenceNumber,
                CaseType = item.CaseType,
                RollSource = item.RollSource,
                Status = item.Status,
                Url = $"/notice/section53/download?objectionNo=" +
                      $"{Uri.EscapeDataString(item.ReferenceNumber)}&rollSource=" +
                      $"{Uri.EscapeDataString(item.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
                Icon = "fa-file-signature",
                IsAvailable = true
            });
        }

        var dearJohnny = item.Status.Equals(
            "Notice-Sent-Dear-Johnny",
            StringComparison.OrdinalIgnoreCase);
        if (dearJohnny)
        {
            notices.Add(new AdminEnquiryNoticeItem
            {
                Group = "Section 53 notices",
                NoticeName = "Section 53 – Dear Johnny Notice",
                ReferenceNumber = item.ReferenceNumber,
                CaseType = item.CaseType,
                RollSource = item.RollSource,
                Status = item.Status,
                Url = $"/notice/objection-outcome/download?objectionNo=" +
                      $"{Uri.EscapeDataString(item.ReferenceNumber)}&rollSource=" +
                      $"{Uri.EscapeDataString(item.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
                Icon = "fa-file-circle-exclamation",
                IsAvailable = true
            });
        }

        var invalidObjection = item.Status.Equals(
            "Notice-Sent-Invalid-Objection",
            StringComparison.OrdinalIgnoreCase);
        var invalidOmission = item.Status.Equals(
            "Notice-Sent-Invalid-Omission",
            StringComparison.OrdinalIgnoreCase);
        var invalid = invalidObjection || invalidOmission;

        if (invalid)
        {
            notices.Add(new AdminEnquiryNoticeItem
            {
                Group = "Invalid notices",
                NoticeName = invalidOmission
                    ? "Invalid Omission Notice"
                    : "Invalid Objection Notice",
                ReferenceNumber = item.ReferenceNumber,
                CaseType = item.CaseType,
                RollSource = item.RollSource,
                Status = item.Status,
                Url = $"/notice/invalid-outcome/download?objectionNo=" +
                      $"{Uri.EscapeDataString(item.ReferenceNumber)}&rollSource=" +
                      $"{Uri.EscapeDataString(item.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
                Icon = "fa-triangle-exclamation",
                IsAvailable = true
            });
        }
    }

    private static void AddAppealNotice(
        ICollection<AdminEnquiryNoticeItem> notices,
        AdminCaseHistoryItem item)
    {
        var finalised = Is(item.Status, "App-Finalized", "App-Finalised");
        if (finalised)
        {
            notices.Add(new AdminEnquiryNoticeItem
            {
                Group = "Appeal notices",
                NoticeName = "Appeal Decision / Section 52 Review",
                ReferenceNumber = item.ReferenceNumber,
                CaseType = item.CaseType,
                RollSource = item.RollSource,
                Status = item.Status,
                Url = $"/notice/appeal-outcome/download?referenceNumber=" +
                      $"{Uri.EscapeDataString(item.ReferenceNumber)}&rollSource=" +
                      $"{Uri.EscapeDataString(item.RollSource)}&returnUrl=%2Fadmin%2Fsearch",
                Icon = "fa-scale-balanced",
                IsAvailable = true
            });
        }
    }

    private static void AddSection78Notice(
        ICollection<AdminEnquiryNoticeItem> notices,
        AdminCaseHistoryItem item)
    {
        var available = Is(
            item.Status,
            "Query-Finalized",
            "Review-Finalized",
            "Notice-Sent");

        if (available)
        {
            notices.Add(new AdminEnquiryNoticeItem
            {
                Group = "Section 78 notices",
                NoticeName = $"Section 78 {item.CaseType} Outcome",
                ReferenceNumber = item.ReferenceNumber,
                CaseType = item.CaseType,
                RollSource = item.RollSource,
                Status = item.Status,
                Url = AvailableNoticeUrl(item, "Section78Outcome"),
                Icon = "fa-file-magnifying-glass",
                IsAvailable = true
            });
        }
    }

    private static string AvailableNoticeUrl(
        AdminCaseHistoryItem item,
        string type) =>
        $"/notices/download-available?referenceNo=" +
        $"{Uri.EscapeDataString(item.ReferenceNumber)}&type={type}&rollSource=" +
        $"{Uri.EscapeDataString(item.RollSource)}&returnUrl=%2Fadmin%2Fsearch" +
        $"&ownerUserId={Uri.EscapeDataString(item.UserId)}";

    private static bool Is(string value, params string[] expected) =>
        expected.Any(x => value.Equals(x, StringComparison.OrdinalIgnoreCase));

    private static int GroupOrder(string group) => group switch
    {
        "Property notices" => 0,
        "Acknowledgements" => 1,
        "Objection notices" => 2,
        "Section 53 notices" => 3,
        "Invalid notices" => 4,
        "Appeal notices" => 5,
        "Section 78 notices" => 6,
        _ => 99
    };
}
