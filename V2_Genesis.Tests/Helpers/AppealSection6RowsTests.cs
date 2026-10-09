using V2_Genesis.Helpers;
using V2_Genesis.Models.Objections;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// Section 6 of an appeal = the MVD of the objection (main row + MVD splits).
public class AppealSection6RowsTests
{
    private static CheckPropertyResult Mvd() => new()
    {
        CatDesc = "Multiple Purposes",
        LisStreetAddress = "95 POLLY STREET",
        RateableArea = "250",
        MarketValue = "R 12 400 000"
    };

    [Fact]
    public void Single_property_gives_one_row()
    {
        var rows = AppealSection6Rows.Build(Mvd());

        var row = Assert.Single(rows);
        Assert.Equal("Multiple Purposes", row.CatDesc);
        Assert.Equal("95 POLLY STREET", row.LisStreetAddress);
        Assert.Equal("250", row.RateableArea);
        Assert.Equal("R 12 400 000", row.MarketValue);
    }

    [Fact]
    public void Mvd_splits_follow_the_main_row_in_order()
    {
        var mvd = Mvd();
        mvd.Mvd2Category = "Residential";
        mvd.Mvd2Extent = "150";
        mvd.Mvd2MarketValue = "R 4 000 000";
        mvd.Mvd3Category = "Business and Commercial";
        mvd.Mvd3Extent = "100";
        mvd.Mvd3MarketValue = "R 8 400 000";

        var rows = AppealSection6Rows.Build(mvd);

        Assert.Equal(3, rows.Count);
        Assert.Equal("Residential", rows[1].CatDesc);
        Assert.Equal("R 4 000 000", rows[1].MarketValue);
        Assert.Equal("Business and Commercial", rows[2].CatDesc);
        Assert.Equal("100", rows[2].RateableArea);
    }

    [Fact]
    public void Empty_split_is_skipped()
    {
        var mvd = Mvd();
        mvd.Mvd2Category = "  ";
        mvd.Mvd3MarketValue = "R 1 000 000";

        var rows = AppealSection6Rows.Build(mvd);

        Assert.Equal(2, rows.Count);
        Assert.Equal("R 1 000 000", rows[1].MarketValue);
    }

    [Fact]
    public void Split_with_only_a_market_value_counts()
    {
        var mvd = Mvd();
        mvd.Mvd2MarketValue = "R 2 000 000";

        Assert.Equal(2, AppealSection6Rows.Build(mvd).Count);
    }
}
