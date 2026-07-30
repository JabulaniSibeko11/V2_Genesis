using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.ViewModels.Submissions;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public sealed class SubmissionViewService : ISubmissionViewService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SubmissionViewService> _logger;
        private readonly AttributesDbContext _attributesDb;
        private readonly IAttributeSubmissionService _attributeSubmissionService;

        private static readonly HashSet<string> HiddenFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "UserID", "A_UserID", "Password", "Pin", "Section51Pin",
        "Objection_ID", "Appeal_ID", "Query_ID", "ID", "Id",
        "CreatedBy", "UpdatedBy"
    };

        private static readonly Dictionary<string, string> LabelOverrides =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Objection_No"] = "Objection reference",
                ["Appeal_No"] = "Appeal reference",
                ["Query_No"] = "Query / Review reference",
                ["Obj_Ref"] = "Objection reference",
                ["Property_Desc"] = "Property description",
                ["A_Property_Desc"] = "Property description",
                ["Premise_id"] = "Premise ID",
                ["A_Premise_id"] = "Premise ID",
                ["Property_id"] = "Property ID",
                ["A_Property_id"] = "Property ID",
                ["Unit_key"] = "Unit key",
                ["A_Unit_key"] = "Unit key",
                ["Valuation_Key"] = "Valuation key",
                ["A_Valuation_Key"] = "Valuation key",
                ["Property_Type"] = "Property type",
                ["A_Property_Type"] = "Property type",
                ["Objection_Status"] = "Status",
                ["Appeal_Status"] = "Status",
                ["Query_Status"] = "Status",
                ["Objector_Type"] = "Applicant type",
                ["Owner_Name"] = "Owner name",
                ["Owner_Email"] = "Owner email",
                ["Representative_name"] = "Representative name",
                ["Representative_Email"] = "Representative email",
                ["Motivation_for_Supp_Request"] = "Motivation for request",
                ["Objection_Reasons"] = "Reasons",
                ["Old_Market_Value"] = "Current market value",
                ["New_Market_Value"] = "Requested market value",
                ["Old_Category"] = "Current category",
                ["New_Category"] = "Requested category",
                ["Old_Extent"] = "Current extent",
                ["New_Extent"] = "Requested extent"
            };

        public SubmissionViewService(
            IConfiguration config,
            ILogger<SubmissionViewService> logger, AttributesDbContext attributesDb, IAttributeSubmissionService attributeSubmissionService)
        {
            _config = config;
            _logger = logger;
            _attributesDb = attributesDb;
            _attributeSubmissionService = attributeSubmissionService;
        }

        public async Task<SubmissionViewResult> GetSubmissionAsync(
            string submissionType,
            string referenceNumber,
            string rollSource,
            string userId,
            CancellationToken cancellationToken = default,
            bool allowAdministrativeAccess = false)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber))
                return SubmissionViewResult.Fail("The submission reference number is required.");

            if (string.IsNullOrWhiteSpace(userId))
                return SubmissionViewResult.Fail("The logged-in client could not be identified.");

            var type = NormalizeSubmissionType(submissionType, referenceNumber);

            try
            {
                var cleanReference = referenceNumber.Trim();

                return type switch
                {
                    "Attribute" =>
                        await LoadAttributeAsync(
                            cleanReference,
                            userId,
                            cancellationToken),

                    "Query" or "Review" =>
                        await LoadSection78Async(
                            type,
                            cleanReference,
                            userId,
                            cancellationToken),

                    "Objection" or "Appeal" =>
                          await LoadObjectionOrAppealAsync(
        type,
        cleanReference,
        NormalizeRollSource(rollSource),
        userId,
        cancellationToken,
        allowAdministrativeAccess),

                    _ =>
                        SubmissionViewResult.Fail(
                            $"Unsupported submission type '{submissionType}'.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load submitted {SubmissionType} {ReferenceNumber} from {RollSource}.",
                    type,
                    referenceNumber,
                    type == "Attribute"
                        ? "Attributes"
                        : rollSource);

                return SubmissionViewResult.Fail(
                    "The submitted form could not be loaded. Please try again or contact Valuation Services.");
            }
        }

        private async Task<SubmissionViewResult> LoadAttributeAsync(
            string referenceNumber,
            string userId,
            CancellationToken cancellationToken)
        {
            referenceNumber = referenceNumber.Trim();
            userId = userId.Trim();

            var info = await _attributesDb.AttrPropertyInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Attr_No != null
                        && x.Attr_No.Trim() == referenceNumber
                        && x.SubmittedByUserId != null
                        && x.SubmittedByUserId.Trim() == userId
                        && x.IsActive,
                    cancellationToken);

            if (info is null)
            {
                return SubmissionViewResult.Fail(
                    $"Attribute submission {referenceNumber} was not found " +
                    "or does not belong to your account.");
            }

            var attribute =
                await _attributeSubmissionService.GetSubmittedViewAsync(
                    referenceNumber,
                    userId,
                    cancellationToken);

            if (attribute is null)
            {
                return SubmissionViewResult.Fail(
                    $"Attribute submission {referenceNumber} could not be loaded.");
            }

            var files = await _attributesDb.AttrFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Attr_ID == info.Attr_ID && x.IsActive,
                    cancellationToken);

            var documents = new List<SubmissionDocumentViewModel>();

            if (files is not null)
            {
                AddAttributeDocument(documents, files.Files1, files.RootFolder);
                AddAttributeDocument(documents, files.Files2, files.RootFolder);
                AddAttributeDocument(documents, files.Files3, files.RootFolder);
                AddAttributeDocument(documents, files.Files4, files.RootFolder);
                AddAttributeDocument(documents, files.Files5, files.RootFolder);
                AddAttributeDocument(documents, files.Files6, files.RootFolder);
                AddAttributeDocument(documents, files.Files7, files.RootFolder);
                AddAttributeDocument(documents, files.Files8, files.RootFolder);
                AddAttributeDocument(documents, files.Files9, files.RootFolder);
                AddAttributeDocument(documents, files.Files10, files.RootFolder);
            }

            var property = attribute.PropertyDetails;
            var valuation = attribute.ValuationDetails;
            var owner = attribute.ContactInfos
                .FirstOrDefault(x =>
                    string.Equals(
                        x.ContactType,
                        "Owner",
                        StringComparison.OrdinalIgnoreCase))
                ?? attribute.ContactInfos.FirstOrDefault();

            var model = new SubmissionViewModel
            {
                SubmissionType = "Attribute",
                ReferenceNumber = referenceNumber,
                Status = info.Attr_Status,
                RollSource = "Attributes",
                RollDisplayName =
                    property.RollDescription
                    ?? property.RollType
                    ?? "Property Attributes",
                FormType = attribute.FormType,
                PropertyDescription =
                    property.PropertyDesc
                    ?? info.Property_Desc
                    ?? string.Empty,
                PropertyKey =
                    property.PremiseId
                    ?? property.UnitKey
                    ?? property.PropertyId
                    ?? string.Empty,
                SubmittedAt = info.SubmissionDateTime,
                Attribute = attribute,
                Documents = documents,

                Property = new SubmissionPropertyViewModel
                {
                    PropertyDescription =
                        property.PropertyDesc
                        ?? info.Property_Desc
                        ?? string.Empty,
                    PropertyType = attribute.FormType,
                    PremiseId = property.PremiseId ?? string.Empty,
                    PropertyId = property.PropertyId ?? string.Empty,
                    UnitKey = property.UnitKey ?? string.Empty,
                    ValuationKey = property.ValuationKey ?? string.Empty,
                    Address = property.Address ?? string.Empty,
                    Township = property.Township ?? string.Empty,
                    Erf = property.Erf ?? string.Empty,
                    Sector = property.Sector ?? string.Empty,
                    Category =
                        valuation.ValuationCategoryOnRoll
                        ?? string.Empty,
                    Extent = property.Extent ?? string.Empty,
                    MarketValue =
                        attribute.Calculations.DRCFinalValue?.ToString("N0")
                        ?? attribute.Calculations.TotalValueNonRes?.ToString("N0")
                        ?? attribute.Calculations.Tla?.ToString("N0")
                        ?? string.Empty,
                    OwnerName = owner is null
                        ? string.Empty
                        : owner.IsCompany
                            ? owner.CompanyName ?? string.Empty
                            : string.Join(
                                " ",
                                new[]
                                {
                                    owner.FirstNames,
                                    owner.LastName
                                }.Where(x =>
                                    !string.IsNullOrWhiteSpace(x)))
                }
            };

            return SubmissionViewResult.Ok(model);
        }

        private static void AddAttributeDocument(
            ICollection<SubmissionDocumentViewModel> documents,
            string? fileName,
            string? rootFolder)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            documents.Add(new SubmissionDocumentViewModel
            {
                FileName = fileName,
                DisplayName = Path.GetFileName(fileName),
                FilePath = string.IsNullOrWhiteSpace(rootFolder)
                    ? fileName
                    : Path.Combine(rootFolder, fileName),
                Exists = true
            });
        }

        private async Task<SubmissionViewResult> LoadObjectionOrAppealAsync(
            string submissionType,
            string referenceNumber,
            string rollSource,
            string userId,
            CancellationToken cancellationToken,
            bool allowAdministrativeAccess)
        {
            var isAppeal =
                submissionType.Equals(
                    "Appeal",
                    StringComparison.OrdinalIgnoreCase);

            var connectionString =
                GetConnectionStringForRoll(rollSource);

            await using var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            var rawPropertyType =
                await ResolvePropertyTypeAsync(
                    connection,
                    referenceNumber,
                    isAppeal);

            var formType =
                NormalizePropertyType(rawPropertyType);

            var procedure =
                ResolveFormProcedure(formType);

            var command = new CommandDefinition(
                procedure,
                new
                {
                    InquiryType = isAppeal
                        ? "Appeal"
                        : "Objection",

                    RefNo = referenceNumber
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60,
                cancellationToken: cancellationToken);

            using var grid =
                await connection.QueryMultipleAsync(command);

            var resultSets = new List<object>();

            var main =
                (await grid.ReadAsync())
                .FirstOrDefault();

            if (main is null)
            {
                return SubmissionViewResult.Fail(
                    $"{submissionType} {referenceNumber} was not found.");
            }

            resultSets.Add(main);

            if (!allowAdministrativeAccess
                && !BelongsToUser(main, userId, isAppeal))
            {
                return SubmissionViewResult.Fail(
                    "This submission does not belong to your account.");
            }

            while (!grid.IsConsumed)
            {
                var row =
                    (await grid.ReadAsync())
                    .FirstOrDefault();

                if (row is not null)
                    resultSets.Add(row);
            }

            var section1 =
                resultSets.ElementAtOrDefault(1);

            var section2 =
                resultSets.ElementAtOrDefault(2);

            var section6 =
                FindBestResultSet(
                    resultSets,
                    "Objection_Reasons",
                    "Old_Market_Value",
                    "New_Market_Value",
                    "Old_Category",
                    "New_Category");

            var model = new SubmissionViewModel
            {
                SubmissionType = submissionType,
                ReferenceNumber = referenceNumber,
                RollSource = rollSource,
                RollDisplayName =
                    GetRollDisplayName(rollSource),
                FormType = formType,

                Status = FirstValue(
                    main,
                    "Appeal_Status",
                    "Objection_Status",
                    "Status"),

                PropertyDescription = FirstValue(
                    main,
                    "A_Property_Desc",
                    "Property_Desc",
                    "PropertyDesc"),

                PropertyKey = FirstValue(
                    main,
                    "A_Premise_id",
                    "Premise_id",
                    "PremiseId",
                    "A_Unit_key",
                    "Unit_key"),

                SubmittedAt = FirstDate(
                    main,
                    "Objection_Start_DateTime",
                    "Appeal_Start_DateTime",
                    "Date_Submitted",
                    "CreatedAt"),

                Property = BuildProperty(
                    resultSets.ToArray()),

                Applicant = BuildApplicant(
                    main,
                    section1,
                    section2),

                CurrentValuation = BuildCurrentValuation(
                    main,
                    section6),

                RequestedValuation = BuildRequestedValuation(
                    main,
                    section6),

                MultiPurposeLines = BuildMultiPurposeLines(
                    resultSets.ToArray()),

                Reasons = BuildReasons(
                    main,
                    section6),

                Documents = BuildDocuments(
                    referenceNumber,
                    resultSets)
            };

            if (isAppeal)
            {
                model.Appeal =
                    BuildAppeal(
                        main,
                        section6);
            }

            model.Sections.Add(
                BuildSection(
                    "Main",
                    "Submission and property details",
                    0,
                    main));

            for (var index = 1;
                 index < resultSets.Count;
                 index++)
            {
                model.Sections.Add(
                    BuildSection(
                        $"Section{index}",
                        ResolveSectionTitle(
                            submissionType,
                            formType,
                            index),
                        index,
                        resultSets[index]));
            }

            RemoveEmptySections(model);

            HydrateTypedModelsFromSections(model);

            model.FormSections =
                BuildFormSections(model);

            return SubmissionViewResult.Ok(model);
        }

        private async Task<SubmissionViewResult> LoadSection78Async(
            string submissionType,
            string referenceNumber,
            string userId,
            CancellationToken cancellationToken)
        {
            var connectionString =
                _config.GetConnectionString("QueryConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'QueryConnection' was not found.");

            await using var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync(cancellationToken);

            var command = new CommandDefinition(
                "dbo.Section78_GetSubmittedFormData",
                new
                {
                    QueryRef = referenceNumber
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: 60,
                cancellationToken: cancellationToken);

            using var grid =
                await connection.QueryMultipleAsync(command);

            var main =
                await grid.ReadFirstOrDefaultAsync<
                    Que_Property_InfoModel>();

            if (main is null)
            {
                return SubmissionViewResult.Fail(
                    $"{submissionType} {referenceNumber} was not found.");
            }

            if (!string.Equals(
                    main.UserID?.Trim(),
                    userId.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                return SubmissionViewResult.Fail(
                    "This submission does not belong to your account.");
            }

            var section1 =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section1Model>()
                ?? new Obj_Section1Model();

            var section2 =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section2Model>()
                ?? new Obj_Section2Model();

            var section2Query =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section2QueryModel>()
                ?? new Obj_Section2QueryModel();

            var section3Res =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section3ResModel>()
                ?? new Obj_Section3ResModel();

            var section3Bus =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section3BusModel>()
                ?? new Obj_Section3BusModel();

            var section3Agri =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section3AgriModel>()
                ?? new Obj_Section3AgriModel();

            var section4Res =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section4ResModel>()
                ?? new Obj_Section4ResModel();

            var section4Bus =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section4BusModel>()
                ?? new Obj_Section4BusModel();

            var section5 =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section5Model>()
                ?? new Obj_Section5Model();

            var section6 =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section6Model>()
                ?? new Obj_Section6Model();

            var section7 =
                await grid.ReadFirstOrDefaultAsync<
                    Obj_Section7Model>()
                ?? new Obj_Section7Model();

            var resultSets = new List<object>
            {
                main,
                section1,
                section2,
                section2Query,
                section3Res,
                section3Bus,
                section3Agri,
                section4Res,
                section4Bus,
                section5,
                section6,
                section7
            };

            var sectionObjects =
                new List<(string Key, string Title, object Value)>
                {
                    (
                        "Section1",
                        "Applicant and owner details",
                        section1
                    ),
                    (
                        "Section2",
                        "Representative and contact details",
                        section2
                    ),
                    (
                        "Section2Query",
                        "Section 78 request",
                        section2Query
                    ),
                    (
                        "Section3Res",
                        "Residential property details",
                        section3Res
                    ),
                    (
                        "Section3Bus",
                        "Business property details",
                        section3Bus
                    ),
                    (
                        "Section3Agri",
                        "Agricultural property details",
                        section3Agri
                    ),
                    (
                        "Section4Res",
                        "Residential improvements",
                        section4Res
                    ),
                    (
                        "Section4Bus",
                        "Business improvements",
                        section4Bus
                    ),
                    (
                        "Section5",
                        "Additional information",
                        section5
                    ),
                    (
                        "Section6",
                        "Current and requested valuation details",
                        section6
                    ),
                    (
                        "Section7",
                        "Declaration",
                        section7
                    )
                };

            var isReview =
                main.Sub_typ == 1
                || referenceNumber.EndsWith(
                    "-R",
                    StringComparison.OrdinalIgnoreCase);

            var resolvedType =
                isReview
                    ? "Review"
                    : "Query";

            var model = new SubmissionViewModel
            {
                SubmissionType = resolvedType,
                ReferenceNumber = referenceNumber,
                RollSource = "Objection_Query",
                RollDisplayName = "Section 78",
                FormType =
                    NormalizePropertyType(
                        main.Property_Type),

                Status = FirstPropertyValue(
                    main,
                    "Query_Status",
                    "Status"),

                PropertyDescription =
                    main.Property_Desc?.Trim()
                    ?? string.Empty,

                PropertyKey = FirstNonEmpty(
                    main.Premise_id,
                    main.Unit_key,
                    main.Valuation_Key),

                SubmittedAt = FirstPropertyDate(
                    main,
                    "Query_Start_DateTime",
                    "Date_Submitted",
                    "CreatedAt"),

                Property = BuildProperty(
                    resultSets.ToArray()),

                Applicant = BuildApplicant(
                    main,
                    section1,
                    section2),

                CurrentValuation = BuildCurrentValuation(
                    main,
                    section6),

                RequestedValuation = BuildRequestedValuation(
                    main,
                    section6),

                MultiPurposeLines = BuildMultiPurposeLines(
                    resultSets.ToArray()),

                Reasons = BuildReasons(
                    section2Query,
                    section6,
                    main),

                Documents = BuildDocuments(
                    referenceNumber,
                    resultSets)
            };

            model.Sections.Add(
                BuildSection(
                    "Main",
                    "Submission and property details",
                    0,
                    main));

            var order = 1;

            foreach (var item in sectionObjects)
            {
                model.Sections.Add(
                    BuildSection(
                        item.Key,
                        item.Title,
                        order++,
                        item.Value));
            }

            RemoveEmptySections(model);

            HydrateTypedModelsFromSections(model);

            model.FormSections =
                BuildFormSections(model);

            return SubmissionViewResult.Ok(model);
        }

        private static SubmissionPropertyViewModel BuildProperty(
            params object?[] sources)
        {
            var addressParts = new[]
            {
                FirstValueFromSources(sources, "ADDR1", "Address1", "Street_Address", "LisStreetAddress"),
                FirstValueFromSources(sources, "ADDR2", "Address2"),
                FirstValueFromSources(sources, "ADDR3", "Address3"),
                FirstValueFromSources(sources, "ADDR4", "Address4"),
                FirstValueFromSources(sources, "ADDR5", "Address5")
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase);

            return new SubmissionPropertyViewModel
            {
                PropertyDescription = FirstValueFromSources(
                    sources,
                    "A_Property_Desc",
                    "Property_Desc",
                    "PropertyDesc"),

                PropertyType = FirstValueFromSources(
                    sources,
                    "A_Property_Type",
                    "Property_Type",
                    "PropertyType"),

                PremiseId = FirstValueFromSources(
                    sources,
                    "A_Premise_id",
                    "Premise_id",
                    "PremiseId"),

                PropertyId = FirstValueFromSources(
                    sources,
                    "A_Property_id",
                    "Property_id",
                    "PropertyId"),

                UnitKey = FirstValueFromSources(
                    sources,
                    "A_Unit_key",
                    "Unit_key",
                    "UnitKey"),

                ValuationKey = FirstValueFromSources(
                    sources,
                    "A_Valuation_Key",
                    "Valuation_Key",
                    "ValuationKey"),

                Address = string.Join(", ", addressParts),

                Township = FirstValueFromSources(
                    sources,
                    "TownNameDesc",
                    "Township",
                    "Town_Name"),

                Erf = FirstValueFromSources(
                    sources,
                    "ERF",
                    "Erf",
                    "Stand_No",
                    "StandNo"),

                Sector = FirstValueFromSources(
                    sources,
                    "A_Sector",
                    "Sector"),

                Category = FirstValueFromSources(
                    sources,
                    "CatDesc",
                    "Category",
                    "Old_Category",
                    "GV_Category"),

                Extent = FirstValueFromSources(
                    sources,
                    "RateableArea",
                    "Extent",
                    "Old_Extent",
                    "GV_Extent"),

                MarketValue = FirstValueFromSources(
                    sources,
                    "MarketValue",
                    "Market_Value",
                    "Old_Market_Value",
                    "GV_Market_Value"),

                OwnerName = FirstValueFromSources(
                    sources,
                    "Owner_Name",
                    "OwnerName")
            };
        }

        private static SubmissionValuationViewModel BuildCurrentValuation(
            params object?[] sources)
        {
            return new SubmissionValuationViewModel
            {
                PropertyDescription = FirstValueFromSources(
                    sources,
                    "Old_Property_Description",
                    "Old_Property_Desc",
                    "Property_Desc",
                    "A_Property_Desc"),

                Category = FirstValueFromSources(
                    sources,
                    "Old_Category",
                    "GV_Category",
                    "Category"),

                Address = FirstValueFromSources(
                    sources,
                    "Old_Address",
                    "GV_Address",
                    "LisStreetAddress"),

                Extent = FirstValueFromSources(
                    sources,
                    "Old_Extent",
                    "GV_Extent",
                    "RateableArea"),

                MarketValue = FirstValueFromSources(
                    sources,
                    "Old_Market_Value",
                    "GV_Market_Value",
                    "MarketValue"),

                Owner = FirstValueFromSources(
                    sources,
                    "Old_Owner",
                    "Owner_Name",
                    "OwnerName")
            };
        }

        private static SubmissionValuationViewModel BuildRequestedValuation(
            params object?[] sources)
        {
            return new SubmissionValuationViewModel
            {
                PropertyDescription = FirstValueFromSources(
                    sources,
                    "new_Property_Description",
                    "New_Property_Description",
                    "New_Property_Desc"),

                Category = FirstValueFromSources(
                    sources,
                    "new_Category",
                    "New_Category",
                    "Requested_Category"),

                Address = FirstValueFromSources(
                    sources,
                    "new_Address",
                    "New_Address",
                    "Requested_Address"),

                Extent = FirstValueFromSources(
                    sources,
                    "new_Extent",
                    "New_Extent",
                    "Requested_Extent"),

                MarketValue = FirstValueFromSources(
                    sources,
                    "new_Market_Value",
                    "New_Market_Value",
                    "Requested_Market_Value"),

                Owner = FirstValueFromSources(
                    sources,
                    "new_Owner",
                    "New_Owner",
                    "Requested_Owner")
            };
        }

        private static List<MultiPurposeLineViewModel> BuildMultiPurposeLines(
            params object?[] sources)
        {
            var lines = new List<MultiPurposeLineViewModel>();

            for (var index = 1; index <= 10; index++)
            {
                var suffix = index == 1
                    ? string.Empty
                    : index.ToString(CultureInfo.InvariantCulture);

                var line = new MultiPurposeLineViewModel
                {
                    LineNumber = index,

                    CurrentCategory = FirstValueFromSources(
                        sources,
                        $"Old{suffix}_Category",
                        $"Old_Category{suffix}",
                        $"GV_Category{suffix}"),

                    CurrentExtent = FirstValueFromSources(
                        sources,
                        $"Old{suffix}_Extent",
                        $"Old_Extent{suffix}",
                        $"GV_Extent{suffix}"),

                    CurrentMarketValue = FirstValueFromSources(
                        sources,
                        $"Old{suffix}_Market_Value",
                        $"Old_Market_Value{suffix}",
                        $"GV_Market_Value{suffix}"),

                    RequestedCategory = FirstValueFromSources(
                        sources,
                        $"new{suffix}_Category",
                        $"New{suffix}_Category",
                        $"New_Category{suffix}"),

                    RequestedExtent = FirstValueFromSources(
                        sources,
                        $"new{suffix}_Extent",
                        $"New{suffix}_Extent",
                        $"New_Extent{suffix}"),

                    RequestedMarketValue = FirstValueFromSources(
                        sources,
                        $"new{suffix}_Market_Value",
                        $"New{suffix}_Market_Value",
                        $"New_Market_Value{suffix}"),

                    Remarks = FirstValueFromSources(
                        sources,
                        $"Remarks{suffix}",
                        $"GV_Remarks{suffix}",
                        $"New_Remarks{suffix}")
                };

                if (line.HasValues)
                    lines.Add(line);
            }

            return lines;
        }

        private static SubmissionApplicantViewModel BuildApplicant(
            params object?[] sources)
        {
            return new SubmissionApplicantViewModel
            {
                ObjectorType = FirstValueFromSources(
                    sources,
                    "Objector_Type",
                    "Applicant_Type",
                    "Query_Type"),

                ApplicantName = FirstValueFromSources(
                    sources,
                    "Objector_Name",
                    "Applicant_Name",
                    "First_Name",
                    "Name"),

                ApplicantSurname = FirstValueFromSources(
                    sources,
                    "Objector_Surname",
                    "Applicant_Surname",
                    "Surname"),

                ApplicantIdNumber = FirstValueFromSources(
                    sources,
                    "Objector_ID",
                    "Applicant_ID",
                    "ID_Number"),

                ApplicantCompanyRegistrationNumber =
                    FirstValueFromSources(
                        sources,
                        "Objector_Company_Reg_No",
                        "Applicant_Company_Reg_No",
                        "Company_Registration_Number"),

                ApplicantEmail = FirstValueFromSources(
                    sources,
                    "Objector_Email",
                    "Applicant_Email",
                    "Email"),

                ApplicantTelephone = FirstValueFromSources(
                    sources,
                    "Objector_Telephone",
                    "Applicant_Telephone",
                    "Telephone"),

                ApplicantCellphone = FirstValueFromSources(
                    sources,
                    "Objector_Cell",
                    "Applicant_Cellphone",
                    "Cellphone",
                    "Cell_Number"),

                ApplicantAddress1 = FirstValueFromSources(
                    sources,
                    "Objector_Address1",
                    "Applicant_Address1",
                    "Address1"),

                ApplicantAddress2 = FirstValueFromSources(
                    sources,
                    "Objector_Address2",
                    "Applicant_Address2",
                    "Address2"),

                ApplicantAddress3 = FirstValueFromSources(
                    sources,
                    "Objector_Address3",
                    "Applicant_Address3",
                    "Address3"),

                ApplicantAddress4 = FirstValueFromSources(
                    sources,
                    "Objector_Address4",
                    "Applicant_Address4",
                    "Address4"),

                ApplicantPostalCode = FirstValueFromSources(
                    sources,
                    "Objector_Postal_Code",
                    "Applicant_Postal_Code",
                    "Postal_Code"),

                OwnerName = FirstValueFromSources(
                    sources,
                    "Owner_Name",
                    "Owner_First_Name"),

                OwnerSurname = FirstValueFromSources(
                    sources,
                    "Owner_Surname"),

                OwnerIdNumber = FirstValueFromSources(
                    sources,
                    "Owner_ID",
                    "Owner_ID_Number"),

                OwnerEmail = FirstValueFromSources(
                    sources,
                    "Owner_Email"),

                OwnerTelephone = FirstValueFromSources(
                    sources,
                    "Owner_Telephone",
                    "Owner_Tel"),

                OwnerCellphone = FirstValueFromSources(
                    sources,
                    "Owner_Cellphone",
                    "Owner_Cell"),

                RepresentativeName = FirstValueFromSources(
                    sources,
                    "Representative_name",
                    "Representative_Name",
                    "Rep_Name"),

                RepresentativeSurname =
                    FirstValueFromSources(
                        sources,
                        "Representative_Surname",
                        "Rep_Surname"),

                RepresentativeIdNumber =
                    FirstValueFromSources(
                        sources,
                        "Representative_ID",
                        "Representative_ID_Number",
                        "Rep_ID"),

                RepresentativeCompanyName =
                    FirstValueFromSources(
                        sources,
                        "Representative_Company",
                        "Representative_Company_Name",
                        "Rep_Company"),

                RepresentativeCompanyRegistrationNumber =
                    FirstValueFromSources(
                        sources,
                        "Representative_Company_Reg_No",
                        "Rep_Company_Reg_No"),

                RepresentativeEmail =
                    FirstValueFromSources(
                        sources,
                        "Representative_Email",
                        "Rep_Email"),

                RepresentativeTelephone =
                    FirstValueFromSources(
                        sources,
                        "Representative_Telephone",
                        "Representative_Tel",
                        "Rep_Tel"),

                RepresentativeCellphone =
                    FirstValueFromSources(
                        sources,
                        "Representative_Cellphone",
                        "Representative_Cell",
                        "Rep_Cell"),

                RepresentativeAddress1 =
                    FirstValueFromSources(
                        sources,
                        "Representative_Address1",
                        "Rep_Address1"),

                RepresentativeAddress2 =
                    FirstValueFromSources(
                        sources,
                        "Representative_Address2",
                        "Rep_Address2"),

                RepresentativeAddress3 =
                    FirstValueFromSources(
                        sources,
                        "Representative_Address3",
                        "Rep_Address3"),

                RepresentativeAddress4 =
                    FirstValueFromSources(
                        sources,
                        "Representative_Address4",
                        "Rep_Address4"),

                RepresentativePostalCode =
                    FirstValueFromSources(
                        sources,
                        "Representative_Postal_Code",
                        "Rep_Postal_Code"),

                Capacity = FirstValueFromSources(
                    sources,
                    "Capacity",
                    "Representative_Capacity")
            };
        }

        private static SubmissionReasonViewModel BuildReasons(
            params object?[] sources)
        {
            return new SubmissionReasonViewModel
            {
                PrimaryReason = FirstValueFromSources(
                    sources,
                    "Objection_Reasons",
                    "Query_Reason",
                    "Review_Reason",
                    "Reason"),

                AdditionalReason = FirstValueFromSources(
                    sources,
                    "Additional_Reason",
                    "Additional_Reasons"),

                Motivation = FirstValueFromSources(
                    sources,
                    "Motivation_for_Supp_Request",
                    "Motivation",
                    "Query_Motivation",
                    "Review_Motivation"),

                RequestedOutcome = FirstValueFromSources(
                    sources,
                    "Requested_Outcome",
                    "Appellant_Request",
                    "Requested_Decision"),

                ValuationReason = FirstValueFromSources(
                    sources,
                    "Valuation_Reason",
                    "Market_Value_Reason"),

                PropertyDescriptionReason =
                    FirstValueFromSources(
                        sources,
                        "Property_Description_Reason",
                        "Property_Desc_Reason"),

                CategoryReason = FirstValueFromSources(
                    sources,
                    "Category_Reason"),

                ExtentReason = FirstValueFromSources(
                    sources,
                    "Extent_Reason"),

                MarketValueReason = FirstValueFromSources(
                    sources,
                    "Market_Value_Reason",
                    "Value_Reason"),

                OwnerReason = FirstValueFromSources(
                    sources,
                    "Owner_Reason"),

                AddressReason = FirstValueFromSources(
                    sources,
                    "Address_Reason"),

                OtherReason = FirstValueFromSources(
                    sources,
                    "Other_Reason",
                    "Other_Reasons",
                    "Remarks",
                    "Comments")
            };
        }

        private static AppealSubmissionViewModel BuildAppeal(
            params object?[] sources)
        {
            return new AppealSubmissionViewModel
            {
                AppealNumber = FirstValueFromSources(
                    sources,
                    "Appeal_No"),

                ObjectionNumber = FirstValueFromSources(
                    sources,
                    "Obj_Ref",
                    "Objection_No"),

                AppealType = FirstValueFromSources(
                    sources,
                    "Appeal_Type"),

                AppealStatus = FirstValueFromSources(
                    sources,
                    "Appeal_Status"),

                AppealDate = FirstDateFromSources(
                    sources,
                    "Appeal_Start_DateTime",
                    "Appeal_Date",
                    "Date_Submitted"),

                ObjectionOutcome = FirstValueFromSources(
                    sources,
                    "Objection_Outcome",
                    "Decision_Being_Appealed"),

                AppellantRequest = FirstValueFromSources(
                    sources,
                    "Appellant_Request",
                    "Requested_Outcome"),

                GroundsOfAppeal = FirstValueFromSources(
                    sources,
                    "Grounds_Of_Appeal",
                    "Appeal_Grounds",
                    "Objection_Reasons"),

                DecisionBeingAppealed =
                    FirstValueFromSources(
                        sources,
                        "Decision_Being_Appealed",
                        "Objection_Decision"),

                RequestedDecision = FirstValueFromSources(
                    sources,
                    "Requested_Decision",
                    "Appellant_Request"),

                AppealMotivation = FirstValueFromSources(
                    sources,
                    "Appeal_Motivation",
                    "Motivation"),

                BoardStatus = FirstValueFromSources(
                    sources,
                    "Board_Status"),

                HearingDate = FirstDateFromSources(
                    sources,
                    "Schedule_Date",
                    "Hearing_Date"),

                AppealBoard = FirstValueFromSources(
                    sources,
                    "Appeal_Board"),

                Chairperson = FirstValueFromSources(
                    sources,
                    "Chair_Person",
                    "Chairperson"),

                BoardMember1 = FirstValueFromSources(
                    sources,
                    "Board_Member1"),

                BoardMember2 = FirstValueFromSources(
                    sources,
                    "Board_Member2"),

                ExternalValuer = FirstValueFromSources(
                    sources,
                    "External_Valuer"),

                Decision = FirstValueFromSources(
                    sources,
                    "Decision",
                    "Appeal_Decision"),

                DecisionComment = FirstValueFromSources(
                    sources,
                    "DecisionComment",
                    "Decision_Comment"),

                DecisionDate = FirstDateFromSources(
                    sources,
                    "DecisionDate",
                    "Decision_Date")
            };
        }

        private static List<SubmissionDocumentViewModel> BuildDocuments(
            string referenceNumber,
            IEnumerable<object> sources)
        {
            var documents =
                new List<SubmissionDocumentViewModel>();

            var seen =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var source in sources)
            {
                foreach (var item in ToDictionary(source))
                {
                    if (!item.Key.Contains(
                            "File",
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        !item.Key.Contains(
                            "Evidence",
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        !item.Key.Contains(
                            "Document",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value =
                        Convert.ToString(
                            item.Value,
                            CultureInfo.InvariantCulture)
                        ?.Trim();

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    var values =
                        value.Split(
                            new[]
                            {
                                ';',
                                '|',
                                ','
                            },
                            StringSplitOptions.RemoveEmptyEntries
                            | StringSplitOptions.TrimEntries);

                    foreach (var fileValue in values)
                    {
                        var fileName =
                            Path.GetFileName(fileValue);

                        if (string.IsNullOrWhiteSpace(fileName)
                            || !seen.Add(fileName))
                        {
                            continue;
                        }

                        documents.Add(
                            new SubmissionDocumentViewModel
                            {
                                ReferenceNumber =
                                    referenceNumber,

                                FileName =
                                    fileName,

                                StoredFileName =
                                    fileName,

                                FileExtension =
                                    Path.GetExtension(fileName),

                                DocumentType =
                                    ResolveLabel(item.Key),

                                Description =
                                    ResolveLabel(item.Key),

                                Exists = true
                            });
                    }
                }
            }

            return documents;
        }

        private static object? FindBestResultSet(
            IEnumerable<object> sources,
            params string[] expectedFields)
        {
            return sources
                .Select(source => new
                {
                    Source = source,
                    Fields = ToDictionary(source)
                })
                .OrderByDescending(x =>
                    expectedFields.Count(field =>
                        x.Fields.ContainsKey(field)))
                .FirstOrDefault(x =>
                    expectedFields.Any(field =>
                        x.Fields.ContainsKey(field)))
                ?.Source;
        }

        private static string FirstValueFromSources(
            IEnumerable<object?> sources,
            params string[] names)
        {
            foreach (var source in FlattenSources(sources))
            {
                var value =
                    FirstValue(source, names);

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }

        private static DateTime? FirstDateFromSources(
            IEnumerable<object?> sources,
            params string[] names)
        {
            foreach (var source in FlattenSources(sources))
            {
                var value =
                    FirstDate(source, names);

                if (value.HasValue)
                    return value;
            }

            return null;
        }

        private static IEnumerable<object> FlattenSources(
            IEnumerable<object?> sources)
        {
            foreach (var source in sources)
            {
                if (source is null)
                    continue;

                // Never reflect over List<object>, arrays or other source collections.
                if (source is IEnumerable enumerable
                    && source is not string
                    && source is not IDictionary)
                {
                    foreach (var nested in enumerable)
                    {
                        if (nested is not null)
                            yield return nested;
                    }

                    continue;
                }

                yield return source;
            }
        }

        private static SubmissionSectionViewModel BuildSection(
            string key,
            string title,
            int order,
            object source)
        {
            // Keep every database column in the submitted-view model,
            // including columns whose value is NULL or empty. The Razor
            // partials decide how empty values are displayed.
            var fields = ToDictionary(source)
                .Where(x => !HiddenFields.Contains(x.Key))
                .Select(x => new SubmissionFieldViewModel
                {
                    Name = x.Key,
                    Label = ResolveLabel(x.Key),
                    Value = FormatValue(x.Key, x.Value),
                    IsLongText = IsLongTextField(x.Key, x.Value)
                })
                .ToList();

            return new SubmissionSectionViewModel
            {
                Key = key,
                Title = title,
                Order = order,
                Fields = fields
            };
        }

        private static IDictionary<string, object?> ToDictionary(object source)
        {
            var result =
                new Dictionary<string, object?>(
                    StringComparer.OrdinalIgnoreCase);

            if (source is null)
                return result;

            // DapperRow and other generic dictionaries.
            if (source is IDictionary<string, object?> nullableDictionary)
            {
                foreach (var item in nullableDictionary)
                {
                    if (!string.IsNullOrWhiteSpace(item.Key))
                        result[item.Key] = item.Value;
                }

                return result;
            }

            if (source is IDictionary<string, object> objectDictionary)
            {
                foreach (var item in objectDictionary)
                {
                    if (!string.IsNullOrWhiteSpace(item.Key))
                        result[item.Key] = item.Value;
                }

                return result;
            }

            // Non-generic dictionaries.
            if (source is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    var key = entry.Key?.ToString();

                    if (!string.IsNullOrWhiteSpace(key))
                        result[key] = entry.Value;
                }

                return result;
            }

            var properties = source
                .GetType()
                .GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public)
                .Where(property => property.CanRead)

                // Exclude Item[index] and any other indexed property.
                .Where(property =>
                    property.GetIndexParameters().Length == 0);

            foreach (var property in properties)
            {
                try
                {
                    result[property.Name] =
                        property.GetValue(source);
                }
                catch (TargetParameterCountException)
                {
                    // Ignore indexed or parameterised getters.
                }
                catch (TargetInvocationException)
                {
                    // Ignore properties whose getter throws.
                }
                catch (MethodAccessException)
                {
                    // Ignore inaccessible getters.
                }
                catch (NotSupportedException)
                {
                    // Ignore unsupported reflected values.
                }
            }

            return result;
        }

        private static bool BelongsToUser(object source, string userId, bool isAppeal)
        {
            var storedUser = isAppeal
                ? FirstValue(source, "A_UserID", "UserID")
                : FirstValue(source, "UserID", "A_UserID");

            return !string.IsNullOrWhiteSpace(storedUser) &&
                   string.Equals(storedUser.Trim(), userId.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string FirstValue(object source, params string[] names)
        {
            var data = ToDictionary(source);
            foreach (var name in names)
            {
                if (data.TryGetValue(name, out var value) && HasDisplayValue(value))
                    return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
            }
            return string.Empty;
        }

        private static DateTime? FirstDate(object source, params string[] names)
        {
            var data = ToDictionary(source);
            foreach (var name in names)
            {
                if (!data.TryGetValue(name, out var value) || value is null)
                    continue;
                if (value is DateTime date) return date;
                if (DateTime.TryParse(value.ToString(), out date)) return date;
            }
            return null;
        }

        private static string FirstPropertyValue(object source, params string[] names) =>
            FirstValue(source, names);

        private static DateTime? FirstPropertyDate(object source, params string[] names) =>
            FirstDate(source, names);

        private static string FirstNonEmpty(params string?[] values) =>
            values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? string.Empty;

        private static bool HasDisplayValue(object? value)
        {
            if (value is null || value == DBNull.Value) return false;
            if (value is string text) return !string.IsNullOrWhiteSpace(text);
            return true;
        }

        private static string FormatValue(string name, object? value)
        {
            if (value is null || value == DBNull.Value) return string.Empty;
            if (value is bool boolean) return boolean ? "Yes" : "No";
            if (value is DateTime date) return date.ToString("dd MMMM yyyy HH:mm", CultureInfo.GetCultureInfo("en-ZA"));
            if (value is decimal decimalValue && name.Contains("Value", StringComparison.OrdinalIgnoreCase))
                return decimalValue.ToString("C", CultureInfo.GetCultureInfo("en-ZA"));
            return Convert.ToString(value, CultureInfo.GetCultureInfo("en-ZA"))?.Trim() ?? string.Empty;
        }

        private static string ResolveLabel(string name)
        {
            if (LabelOverrides.TryGetValue(name, out var label)) return label;
            var value = name.Replace("_", " ");
            value = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
            return CultureInfo.GetCultureInfo("en-ZA").TextInfo.ToTitleCase(value.ToLowerInvariant());
        }

        private static bool IsLongTextField(string name, object? value)
        {
            var text = value?.ToString() ?? string.Empty;
            return text.Length > 120 ||
                   name.Contains("Reason", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Motivation", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Comment", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Remarks", StringComparison.OrdinalIgnoreCase);
        }

        private static void RemoveEmptySections(SubmissionViewModel model) =>
            model.Sections = model.Sections
                .Where(x => x.Fields.Count > 0)
                .OrderBy(x => x.Order)
                .ToList();

        private static void HydrateTypedModelsFromSections(
            SubmissionViewModel model)
        {
            string Value(params string[] names)
            {
                foreach (var name in names)
                {
                    var value = model.Sections
                        .SelectMany(section => section.Fields)
                        .FirstOrDefault(field =>
                            field.Name.Equals(
                                name,
                                StringComparison.OrdinalIgnoreCase))
                        ?.Value
                        ?.Trim();

                    if (!string.IsNullOrWhiteSpace(value))
                        return value;
                }

                return string.Empty;
            }

            static string FirstExisting(
                string current,
                params string[] candidates)
            {
                if (!string.IsNullOrWhiteSpace(current))
                    return current.Trim();

                return candidates
                    .FirstOrDefault(value =>
                        !string.IsNullOrWhiteSpace(value))
                    ?.Trim()
                    ?? string.Empty;
            }

            var property = model.Property ?? new SubmissionPropertyViewModel();

            property.PropertyDescription = FirstExisting(
                property.PropertyDescription,
                model.PropertyDescription,
                Value(
                    "A_Property_Desc",
                    "Property_Desc",
                    "PropertyDesc",
                    "Old_Property_Description",
                    "Old_Property_Desc"));

            property.PropertyType = FirstExisting(
                property.PropertyType,
                Value(
                    "A_Property_Type",
                    "Property_Type",
                    "PropertyType"),
                model.FormType);

            property.PropertyId = FirstExisting(
                property.PropertyId,
                Value(
                    "A_Property_id",
                    "Property_id",
                    "PropertyId"));

            property.UnitKey = FirstExisting(
                property.UnitKey,
                Value(
                    "A_Unit_key",
                    "Unit_key",
                    "UnitKey"));

            property.ValuationKey = FirstExisting(
                property.ValuationKey,
                Value(
                    "A_Valuation_Key",
                    "Valuation_Key",
                    "ValuationKey"));

            property.Township = FirstExisting(
                property.Township,
                Value(
                    "TownNameDesc",
                    "Township",
                    "Town_Name",
                    "Town_Name_Desc"));

            property.Erf = FirstExisting(
                property.Erf,
                Value(
                    "ERF",
                    "Erf",
                    "Stand_No",
                    "StandNo",
                    "Unit_No"));

            property.Sector = FirstExisting(
                property.Sector,
                Value(
                    "A_Sector",
                    "Sector",
                    "Sector_Type"));

            property.Category = FirstExisting(
                property.Category,
                Value(
                    "Old_Category",
                    "GV_Category",
                    "Category",
                    "CatDesc"));

            property.Extent = FirstExisting(
                property.Extent,
                Value(
                    "Old_Extent",
                    "GV_Extent",
                    "Extent",
                    "RateableArea"));

            property.MarketValue = FirstExisting(
                property.MarketValue,
                Value(
                    "Old_Market_Value",
                    "GV_Market_Value",
                    "MarketValue",
                    "Market_Value"));

            property.OwnerName = FirstExisting(
                property.OwnerName,
                Value(
                    "Owner_Name",
                    "OwnerName"));

            var addressParts = new[]
            {
                Value("physical_address", "Property_Address", "ADDR1", "Address1", "LisStreetAddress"),
                Value("ADDR2", "Address2"),
                Value("ADDR3", "Address3"),
                Value("ADDR4", "Address4"),
                Value("ADDR5", "Address5")
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

            property.Address = FirstExisting(
                property.Address,
                string.Join(", ", addressParts));

            model.Property = property;

            model.PropertyDescription = FirstExisting(
                model.PropertyDescription,
                property.PropertyDescription);

            model.PropertyKey = FirstExisting(
                model.PropertyKey,
                property.PropertyId,
                property.UnitKey,
                property.ValuationKey);

            var applicant = model.Applicant ?? new SubmissionApplicantViewModel();

            applicant.ObjectorType = FirstExisting(
                applicant.ObjectorType,
                Value("Objector_Type", "Applicant_Type", "Query_Type"));

            applicant.ApplicantName = FirstExisting(
                applicant.ApplicantName,
                Value("Objector_Name", "Applicant_Name"));

            applicant.ApplicantSurname = FirstExisting(
                applicant.ApplicantSurname,
                Value("Objector_Surname", "Applicant_Surname"));

            applicant.ApplicantIdNumber = FirstExisting(
                applicant.ApplicantIdNumber,
                Value("Objector_Identity", "Objector_ID", "Applicant_ID", "ID_Number"));

            applicant.ApplicantCompanyRegistrationNumber = FirstExisting(
                applicant.ApplicantCompanyRegistrationNumber,
                Value("Objector_Company", "Objector_Company_Reg_No", "Applicant_Company_Reg_No"));

            applicant.ApplicantEmail = FirstExisting(
                applicant.ApplicantEmail,
                Value("Objector_Email", "Applicant_Email", "Email"));

            applicant.ApplicantTelephone = FirstExisting(
                applicant.ApplicantTelephone,
                Value("Objector_Home_Phone", "Objector_Work_Phone", "Objector_Telephone"));

            applicant.ApplicantCellphone = FirstExisting(
                applicant.ApplicantCellphone,
                Value("Objector_Cell", "Objector_Cell_Phone", "Objector_Cellphone"));

            applicant.ApplicantAddress1 = FirstExisting(applicant.ApplicantAddress1, Value("Objector_Postal_1"));
            applicant.ApplicantAddress2 = FirstExisting(applicant.ApplicantAddress2, Value("Objector_Postal_2"));
            applicant.ApplicantAddress3 = FirstExisting(applicant.ApplicantAddress3, Value("Objector_Postal_3"));
            applicant.ApplicantAddress4 = FirstExisting(applicant.ApplicantAddress4, Value("Objector_Postal_4"));
            applicant.ApplicantPostalCode = FirstExisting(applicant.ApplicantPostalCode, Value("Objector_Postal_5"));

            applicant.OwnerName = FirstExisting(applicant.OwnerName, Value("Owner_Name"));
            applicant.OwnerSurname = FirstExisting(applicant.OwnerSurname, Value("Owner_Surname"));
            applicant.OwnerIdNumber = FirstExisting(applicant.OwnerIdNumber, Value("Owner_Identity", "Owner_ID", "Owner_ID_Number"));
            applicant.OwnerEmail = FirstExisting(applicant.OwnerEmail, Value("Owner_Email"));
            applicant.OwnerTelephone = FirstExisting(applicant.OwnerTelephone, Value("Owner_Home_Phone", "Owner_Work_Phone", "Owner_Telephone"));
            applicant.OwnerCellphone = FirstExisting(applicant.OwnerCellphone, Value("Owner_Cell_Phone", "Owner_Cellphone", "Owner_Cell"));

            applicant.RepresentativeName = FirstExisting(applicant.RepresentativeName, Value("Representative_name", "Representative_Name", "Rep_Name"));
            applicant.RepresentativeSurname = FirstExisting(applicant.RepresentativeSurname, Value("Representative_Surname", "Rep_Surname"));
            applicant.RepresentativeIdNumber = FirstExisting(applicant.RepresentativeIdNumber, Value("Representative_ID", "Representative_Identity", "Rep_ID"));
            applicant.RepresentativeCompanyName = FirstExisting(applicant.RepresentativeCompanyName, Value("Representative_Company", "Rep_Company"));
            applicant.RepresentativeCompanyRegistrationNumber = FirstExisting(applicant.RepresentativeCompanyRegistrationNumber, Value("Representative_Company_Reg_No", "Rep_Company_Reg_No"));
            applicant.RepresentativeEmail = FirstExisting(applicant.RepresentativeEmail, Value("Representative_Email", "Rep_Email"));
            applicant.RepresentativeTelephone = FirstExisting(applicant.RepresentativeTelephone, Value("Rep_Home_Phone", "Rep_Work_Phone", "Representative_Telephone"));
            applicant.RepresentativeCellphone = FirstExisting(applicant.RepresentativeCellphone, Value("Rep_Cell_Phone", "Representative_Cellphone", "Rep_Cell"));
            applicant.RepresentativeAddress1 = FirstExisting(applicant.RepresentativeAddress1, Value("Rep_Postal_1"));
            applicant.RepresentativeAddress2 = FirstExisting(applicant.RepresentativeAddress2, Value("Rep_Postal_2"));
            applicant.RepresentativeAddress3 = FirstExisting(applicant.RepresentativeAddress3, Value("Rep_Postal_3"));
            applicant.RepresentativeAddress4 = FirstExisting(applicant.RepresentativeAddress4, Value("Rep_Postal_4"));
            applicant.RepresentativePostalCode = FirstExisting(applicant.RepresentativePostalCode, Value("Rep_Postal_5"));
            applicant.Capacity = FirstExisting(applicant.Capacity, Value("Objector_Status", "Capacity", "Representative_Capacity"));

            model.Applicant = applicant;

            model.CurrentValuation = BuildCurrentValuationFromFields(
                model,
                model.CurrentValuation);

            model.RequestedValuation = BuildRequestedValuationFromFields(
                model,
                model.RequestedValuation);

            model.Reasons = BuildReasonsFromFields(
                model,
                model.Reasons);

            model.MultiPurposeLines = BuildMultiPurposeLinesFromFields(
                model,
                model.MultiPurposeLines);
        }

        private static SubmissionValuationViewModel BuildCurrentValuationFromFields(
            SubmissionViewModel model,
            SubmissionValuationViewModel current)
        {
            string Value(params string[] names) => model.Sections
                .SelectMany(section => section.Fields)
                .FirstOrDefault(field => names.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                ?.Value
                ?.Trim()
                ?? string.Empty;

            current.PropertyDescription = FirstNonEmpty(
                current.PropertyDescription,
                Value("Old_Property_Description", "Old_Property_Desc", "Property_Desc"));
            current.Category = FirstNonEmpty(current.Category, Value("Old_Category", "GV_Category"));
            current.Address = FirstNonEmpty(current.Address, Value("Old_Address", "GV_Address", "LisStreetAddress"));
            current.Extent = FirstNonEmpty(current.Extent, Value("Old_Extent", "GV_Extent", "RateableArea"));
            current.MarketValue = FirstNonEmpty(current.MarketValue, Value("Old_Market_Value", "GV_Market_Value", "MarketValue"));
            current.Owner = FirstNonEmpty(current.Owner, Value("Old_Owner", "Owner_Name"));
            return current;
        }

        private static SubmissionValuationViewModel BuildRequestedValuationFromFields(
            SubmissionViewModel model,
            SubmissionValuationViewModel requested)
        {
            string Value(params string[] names) => model.Sections
                .SelectMany(section => section.Fields)
                .FirstOrDefault(field => names.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                ?.Value
                ?.Trim()
                ?? string.Empty;

            requested.PropertyDescription = FirstNonEmpty(requested.PropertyDescription, Value("new_Property_Description", "New_Property_Description", "New_Property_Desc"));
            requested.Category = FirstNonEmpty(requested.Category, Value("new_Category", "New_Category", "Requested_Category"));
            requested.Address = FirstNonEmpty(requested.Address, Value("new_Address", "New_Address", "Requested_Address"));
            requested.Extent = FirstNonEmpty(requested.Extent, Value("new_Extent", "New_Extent", "Requested_Extent"));
            requested.MarketValue = FirstNonEmpty(requested.MarketValue, Value("new_Market_Value", "New_Market_Value", "Requested_Market_Value"));
            requested.Owner = FirstNonEmpty(requested.Owner, Value("new_Owner", "New_Owner", "Requested_Owner"));
            return requested;
        }

        private static SubmissionReasonViewModel BuildReasonsFromFields(
            SubmissionViewModel model,
            SubmissionReasonViewModel reasons)
        {
            string Value(params string[] names) => model.Sections
                .SelectMany(section => section.Fields)
                .FirstOrDefault(field => names.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                ?.Value
                ?.Trim()
                ?? string.Empty;

            reasons.PrimaryReason = FirstNonEmpty(reasons.PrimaryReason, Value("Objection_Reasons", "Query_Reason", "Review_Reason", "Reason"));
            reasons.AdditionalReason = FirstNonEmpty(reasons.AdditionalReason, Value("Additional_Reason", "Additional_Reasons"));
            reasons.Motivation = FirstNonEmpty(reasons.Motivation, Value("Motivation_for_Supp_Request", "Motivation", "Query_Motivation", "Review_Motivation"));
            reasons.RequestedOutcome = FirstNonEmpty(reasons.RequestedOutcome, Value("Requested_Outcome", "Appellant_Request", "Requested_Decision"));
            reasons.OtherReason = FirstNonEmpty(reasons.OtherReason, Value("Other_Reason", "Other_Reasons", "Remarks", "Comments"));
            return reasons;
        }

        private static List<MultiPurposeLineViewModel> BuildMultiPurposeLinesFromFields(
            SubmissionViewModel model,
            List<MultiPurposeLineViewModel> currentLines)
        {
            string Value(params string[] names) => model.Sections
                .SelectMany(section => section.Fields)
                .FirstOrDefault(field => names.Contains(field.Name, StringComparer.OrdinalIgnoreCase))
                ?.Value
                ?.Trim()
                ?? string.Empty;

            var lines = new List<MultiPurposeLineViewModel>();

            for (var index = 1; index <= 10; index++)
            {
                var suffix = index == 1 ? string.Empty : index.ToString(CultureInfo.InvariantCulture);
                var line = new MultiPurposeLineViewModel
                {
                    LineNumber = index,
                    CurrentCategory = Value($"Old{suffix}_Category", $"Old_Category{suffix}", $"GV_Category{suffix}"),
                    CurrentExtent = Value($"Old{suffix}_Extent", $"Old_Extent{suffix}", $"GV_Extent{suffix}"),
                    CurrentMarketValue = Value($"Old{suffix}_Market_Value", $"Old_Market_Value{suffix}", $"GV_Market_Value{suffix}"),
                    RequestedCategory = Value($"new{suffix}_Category", $"New{suffix}_Category", $"New_Category{suffix}"),
                    RequestedExtent = Value($"new{suffix}_Extent", $"New{suffix}_Extent", $"New_Extent{suffix}"),
                    RequestedMarketValue = Value($"new{suffix}_Market_Value", $"New{suffix}_Market_Value", $"New_Market_Value{suffix}"),
                    Remarks = Value($"Remarks{suffix}", $"GV_Remarks{suffix}", $"New_Remarks{suffix}")
                };

                if (line.HasValues)
                    lines.Add(line);
            }

            return lines.Count > 0 ? lines : currentLines;
        }

        private static List<SubmissionFormSectionViewModel> BuildFormSections(
    SubmissionViewModel model)
        {
            var definitions = new List<SubmissionFormSectionViewModel>
    {
        new()
        {
            Key = "section-1",
            TabText = "Section 1",
            Title = "Objector Information",
            Description = "Owner, objector and authorised representative information.",
            Order = 1,

            // The submitted procedures may return Section 1 across
            // one or two result sets.
            SourceKeys =
            {
                "Section1",
                "Section2"
            }
        },

        new()
        {
            Key = "section-78-request",
            TabText = model.IsReview
                ? "Review Request"
                : "Section 78 Request",
            Title = model.IsReview
                ? "Section 78 Review Request"
                : "Section 78 Query Request",
            Description = "The Section 78 request, motivation and requested outcome.",
            Order = 2,
            IsAvailable = model.IsSection78,
            SourceKeys =
            {
                "Section2Query"
            }
        },

        new()
        {
            Key = "section-2",
            TabText = "Section 2",
            Title = "Property Details",
            Description = "Physical property, bond, servitude and compensation details.",
            Order = 3,

            // Objection procedures generally expose this as Section2.
            // Section 78 procedure mappings can be adjusted after the
            // exact stored-procedure result order is confirmed.
            SourceKeys =
            {
                "Section2",
                "Section3"
            }
        },

        new()
        {
            Key = "section-3-res",
            TabText = "Section 3 · Residential",
            Title = "Description of Residential Dwelling",
            Description = "Residential dwelling, outbuilding and property features.",
            Order = 4,
            IsAvailable =
                model.FormType.Equals("Res", StringComparison.OrdinalIgnoreCase)
                || model.IsMulti,
            SourceKeys =
            {
                "Section3Res",
                "Section3"
            }
        },

        new()
        {
            Key = "section-3-agri",
            TabText = "Section 3 · Agricultural",
            Title = "Agricultural Property Details",
            Description = "Agricultural land, improvements and farming information.",
            Order = 5,
            IsAvailable =
                model.FormType.Equals("Agric", StringComparison.OrdinalIgnoreCase)
                || model.IsMulti,
            SourceKeys =
            {
                "Section3Agri",
                "Section5"
            }
        },

        new()
        {
            Key = "section-3-bus",
            TabText = "Section 3 · Business",
            Title = "Business Property Details",
            Description = "Business use, accommodation and property information.",
            Order = 6,
            IsAvailable =
                model.FormType.Equals("Bus", StringComparison.OrdinalIgnoreCase)
                || model.IsMulti,
            SourceKeys =
            {
                "Section3Bus",
                "Section4"
            }
        },

        new()
        {
            Key = "section-4-res",
            TabText = "Section 4 · Residential",
            Title = "Residential Sectional Title Units",
            Description = "Residential sectional-title and exclusive-use areas.",
            Order = 7,
            IsAvailable =
                model.FormType.Equals("Res", StringComparison.OrdinalIgnoreCase)
                || model.IsMulti,
            SourceKeys =
            {
                "Section4Res",
                "Section6"
            }
        },

        new()
        {
            Key = "section-4-bus",
            TabText = "Section 4 · Business",
            Title = "Business Sectional Title Units",
            Description = "Business sectional-title and common-property information.",
            Order = 8,
            IsAvailable =
                model.FormType.Equals("Bus", StringComparison.OrdinalIgnoreCase)
                || model.IsMulti,
            SourceKeys =
            {
                "Section4Bus",
                "Section7"
            }
        },

        new()
        {
            Key = "section-5",
            TabText = "Section 5",
            Title = "Market Information",
            Description = "Asking prices, offers, agents and comparable transactions.",
            Order = 9,
            SourceKeys =
            {
                "Section5",
                "Section8"
            }
        },

        new()
        {
            Key = "section-6",
            TabText = "Section 6",
            Title = ResolveSubmissionDetailsTitle(model),
            Description = "Current valuation record, requested changes and supporting reasons.",
            Order = 10,
            SourceKeys =
            {
                "Section6",
                "Section9"
            }
        },

        new()
        {
            Key = "section-7",
            TabText = "Section 7",
            Title = "Declaration",
            Description = "Declaration and submission confirmation.",
            Order = 11,
            SourceKeys =
            {
                "Section7",
                "Section10"
            }
        }
    };

            foreach (var definition in definitions)
            {
                definition.DataSections = model.Sections
                    .Where(section =>
                        definition.SourceKeys.Contains(
                            section.Key,
                            StringComparer.OrdinalIgnoreCase))
                    .Where(section => section.Fields.Count > 0)
                    .OrderBy(section => section.Order)
                    .ToList();
            }

            return definitions
                .Where(section => section.IsAvailable)
                .Where(section =>
                    section.HasFields
                    || section.Key == "section-6"
                    || section.Key == "section-7")
                .OrderBy(section => section.Order)
                .ToList();
        }

        private static string ResolveSubmissionDetailsTitle(
            SubmissionViewModel model)
        {
            if (model.IsAppeal)
                return "Appeal Details";

            if (model.IsReview)
                return "Review Details";

            if (model.IsQuery)
                return "Query Details";

            if (model.IsAttribute)
                return "Attribute Details";

            return "Objection Details";
        }

        private string GetConnectionStringForRoll(string rollSource)
        {
            var key = rollSource switch
            {
                "Objection" => "DefaultConnection",
                "Objection_Supp1" => "Sup1Connection",
                "Objection_Supp2" => "Sup2Connection",
                "Objection_Supp3" => "Sup3Connection",
                "Objection_Supp4" => "Sup4Connection",
                "Objection_Supp5" => "Sup5Connection",
                _ => throw new InvalidOperationException($"Unsupported roll source '{rollSource}'.")
            };

            return _config.GetConnectionString(key)
                ?? throw new InvalidOperationException($"Connection string '{key}' was not found.");
        }

        private static async Task<string> ResolvePropertyTypeAsync(
            SqlConnection connection,
            string referenceNumber,
            bool isAppeal)
        {
            var sql = isAppeal
                ? @"SELECT TOP 1 COALESCE(NULLIF(LTRIM(RTRIM(a.A_Property_Type)), ''), NULLIF(LTRIM(RTRIM(o.Property_Type)), ''))
                FROM dbo.Obj_Property_Info_Appeal a
                LEFT JOIN dbo.Obj_Property_Info o ON LTRIM(RTRIM(o.Objection_No)) = LTRIM(RTRIM(a.Obj_Ref))
                WHERE LTRIM(RTRIM(a.Appeal_No)) = LTRIM(RTRIM(@Ref));"
                : @"SELECT TOP 1 Property_Type FROM dbo.Obj_Property_Info
                WHERE LTRIM(RTRIM(Objection_No)) = LTRIM(RTRIM(@Ref));";

            return (await connection.ExecuteScalarAsync<string>(sql, new { Ref = referenceNumber }))?.Trim() ?? "Res";
        }

        private static string ResolveFormProcedure(string formType) => formType switch
        {
            "Res" => "usp_GetFormA_Data",
            "Bus" => "usp_GetFormB_Data",
            "Agric" => "usp_GetFormC_Data",
            "Multi" => "usp_GetFormD_Data",
            _ => throw new NotSupportedException($"No submitted-form procedure is configured for '{formType}'.")
        };

        private static string NormalizePropertyType(string? value)
        {
            var type = value?.Trim().ToUpperInvariant() ?? string.Empty;
            if (type.StartsWith("RES")) return "Res";
            if (type.StartsWith("BUS") || type.StartsWith("COM")) return "Bus";
            if (type.StartsWith("AGR") || type.StartsWith("FARM")) return "Agric";
            if (type.StartsWith("MULT")) return "Multi";
            return "Res";
        }

        private static string NormalizeRollSource(string? value)
        {
            var source = value?.Trim() ?? "Objection";
            return source.ToUpperInvariant() switch
            {
                "GV23" => "Objection",
                "GV23-SUP1" or "SUP1" => "Objection_Supp1",
                "GV23-SUP2" or "SUP2" => "Objection_Supp2",
                "GV23-SUP3" or "SUP3" => "Objection_Supp3",
                "GV23-SUP4" or "SUP4" => "Objection_Supp4",
                "GV23-SUP5" or "SUP5" => "Objection_Supp5",
                _ => source
            };
        }

        private static string NormalizeSubmissionType(
            string? value,
            string referenceNumber)
        {
            var requestedType =
                value?.Trim()
                ?? string.Empty;

            var reference =
                referenceNumber?.Trim()
                ?? string.Empty;

            if (requestedType.Equals(
                    "Attribute",
                    StringComparison.OrdinalIgnoreCase)
                || requestedType.Equals(
                    "Attributes",
                    StringComparison.OrdinalIgnoreCase)
                || reference.StartsWith(
                    "ATTR-",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Attribute";
            }

            if (reference.EndsWith(
                    "-R",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Review";
            }

            if (reference.StartsWith(
                    "APP-",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Appeal";
            }

            if (reference.Contains(
                    "QUE",
                    StringComparison.OrdinalIgnoreCase))
            {
                return requestedType.Contains(
                    "Review",
                    StringComparison.OrdinalIgnoreCase)
                        ? "Review"
                        : "Query";
            }

            return requestedType.ToLowerInvariant() switch
            {
                "attribute" or "attributes" =>
                    "Attribute",

                "appeal" =>
                    "Appeal",

                "query"
                or "section78query"
                or "section78" =>
                    "Query",

                "review"
                or "section78review" =>
                    "Review",

                "objection" or "" =>
                    "Objection",

                _ =>
                    requestedType
            };
        }

        private static string GetRollDisplayName(string rollSource) => rollSource switch
        {
            "Objection" => "General Valuation Roll 2023",
            "Objection_Supp1" => "Supplementary Valuation Roll 1",
            "Objection_Supp2" => "Supplementary Valuation Roll 2",
            "Objection_Supp3" => "Supplementary Valuation Roll 3",
            "Objection_Supp4" => "Supplementary Valuation Roll 4",
            "Objection_Supp5" => "Supplementary Valuation Roll 5",
            _ => rollSource
        };

        private static string ResolveSectionTitle(string type, string formType, int index)
        {
            if (index == 1) return "Applicant and owner details";
            if (index == 2) return "Representative and contact details";

            return formType switch
            {
                "Res" => index switch
                {
                    3 => "Residential property details",
                    4 => "Residential improvements",
                    5 => "Additional information",
                    6 => "Current and requested valuation details",
                    7 => "Declaration",
                    _ => $"Section {index}"
                },
                "Bus" => index switch
                {
                    3 => "Business property details",
                    4 => "Business improvements",
                    5 => "Additional information",
                    6 => "Current and requested valuation details",
                    7 => "Declaration",
                    _ => $"Section {index}"
                },
                "Agric" => index switch
                {
                    3 => "Agricultural property details",
                    4 => "Additional information",
                    5 => "Current and requested valuation details",
                    6 => "Declaration",
                    _ => $"Section {index}"
                },
                "Multi" => index switch
                {
                    3 => "Residential property details",
                    4 => "Business property details",
                    5 => "Agricultural property details",
                    6 => "Residential improvements",
                    7 => "Business improvements",
                    8 => "Additional information",
                    9 => "Current and requested valuation details",
                    10 => "Declaration",
                    _ => $"Section {index}"
                },
                _ => $"Section {index}"
            };
        }
    }

}