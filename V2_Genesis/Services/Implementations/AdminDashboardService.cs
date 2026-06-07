// ═══════════════════════════════════════════════════════════════
//  Services/Implementations/AdminDashboardService.cs  — REPLACE FULL FILE
// ═══════════════════════════════════════════════════════════════
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models.Admin;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.Results.Admin;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Admin;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(IConfiguration config,
        ILogger<AdminDashboardService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private SqlConnection GetConn(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            throw new InvalidOperationException($"Unknown roll: {rollSource}");
        var cs = _config.GetConnectionString(cfg.ConnectionKey)!;
        return new SqlConnection(cs);
    }

    // ── Stats ─────────────────────────────────────────────────────────
    public async Task<AdminRollStats> GetStatsAsync(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new AdminRollStats { RollSource = rollSource };

        try
        {
            await using var conn = GetConn(rollSource);
            var result = await conn.QueryFirstOrDefaultAsync<AdminRollStats>(
                cfg.StatsSp,
                commandType: CommandType.StoredProcedure);

            if (result is not null)
            {
                result.RollSource = rollSource;
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Stats SP failed for {Roll}", rollSource);
        }

        return new AdminRollStats { RollSource = rollSource };
    }

    // ── Search Objections ─────────────────────────────────────────────
    public async Task<List<AdminObjectionResult>> SearchObjectionsAsync(
        string rollSource, string searchValue)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new();

        try
        {
            await using var conn = GetConn(rollSource);
            var results = await conn.QueryAsync<AdminObjectionResult>(
                cfg.ObjSearchSp,
                new { SearchValue = searchValue },
                commandType: CommandType.StoredProcedure);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Search SP failed for {Roll}", rollSource);
            return new();
        }
    }

    // ── Search Appeals ────────────────────────────────────────────────
    public async Task<List<AdminAppealResult>> SearchAppealsAsync(
        string rollSource, string searchValue)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new();

        bool isAppeal = searchValue.Trim().ToUpper().StartsWith("APP");
        var sp = isAppeal ? cfg.AppSearchSp : cfg.PropSearchSp;

        try
        {
            await using var conn = GetConn(rollSource);
            var results = await conn.QueryAsync<AdminAppealResult>(
                sp,
                new { searchAppealValue = searchValue },
                commandType: CommandType.StoredProcedure);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] Appeal search SP failed for {Roll}", rollSource);
            return new();
        }
    }

    // ── All-users roll data for admin dashboard ───────────────────────
    // FIX 1: removed New_Market_Value_MVD / New_Category_MVD — not on Obj_Property_Info
    // FIX 2: added Appeals query
    public async Task<RollData> GetAllRollDataAsync(string rollSource)
    {
        if (!AdminRollRegistry.Configs.TryGetValue(rollSource, out var cfg))
            return new RollData();

        try
        {
            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            await using var conn = new SqlConnection(connStr);

            // ── All objections (no UserID filter) ──────────────────
            var objRows = await conn.QueryAsync(
                @"SELECT TOP 500
                    Objection_No,
                    Property_Desc,
                    Old_Category,
                    Town_Name,
                    Old_Market_Value,
                    objection_Status,
                    Sub_typ,
                    Unit_key,
                    Valuation_Key,
                    Property_Type,
                    PropertyFrom
                  FROM dbo.Obj_Property_Info
                  ORDER BY Objection_No DESC");

            var objProps = objRows.Select(r => new ObjectedPropertyResult
            {
                Objection_No = r.Objection_No?.ToString(),
                Property_Desc = r.Property_Desc?.ToString(),
                Old_Category = r.Old_Category?.ToString(),
                Town_Name = r.Town_Name?.ToString(),
                Old_Market_Value = r.Old_Market_Value?.ToString(),
                objection_Status = r.objection_Status?.ToString(),
                Sub_typ = Convert.ToInt32(r.Sub_typ ?? 0),
                Unit_key = r.Unit_key?.ToString(),
                Valuation_Key = r.Valuation_Key?.ToString(),
                Property_Type = r.Property_Type?.ToString(),
                PropertyFrom = r.PropertyFrom?.ToString(),
            }).ToList();

            // ── All appeals (no UserID filter) ─────────────────────
            var appRows = await conn.QueryAsync(
                @"SELECT TOP 200
                    Appeal_No,
                    A_Property_Desc,
                    Town_Name,
                    Old_Market_Value,
                    Old_Category,
                    A_Unit_key,
                    A_Valuation_Key,
                    A_Property_Type,
                    Appeal_Status
                  FROM dbo.Obj_Property_Info_Appeal
                  ORDER BY Appeal_No DESC");

            var appeals = appRows.Select(r => new AppealResult
            {
                Appeal_No = r.Appeal_No?.ToString(),
                A_Property_Desc = r.A_Property_Desc?.ToString(),
                Town_Name = r.Town_Name?.ToString(),
                Old_Market_Value = r.Old_Market_Value?.ToString(),
                Old_Category = r.Old_Category?.ToString(),
                A_Unit_key = r.A_Unit_key?.ToString(),
                A_Valuation_Key = r.A_Valuation_Key?.ToString(),
                A_Property_Type = r.A_Property_Type?.ToString(),
                Appeal_Status = r.Appeal_Status?.ToString(),
            }).ToList();

            return new RollData
            {
                LinkedProperties = new(),    // admin doesn't link properties
                ObjectedProperties = objProps,
                Appeals = appeals,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AdminDashboard] GetAllRollData failed for {Roll} — {Msg}",
                rollSource, ex.Message);
            return new RollData();
        }
    }

    // ── Unified search by reference number ───────────────────────────
    public async Task<AdminSearchResult> SearchByReferenceAsync(
        string refNo, string? rollSource)
    {
        var result = new AdminSearchResult
        {
            SearchType = "Reference",
            SearchInput = refNo,
            RollFilter = rollSource
        };

        var refUpper = refNo.Trim().ToUpper();
        bool isAppeal = refUpper.Contains("-APP-") || refUpper.StartsWith("APP");
        bool isQuery = refUpper.Contains("QUE-") || refUpper.StartsWith("QUE");

        var rolls = AdminRollRegistry.Configs
            .Where(kv => string.IsNullOrEmpty(rollSource) || kv.Key == rollSource)
            .ToList();

        foreach (var (roll, cfg) in rolls)
        {
            try
            {
                var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
                await using var conn = new SqlConnection(connStr);

                string rollName = roll switch
                {
                    "Objection" => "General Valuation Roll 2023",
                    "Objection_Supp1" => "Supplementary Roll 1",
                    "Objection_Supp2" => "Supplementary Roll 2",
                    "Objection_Supp3" => "Supplementary Roll 3",
                    _ => roll
                };

                if (isQuery)
                {
                    var connStrQ = _config.GetConnectionString("QueryConnection")!;
                    await using var connQ = new SqlConnection(connStrQ);
                    var qRow = await connQ.QueryFirstOrDefaultAsync(
                        @"SELECT TOP 1 Query_No, Property_Desc, Town_Name,
                                 Old_Category, Old_Market_Value,
                                 Query_Status, Unit_key, Valuation_Key
                          FROM dbo.Que_Property_Info
                          WHERE Query_No = @Ref",
                        new { Ref = refNo });

                    if (qRow is not null)
                        result.RefMatches.Add(new AdminRefMatch
                        {
                            RollSource = "Objection_Query",
                            RollName = "Section 78 Query",
                            RefType = "Query",
                            Query_No = qRow.Query_No?.ToString(),
                            Property_Desc = qRow.Property_Desc?.ToString(),
                            Town_Name = qRow.Town_Name?.ToString(),
                            Old_Category = qRow.Old_Category?.ToString(),
                            Old_Market_Value = qRow.Old_Market_Value?.ToString(),
                            Query_Status = qRow.Query_Status?.ToString(),
                            Unit_key = qRow.Unit_key?.ToString(),
                            Valuation_Key = qRow.Valuation_Key?.ToString(),
                        });
                    break;
                }
                else if (isAppeal)
                {
                    var aRow = await conn.QueryFirstOrDefaultAsync(
                        @"SELECT TOP 1 * FROM dbo.Obj_Property_Info_Appeal
                          WHERE Appeal_No = @Ref",
                        new { Ref = refNo });

                    if (aRow is not null)
                        result.RefMatches.Add(new AdminRefMatch
                        {
                            RollSource = roll,
                            RollName = rollName,
                            RefType = "Appeal",
                            Appeal_No = aRow.Appeal_No?.ToString(),
                            Property_Desc = aRow.A_Property_Desc?.ToString(),
                            Town_Name = aRow.Town_Name?.ToString(),
                            Old_Category = aRow.Old_Category?.ToString(),
                            Old_Market_Value = aRow.Old_Market_Value?.ToString(),
                            Appeal_Status = aRow.Appeal_Status?.ToString(),
                            Unit_key = aRow.A_Unit_key?.ToString(),
                            Valuation_Key = aRow.A_Valuation_Key?.ToString(),
                        });
                }
                else
                {
                    var oRow = await conn.QueryFirstOrDefaultAsync(
                        @"SELECT TOP 1 * FROM dbo.Obj_Property_Info
                          WHERE Objection_No = @Ref",
                        new { Ref = refNo });

                    if (oRow is not null)
                        result.RefMatches.Add(new AdminRefMatch
                        {
                            RollSource = roll,
                            RollName = rollName,
                            RefType = "Objection",
                            Objection_No = oRow.Objection_No?.ToString(),
                            Property_Desc = oRow.Property_Desc?.ToString(),
                            Town_Name = oRow.Town_Name?.ToString(),
                            Old_Category = oRow.Old_Category?.ToString(),
                            Old_Market_Value = oRow.Old_Market_Value?.ToString(),
                            objection_Status = oRow.objection_Status?.ToString(),
                            Unit_key = oRow.Unit_key?.ToString(),
                            Valuation_Key = oRow.Valuation_Key?.ToString(),
                            PropertyFrom = oRow.PropertyFrom?.ToString(),
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AdminSearch] RefSearch failed for {Roll}", roll);
            }
        }

        return result;
    }

    // ── Unified search by property attributes ────────────────────────
    // FIX 3: Town_Name (was TownName — wrong column name)
    public async Task<AdminSearchResult> SearchByPropertyAsync(
        string? town, string? stand, string? address,
        string? scheme, string? unit, string? rollSource)
    {
        var result = new AdminSearchResult
        {
            SearchType = "Property",
            SearchInput = string.Join(" ",
                new[] { town, stand, address, scheme, unit }
                    .Where(v => !string.IsNullOrWhiteSpace(v))),
            RollFilter = rollSource
        };

        string Like(string? v) => string.IsNullOrWhiteSpace(v) ? "%%" : $"%{v.Trim()}%";

        string RollName(string src) => src switch
        {
            "Objection" => "GV 2023",
            "Objection_Supp1" => "Supp 1",
            "Objection_Supp2" => "Supp 2",
            "Objection_Supp3" => "Supp 3",
            _ => src
        };

        var rolls = AdminRollRegistry.Configs
            .Where(kv => string.IsNullOrEmpty(rollSource) || kv.Key == rollSource)
            .ToList();

        foreach (var (roll, cfg) in rolls)
        {
            try
            {
                var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
                await using var conn = new SqlConnection(connStr);

                var conditions = new List<string> { "1=1" };
                var parms = new DynamicParameters();

                if (!string.IsNullOrWhiteSpace(town))
                {
                    conditions.Add("Town_Name LIKE @Town");   // FIX: was TownName
                    parms.Add("Town", Like(town));
                }
                if (!string.IsNullOrWhiteSpace(stand))
                {
                    conditions.Add("Property_Desc LIKE @Stand");
                    parms.Add("Stand", Like(stand));
                }
                if (!string.IsNullOrWhiteSpace(address))
                {
                    conditions.Add("Property_Desc LIKE @Addr");
                    parms.Add("Addr", Like(address));
                }
                if (!string.IsNullOrWhiteSpace(scheme))
                {
                    conditions.Add("Property_Desc LIKE @Scheme");
                    parms.Add("Scheme", Like(scheme));
                }
                if (!string.IsNullOrWhiteSpace(unit))
                {
                    conditions.Add("Property_Desc LIKE @Unit");
                    parms.Add("Unit", Like(unit));
                }

                var sql = $@"SELECT TOP 100
                                Objection_No, Property_Desc, Town_Name,
                                Old_Category, Old_Market_Value,
                                objection_Status, Sub_typ,
                                Unit_key, Valuation_Key, PropertyFrom
                             FROM dbo.Obj_Property_Info
                             WHERE {string.Join(" AND ", conditions)}
                             ORDER BY Objection_No DESC";

                var rows = await conn.QueryAsync(sql, parms);

                foreach (var r in rows)
                    result.PropMatches.Add(new AdminPropMatch
                    {
                        RollSource = roll,
                        RollName = RollName(roll),
                        Objection_No = r.Objection_No?.ToString(),
                        Property_Desc = r.Property_Desc?.ToString(),
                        Town_Name = r.Town_Name?.ToString(),
                        Old_Category = r.Old_Category?.ToString(),
                        Old_Market_Value = r.Old_Market_Value?.ToString(),
                        objection_Status = r.objection_Status?.ToString(),
                        Sub_typ = Convert.ToInt32(r.Sub_typ ?? 0),
                        Unit_key = r.Unit_key?.ToString(),
                        Valuation_Key = r.Valuation_Key?.ToString(),
                        PropertyFrom = r.PropertyFrom?.ToString(),
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[AdminSearch] PropSearch failed for {Roll}", roll);
            }
        }

        return result;
    }
}