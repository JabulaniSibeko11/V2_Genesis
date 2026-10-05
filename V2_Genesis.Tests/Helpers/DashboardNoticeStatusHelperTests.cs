using V2_Genesis.Helpers;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// <summary>
/// Which notice a client may download from the dashboard, by status.
/// </summary>
public class DashboardNoticeStatusHelperTests
{
    [Theory]
    [InlineData("Obj-Section51", true)]
    [InlineData("  obj-section51 ", true)]      // spaces and case do not matter
    [InlineData("Obj-Unallocated", false)]
    [InlineData("Notice-Sent", false)]
    [InlineData(null, false)]
    public void Section51_notice_only_for_Obj_Section51(string? status, bool expected)
        => Assert.Equal(expected, DashboardNoticeStatusHelper.CanDownloadSection51(status));

    [Theory]
    [InlineData("Notice-Sent", true)]
    [InlineData("Appeal-Closed", true)]
    [InlineData("Obj-InProgress", false)]
    [InlineData("", false)]
    public void Section53_notice_after_the_decision_was_sent(string? status, bool expected)
        => Assert.Equal(expected, DashboardNoticeStatusHelper.CanDownloadSection53(status));

    [Theory]
    [InlineData("App-Finalized", true)]
    [InlineData("App-Finalised", true)]       // both spellings
    [InlineData("App-Unallocated", false)]
    public void Appeal_outcome_only_when_finalised(string? status, bool expected)
        => Assert.Equal(expected, DashboardNoticeStatusHelper.CanDownloadAppealOutcome(status));

    [Theory]
    [InlineData("Notice-Sent-Invalid-Objection", true)]
    [InlineData("Notice-Sent-Invalid-Omission", true)]
    [InlineData("Notice-Sent", false)]
    public void Invalid_notice_only_for_invalid_statuses(string? status, bool expected)
        => Assert.Equal(expected, DashboardNoticeStatusHelper.CanDownloadInvalid(status));

    [Theory]
    [InlineData("Query-Finalized", true)]
    [InlineData("Review-Finalized", true)]
    [InlineData("Notice-Sent", true)]
    [InlineData("Query-Pending", false)]
    public void Section78_outcome_only_when_finalised(string? status, bool expected)
        => Assert.Equal(expected, DashboardNoticeStatusHelper.CanDownloadSection78Outcome(status));
}
