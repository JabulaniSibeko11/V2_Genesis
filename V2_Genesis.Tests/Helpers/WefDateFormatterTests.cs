using V2_Genesis.Helpers;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// <summary>
/// The "With Effective Date" on the Section 51 notice is written in full.
/// Roll dates are day-first (South Africa): 10/01/2025 is 10 January.
/// </summary>
public class WefDateFormatterTests
{
    [Theory]
    [InlineData("10/01/2025", "10 January 2025")]
    [InlineData("10/01/2025 00:00:00", "10 January 2025")]
    [InlineData("2025-01-10", "10 January 2025")]
    [InlineData("1/2/2024", "01 February 2024")]
    public void Writes_the_date_in_full_day_first(string input, string expected)
        => Assert.Equal(expected, WefDateFormatter.Format(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NULL")]
    public void Empty_or_null_roll_value_stays_empty(string? input)
        => Assert.Equal(string.Empty, WefDateFormatter.Format(input));

    [Fact]
    public void Text_that_is_not_a_date_is_kept_as_it_is()
        => Assert.Equal("not a date", WefDateFormatter.Format("not a date"));

    [Fact]
    public void DateTime_overload_writes_the_date_in_full()
        => Assert.Equal("07 October 2026", WefDateFormatter.Format(new DateTime(2026, 10, 7)));

    [Fact]
    public void Null_DateTime_is_empty()
        => Assert.Equal(string.Empty, WefDateFormatter.Format((DateTime?)null));
}
