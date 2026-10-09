using V2_Genesis.Helpers;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// Headings of the acknowledgement PDF (objection and appeal).
public class AcknowledgementTextTests
{
    private const string Roll = "Supplementary Valuation Roll 3";

    [Fact]
    public void Appeal_title_starts_with_the_roll() =>
        Assert.Equal("SUPPLEMENTARY VALUATION ROLL 3 APPEAL ACKNOWLEDGEMENT",
            AcknowledgementText.Title(isAppeal: true, isMulti: false, Roll));

    [Fact]
    public void Multipurpose_appeal_title() =>
        Assert.Equal("SUPPLEMENTARY VALUATION ROLL 3 MULTIPURPOSE APPEAL ACKNOWLEDGEMENT",
            AcknowledgementText.Title(true, true, Roll));

    [Theory]
    [InlineData(false, "OBJECTION ACKNOWLEDGEMENT")]
    [InlineData(true, "MULTIPURPOSE OBJECTION ACKNOWLEDGEMENT")]
    public void Objection_title_is_unchanged(bool isMulti, string expected) =>
        Assert.Equal(expected, AcknowledgementText.Title(false, isMulti, Roll));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Appeal_details_are_the_municipal_valuer_decision(bool isLis) =>
        Assert.Equal("PROPERTY DETAILS AS LISTED IN MUNICIPAL VALUER DECISION",
            AcknowledgementText.ListedTitle(true, isLis, Roll));

    [Fact]
    public void Objection_details_are_the_roll() =>
        Assert.Equal("PROPERTY DETAILS AS LISTED IN SUPPLEMENTARY VALUATION ROLL 3",
            AcknowledgementText.ListedTitle(false, false, Roll));

    [Fact]
    public void Lis_objection_details_are_the_lis() =>
        Assert.Equal("PROPERTY DETAILS AS LISTED IN LIS",
            AcknowledgementText.ListedTitle(false, true, Roll));

    [Fact]
    public void Appeal_period_uses_the_objection_mvd_dates()
    {
        var text = AcknowledgementText.AppealPeriod(Roll, new DateTime(2026, 4, 16), new DateTime(2026, 6, 1));

        Assert.Equal("SUPPLEMENTARY VALUATION ROLL 3\nAPPEAL PERIOD: 16 April 2026 AT 08:00 - 01 June 2026 AT 15:00", text);
    }

    [Fact]
    public void Appeal_period_without_dates_shows_only_the_roll() =>
        Assert.Equal("SUPPLEMENTARY VALUATION ROLL 3", AcknowledgementText.AppealPeriod(Roll, null, null));

    [Fact]
    public void Appeal_period_without_start_shows_the_close_date() =>
        Assert.Equal("SUPPLEMENTARY VALUATION ROLL 3\nAPPEAL PERIOD: 01 June 2026 AT 15:00",
            AcknowledgementText.AppealPeriod(Roll, null, new DateTime(2026, 6, 1)));
}
