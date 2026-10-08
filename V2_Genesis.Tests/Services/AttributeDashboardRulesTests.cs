using V2_Genesis.Services.Attributes;
using Xunit;

namespace V2_Genesis.Tests.Services;

/// <summary>
/// Dashboard: a submission with a physical inspection shows only under
/// "My Appointments with Valuer", never also under "My Submissions".
/// </summary>
public class AttributeDashboardRulesTests
{
    private static List<AttributeSubmission> Submissions() => new()
    {
        new AttributeSubmission { Id = 27, SubmissionRef = "ATTR-GV23-27" },
        new AttributeSubmission { Id = 28, SubmissionRef = "ATTR-GV23-28" },
        new AttributeSubmission { Id = 35, SubmissionRef = "ATTR-GV23-35" }
    };

    private static List<string?> Refs(List<AttributeSubmission> list) =>
        list.Select(s => s.SubmissionRef).ToList();

    [Theory]
    [InlineData("PendingClientResponse")]
    [InlineData("Confirmed")]
    [InlineData("InspectionDetailsSent")]
    [InlineData("InspectionCompleted")]
    public void Submission_with_an_inspection_moves_to_appointments(string status)
    {
        var result = AttributeDashboardRules.WithoutInspectionDuplicates(
            Submissions(),
            new() { new AttributeAppointment { AttrId = 35, AppointmentRef = "ATTR-GV23-35", Status = status } });

        Assert.Equal(new List<string?> { "ATTR-GV23-27", "ATTR-GV23-28" }, Refs(result));
    }

    [Theory]
    [InlineData("Expired")]
    [InlineData("Cancelled")]
    [InlineData("withdrawn")]
    public void Ended_inspection_gives_the_submission_back(string status)
    {
        var result = AttributeDashboardRules.WithoutInspectionDuplicates(
            Submissions(),
            new() { new AttributeAppointment { AttrId = 35, AppointmentRef = "ATTR-GV23-35", Status = status } });

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Matched_on_the_reference_when_the_id_differs()
    {
        var result = AttributeDashboardRules.WithoutInspectionDuplicates(
            Submissions(),
            new() { new AttributeAppointment { AttrId = 999, AppointmentRef = " attr-gv23-28 ", Status = "Confirmed" } });

        Assert.DoesNotContain("ATTR-GV23-28", Refs(result));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void No_appointments_keeps_every_submission()
        => Assert.Equal(3, AttributeDashboardRules.WithoutInspectionDuplicates(Submissions(), new()).Count);
}
