using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models.Lis;
using V2_Genesis.Models.LIS;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Lis;

namespace V2_Genesis.Services.Implementations
{
    public class LisSearchService : ILisSearchService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LisSearchService> _logger;
        private readonly IReadOnlyDictionary<string, LisRollConfig> _registry;

        public LisSearchService(
            IConfiguration config,
            ILogger<LisSearchService> logger)
        {
            _config = config;
            _logger = logger;
            _registry = LisRollRegistry.Build();
        }

        // ── Main search — picks the right SP based on params ─────────────
        public async Task<List<LisProperty>> SearchAsync(
        string rollSource,
        LisSearchParams p,
        CancellationToken cancellationToken = default)
        {
            rollSource = ObjectionService.NormalizeRollSource(rollSource);

            if (!_registry.TryGetValue(rollSource, out var cfg))
            {
                _logger.LogWarning(
                    "[LIS] No LIS registry config found for rollSource {RollSource}.",
                    rollSource);

                return new();
            }

            var connStr = _config.GetConnectionString(cfg.ConnectionKey);

            if (string.IsNullOrWhiteSpace(connStr))
            {
                _logger.LogWarning(
                    "[LIS] Connection string {ConnKey} not found for roll {RollSource}.",
                    cfg.ConnectionKey,
                    rollSource);

                return new();
            }

            var town = Normalise(p.SearchTownName);
            var stand = Normalise(p.SearchStand);
            var address = Normalise(p.SearchAddress);
            var scheme = Normalise(p.SearchScheme);
            var unit = Normalise(p.SearchUnit);

            var plans = BuildSearchPlans(cfg, town, stand, address, scheme, unit);

            var allRows = new List<LisProperty>();

            await using var conn = new SqlConnection(connStr);

            foreach (var plan in plans)
            {
                _logger.LogInformation(
                    "[LIS] Roll={RollSource}, SP={SpName}, Params={Params}",
                    rollSource,
                    plan.SpName,
                    System.Text.Json.JsonSerializer.Serialize(plan.Params));

                var rows = await ExecuteLisSpAsync(
                    conn,
                    plan.SpName,
                    plan.Params,
                    rollSource,
                    cancellationToken);

                allRows.AddRange(rows);
            }

            return DeduplicateLisRows(allRows);
        }
        private sealed record LisSpPlan(string SpName, object Params);

        private static List<LisSpPlan> BuildSearchPlans(
    LisRollConfig cfg,
    string? town,
    string? stand,
    string? address,
    string? scheme,
    string? unit)
        {
            var hasStand = stand != null;
            var hasAddress = address != null;
            var hasScheme = scheme != null;
            var hasUnit = unit != null;

            LisSpPlan? plan = null;

            // Select one LIS stored procedure using the same decision order
            // as PropertySearchService.ResolveSp. Previously every matching
            // procedure was executed and TownOnly was always added, which
            // widened a stand/address/scheme search to the whole township.
            if (hasStand && !hasAddress && !hasScheme)
            {
                plan = new LisSpPlan(cfg.TownStand, new
                {
                    SearchTownName = Like(town),
                    SearchStand = Like(stand)
                });
            }
            else if (hasStand && hasAddress && !hasScheme)
            {
                plan = new LisSpPlan(cfg.TownStandAddress, new
                {
                    SearchTownName = Like(town),
                    SearchStand = Like(stand),
                    SearchAddress = Like(address)
                });
            }
            else if (!hasStand && !hasAddress && hasScheme && !hasUnit)
            {
                plan = new LisSpPlan(cfg.TownScheme, new
                {
                    SearchTownName = Like(town),
                    SearchScheme = Like(scheme)
                });
            }
            else if (!hasStand && hasAddress && !hasScheme)
            {
                plan = new LisSpPlan(cfg.TownAddress, new
                {
                    SearchTownName = Like(town),
                    SearchAddress = Like(address)
                });
            }
            else if (!hasStand && !hasAddress && !hasScheme && hasUnit)
            {
                plan = new LisSpPlan(cfg.TownUnit, new
                {
                    SearchTownName = Like(town),
                    SearchUnit = Like(unit)
                });
            }
            else if (hasScheme && hasUnit)
            {
                plan = new LisSpPlan(cfg.SchemeUnit, new
                {
                    SearchTownName = Like(town),
                    SearchScheme = Like(scheme),
                    SearchUnit = Like(unit)
                });
            }
            else if (hasStand && !hasAddress && hasScheme)
            {
                plan = new LisSpPlan(cfg.TownErfScheme, new
                {
                    SearchTownName = Like(town),
                    SearchStand = Like(stand),
                    SearchScheme = Like(scheme)
                });
            }
            else if (!hasStand && hasAddress && hasScheme)
            {
                plan = new LisSpPlan(cfg.TownAddressScheme, new
                {
                    SearchTownName = Like(town),
                    SearchAddress = Like(address),
                    SearchScheme = Like(scheme)
                });
            }
            else if (!hasStand && !hasAddress && !hasScheme && !hasUnit &&
                     !string.IsNullOrWhiteSpace(town))
            {
                // Township-only is valid only when the client searched using
                // township alone. It must never be a fallback for a more
                // specific search.
                plan = new LisSpPlan(cfg.TownOnly, new
                {
                    SearchTownName = Like(town)
                });
            }

            if (plan is null || string.IsNullOrWhiteSpace(plan.SpName))
                return new();

            return new List<LisSpPlan> { plan };
        }

        private async Task<List<LisProperty>> ExecuteLisSpAsync(
    SqlConnection conn,
    string spName,
    object parameters,
    string rollSource,
    CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(spName))
                return new();

            try
            {
                var rows = await conn.QueryAsync(
                    new CommandDefinition(
                        spName,
                        parameters,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 15,
                        cancellationToken: cancellationToken));

                return rows.Select(MapRow).ToList();
            }
            catch (SqlException ex) when (ex.Number == 2812)
            {
                // 2812 = Could not find stored procedure
                _logger.LogWarning(
                    "[LIS] Stored procedure {Sp} does not exist for roll {Roll}. Skipping.",
                    spName,
                    rollSource);

                return new();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "[LIS] Stored procedure {Sp} failed for roll {Roll}. Skipping.",
                    spName,
                    rollSource);

                return new();
            }
        }
        private static List<LisProperty> DeduplicateLisRows(List<LisProperty> rows)
        {
            return rows
                .GroupBy(x =>
                {
                    var unitKey = NormaliseKey(x.UnitKey);
                    var valuationKey = NormaliseKey(x.ValuationKey);

                    if (!string.IsNullOrWhiteSpace(unitKey) ||
                        !string.IsNullOrWhiteSpace(valuationKey))
                    {
                        return $"{unitKey}|{valuationKey}";
                    }

                    return $"{x.TownNameDescription}|{x.Erf}|{x.Ptn}|{x.Re}|{x.UnitNo}|{x.SchemeName}";
                })
                .Select(g => g.First())
                .ToList();
        }
        // ── Town/scheme dropdown ──────────────────────────────────────────
        public async Task<List<LisProperty>> GetTownSchemesAsync(string rollSource)
        {
            if (!_registry.TryGetValue(rollSource, out var cfg))
                return new();

            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            try
            {
                await using var conn = new SqlConnection(connStr);
                var rows = await conn.QueryAsync(
                    cfg.TownAndScheme, commandType: CommandType.StoredProcedure);
                return rows.Select(MapRow).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[LIS] GetTownSchemes failed on {Roll}", rollSource);
                return new();
            }
        }



        // ── Helpers ───────────────────────────────────────────────────────
        private static string? Normalise(string? val)
            => string.IsNullOrWhiteSpace(val) ? null : val.Trim();

        // Wraps value in %…% wildcard for LIKE, empty string if null
        private static string Like(string? val)
            => string.IsNullOrWhiteSpace(val) ? "%%" : $"%{val.Trim()}%";
        private static LisProperty MapRow(dynamic dr)
        {
            var d = (IDictionary<string, object>)dr;

            string? GetString(params string[] keys)
            {
                foreach (var key in keys)
                {
                    if (d.TryGetValue(key, out var v) && v is not null && v is not DBNull)
                    {
                        var text = v.ToString();

                        if (!string.IsNullOrWhiteSpace(text))
                            return text.Trim();
                    }
                }

                return null;
            }

            int GetInt(params string[] keys)
            {
                var value = GetString(keys);

                if (string.IsNullOrWhiteSpace(value))
                    return 0;

                if (int.TryParse(value, out var i))
                    return i;

                if (double.TryParse(value, out var d))
                    return Convert.ToInt32(d);

                return 0;
            }

            return new LisProperty
            {
                // IndexObjectionLIS column: Town_Name_Description
                TownNameDescription = GetString(
                    "Town_Name_Description",
                    "TownNameDescription",
                    "TownNameDesc"),

                // IndexObjectionLIS column: Property_Desc
                PropertyDescription = GetString(
                    "Property_Desc",
                    "PropertyDesc",
                    "PropertyDescription"),

                // IndexObjectionLIS column: Owner_Name
                OwnerName = GetString(
                    "Owner_Name",
                    "OwnerName"),

                // IndexObjectionLIS column: Erf_Number
                Erf = GetInt(
                    "Erf_Number",
                    "Erf",
                    "ERF"),

                // IndexObjectionLIS column: Portion_Number
                Ptn = GetInt(
                    "Portion_Number",
                    "Ptn",
                    "PTN"),

                // IndexObjectionLIS column: Property_Remainder_Indicator
                Re = GetString(
                    "Property_Remainder_Indicator",
                    "Re",
                    "RE"),

                // IndexObjectionLIS alias: LisStreetAddress
                LisStreetAddress = GetString(
                    "LisStreetAddress",
                    "StreetAddress",
                    "Address"),

                // IndexObjectionLIS column: CAT_Description
                CATDescription = GetString(
                    "CAT_Description",
                    "CATDescription",
                    "CatDesc"),

                // IndexObjectionLIS column: Rateable_Area
                RateableArea = GetString(
                    "Rateable_Area",
                    "RateableArea",
                    "Extent"),

                // IndexObjectionLIS column: Market_Value
                MarketValue = GetString(
                    "Market_Value",
                    "MarketValue"),

                // IndexObjectionLIS column: Valuation_Effective_Date_Wef_Date
                ValuationEffectiveDateWefDate = GetString(
                    "Valuation_Effective_Date_Wef_Date",
                    "ValuationEffectiveDateWefDate",
                    "WefDate"),

                // IndexObjectionLIS column: Reason
                Reason = GetString(
                    "Reason",
                    "Remarks"),

                // IndexObjectionLIS column: Scheme_Name
                SchemeName = GetString(
                    "Scheme_Name",
                    "Schemename",
                    "SchemeName"),

                // IndexObjectionLIS column: Scheme_Number
                SchemeNumber = GetString(
                    "Scheme_Number",
                    "SchemeNumber"),

                // IndexObjectionLIS column: Scheme_Year
                SchemeYear = GetString(
                    "Scheme_Year",
                    "SchemeYear"),

                // IndexObjectionLIS column: Unit_Number
                UnitNo = GetString(
                    "Unit_Number",
                    "UnitNo",
                    "UnitNumber"),

                // IndexObjectionLIS column: Premise_ID
                PremiseId = GetString(
                    "Premise_ID",
                    "PremiseId",
                    "PremiseID"),

                // IndexObjectionLIS column: Unit_Key
                UnitKey = GetString(
                    "Unit_Key",
                    "UnitKey",
                    "UNITKEY"),

                // IndexObjectionLIS column: Property_ID
                PropertyId = GetString(
                    "Property_ID",
                    "PropertyId",
                    "PropertyID"),

                // IndexObjectionLIS column: Valuation_Key
                ValuationKey = GetString(
                    "Valuation_Key",
                    "ValuationKey",
                    "VALUATIONKEY"),

                // IndexObjectionLIS column: Valuation_End_Date
                ValuationEndDate = GetString(
                    "Valuation_End_Date",
                    "ValuationEndDate"),

                AdditionalNotes = BuildSapAddress(
                    GetString("ADDR1"),
                    GetString("ADDR2"),
                    GetString("ADDR3"),
                    GetString("ADDR4"),
                    GetString("ADDR5")),

                Lease = GetString(
                    "LeaseStatus",
                    "Lease")
            };
        }
        private static string? BuildSapAddress(
    string? addr1,
    string? addr2,
    string? addr3,
    string? addr4,
    string? addr5)
        {
            var parts = new[]
            {
        addr1,
        addr2,
        addr3,
        addr4,
        addr5
    }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();

            return parts.Any()
                ? string.Join(", ", parts)
                : null;
        }

        public async Task<LisProperty?> GetPropertyByKeysAsync(
            string rollSource,
            string unitKey,
            string valuationKey,
            CancellationToken cancellationToken = default)
        {
            if (!_registry.TryGetValue(rollSource, out var cfg))
            {
                _logger.LogWarning(
                    "[LIS] Roll source {RollSource} not found in LIS registry.",
                    rollSource);

                return null;
            }

            unitKey = NormaliseKey(unitKey);
            valuationKey = NormaliseKey(valuationKey);

            if (string.IsNullOrWhiteSpace(unitKey) &&
                string.IsNullOrWhiteSpace(valuationKey))
            {
                _logger.LogWarning("[LIS] Detail lookup failed because both keys are empty.");
                return null;
            }

            var connStr = _config.GetConnectionString(cfg.ConnectionKey);

            if (string.IsNullOrWhiteSpace(connStr))
            {
                _logger.LogWarning(
                    "[LIS] Connection string {ConnKey} not found for roll {RollSource}.",
                    cfg.ConnectionKey,
                    rollSource);

                return null;
            }

            if (string.IsNullOrWhiteSpace(cfg.DetailSp))
            {
                _logger.LogWarning(
                    "[LIS] DetailSp is not configured for roll {RollSource}.",
                    rollSource);

                return null;
            }

            try
            {
                await using var conn = new SqlConnection(connStr);

                var attempts = BuildDetailSpParameterAttempts(unitKey, valuationKey);

                foreach (var parameters in attempts)
                {
                    try
                    {
                        var rows = await conn.QueryAsync(
                            new CommandDefinition(
                                cfg.DetailSp,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: 15,
                                cancellationToken: cancellationToken));

                        var mapped = rows.Select(MapRow).ToList();

                        if (!mapped.Any())
                            continue;

                        var exactMatch = mapped.FirstOrDefault(x =>
                            SameKey(x.UnitKey, unitKey) &&
                            SameKey(x.ValuationKey, valuationKey));

                        var looseMatch = mapped.FirstOrDefault(x =>
                            SameKey(x.UnitKey, unitKey) ||
                            SameKey(x.ValuationKey, valuationKey));

                        var result = exactMatch ?? looseMatch ?? mapped.FirstOrDefault();

                        if (result != null)
                        {
                            _logger.LogInformation(
                                "[LIS] Detail property found using {Sp}. Roll={Roll}, UnitKey={UnitKey}, ValuationKey={ValuationKey}",
                                cfg.DetailSp,
                                rollSource,
                                unitKey,
                                valuationKey);

                            return result;
                        }
                    }
                    catch (SqlException ex) when (ex.Number == 8144 || ex.Number == 201)
                    {
                        // 8144 = too many arguments specified
                        // 201  = procedure expects parameter not supplied
                        // Try next parameter-name set.
                        _logger.LogWarning(
                            "[LIS] DetailSp {Sp} parameter attempt failed for roll {Roll}. Trying next parameter set. SQL={SqlNumber}",
                            cfg.DetailSp,
                            rollSource,
                            ex.Number);
                    }
                }

                _logger.LogWarning(
                    "[LIS] No detail property found using {Sp}. Roll={Roll}, UnitKey={UnitKey}, ValuationKey={ValuationKey}",
                    cfg.DetailSp,
                    rollSource,
                    unitKey,
                    valuationKey);

                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[LIS] GetPropertyByKeysAsync failed using {Sp}. Roll={Roll}, UnitKey={UnitKey}, ValuationKey={ValuationKey}",
                    cfg.DetailSp,
                    rollSource,
                    unitKey,
                    valuationKey);

                return null;
            }
        }
        private static List<DynamicParameters> BuildDetailSpParameterAttempts(
    string unitKey,
    string valuationKey)
        {
            var attempts = new List<DynamicParameters>();

            DynamicParameters Params(params (string Name, string Value)[] values)
            {
                var p = new DynamicParameters();

                foreach (var item in values)
                {
                    if (!string.IsNullOrWhiteSpace(item.Value))
                        p.Add(item.Name, item.Value);
                }

                return p;
            }

            // Try the most common stored procedure parameter names.
            attempts.Add(Params(
                ("UnitKey", unitKey),
                ("ValuationKey", valuationKey)));

            attempts.Add(Params(
                ("Unit_Key", unitKey),
                ("Valuation_Key", valuationKey)));

            attempts.Add(Params(
                ("unitKey", unitKey),
                ("valuationKey", valuationKey)));

            attempts.Add(Params(
                ("SearchUnitKey", unitKey),
                ("SearchValuationKey", valuationKey)));

            attempts.Add(Params(
                ("SearchUnit", unitKey),
                ("SearchValuation", valuationKey)));

            // Fallbacks if the detail SP accepts only one key.
            attempts.Add(Params(("UnitKey", unitKey)));
            attempts.Add(Params(("ValuationKey", valuationKey)));
            attempts.Add(Params(("Unit_Key", unitKey)));
            attempts.Add(Params(("Valuation_Key", valuationKey)));

            return attempts;
        }
        private static string NormaliseKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            value = value.Trim();

            // Handles bad scientific notation like "5.48365e 008"
            value = value.Replace("e ", "e+", StringComparison.OrdinalIgnoreCase);

            if (double.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var d))
            {
                return Math.Round(d)
                    .ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            }

            return value;
        }

        private static bool SameKey(string? left, string? right)
        {
            var a = NormaliseKey(left);
            var b = NormaliseKey(right);

            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            return a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }
    }

}
