using Dapper;
using GV_Forms.Pdf;
using Microsoft.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class SubmittedFormPdfService : ISubmittedFormPdfService
{
    private readonly ILogger<SubmittedFormPdfService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly string _queryConn;

    public SubmittedFormPdfService(
        ILogger<SubmittedFormPdfService> logger,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _logger = logger;
        _env = env;
        _config = config;

        _queryConn = config.GetConnectionString("QueryConnection")
            ?? throw new InvalidOperationException("Connection string 'QueryConnection' was not found.");
    }

    // ─────────────────────────────────────────────────────────────
    // OBJECTION / APPEAL — DB STORED PROCEDURE VERSION
    // This is the preferred method for Form A / B / C / D.
    // ─────────────────────────────────────────────────────────────
    public async Task<SubmittedFormPdfResult> GenerateObjectionOrAppealFormFromDbAsync(
        string rollSource,
        bool isAppeal,
        string referenceNo,
        string folderPath,
        DateTime? dateSubmitted = null)
    {
        if (string.IsNullOrWhiteSpace(referenceNo))
            throw new ArgumentException("Reference number is required.", nameof(referenceNo));

        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        Directory.CreateDirectory(folderPath);

        var submittedDate = dateSubmitted ?? DateTime.Now;
        var submissionType = isAppeal ? "Appeal" : "Objection";

        var connStr = GetConnectionStringForRoll(rollSource);

        await using var conn = new SqlConnection(connStr);

        var rawPropertyType = await ResolvePropertyTypeFromDbAsync(
            conn,
            referenceNo,
            isAppeal);

        var formType = NormalisePropertyType(rawPropertyType);

        var procName = ResolveObjectionAppealDetailProc(formType);

        var parameters = new DynamicParameters();
        parameters.Add("@InquiryType", submissionType);
        parameters.Add("@RefNo", referenceNo.Trim());

        var aggregate = new InquiryAggregate();

        _logger.LogInformation(
            "[Submitted Form PDF] Loading {SubmissionType} {FormType} data using {ProcName} for {ReferenceNo}",
            submissionType,
            formType,
            procName,
            referenceNo);

        using var multi = await conn.QueryMultipleAsync(
            procName,
            parameters,
            commandType: CommandType.StoredProcedure);

        aggregate.Main = (await multi.ReadAsync<dynamic>()).FirstOrDefault();

        if (aggregate.Main == null)
        {
            throw new InvalidOperationException(
                $"{procName} returned no main record for {referenceNo}.");
        }

        var sectionIndex = 1;

        while (!multi.IsConsumed)
        {
            var sectionRows = await multi.ReadAsync<dynamic>();
            var section = sectionRows.FirstOrDefault();

            aggregate.Sections[$"Section{sectionIndex}"] = section;

            sectionIndex++;
        }

        var wording = Wording.ForType(submissionType);

        byte[] pdfBytes = GenerateObjectionAppealPdfBytes(
            aggregate,
            wording,
            formType,
            _env);

        if (pdfBytes.Length == 0)
        {
            throw new InvalidOperationException(
                $"The generated {submissionType} {formType} form PDF is empty for {referenceNo}.");
        }

        var fileName = BuildObjectionAppealFormFileName(
            referenceNo,
            formType,
            submissionType,
            submittedDate);

        var filePath = Path.Combine(folderPath, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);

        if (!File.Exists(filePath))
        {
            throw new IOException(
                $"The submitted form PDF was generated but was not found on disk: {filePath}");
        }

        _logger.LogInformation(
            "[Submitted Form PDF] {SubmissionType} {FormType} saved for {ReferenceNo}. File: {FileName}. Size: {Size} bytes. Path: {FilePath}",
            submissionType,
            formType,
            referenceNo,
            fileName,
            pdfBytes.Length,
            filePath);

        return new SubmittedFormPdfResult
        {
            ReferenceNumber = referenceNo,
            FileName = fileName,
            FilePath = filePath,
            PdfBytes = pdfBytes,
            SubmissionType = submissionType
        };
    }

    // ─────────────────────────────────────────────────────────────
    // OBJECTION / APPEAL — POSTED MODEL FALLBACK VERSION
    // This is used only if the stored procedure version fails.
    // ─────────────────────────────────────────────────────────────
    public async Task<SubmittedFormPdfResult> GenerateObjectionOrAppealFormAsync(
        bool isAppeal,
        string folderPath,
        Obj_Property_InfoModel obj,
        Obj_Property_Info_AppealModel? appeal,
        Obj_Section1Model obj1,
        Obj_Section2Model obj2,
        Obj_Section3ResModel objR3,
        Obj_Section3BusModel objB3,
        Obj_Section3AgriModel objA3,
        Obj_Section4BusModel objB4,
        Obj_Section4ResModel objR4,
        Obj_Section5Model obj5,
        Obj_Section6Model obj6,
        Obj_Section7Model obj7,
        DateTime? dateSubmitted = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        Directory.CreateDirectory(folderPath);

        var submittedDate = dateSubmitted ?? DateTime.Now;
        var submissionType = isAppeal ? "Appeal" : "Objection";

        var referenceNumber = isAppeal
            ? appeal?.Appeal_No
            : obj.Objection_No;

        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new InvalidOperationException("Reference number is empty. The submitted form PDF cannot be generated.");

        var category = isAppeal
            ? appeal?.A_Property_Type ?? obj.Property_Type
            : obj.Property_Type;

        var formType = NormalisePropertyType(category);

        var aggregate = new InquiryAggregate
        {
            Main = isAppeal
                ? appeal ?? throw new InvalidOperationException("Appeal model is required for Appeal PDF.")
                : obj
        };

        aggregate.Sections["Section1"] = obj1;
        aggregate.Sections["Section2"] = obj2;

        if (formType == "Res")
        {
            aggregate.Sections["Section3"] = objR3;
            aggregate.Sections["Section4"] = objR4;
            aggregate.Sections["Section5"] = obj5;
            aggregate.Sections["Section6"] = obj6;
            aggregate.Sections["Section7"] = obj7;
        }
        else if (formType == "Bus")
        {
            aggregate.Sections["Section3"] = objB3;
            aggregate.Sections["Section4"] = objB4;
            aggregate.Sections["Section5"] = obj5;
            aggregate.Sections["Section6"] = obj6;
            aggregate.Sections["Section7"] = obj7;
        }
        else if (formType == "Agric")
        {
            aggregate.Sections["Section3"] = objA3;
            aggregate.Sections["Section4"] = obj5;
            aggregate.Sections["Section5"] = obj6;
            aggregate.Sections["Section6"] = obj7;
        }
        else if (formType == "Multi")
        {
            aggregate.Sections["Section3"] = objR3;
            aggregate.Sections["Section4"] = objB3;
            aggregate.Sections["Section5"] = objA3;
            aggregate.Sections["Section6"] = objR4;
            aggregate.Sections["Section7"] = objB4;
            aggregate.Sections["Section8"] = obj5;
            aggregate.Sections["Section9"] = obj6;
            aggregate.Sections["Section10"] = obj7;
        }
        else
        {
            throw new NotSupportedException(
                $"Cannot map sections for property type '{formType}'.");
        }

        var wording = Wording.ForType(submissionType);

        byte[] pdfBytes = GenerateObjectionAppealPdfBytes(
            aggregate,
            wording,
            formType,
            _env);

        if (pdfBytes.Length == 0)
        {
            throw new InvalidOperationException(
                $"The generated {submissionType} {formType} form PDF is empty for {referenceNumber}.");
        }

        string fileName = BuildObjectionAppealFormFileName(
            referenceNumber,
            formType,
            submissionType,
            submittedDate);

        string filePath = Path.Combine(folderPath, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);

        if (!File.Exists(filePath))
        {
            throw new IOException(
                $"The submitted form PDF was generated but was not found on disk: {filePath}");
        }

        _logger.LogInformation(
            "[Submitted Form PDF] {SubmissionType} {FormType} fallback form saved for {ReferenceNumber}. File: {FileName}. Size: {Size} bytes. Path: {FilePath}",
            submissionType,
            formType,
            referenceNumber,
            fileName,
            pdfBytes.Length,
            filePath);

        return new SubmittedFormPdfResult
        {
            ReferenceNumber = referenceNumber,
            FileName = fileName,
            FilePath = filePath,
            PdfBytes = pdfBytes,
            SubmissionType = submissionType
        };
    }

    // ─────────────────────────────────────────────────────────────
    // SECTION 78 — POSTED MODEL VERSION
    // ─────────────────────────────────────────────────────────────
    public async Task<SubmittedFormPdfResult> GenerateSection78FormAsync(
        bool isReview,
        string folderPath,
        Que_Property_InfoModel que,
        Obj_Section1Model obj1,
        Obj_Section2Model obj2,
        Obj_Section2QueryModel que1,
        Obj_Section3ResModel objR3,
        Obj_Section3BusModel objB3,
        Obj_Section3AgriModel objA3,
        Obj_Section4BusModel objB4,
        Obj_Section4ResModel objR4,
        Obj_Section5Model obj5,
        Obj_Section6Model obj6,
        Obj_Section7Model obj7,
        DateTime? dateSubmitted = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        Directory.CreateDirectory(folderPath);

        var submittedDate = dateSubmitted ?? DateTime.Now;

        string referenceNumber = !string.IsNullOrWhiteSpace(que.Query_No)
            ? que.Query_No
            : isReview
                ? $"Que-GV23-{que.Query_ID}-R"
                : $"Que-GV23-{que.Query_ID}";

        string submissionType = isReview
            ? "Section78Review"
            : "Section78Query";

        string propertyDesc = que.Property_Desc
            ?? obj6.Old_Property_Description
            ?? string.Empty;

        string category = que.Property_Type ?? string.Empty;
        string formType = NormalisePropertyType(category);

        var aggregate = new InquiryAggregate
        {
            Main = que
        };

        aggregate.Sections["Section1"] = obj1;
        aggregate.Sections["Section2"] = obj2;
        aggregate.Sections["Section2Query"] = que1;
        aggregate.Sections["Section3"] = objR3;
        aggregate.Sections["Section4"] = objB3;
        aggregate.Sections["Section5"] = objA3;
        aggregate.Sections["Section6"] = objB4;
        aggregate.Sections["Section7"] = objR4;
        aggregate.Sections["Section8"] = obj5;
        aggregate.Sections["Section9"] = obj6;
        aggregate.Sections["Section10"] = obj7;

        var wording = Wording.ForType(isReview ? "Review" : "Query");

        byte[] pdfBytes = GenerateSection78PdfBytes(
            aggregate,
            wording,
            formType);

        string fileName = BuildSubmittedFormFileName(
            referenceNumber,
            propertyDesc,
            category,
            submissionType,
            submittedDate);

        string filePath = Path.Combine(folderPath, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);

        _logger.LogInformation(
            "[Submitted Form PDF] {SubmissionType} form saved for {ReferenceNumber} at {FilePath}",
            submissionType,
            referenceNumber,
            filePath);

        return new SubmittedFormPdfResult
        {
            ReferenceNumber = referenceNumber,
            FileName = fileName,
            FilePath = filePath,
            PdfBytes = pdfBytes,
            SubmissionType = submissionType
        };
    }

    // ─────────────────────────────────────────────────────────────
    // SECTION 78 — DB VERSION
    // ─────────────────────────────────────────────────────────────
    public async Task<SubmittedFormPdfResult> GenerateSection78FormFromDbAsync(
        string queryRef,
        string folderPath)
    {
        if (string.IsNullOrWhiteSpace(queryRef))
            throw new ArgumentException("Query reference is required.", nameof(queryRef));

        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path is required.", nameof(folderPath));

        Directory.CreateDirectory(folderPath);

        await using var conn = new SqlConnection(_queryConn);

        var que = await conn.QueryFirstOrDefaultAsync<Que_Property_InfoModel>(
            @"
            SELECT TOP 1 *
            FROM dbo.QUE_Property_Info
            WHERE LTRIM(RTRIM(QUERY_No)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef });

        if (que == null)
            throw new InvalidOperationException($"QUE_Property_Info not found for {queryRef}.");

        var obj1 = await conn.QueryFirstOrDefaultAsync<Obj_Section1Model>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section1
            WHERE LTRIM(RTRIM(Objection_Ref_S1)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section1Model();

        var obj2 = await conn.QueryFirstOrDefaultAsync<Obj_Section2Model>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section2
            WHERE LTRIM(RTRIM(Objection_Ref_S2)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section2Model();

        var que1 = await conn.QueryFirstOrDefaultAsync<Obj_Section2QueryModel>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section2Query
            WHERE LTRIM(RTRIM(Objection_Ref_SQ)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section2QueryModel();

        var objB3 = await conn.QueryFirstOrDefaultAsync<Obj_Section3BusModel>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section3Bus
            WHERE LTRIM(RTRIM(Objection_Ref_SB3)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section3BusModel();

        var objA3 = await conn.QueryFirstOrDefaultAsync<Obj_Section3AgriModel>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section3Agri
            WHERE LTRIM(RTRIM(Objection_Ref_SA3)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section3AgriModel();

        var objB4 = await conn.QueryFirstOrDefaultAsync<Obj_Section4BusModel>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section4Bus
            WHERE LTRIM(RTRIM(Objection_Ref_SB4)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section4BusModel();

        var objR4 = await conn.QueryFirstOrDefaultAsync<Obj_Section4ResModel>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section4Res
            WHERE LTRIM(RTRIM(Objection_Ref_SR4)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section4ResModel();

        var obj5 = await conn.QueryFirstOrDefaultAsync<Obj_Section5Model>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section5
            WHERE LTRIM(RTRIM(Objection_Ref_S5)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section5Model();

        var obj6 = await conn.QueryFirstOrDefaultAsync<Obj_Section6Model>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section6
            WHERE LTRIM(RTRIM(Objection_Ref_S6)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section6Model();

        var obj7 = await conn.QueryFirstOrDefaultAsync<Obj_Section7Model>(
            @"
            SELECT TOP 1 *
            FROM dbo.Obj_Section7
            WHERE LTRIM(RTRIM(Objection_Ref_S7)) = LTRIM(RTRIM(@Ref))
            ",
            new { Ref = queryRef }) ?? new Obj_Section7Model();

        bool isReview = que.Sub_typ == 1;

        var aggregate = new InquiryAggregate
        {
            Main = que
        };

        aggregate.Sections["Section1"] = obj1;
        aggregate.Sections["Section2"] = obj2;
        aggregate.Sections["Section2Query"] = que1;
        aggregate.Sections["Section3Bus"] = objB3;
        aggregate.Sections["Section3Agri"] = objA3;
        aggregate.Sections["Section4Bus"] = objB4;
        aggregate.Sections["Section4Res"] = objR4;
        aggregate.Sections["Section5"] = obj5;
        aggregate.Sections["Section6"] = obj6;
        aggregate.Sections["Section7"] = obj7;

        var wording = Wording.ForType(isReview ? "Review" : "Query");

        string formType = NormalisePropertyType(que.Property_Type);

        byte[] pdfBytes = GenerateSection78PdfBytes(
            aggregate,
            wording,
            formType);

        string propertyDesc = que.Property_Desc
            ?? obj6.Old_Property_Description
            ?? "NA";

        string category = que.Property_Type
            ?? obj6.Old_Category
            ?? "NA";

        string submissionType = isReview
            ? "Section78Review"
            : "Section78Query";

        string fileName = BuildSubmittedFormFileName(
            queryRef,
            propertyDesc,
            category,
            submissionType,
            DateTime.Now);

        string filePath = Path.Combine(folderPath, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);

        _logger.LogInformation(
            "[Submitted Form PDF] {SubmissionType} form saved for {ReferenceNumber} at {FilePath}",
            submissionType,
            queryRef,
            filePath);

        return new SubmittedFormPdfResult
        {
            ReferenceNumber = queryRef,
            FileName = fileName,
            FilePath = filePath,
            PdfBytes = pdfBytes,
            SubmissionType = submissionType
        };
    }

    // ─────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────
    private string GetConnectionStringForRoll(string rollSource)
    {
        var normalized = NormalizeRollSource(rollSource);

        var connectionKey = normalized switch
        {
            "Objection" => "DefaultConnection",

            "Objection_Supp1" => "Sup1Connection",
            "Objection_Supp2" => "Sup2Connection",
            "Objection_Supp3" => "Sup3Connection",
            "Objection_Supp4" => "Sup4Connection",
            "Objection_Supp5" => "Sup5Connection",

            _ => "DefaultConnection"
        };

        var connStr = _config.GetConnectionString(connectionKey);

        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionKey}' was not found for rollSource '{rollSource}'.");
        }

        return connStr;
    }

    private static string NormalizeRollSource(string? rollSource)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
            return "Objection";

        var value = rollSource.Trim();

        return value.ToUpperInvariant() switch
        {
            "GV23" => "Objection",

            "GV23-SUP1" => "Objection_Supp1",
            "GV23-SUP2" => "Objection_Supp2",
            "GV23-SUP3" => "Objection_Supp3",
            "GV23-SUP4" => "Objection_Supp4",
            "GV23-SUP5" => "Objection_Supp5",

            "SUP1" => "Objection_Supp1",
            "SUP2" => "Objection_Supp2",
            "SUP3" => "Objection_Supp3",
            "SUP4" => "Objection_Supp4",
            "SUP5" => "Objection_Supp5",

            "OBJECTION_SUPP1" => "Objection_Supp1",
            "OBJECTION_SUPP2" => "Objection_Supp2",
            "OBJECTION_SUPP3" => "Objection_Supp3",
            "OBJECTION_SUPP4" => "Objection_Supp4",
            "OBJECTION_SUPP5" => "Objection_Supp5",

            _ => value
        };
    }

    private static string ResolveObjectionAppealDetailProc(string formType)
    {
        return formType switch
        {
            "Res" => "usp_GetFormA_Data",
            "Bus" => "usp_GetFormB_Data",
            "Agric" => "usp_GetFormC_Data",
            "Multi" => "usp_GetFormD_Data",

            _ => throw new NotSupportedException(
                $"No stored procedure mapped for property type '{formType}'.")
        };
    }

    private static async Task<string> ResolvePropertyTypeFromDbAsync(
        SqlConnection conn,
        string referenceNo,
        bool isAppeal)
    {
        if (isAppeal)
        {
            var type = await conn.ExecuteScalarAsync<string>(
                @"
                SELECT TOP 1
                       COALESCE(
                           NULLIF(LTRIM(RTRIM(a.A_Property_Type)), ''),
                           NULLIF(LTRIM(RTRIM(o.Property_Type)), '')
                       )
                FROM dbo.Obj_Property_Info_Appeal a
                LEFT JOIN dbo.Obj_Property_Info o
                       ON LTRIM(RTRIM(o.Objection_No)) = LTRIM(RTRIM(a.Obj_Ref))
                WHERE LTRIM(RTRIM(a.Appeal_No)) = LTRIM(RTRIM(@Ref));
                ",
                new { Ref = referenceNo });

            return string.IsNullOrWhiteSpace(type)
                ? "Res"
                : type.Trim();
        }

        var objectionType = await conn.ExecuteScalarAsync<string>(
            @"
            SELECT TOP 1 Property_Type
            FROM dbo.Obj_Property_Info
            WHERE LTRIM(RTRIM(Objection_No)) = LTRIM(RTRIM(@Ref));
            ",
            new { Ref = referenceNo });

        return string.IsNullOrWhiteSpace(objectionType)
            ? "Res"
            : objectionType.Trim();
    }

    private static byte[] GenerateObjectionAppealPdfBytes(
        InquiryAggregate aggregate,
        Wording wording,
        string formType,
        IWebHostEnvironment env)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return formType switch
        {
            "Res" => new FormADocument(aggregate, wording, env).GeneratePdf(),
            "Bus" => new FormBDocument(aggregate, wording, env).GeneratePdf(),
            "Agric" => new FormCDocument(aggregate, wording, env).GeneratePdf(),
            "Multi" => new FormDDocument(aggregate, wording, env).GeneratePdf(),

            _ => throw new NotSupportedException(
                $"Cannot generate submitted form PDF for property type '{formType}'.")
        };
    }

    private static byte[] GenerateSection78PdfBytes(
        InquiryAggregate aggregate,
        Wording wording,
        string formType)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return formType switch
        {
            "Agric" => new QueryFarmDocument(aggregate, wording).GeneratePdf(),
            _ => new QueryFormBDocument(aggregate, wording).GeneratePdf()
        };
    }

    private static string BuildObjectionAppealFormFileName(
        string referenceNumber,
        string? category,
        string submissionType,
        DateTime submittedDate)
    {
        string safeRef = SanitizeFileName(referenceNumber);
        string safeCategory = SanitizeFileName(category);
        string safeType = SanitizeFileName(submissionType);
        string datePart = submittedDate.ToString("yyyyMMdd_HHmmss");

        return $"{safeRef}_{safeCategory}_{safeType}_Form_{datePart}.pdf";
    }

    private static string BuildSubmittedFormFileName(
        string referenceNumber,
        string? propertyDesc,
        string? category,
        string submissionType,
        DateTime submittedDate)
    {
        string safeRef = SanitizeFileName(referenceNumber);
        string safeDesc = SanitizeFileName(propertyDesc);
        string safeCategory = SanitizeFileName(category);
        string safeType = SanitizeFileName(submissionType);
        string datePart = submittedDate.ToString("yyyyMMdd_HHmmss");

        return $"{safeRef}_{safeDesc}_{safeCategory}_{safeType}_Form_{datePart}.pdf";
    }

    private static string SanitizeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "NA";

        var invalidChars = Path.GetInvalidFileNameChars();

        var cleaned = new string(value
            .Trim()
            .Where(c => !invalidChars.Contains(c))
            .ToArray());

        cleaned = cleaned
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace(":", "_")
            .Replace(";", "_")
            .Replace(",", "_")
            .Replace(".", "_");

        while (cleaned.Contains("__"))
            cleaned = cleaned.Replace("__", "_");

        return string.IsNullOrWhiteSpace(cleaned)
            ? "NA"
            : cleaned.Trim('_');
    }

    private static string NormalisePropertyType(string? propertyType)
    {
        var value = propertyType?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return "Res";

        if (value.Equals("Res", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Residential", StringComparison.OrdinalIgnoreCase))
            return "Res";

        if (value.Equals("Bus", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Business", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Commercial", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Industrial", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Office", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("School", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Hospital", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Hotel", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Shopping", StringComparison.OrdinalIgnoreCase))
            return "Bus";

        if (value.Equals("Agric", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Agricultural", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Agriculture", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Farm", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Vacant", StringComparison.OrdinalIgnoreCase))
            return "Agric";

        if (value.Equals("Multi", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Multiple", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Multi", StringComparison.OrdinalIgnoreCase))
            return "Multi";

        return value;
    }
}