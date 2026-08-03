using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Models.ViewModels;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class AdminFormViewService : IAdminFormViewService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AdminFormViewService> _logger;
        private readonly QueryDbContext _queryDb;

        public AdminFormViewService(
            IConfiguration config,
            ILogger<AdminFormViewService> logger,
            QueryDbContext queryDb)
        {
            _config = config;
            _logger = logger;
            _queryDb = queryDb;

            // Allows DB column Owner_Name to map to C# property OwnerName
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public async Task<AdminFormViewResult> GetFormViewAsync(
            string referenceNo,
            string rollSource,
            string? propertyType,
            bool isAppeal,
            bool isQuery)
        {
            referenceNo = referenceNo?.Trim() ?? "";
            rollSource = NormalizeRollSource(rollSource);
            propertyType = NormalizePropertyType(propertyType);

            if (string.IsNullOrWhiteSpace(referenceNo))
            {
                return new AdminFormViewResult
                {
                    Success = false,
                    Error = "Reference number is required."
                };
            }

            if (isQuery || rollSource.Equals("Objection_Query", StringComparison.OrdinalIgnoreCase))
            {
                return await GetQueryFormViewAsync(referenceNo);
            }

            var connectionKey = GetConnectionKey(rollSource);
            var connectionString = _config.GetConnectionString(connectionKey);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return new AdminFormViewResult
                {
                    Success = false,
                    Error = $"Connection string not found for {rollSource}."
                };
            }

            var storedProcedure = GetStoredProcedure(propertyType, isAppeal);

            try
            {
                await using var conn = new SqlConnection(connectionString);

                var param = new DynamicParameters();

                if (isAppeal)
                    param.Add("@Appeal_No", referenceNo);
                else
                    param.Add("@Objection_no", referenceNo);

                var rows = (await conn.QueryAsync<ObjectionTB>(
                    storedProcedure,
                    param,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 120)).ToList();

                if (!rows.Any())
                {
                    return new AdminFormViewResult
                    {
                        Success = false,
                        Error = $"No submitted form was found for {referenceNo}."
                    };
                }

                return new AdminFormViewResult
                {
                    Success = true,
                    ReferenceNo = referenceNo,
                    RollSource = rollSource,
                    SourceTable = RollSourceToSourceTable(rollSource),
                    PropertyType = propertyType,
                    IsAppeal = isAppeal,
                    IsQuery = false,
                    PartialViewName = GetPartialViewName(propertyType),
                    Items = rows
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Admin form view failed. Ref={ReferenceNo}, Roll={RollSource}, PropertyType={PropertyType}, IsAppeal={IsAppeal}",
                    referenceNo,
                    rollSource,
                    propertyType,
                    isAppeal);

                return new AdminFormViewResult
                {
                    Success = false,
                    Error = "Could not load submitted form details."
                };
            }
        }

        private async Task<AdminFormViewResult> GetQueryFormViewAsync(string referenceNo)
        {
            try
            {
                var queryRow = await _queryDb.Que_Property_Info
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Query_No == referenceNo ||
                        x.Review_No == referenceNo);

                var section78Row = await _queryDb.Obj_Section2Query
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Objection_Ref_SQ == referenceNo ||
                        x.Review_No == referenceNo);

                var rows = queryRow == null
                    ? new List<ObjectionTB>()
                    : new List<ObjectionTB>
                    {
                        new()
                        {
                            ObjectionId = queryRow.Query_ID,
                            ObjectionNo = queryRow.Query_No ?? queryRow.Review_No,
                            ObjectorType = queryRow.Query_Type,
                            PropertyType = queryRow.Property_Type,
                            PropertyDesc = queryRow.Property_Desc,
                            PremiseId = queryRow.Premise_id,
                            UnitKey = queryRow.Unit_key,
                            PropertyId = queryRow.Property_id,
                            ValuationKey = queryRow.Valuation_Key,
                            Sector = queryRow.Sector,
                            objectionStatus = queryRow.Query_Status
                        }
                    };

                var section78 = section78Row == null
                    ? null
                    : new Section78FormViewModel
                    {
                        QueryNo = section78Row.Objection_Ref_SQ,
                        ReviewNo = section78Row.Review_No,
                        Option_A = IsSelected(section78Row.Option_A),
                        Option_B = IsSelected(section78Row.Option_B),
                        Option_C = IsSelected(section78Row.Option_C),
                        Option_D = IsSelected(section78Row.Option_D),
                        Option_E = IsSelected(section78Row.Option_E),
                        Option_F = IsSelected(section78Row.Option_F),
                        Option_G = IsSelected(section78Row.Option_G),
                        Option_H = IsSelected(section78Row.Option_H),
                        Motivation_for_Supp_Request = section78Row.Motivation_for_Supp_Request
                    };

                if (!rows.Any() && section78 == null)
                {
                    return new AdminFormViewResult
                    {
                        Success = false,
                        Error = $"No Section 78 query/review form was found for {referenceNo}."
                    };
                }

                return new AdminFormViewResult
                {
                    Success = true,
                    ReferenceNo = referenceNo,
                    RollSource = "Objection_Query",
                    SourceTable = "Query",
                    PropertyType = "Query",
                    IsQuery = true,
                    IsAppeal = false,
                    PartialViewName = "QueryForm",
                    Items = rows,
                    Section78 = section78
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin query form view failed. Ref={ReferenceNo}", referenceNo);

                return new AdminFormViewResult
                {
                    Success = false,
                    Error = "Could not load Section 78 form details."
                };
            }
        }

        private static bool IsSelected(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value.Trim();

            return normalized.Equals("true", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("y", StringComparison.OrdinalIgnoreCase)
                || normalized == "1"
                || normalized.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePropertyType(string? propertyType)
        {
            if (string.IsNullOrWhiteSpace(propertyType))
                return "Res";

            propertyType = propertyType.Trim();

            return propertyType switch
            {
                "Res_omission" => "Res",
                "Bus_omission" => "Bus",
                "Agric_omission" => "Agric",
                "Multi_omission" => "Multi",
                _ => propertyType
            };
        }

        private static string GetStoredProcedure(string propertyType, bool isAppeal)
        {
            return propertyType switch
            {
                "Res" => isAppeal ? "AppealFormViewRes" : "FormViewRes",
                "Bus" => isAppeal ? "AppealFormViewBus" : "FormViewBus",
                "Agric" => isAppeal ? "AppealFormViewAgri" : "FormViewAgri",
                "Multi" => isAppeal ? "AppealFormViewMult" : "FormViewMult",
                _ => isAppeal ? "AppealFormViewRes" : "FormViewRes"
            };
        }

        private static string GetPartialViewName(string propertyType)
        {
            return propertyType switch
            {
                "Res" => "ResForm",
                "Bus" => "BusForm",
                "Agric" => "AgricForm",
                "Multi" => "MultiForm",
                _ => "ResForm"
            };
        }

        private static string NormalizeRollSource(string? rollSource)
        {
            if (string.IsNullOrWhiteSpace(rollSource))
                return "Objection";

            return rollSource.Trim() switch
            {
                "GV23" => "Objection",
                "GV23-SUP1" => "Objection_Supp1",
                "GV23-SUP2" => "Objection_Supp2",
                "GV23-SUP3" => "Objection_Supp3",
                "GV23-SUP4" => "Objection_Supp4",
                "GV23-SUP5" => "Objection_Supp5",
                "Query" => "Objection_Query",
                _ => rollSource.Trim()
            };
        }

        private static string GetConnectionKey(string rollSource)
        {
            return rollSource switch
            {
                "Objection" => "DefaultConnection",
                "Objection_Supp1" => "Sup1Connection",
                "Objection_Supp2" => "Sup2Connection",
                "Objection_Supp3" => "Sup3Connection",
                "Objection_Supp4" => "Sup4Connection",
                "Objection_Supp5" => "Sup5Connection",
                "Objection_Query" => "QueryConnection",
                _ => "DefaultConnection"
            };
        }

        private static string RollSourceToSourceTable(string rollSource)
        {
            return rollSource switch
            {
                "Objection" => "GV23",
                "Objection_Supp1" => "GV23-SUP1",
                "Objection_Supp2" => "GV23-SUP2",
                "Objection_Supp3" => "GV23-SUP3",
                "Objection_Supp4" => "GV23-SUP4",
                "Objection_Supp5" => "GV23-SUP5",
                "Objection_Query" => "Query",
                _ => rollSource
            };
        }
    }
}
