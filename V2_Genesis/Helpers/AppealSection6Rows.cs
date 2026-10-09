using V2_Genesis.Models.Objections;

namespace V2_Genesis.Helpers;

/// <summary>
/// Section 6 "as decided" of an appeal form: the Municipal Valuer's Decision
/// of the objection (Obj_Property_Info) — main row first, then the New2 and
/// New3 MVD splits when they have values. Used by ObjectionController
/// (CheckProperty, appeal) and unit-tested in V2_Genesis.Tests.
/// </summary>
public static class AppealSection6Rows
{
    public static List<CheckPropertyResult> Build(CheckPropertyResult mvd)
    {
        var rows = new List<CheckPropertyResult>
        {
            new()
            {
                CatDesc = mvd.CatDesc,
                LisStreetAddress = mvd.LisStreetAddress,
                RateableArea = mvd.RateableArea,
                MarketValue = mvd.MarketValue
            }
        };

        if (HasValue(mvd.Mvd2Category, mvd.Mvd2Extent, mvd.Mvd2MarketValue))
        {
            rows.Add(new CheckPropertyResult
            {
                CatDesc = mvd.Mvd2Category,
                RateableArea = mvd.Mvd2Extent,
                MarketValue = mvd.Mvd2MarketValue
            });
        }

        if (HasValue(mvd.Mvd3Category, mvd.Mvd3Extent, mvd.Mvd3MarketValue))
        {
            rows.Add(new CheckPropertyResult
            {
                CatDesc = mvd.Mvd3Category,
                RateableArea = mvd.Mvd3Extent,
                MarketValue = mvd.Mvd3MarketValue
            });
        }

        return rows;
    }

    private static bool HasValue(params string?[] values) =>
        values.Any(v => !string.IsNullOrWhiteSpace(v));
}
