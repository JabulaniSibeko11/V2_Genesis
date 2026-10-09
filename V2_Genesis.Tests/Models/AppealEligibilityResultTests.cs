using V2_Genesis.Models.Results;
using Xunit;

namespace V2_Genesis.Tests.Models;

/// <summary>
/// Business rules for lodging an appeal:
///   Client  – objection exists, MVD notice sent, appeal period open, no appeal yet.
///   Admin   – same rules as clients: no appeal outside the appeal period
///             (decision 8 Oct 2026 — no late appeals for the admin team).
/// </summary>
public class AppealEligibilityResultTests
{
    private static AppealEligibilityResult Eligible(
        bool objectionExists = true,
        bool noticeSent = true,
        bool periodExists = true,
        bool periodOpen = true,
        bool appealExists = false,
        string existingAppealNo = "") => new()
    {
        ObjectionExists = objectionExists,
        HasNoticeSentStatus = noticeSent,
        AppealPeriodExists = periodExists,
        IsAppealPeriodOpen = periodOpen,
        ExistingAppealFound = appealExists,
        ExistingAppealNumber = existingAppealNo
    };

    [Fact]
    public void Client_can_lodge_when_every_rule_is_met()
    {
        var result = Eligible();

        Assert.True(result.CanLodge);
        Assert.True(result.CanLodgeAsAdmin);
    }

    [Fact]
    public void Client_cannot_lodge_when_the_appeal_period_is_closed()
        => Assert.False(Eligible(periodOpen: false).CanLodge);

    [Fact]
    public void Admin_cannot_lodge_when_the_appeal_period_is_closed()
        => Assert.False(Eligible(periodOpen: false).CanLodgeAsAdmin);

    [Fact]
    public void Admin_cannot_lodge_when_no_appeal_period_is_configured()
        => Assert.False(Eligible(periodExists: false, periodOpen: false).CanLodgeAsAdmin);

    [Theory]
    [InlineData(true, true, true, true, false)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, true, true, true, true)]
    public void Admin_and_client_follow_the_same_rule(bool exists, bool sent, bool periodExists, bool open, bool lodged)
    {
        var r = Eligible(exists, sent, periodExists, open, lodged);
        Assert.Equal(r.CanLodge, r.CanLodgeAsAdmin);
    }

    [Fact]
    public void Closed_period_message_gives_the_close_date()
    {
        var r = new AppealEligibilityResult
        {
            ObjectionExists = true,
            HasNoticeSentStatus = true,
            AppealPeriodExists = true,
            IsAppealPeriodOpen = false,
            AppealStartDate = new DateTime(2026, 4, 16),
            AppealCloseDate = new DateTime(2026, 6, 1)
        };

        Assert.Contains("01 June 2026", r.Message);
    }

    [Theory]
    [InlineData(false, true, false)]   // objection not found
    [InlineData(true, false, false)]   // MVD notice not sent yet
    [InlineData(true, true, true)]     // appeal already lodged
    public void Nobody_can_lodge_when_a_core_rule_fails(bool objectionExists, bool noticeSent, bool appealExists)
    {
        var result = Eligible(objectionExists: objectionExists, noticeSent: noticeSent, appealExists: appealExists);

        Assert.False(result.CanLodge);
        Assert.False(result.CanLodgeAsAdmin);
    }

    [Fact]
    public void Message_says_the_objection_was_not_found()
        => Assert.Contains("could not be found", Eligible(objectionExists: false).Message);

    [Fact]
    public void Message_says_the_mvd_notice_is_not_issued()
        => Assert.Contains("has not been issued", Eligible(noticeSent: false).Message);

    [Fact]
    public void Message_shows_the_existing_appeal_reference()
    {
        var message = Eligible(appealExists: true, existingAppealNo: "APP-GV23-Sup4-7").Message;

        Assert.Contains("already been lodged", message);
        Assert.Contains("APP-GV23-Sup4-7", message);
    }

    [Fact]
    public void Client_message_never_mentions_CLO_or_CLA()
    {
        var messages = new[]
        {
            Eligible(objectionExists: false).Message,
            Eligible(noticeSent: false).Message,
            Eligible(appealExists: true).Message,
            Eligible(periodExists: false).Message,
            Eligible(periodOpen: false).Message
        };

        foreach (var message in messages)
        {
            Assert.DoesNotContain("CLO", message);
            Assert.DoesNotContain("CLA", message);
        }
    }
}
