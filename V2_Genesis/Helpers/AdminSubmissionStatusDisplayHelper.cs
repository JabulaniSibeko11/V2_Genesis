using System.Text.RegularExpressions;

namespace V2_Genesis.Helpers;

public static class AdminSubmissionStatusDisplayHelper
{
    private static readonly Dictionary<string, string> DisplayNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Objections
            ["Obj-Lodging"] = "Lodgement in Progress",
            ["Obj-Unallocated"] = "Pending Allocation",
            ["Obj-Section51"] = "Section 51 Review",
            ["Obj-Inprogress"] = "Assessment in Progress",
            ["Obj-Pending-Approval"] = "Pending Approval",
            ["Obj-Approved"] = "Approved",
            ["Obj-Finalized"] = "Finalised",
            ["Obj-Withdrawn"] = "Withdrawn",
            ["Obj-Rejected"] = "Rejected",

            // Appeals
            ["App-Lodging"] = "Appeal Lodgement in Progress",
            ["App-Unallocated"] = "Pending Allocation",
            ["App-Scheduling"] = "Scheduling in Progress",
            ["App-Scheduled"] = "Hearing Scheduled",
            ["App-Pending-Approval"] = "Pending Approval",
            ["App-Approved"] = "Approved",
            ["App-Finalized"] = "Finalised",
            ["App-Reserved"] = "Decision Reserved",
            ["App-Reversed"] = "Decision Reversed",
            ["App-Withdrawn"] = "Withdrawn",
            ["App-Rejected"] = "Rejected",

            // Queries and Reviews
            ["Query-Lodging"] = "Query Lodgement in Progress",
            ["Query-Unallocated"] = "Pending Allocation",
            ["Query-Inprogress"] = "Query in Progress",
            ["Query-Pending-Approval"] = "Pending Approval",
            ["Query-Finalized"] = "Query Finalised",
            ["Query-Withdrawn"] = "Withdrawn",
            ["Review-Lodging"] = "Review Lodgement in Progress",
            ["Review-Unallocated"] = "Pending Allocation",
            ["Review-Inprogress"] = "Review in Progress",
            ["Review-Finalized"] = "Review Finalised",

            // Attributes
            ["Submitted"] = "Submitted",
            ["EvidenceOpen"] = "Evidence Upload Open",
            ["EvidenceClosed"] = "Evidence Upload Closed",
            ["UnderReview"] = "Under Review",
            ["PendingReview"] = "Pending Review",
            ["ReadyForOvvioExtract"] = "Ready for Extract",
            ["ReadyForOvioExtract"] = "Ready for Extract",
            ["ReadyForExtract"] = "Ready for Extract",
            ["InspectionRequested"] = "Inspection Requested",
            ["InspectionDetailsSent"] = "Inspection Details Sent",
            ["InspectionScheduled"] = "Inspection Scheduled",
            ["InspectionConfirmed"] = "Inspection Confirmed",
            ["InspectionCompleted"] = "Inspection Completed",
            ["InspectionCancelled"] = "Inspection Cancelled",
            ["Completed"] = "Completed",
            ["Approved"] = "Approved",
            ["Rejected"] = "Rejected",
            ["Cancelled"] = "Cancelled",

            // Rebates
            ["Acknowledge"] = "Acknowledged",
            ["Auto Reject"] = "Automatically Rejected",
            ["Under Review"] = "Under Review",
            ["Pending"] = "Pending"
        };

    public static string GetDisplayStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "Not Available";

        var clean = status.Trim();

        if (DisplayNames.TryGetValue(clean, out var display))
            return display;

        // Convert values such as InspectionDetailsSent or Ready_For_Extract
        // into readable words without changing the database value.
        var readable = clean
            .Replace("_", " ")
            .Replace("-", " ");

        readable = Regex.Replace(
            readable,
            @"(?<=[a-z0-9])(?=[A-Z])",
            " ");

        readable = Regex.Replace(
            readable,
            @"\s+",
            " ");

        return readable.Trim();
    }

    public static string GetCssClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "account-status account-status-neutral";

        var value = status.Trim();

        if (ContainsAny(
                value,
                "Final",
                "Approved",
                "Completed",
                "Confirmed",
                "Acknowledged",
                "Acknowledge"))
        {
            return "account-status account-status-complete";
        }

        if (ContainsAny(
                value,
                "Reject",
                "Withdraw",
                "Cancel",
                "Closed",
                "Reversed"))
        {
            return "account-status account-status-danger";
        }

        if (ContainsAny(
                value,
                "Scheduled",
                "Scheduling",
                "Inspection",
                "Progress",
                "Review",
                "Extract",
                "EvidenceOpen",
                "DetailsSent"))
        {
            return "account-status account-status-progress";
        }

        if (ContainsAny(
                value,
                "Lodging",
                "Pending",
                "Unallocated",
                "Submitted",
                "Requested",
                "Reserved"))
        {
            return "account-status account-status-pending";
        }

        return "account-status account-status-neutral";
    }

    private static bool ContainsAny(
        string value,
        params string[] terms)
    {
        return terms.Any(term =>
            value.Contains(
                term,
                StringComparison.OrdinalIgnoreCase));
    }
}
