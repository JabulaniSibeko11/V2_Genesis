using V2_Genesis.Helpers;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// <summary>
/// Property keys come from float columns and must never show as
/// scientific notation (5.48365e+008) on screen or in a search.
/// </summary>
public class FloatKeyHelperTests
{
    [Theory]
    [InlineData("5.48365e 008", "548365000")]
    [InlineData("5.48365e+008", "548365000")]
    [InlineData("548365467", "548365467")]
    [InlineData("10573515.0", "10573515")]
    public void Key_is_a_plain_whole_number(string input, string expected)
        => Assert.Equal(expected, FloatKeyHelper.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Empty_key_stays_empty(string? input)
        => Assert.Equal(string.Empty, FloatKeyHelper.Normalize(input));
}
