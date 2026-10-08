using System.Data;
using Dapper;
using V2_Genesis.Models.Results;

namespace V2_Genesis.Services.Objection;

/// <summary>
/// Appeal data for the client and admin dashboards (one query per roll, not
/// one per row):
///   • the appeal period of every Notice-Sent objection (Objection_MVD);
///   • the appeal already lodged for an objection (Obj_Property_Info_Appeal);
///   • the appeals of an account (Obj_Property_Info_Appeal.A_UserID), so the
///     "My Appeals" list does not depend on the dashboard procedure alone.
/// </summary>
public static class AppealDashboardData
{
    /// "Objection_MVD" (default) or "Objection_MVD1" — same setting as ObjectionService.
    public static string MvdTable(IConfiguration config)
    {
        var name = config["AppealMvd:TableName"]?.Trim();
        return name is "Objection_MVD1" ? "Objection_MVD1" : "Objection_MVD";
    }

    public static async Task PopulateAppealWindowsAsync(
        IDbConnection conn,
        IEnumerable<ObjectedPropertyResult> rows,
        string mvdTable)
    {
        var objections = rows
            .Where(r => r.Sub_typ == 0 &&
                        string.Equals(r.objection_Status?.Trim(), "Notice-Sent", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(r.Objection_No))
            .ToList();

        if (objections.Count == 0)
            return;

        var refs = objections
            .Select(r => r.Objection_No!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var table = mvdTable is "Objection_MVD1" ? "Objection_MVD1" : "Objection_MVD";

        var windows = (await conn.QueryAsync<MvdWindowRow>($@"
SELECT LTRIM(RTRIM(Objection_No))                AS Objection_No,
       Appeal_Start_Date                         AS Start,
       Appeal_Close_Date                         AS [Close],
       Appeal_Start_Date_ReviseMVD               AS RevisedStart,
       Appeal_Close_Date_ReviseMVD               AS RevisedClose,
       CAST(Revise_MVD AS nvarchar(20))          AS ReviseMvd,
       Batch_Date                                AS BatchDate
FROM dbo.[{table}]
WHERE LTRIM(RTRIM(Objection_No)) IN @Refs;",
            new { Refs = refs },
            commandTimeout: 30))
            .GroupBy(w => w.Objection_No ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(w => w.BatchDate).First(),
                          StringComparer.OrdinalIgnoreCase);

        // Appeal already lodged for the objection (one appeal per objection).
        Dictionary<string, string?> lodged;
        try
        {
            lodged = (await conn.QueryAsync<LodgedAppealRow>(@"
SELECT LTRIM(RTRIM(Obj_Ref)) AS Obj_Ref, LTRIM(RTRIM(Appeal_No)) AS Appeal_No
FROM dbo.Obj_Property_Info_Appeal
WHERE LTRIM(RTRIM(Obj_Ref)) IN @Refs
  AND ISNULL(Appeal_Status, '') NOT LIKE '%Withdraw%';",
                new { Refs = refs },
                commandTimeout: 30))
                .Where(a => !string.IsNullOrWhiteSpace(a.Obj_Ref))
                .GroupBy(a => a.Obj_Ref!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Appeal_No, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            lodged = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var today = AppealWindowRules.TodaySa();

        foreach (var row in objections)
        {
            var key = row.Objection_No!.Trim();

            if (lodged.TryGetValue(key, out var appealNo))
                row.Lodged_Appeal_No = appealNo;

            if (!windows.TryGetValue(key, out var w))
                continue;

            var window = AppealWindowRules.Resolve(w.Start, w.Close, w.RevisedStart, w.RevisedClose, w.ReviseMvd);
            row.Appeal_Start_Date = window.Start;
            row.Appeal_Close_Date = window.Close;
            row.Appeal_Window_State = AppealWindowRules.State(window.Start, window.Close, today);
        }
    }

    /// Appeals captured on this account, as dashboard rows (Sub_typ = 1).
    public static async Task<List<ObjectedPropertyResult>> LoadUserAppealsAsync(
        IDbConnection conn,
        string userId)
    {
        const string sqlWithMvd = @"
SELECT LTRIM(RTRIM(a.Appeal_No))        AS Appeal_No,
       LTRIM(RTRIM(a.Obj_Ref))          AS Obj_Ref,
       a.A_Property_Desc                AS Property_Desc,
       a.A_Property_Type                AS Property_Type,
       a.A_Unit_key                     AS Unit_key,
       a.A_Valuation_Key                AS Valuation_Key,
       LTRIM(RTRIM(a.Appeal_Status))    AS Appeal_Status,
       a.Appeal_Start_DateTime          AS Start_DateTime,
       o.New_Category_MVD               AS Category,
       o.New_Market_Value_MVD           AS Market_Value,
       o.PropertyFrom                   AS PropertyFrom
FROM dbo.Obj_Property_Info_Appeal a
LEFT JOIN dbo.Obj_Property_Info o
       ON LTRIM(RTRIM(o.Objection_No)) = LTRIM(RTRIM(a.Obj_Ref))
WHERE LTRIM(RTRIM(a.A_UserID)) = @UserId;";

        // Older roll databases (e.g. GV23) have no MVD columns on
        // Obj_Property_Info and/or no Appeal_Start_DateTime on the appeal table.
        const string sqlWithoutMvd = @"
SELECT LTRIM(RTRIM(a.Appeal_No))        AS Appeal_No,
       LTRIM(RTRIM(a.Obj_Ref))          AS Obj_Ref,
       a.A_Property_Desc                AS Property_Desc,
       a.A_Property_Type                AS Property_Type,
       a.A_Unit_key                     AS Unit_key,
       a.A_Valuation_Key                AS Valuation_Key,
       LTRIM(RTRIM(a.Appeal_Status))    AS Appeal_Status,
       CAST(NULL AS datetime)           AS Start_DateTime,
       CAST(NULL AS nvarchar(100))      AS Category,
       CAST(NULL AS nvarchar(100))      AS Market_Value,
       CAST(NULL AS nvarchar(100))      AS PropertyFrom
FROM dbo.Obj_Property_Info_Appeal a
WHERE LTRIM(RTRIM(a.A_UserID)) = @UserId;";

        IEnumerable<UserAppealRow> rows;
        try
        {
            rows = await conn.QueryAsync<UserAppealRow>(sqlWithMvd, new { UserId = userId.Trim() }, commandTimeout: 30);
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
        {
            rows = await conn.QueryAsync<UserAppealRow>(sqlWithoutMvd, new { UserId = userId.Trim() }, commandTimeout: 30);
        }

        var now = DateTime.Now;

        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Appeal_No))
            .Select(r =>
            {
                var expires = r.Start_DateTime?.AddHours(48);
                return new ObjectedPropertyResult
                {
                    Sub_typ = 1,
                    Objection_No = r.Appeal_No,
                    Appeal_No = r.Appeal_No,
                    Appeal_Objection_Ref = r.Obj_Ref,
                    Property_Desc = r.Property_Desc,
                    Property_Type = r.Property_Type,
                    Unit_key = r.Unit_key,
                    Valuation_Key = r.Valuation_Key,
                    objection_Status = r.Appeal_Status,
                    Old_Category = r.Category,
                    Old_Market_Value = r.Market_Value,
                    PropertyFrom = r.PropertyFrom,
                    Submission_Date = r.Start_DateTime,
                    Evidence_Expires_At = expires,
                    Evidence_Window_Open =
                        expires.HasValue && now <= expires.Value &&
                        (string.Equals(r.Appeal_Status, "App-Lodging", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(r.Appeal_Status, "App-Unallocated", StringComparison.OrdinalIgnoreCase))
                };
            })
            .ToList();
    }

    /// Adds the account's appeals that the dashboard procedure did not return,
    /// and fills the 48-hour window on the appeal rows it did return.
    public static void MergeAppeals(List<ObjectedPropertyResult> target, List<ObjectedPropertyResult> appeals)
    {
        var byNo = appeals.ToDictionary(a => a.Appeal_No!.Trim(), StringComparer.OrdinalIgnoreCase);

        foreach (var row in target.Where(r => r.Sub_typ == 1))
        {
            var no = (!string.IsNullOrWhiteSpace(row.Appeal_No) ? row.Appeal_No : row.Objection_No)?.Trim();
            if (no is null || !byNo.TryGetValue(no, out var a))
                continue;

            row.Appeal_No ??= a.Appeal_No;
            row.Appeal_Objection_Ref ??= a.Appeal_Objection_Ref;
            row.Submission_Date ??= a.Submission_Date;
            row.Evidence_Expires_At = a.Evidence_Expires_At;
            row.Evidence_Window_Open = a.Evidence_Window_Open;
            byNo.Remove(no);
        }

        target.AddRange(byNo.Values);
    }

    private sealed class MvdWindowRow
    {
        public string? Objection_No { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? Close { get; set; }
        public DateTime? RevisedStart { get; set; }
        public DateTime? RevisedClose { get; set; }
        public string? ReviseMvd { get; set; }
        public DateTime? BatchDate { get; set; }
    }

    private sealed class LodgedAppealRow
    {
        public string? Obj_Ref { get; set; }
        public string? Appeal_No { get; set; }
    }

    private sealed class UserAppealRow
    {
        public string? Appeal_No { get; set; }
        public string? Obj_Ref { get; set; }
        public string? Property_Desc { get; set; }
        public string? Property_Type { get; set; }
        public string? Unit_key { get; set; }
        public string? Valuation_Key { get; set; }
        public string? Appeal_Status { get; set; }
        public DateTime? Start_DateTime { get; set; }
        public string? Category { get; set; }
        public string? Market_Value { get; set; }
        public string? PropertyFrom { get; set; }
    }
}
