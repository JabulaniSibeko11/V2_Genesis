using V2_Genesis.Models.Results;
using V2_Genesis.Services.Objection;
using Xunit;

namespace V2_Genesis.Tests.Services;

/// Appeal period from Objection_MVD — the same for clients and the admin team.
public class AppealWindowRulesTests
{
    private static readonly DateTime Start = new(2026, 4, 16);
    private static readonly DateTime Close = new(2026, 6, 1);

    [Fact]
    public void Inside_the_period_is_open() =>
        Assert.Equal(AppealWindowRules.Open, AppealWindowRules.State(Start, Close, new DateTime(2026, 5, 10)));

    [Fact]
    public void Close_date_itself_is_still_open() =>
        Assert.Equal(AppealWindowRules.Open, AppealWindowRules.State(Start, Close, Close));

    [Fact]
    public void Day_after_close_is_closed() =>
        Assert.Equal(AppealWindowRules.Closed, AppealWindowRules.State(Start, Close, Close.AddDays(1)));

    [Fact]
    public void Before_start_is_not_open_yet() =>
        Assert.Equal(AppealWindowRules.NotYetOpen, AppealWindowRules.State(Start, Close, Start.AddDays(-1)));

    [Fact]
    public void No_close_date_is_unknown() =>
        Assert.Equal(AppealWindowRules.Unknown, AppealWindowRules.State(Start, null, Start));

    [Fact]
    public void Example_GV23_Sup2_289_is_closed_on_8_October_2026() =>
        Assert.Equal(AppealWindowRules.Closed, AppealWindowRules.State(Start, Close, new DateTime(2026, 10, 8)));

    [Theory]
    [InlineData("1")]
    [InlineData("True")]
    [InlineData("Yes")]
    public void Revised_mvd_uses_the_revised_dates(string flag)
    {
        var w = AppealWindowRules.Resolve(Start, Close, new DateTime(2026, 9, 1), new DateTime(2026, 10, 30), flag);
        Assert.True(w.Revised);
        Assert.Equal(new DateTime(2026, 10, 30), w.Close);
        Assert.Equal(new DateTime(2026, 9, 1), w.Start);
    }

    [Fact]
    public void Revised_close_date_alone_also_counts_as_revised()
    {
        var w = AppealWindowRules.Resolve(Start, Close, null, new DateTime(2026, 10, 30), null);
        Assert.True(w.Revised);
        Assert.Equal(Start, w.Start);
        Assert.Equal(new DateTime(2026, 10, 30), w.Close);
    }

    [Fact]
    public void Not_revised_uses_the_original_dates()
    {
        var w = AppealWindowRules.Resolve(Start, Close, null, null, "0");
        Assert.False(w.Revised);
        Assert.Equal(Close, w.Close);
    }

    [Fact]
    public void Merge_adds_missing_appeals_and_keeps_existing_ones()
    {
        var target = new List<ObjectedPropertyResult>
        {
            new() { Sub_typ = 0, Objection_No = "GV23-Sup2-289" },
            new() { Sub_typ = 1, Objection_No = "GV23-Sup2-A1", Appeal_No = "GV23-Sup2-A1" }
        };
        var fromTable = new List<ObjectedPropertyResult>
        {
            new() { Sub_typ = 1, Objection_No = "GV23-Sup2-A1", Appeal_No = "GV23-Sup2-A1", Evidence_Window_Open = true, Evidence_Expires_At = Close },
            new() { Sub_typ = 1, Objection_No = "GV23-Sup2-A2", Appeal_No = "GV23-Sup2-A2" }
        };

        AppealDashboardData.MergeAppeals(target, fromTable);

        Assert.Equal(3, target.Count);
        Assert.Equal(2, target.Count(r => r.Sub_typ == 1));
        Assert.True(target.Single(r => r.Appeal_No == "GV23-Sup2-A1").Evidence_Window_Open);
    }
}
