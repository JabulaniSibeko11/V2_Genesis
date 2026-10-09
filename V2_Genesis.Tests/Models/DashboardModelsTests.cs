using V2_Genesis.Models.Results;
using V2_Genesis.Models.ViewModels.Dashboard;
using Xunit;

namespace V2_Genesis.Tests.Models;

/// Dashboard rows: objection lists and counts never include appeal rows.
public class DashboardModelsTests
{
    [Fact]
    public void Objection_count_does_not_count_appeal_rows()
    {
        var data = new RollData
        {
            ObjectedProperties = new()
            {
                new() { Sub_typ = 0, Objection_No = "GV23-Sup1-10" },
                new() { Sub_typ = 0, Objection_No = "GV23-Sup1-11" },
                new() { Sub_typ = 1, Objection_No = "APP-GV23-Sup1-1210" }
            }
        };

        Assert.Equal(2, data.ObjectionCount);
        Assert.Equal(3, data.ObjectedCount);
    }

    [Theory]
    [InlineData("Open", true)]
    [InlineData("Closed", false)]
    [InlineData("NotYetOpen", false)]
    [InlineData("Unknown", false)]
    public void Appeal_button_only_when_the_period_is_open(string state, bool open) =>
        Assert.Equal(open, new ObjectedPropertyResult { Appeal_Window_State = state }.IsAppealWindowOpen);

    [Fact]
    public void New_row_has_unknown_appeal_period() =>
        Assert.False(new ObjectedPropertyResult().IsAppealWindowOpen);
}
