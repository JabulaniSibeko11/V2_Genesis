using V2_Genesis.Models.Results;
using Xunit;

namespace V2_Genesis.Tests.Models;

/// <summary>
/// Business rules for lodging an appeal:
///   Client  – objection exists, MVD notice sent, appeal period open, no appeal yet.
///   Admin   – the appeal period is always open (late condonation appeals),
///             but the other rules still apply.
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
    public void Admin_can_lodge_when_the_appeal_period_is_closed()
        => Assert.True(Eligible(periodOpen: false).CanLodgeAsAdmin);

    [Fact]
    public void Admin_can_lodge_when_no_appeal_period_is_configured()
        => Assert.True(Eligible(periodExists: false, periodOpen: false).CanLodgeAsAdmin);

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
