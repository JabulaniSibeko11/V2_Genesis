using Dapper;
using GenesisV2.Services.PropertySearch;
using Microsoft.Data.SqlClient;
using System.Data;
using V2_Genesis.Models;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class AttributesSearchService : IAttributesSearchService
    {
        private readonly IConfiguration _config;

        // Attributes search runs on its own DB but uses GV SP names (no suffix)
        private readonly string _attrConn;

        // GV PropertyIndex config — provides the SP names
        // Attributes DB has the same SPs as GV
        private static readonly RollSearchConfig GvConfig =
            RollSearchRegistry.Configs["Objection"];

        public AttributesSearchService(IConfiguration config)
        {
            _config = config;
            _attrConn = config.GetConnectionString("AttributesConnection")
                ?? throw new InvalidOperationException(
                    "AttributesConnection missing from appsettings");
        }

        // ── Search — same SPs as GV, runs on AttributesConnection ────────
        public async Task<List<PropertySearchResult>> SearchAsync(
            PropertySearchParams p)
        {
            var sp = ResolveSp(GvConfig, p);
            var args = BuildParams(p);

            await using var conn = new SqlConnection(_attrConn);

            var results = await conn.QueryAsync<PropertySearchResult>(
                sp, args,
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }

        // ── Link property into Attributes DB ─────────────────────────────
        public async Task<LinkResult> LinkPropertyAsync(
            string idProperty,
            string userId,
            string propertyFrom)
        {
            try
            {
                await using var conn = new SqlConnection(_attrConn);

                await conn.ExecuteAsync(
                    "Attr_InsertLinkedProperty",
                    new
                    {
                        IDProperty = idProperty,
                        UserID = userId,
                        PropertyFrom = propertyFrom
                    },
                    commandType: CommandType.StoredProcedure);

                return LinkResult.Ok();
            }
            catch (SqlException ex)
                when (ex.Number == 2627 || ex.Number == 2601)
            {
                // Already linked — not an error, just inform the client
                return LinkResult.Duplicate();
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"Error linking property '{idProperty}' for user '{userId}' " +
                    "in Attributes DB.", ex);
            }
        }

        // ── SP selection — mirrors PropertySearchService.ResolveSp ───────
        private static string ResolveSp(RollSearchConfig cfg, PropertySearchParams p)
        {
            if (p.HasStand && !p.HasAddress && !p.HasScheme) return cfg.SpStand;
            if (p.HasStand && p.HasAddress && !p.HasScheme) return cfg.SpStandAddress;
            if (!p.HasStand && !p.HasAddress && p.HasScheme && !p.HasUnit) return cfg.SpScheme;
            if (!p.HasStand && p.HasAddress && !p.HasScheme) return cfg.SpAddress;
            if (!p.HasStand && !p.HasAddress && !p.HasScheme && p.HasUnit) return cfg.SpUnit;
            if (p.HasScheme && p.HasUnit) return cfg.SpSchemeUnit;
            if (p.HasStand && !p.HasAddress && p.HasScheme) return cfg.SpStandScheme;
            if (!p.HasStand && p.HasAddress && p.HasScheme) return cfg.SpAddressScheme;
            return cfg.SpTown;
        }

        // ── Dapper params — matches V1 wildcard pattern ───────────────────
        private static DynamicParameters BuildParams(PropertySearchParams p)
        {
            var dp = new DynamicParameters();
            dp.Add("@SearchTownName", $"%{p.TownName.Trim()}%");
            if (p.HasStand) dp.Add("@SearchStand", $"%{p.Stand!.Trim()}%");
            if (p.HasAddress) dp.Add("@SearchAddress", $"%{p.Address!.Trim()}%");
            if (p.HasScheme) dp.Add("@SearchScheme", $"%{p.Scheme!.Trim()}%");
            if (p.HasUnit) dp.Add("@SearchUnit", $"%{p.Unit!.Trim()}%");
            return dp;
        }
    }

}
