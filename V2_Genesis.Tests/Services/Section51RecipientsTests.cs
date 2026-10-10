using V2_Genesis.Services.Section51;
using Xunit;

namespace V2_Genesis.Tests.Services;

/// Who gets the Section 51 notice e-mail (owner, tracking mailbox, test recipient).
public class Section51RecipientsTests
{
    private const string Cc = "valuationenquiries@joburg.org.za";
    private const string Tracking = "GV23Supp4@joburg.org.za";
    private const string Tester = "tester@joburg.org.za";

    [Fact]
    public void OwnerHasEmail_GoesToOwner_CcEnquiries_BccTracking()
    {
        var plan = Section51Recipients.Resolve("owner@mail.com", false, Tester, Cc, Tracking);

        Assert.Equal("owner@mail.com", plan.To);
        Assert.Equal(new[] { Cc }, plan.Cc);
        Assert.Equal(new[] { Tracking }, plan.Bcc);
        Assert.True(plan.OwnerHasEmail);
        Assert.True(plan.ReachesOwner(testMode: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-email")]
    public void NoOwnerEmail_GoesToTrackingMailbox(string? ownerEmail)
    {
        var plan = Section51Recipients.Resolve(ownerEmail, false, Tester, Cc, Tracking);

        Assert.Equal(Tracking, plan.To);
        Assert.Equal(new[] { Cc }, plan.Cc);
        Assert.Empty(plan.Bcc);
        Assert.False(plan.OwnerHasEmail);
        Assert.True(plan.CanSend);
        Assert.False(plan.ReachesOwner(testMode: false)); // still posted, Section51_Emailed = 'N'
    }

    [Fact]
    public void NoOwnerEmail_NoTrackingMailbox_NotSent()
    {
        var plan = Section51Recipients.Resolve(null, false, Tester, Cc, null);

        Assert.False(plan.CanSend);
    }

    [Theory]
    [InlineData("owner@mail.com")]
    [InlineData(null)]
    public void TestMode_GoesToTester_CcEnquiries_BccTracking(string? ownerEmail)
    {
        var plan = Section51Recipients.Resolve(ownerEmail, true, Tester, Cc, Tracking);

        Assert.Equal(Tester, plan.To);
        Assert.Equal(new[] { Cc }, plan.Cc);
        Assert.Equal(new[] { Tracking }, plan.Bcc);
        Assert.False(plan.ReachesOwner(testMode: true));
    }

    [Fact]
    public void TestMode_NoTester_NotSent()
    {
        var plan = Section51Recipients.Resolve("owner@mail.com", true, " ", Cc, Tracking);

        Assert.False(plan.CanSend);
    }

    [Fact]
    public void Cc_NoDuplicates_AndNeverTheToAddress()
    {
        var plan = Section51Recipients.Resolve("gv23supp4@joburg.org.za", false, Tester, Tracking, Tracking);

        Assert.Equal("gv23supp4@joburg.org.za", plan.To);
        Assert.Empty(plan.Cc);
        Assert.Empty(plan.Bcc);
    }

    [Fact]
    public void NoTrackingMailbox_OwnerStillGetsIt()
    {
        var plan = Section51Recipients.Resolve("owner@mail.com", false, Tester, Cc, "");

        Assert.Equal("owner@mail.com", plan.To);
        Assert.Equal(new[] { Cc }, plan.Cc);
        Assert.Empty(plan.Bcc);
    }

    [Fact]
    public void TrackingMailbox_NeverInCc()
    {
        foreach (var owner in new[] { "owner@mail.com", null })
        foreach (var test in new[] { true, false })
            Assert.DoesNotContain(Tracking, Section51Recipients.Resolve(owner, test, Tester, Cc, Tracking).Cc);
    }
}
