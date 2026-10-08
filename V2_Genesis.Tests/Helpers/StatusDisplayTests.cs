using V2_Genesis.Helpers;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// <summary>
/// What the CLIENT sees for each status.
///   Objection   Obj-Lodging   (48 h) → Obj-Pending
///   Third-Party Obj-Section51: objector sees Obj-Lodging for 48 h, then Obj-Pending
///   Appeal      App-Lodging   (48 h) → App-Pending
///   Section 78  Que-Lodging          → Query-Pending / Review-Pending
/// </summary>
public class StatusDisplayTests
{
    [Theory]
    [InlineData(true, "Obj-Lodging")]
    [InlineData(false, "Obj-Pending")]
    [InlineData(null, "Obj-Pending")]
    public void Third_party_sees_lodging_for_48_hours_then_pending(bool? windowOpen, string expected)
        => Assert.Equal(expected, DashboardDisplayHelpers.GetStatusDisplayText("Obj-Section51", windowOpen));

    [Fact]
    public void Lodging_shows_pending_when_48_hours_ended_even_before_the_sql_job_ran()
    {
        Assert.Equal("Obj-Pending", DashboardDisplayHelpers.GetStatusDisplayText("Obj-Lodging", false));
        Assert.Equal("App-Pending", DashboardDisplayHelpers.GetStatusDisplayText("App-Lodging", false));
    }

    [Fact]
    public void Lodging_stays_lodging_inside_the_48_hours()
    {
        Assert.Equal("Obj-Lodging", DashboardDisplayHelpers.GetStatusDisplayText("Obj-Lodging", true));
        Assert.Equal("App-Lodging", DashboardDisplayHelpers.GetStatusDisplayText("App-Lodging", true));
    }

    [Theory]
    [InlineData("Obj-Pending", "Obj-Pending")]
    [InlineData("Obj-Unallocated", "Obj-Pending")]
    [InlineData("App-Pending", "App-Pending")]
    [InlineData("App-Unallocated", "App-Pending")]
    [InlineData("Que-Lodging", "Query-Lodging")]
    [InlineData("Query-Pending", "Query-Pending")]
    [InlineData("Review-Pending", "Review-Pending")]
    [InlineData(" obj-pending ", "Obj-Pending")]          // spaces and case
    [InlineData("", "Pending")]
    [InlineData(null, "Pending")]
    public void Stored_status_is_shown_with_its_client_name(string? status, string expected)
        => Assert.Equal(expected, DashboardDisplayHelpers.GetStatusDisplayText(status));

    [Fact]
    public void Section51_pill_explains_pending_not_the_owner_period()
    {
        var pill = DashboardDisplayHelpers.GetStatusPill("Obj-Section51", false);

        Assert.Contains("Obj-Pending", pill);
        Assert.Contains("Objection Pending", pill);
        Assert.DoesNotContain("Section51", pill);
    }

    [Theory]
    [InlineData("Obj-Pending", "Objection Pending")]
    [InlineData("App-Pending", "Appeal Pending")]
    [InlineData("Query-Pending", "Query Pending")]
    [InlineData("Review-Pending", "Review Pending")]
    public void Pending_statuses_have_an_explanation(string status, string title)
    {
        Assert.Equal(title, StatusExplanationHelper.GetTitle(status));
        Assert.Contains("48 hours", StatusExplanationHelper.GetDescription(status));
    }

    [Theory]
    [InlineData("Obj-Pending")]
    [InlineData("App-Pending")]
    [InlineData("Query-Pending")]
    [InlineData("Review-Pending")]
    public void Admin_sees_pending_allocation(string status)
        => Assert.StartsWith("Pending Allocation", AdminSubmissionStatusDisplayHelper.GetDisplayStatus(status));
}
