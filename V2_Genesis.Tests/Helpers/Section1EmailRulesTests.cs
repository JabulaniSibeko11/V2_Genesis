using V2_Genesis.Helpers;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// A Representative may not use their own e-mail as the owner's e-mail.
public class Section1EmailRulesTests
{
    [Theory]
    [InlineData("rep@mail.com", "rep@mail.com")]
    [InlineData(" Rep@Mail.com ", "rep@mail.com")]
    public void SameEmail_IsBlocked(string owner, string rep) =>
        Assert.True(Section1EmailRules.OwnerUsesRepEmail(owner, rep));

    [Theory]
    [InlineData("owner@mail.com", "rep@mail.com")]
    [InlineData("owner@mail.com", null)]
    [InlineData("owner@mail.com", "")]
    [InlineData(null, "rep@mail.com")]
    [InlineData(null, null)]
    public void DifferentOrMissing_IsAllowed(string? owner, string? rep) =>
        Assert.False(Section1EmailRules.OwnerUsesRepEmail(owner, rep));
}
