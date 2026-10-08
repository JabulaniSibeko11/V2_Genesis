namespace V2_Genesis.Services.Attributes;

/// <summary>
/// Rules for the client's Property Attributes dashboard.
/// Unit-tested in V2_Genesis.Tests/Services/AttributeDashboardRulesTests.
/// </summary>
public static class AttributeDashboardRules
{
    // Appointment statuses that no longer hold the submission: the
    // submission shows again under "My Submissions".
    private static readonly HashSet<string> EndedAppointmentStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Expired", "Cancelled", "Canceled", "InspectionCancelled", "Withdrawn", "Rejected"
        };

    /// A submission with a (still active) physical inspection is shown ONLY
    /// under "My Appointments with Valuer" - never in both lists.
    /// Matched on the attribute id, or on the reference (ATTR-GV23-…).
    public static List<AttributeSubmission> WithoutInspectionDuplicates(
        List<AttributeSubmission> submissions,
        List<AttributeAppointment> appointments)
    {
        if (submissions.Count == 0 || appointments.Count == 0)
            return submissions;

        var active = appointments
            .Where(a => !EndedAppointmentStatuses.Contains((a.Status ?? string.Empty).Trim()))
            .ToList();

        var attrIds = active.Select(a => a.AttrId).ToHashSet();
        var refs = active
            .Select(a => (a.AppointmentRef ?? string.Empty).Trim())
            .Where(r => r.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return submissions
            .Where(s => !attrIds.Contains(s.Id) &&
                        !refs.Contains((s.SubmissionRef ?? string.Empty).Trim()))
            .ToList();
    }
}
