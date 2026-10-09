using V2_Genesis.Helpers;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// Number formats on the Section 53 notice (same as eNotice).
public class NoticeNumberFormatTests
{
    [Theory]
    [InlineData("29184000", "R 29 184 000")]
    [InlineData("R 2 650 000", "R 2 650 000")]
    [InlineData("R2,650,000", "R 2 650 000")]
    [InlineData("1600000.00", "R 1 600 000")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("n/a", "n/a")]
    public void Rand(string? input, string expected) =>
        Assert.Equal(expected, NoticeNumberFormat.Rand(input));

    [Theory]
    [InlineData("1174", "1 174")]
    [InlineData("1174.00", "1 174")]
    [InlineData("1174.5", "1 174.50")]
    [InlineData("476", "476")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Extent(string? input, string expected) =>
        Assert.Equal(expected, NoticeNumberFormat.Extent(input));
}
