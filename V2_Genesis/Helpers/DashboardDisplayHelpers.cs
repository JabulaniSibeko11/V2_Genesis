using System.Globalization;
using System.Net;

namespace V2_Genesis.Helpers;

/// <summary>
/// Display helpers shared between Views/Dashboard/Index.cshtml and
/// Views/Dashboard/_RollDetailPartial.cshtml.
/// </summary>
public static class DashboardDisplayHelpers
{
    public static string Enc(string? value) =>
        WebUtility.HtmlEncode(value ?? "");

    public static string FormatZAR(string? val)
    {
        if (string.IsNullOrWhiteSpace(val))
            return "–";

        var clean = val
            .Replace("R", "", StringComparison.OrdinalIgnoreCase)
            .Replace(",", "")
            .Trim();

        if (!decimal.TryParse(
                clean,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var num) ||
            num < 0)
        {
            return "–";
        }

        return "R " + num.ToString(
            "N0",
            new CultureInfo("en-ZA"));
    }

    public static string GetStatusDisplayText(string? status) => status switch
    {
        "Obj-Unallocated" or "Obj-Section51" => "Obj-Pending",
        "Obj-Inprogress" or "Obj-Pending-Approval" or "Obj-Rejected" => "Obj-InProgress",
        "Obj-Lodging" => "Obj-Lodging",
        "Obj-PenTest" => "Invalid",

        "App-Unallocated" => "App-Unallocated",
        "App-Lodging" => "App-Lodging",
        "App-Finalized" => "App-Finalized",

        "Query-Lodging" => "Query-Lodging",
        "Query-Unallocated" => "Query-Pending",
        "Query-Inprogress" => "Query-InProgress",
        "Query-Finalized" or "Notice-Sent" => "Finalised",
        "Notice-Sent-Dear-Johnny" => "Outcome Available",
        "Notice-Sent-Invalid-Objection" => "Objection Not Valid",
        "Notice-Sent-Invalid-Omission" => "Omission Objection Not Valid",
        "Query-Withdrawn" => "Withdrawn",

        null or "" => "Pending",
        _ => status
    };

    public static string GetStatusPill(string? status)
    {
        var title = StatusExplanationHelper.GetTitle(status);
        var description = StatusExplanationHelper.GetDescription(status);
        var badgeClass = StatusExplanationHelper.GetBadgeClass(status);
        var displayText = GetStatusDisplayText(status);

        return $@"
<span class='client-status-badge cd-pill {Enc(badgeClass)}'
      title='{Enc(description)}'
      data-status-title='{Enc(title)}'
      data-status-message='{Enc(description)}'>
    {Enc(displayText)}
    <i class='fa-solid fa-circle-question status-help-icon'></i>
</span>";
    }

    public static string GetRebateStatusPill(string? status) => status switch
    {
        "Acknowledge" => "<span class='cd-pill cd-pill-lodging'>Acknowledged</span>",
        "Auto Reject" => "<span class='cd-pill cd-pill-rejected'>Auto Rejected</span>",
        "Under Review" => "<span class='cd-pill cd-pill-inprogress'>Under Review</span>",
        "Approved" => "<span class='cd-pill cd-pill-completed'>Approved</span>",
        "Rejected" => "<span class='cd-pill cd-pill-rejected'>Rejected</span>",
        _ => $"<span class='cd-pill cd-pill-pending'>{status ?? "Pending"}</span>"
    };

    public static string GetAttrStatusPill(string? status)
    {
        var displayText = AttributeStatusDisplayHelper.GetDisplayStatus(status);
        var cssClass = AttributeStatusDisplayHelper.GetStatusCssClass(status);

        return $@"
<span class='cd-status-pill {Enc(cssClass)}'>
    {Enc(displayText)}
</span>";
    }

    public static string GetApptStatusPill(string? status)
    {
        var displayText = AttributeStatusDisplayHelper.GetDisplayStatus(status);
        var cssClass = AttributeStatusDisplayHelper.GetStatusCssClass(status);

        return $@"
<span class='cd-status-pill {Enc(cssClass)}'>
    {Enc(displayText)}
</span>";
    }
}
