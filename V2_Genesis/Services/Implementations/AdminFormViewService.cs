using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
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

        public AdminFormViewService(
            IConfiguration config,
            ILogger<AdminFormViewService> logger)
        {
            _config = config;
            _logger = logger;

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
            var connectionString =
                _config.GetConnectionString("QueryConnection")
                ?? _config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return new AdminFormViewResult
                {
                    Success = false,
                    Error = "Query connection string was not found."
                };
            }

            try
            {
                await using var conn = new SqlConnection(connectionString);

                /*
                 Adjust table/columns here if your Section 78 table names are different.
                 This is intentionally separate from objection/appeal form view.
                */
                var section78 = await conn.QueryFirstOrDefaultAsync<Section78FormViewModel>(
                    """
                    SELECT TOP 1
                      [ID]
                        ,[Ref]
                        ,[Objection_Ref_SQ]
                        ,[Option_A]
                        ,[Option_B]
                        ,[Option_C]
                        ,[Option_D]
                        ,[Option_E]
                        ,[Option_F]
                        ,[Option_G]
                        ,[Option_H]
                        ,[Motivation_for_Supp_Request]
                    FROM [Objection_Query].[dbo].[Obj_Section2Query]
                    WHERE Objection_Ref_SQ = @ReferenceNo
                       OR Review_No = @ReferenceNo
                    """,
                    new { ReferenceNo = referenceNo },
                    commandTimeout: 120);

                var rows = (await conn.QueryAsync<ObjectionTB>(
                    """
                    SELECT TOP 1 *
                    FROM dbo.Que_Property_Info
                    WHERE Query_No = @ReferenceNo
                       OR Review_No = @ReferenceNo
                    """,
                    new { ReferenceNo = referenceNo },
                    commandTimeout: 120)).ToList();

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