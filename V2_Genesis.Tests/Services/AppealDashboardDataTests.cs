using Microsoft.Extensions.Configuration;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Objection;
using Xunit;

namespace V2_Genesis.Tests.Services;

public class AppealDashboardDataTests
{
    private static IConfiguration Config(string? table) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AppealMvd:TableName"] = table })
            .Build();

    [Theory]
    [InlineData(null, "Objection_MVD")]
    [InlineData("", "Objection_MVD")]
    [InlineData("Objection_MVD1", "Objection_MVD1")]
    [InlineData("Objection_MVD; DROP TABLE x", "Objection_MVD")]   // never anything else
    public void Mvd_table_is_only_one_of_the_two_known_tables(string? setting, string expected) =>
        Assert.Equal(expected, AppealDashboardData.MvdTable(Config(setting)));

    [Fact]
    public void Merge_never_touches_objection_rows()
    {
        var target = new List<ObjectedPropertyResult>
        {
            new() { Sub_typ = 0, Objection_No = "GV23-Sup3-75", Evidence_Window_Open = false }
        };
        var appeals = new List<ObjectedPropertyResult>
        {
            new() { Sub_typ = 1, Objection_No = "APP-GV23-Sup3-75", Appeal_No = "APP-GV23-Sup3-75", Evidence_Window_Open = true }
        };

        AppealDashboardData.MergeAppeals(target, appeals);

        Assert.False(target[0].Evidence_Window_Open);
        Assert.Equal(2, target.Count);
    }

    [Fact]
    public void Merge_matches_appeal_numbers_ignoring_case_and_spaces()
    {
        var target = new List<ObjectedPropertyResult>
        {
            new() { Sub_typ = 1, Appeal_No = " app-gv23-sup3-76 " }
        };
        var appeals = new List<ObjectedPropertyResult>
        {
            new() { Sub_typ = 1, Appeal_No = "APP-GV23-Sup3-76", Appeal_Objection_Ref = "GV23-Sup3-12" }
        };

        AppealDashboardData.MergeAppeals(target, appeals);

        var row = Assert.Single(target);
        Assert.Equal("GV23-Sup3-12", row.Appeal_Objection_Ref);
    }
}
