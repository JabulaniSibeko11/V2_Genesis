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

    /// <summary>
    /// Status shown to the CLIENT.
    ///
    /// Status life-cycle (the SQL Agent job "Genesis status job" saves the
    /// change when the time is up — see Database/10_Genesis_Status_Job.sql):
    ///   Objection   Obj-Lodging   ──48 h──►  Obj-Pending
    ///   Third-Party Obj-Section51 ──30 days (owner's Section 51 period)──► Obj-Pending
    ///   Appeal      App-Lodging   ──48 h──►  App-Pending
    ///   Section 78  Que-Lodging   ──48 h──►  Query-Pending / Review-Pending
    ///
    /// The third party (objector) sees "Obj-Lodging" during his own 48 hours
    /// and "Obj-Pending" afterwards — "Obj-Section51" is the owner's period
    /// and is not a status the objector needs to see.
    /// </summary>
    public static string GetStatusDisplayText(string? status) =>
        GetStatusDisplayText(status, evidenceWindowOpen: null);

    /// <param name="evidenceWindowOpen">
    /// The row's 48-hour window (Evidence_Window_Open). When known, the screen
    /// follows the clock even if the SQL job has not run yet.
    /// </param>
    public static string GetStatusDisplayText(string? status, bool? evidenceWindowOpen)
    {
        var s = (status ?? "").Trim();

        // Still inside / already past the 48 hours, before the SQL job ran.
        if (s.Equals("Obj-Section51", StringComparison.OrdinalIgnoreCase))
            return evidenceWindowOpen == true ? "Obj-Lodging" : "Obj-Pending";

        if (evidenceWindowOpen == false)
        {
            if (s.Equals("Obj-Lodging", StringComparison.OrdinalIgnoreCase)) return "Obj-Pending";
            if (s.Equals("App-Lodging", StringComparison.OrdinalIgnoreCase)) return "App-Pending";
        }

        return s.ToLowerInvariant() switch
        {
            "obj-lodging" => "Obj-Lodging",
            "obj-pending" or "obj-unallocated" => "Obj-Pending",
            "obj-inprogress" or "obj-pending-approval" or "obj-rejected" => "Obj-InProgress",
            "obj-pentest" => "Invalid",

            "app-lodging" => "App-Lodging",
            "app-pending" or "app-unallocated" => "App-Pending",
            "app-finalized" or "app-finalised" => "App-Finalized",

            "que-lodging" or "query-lodging" => "Query-Lodging",
            "review-lodging" => "Review-Lodging",
            "query-pending" or "query-unallocated" => "Query-Pending",
            "review-pending" or "review-unallocated" => "Review-Pending",
            "query-inprogress" => "Query-InProgress",
            "query-finalized" or "review-finalized" or "notice-sent" => "Finalised",
            "notice-sent-dear-johnny" => "Outcome Available",
            "notice-sent-invalid-objection" => "Objection Not Valid",
            "notice-sent-invalid-omission" => "Omission Objection Not Valid",
            "query-withdrawn" or "review-withdrawn" => "Withdrawn",

            "" => "Pending",
            _ => s
        };
    }

    public static string GetStatusPill(string? status) =>
        GetStatusPill(status, evidenceWindowOpen: null);

    public static string GetStatusPill(string? status, bool? evidenceWindowOpen)
    {
        var displayText = GetStatusDisplayText(status, evidenceWindowOpen);

        // Explain what the client SEES (e.g. Obj-Section51 shown as Obj-Pending).
        var explainAs = displayText.Contains('-') ? displayText : status;
        var title = StatusExplanationHelper.GetTitle(explainAs);
        var description = StatusExplanationHelper.GetDescription(explainAs);
        var badgeClass = StatusExplanationHelper.GetBadgeClass(explainAs);

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
