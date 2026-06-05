using GV_Forms.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using V2_Genesis.Models;
// These namespaces will be added in the next step when we copy/adapt GV_Forms PDF classes:
// V2_Genesis.Models.Pdf -> InquiryAggregate, Wording
// V2_Genesis.Pdf        -> FormADocument, FormBDocument, FormCDocument, FormDDocument, QueryFormBDocument, QueryFarmDocument

using V2_Genesis.Models.Results;

using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class SubmittedFormPdfService : ISubmittedFormPdfService
{
    private readonly ILogger<SubmittedFormPdfService> _logger;
    private readonly IWebHostEnvironment _env;


    public SubmittedFormPdfService(
        ILogger<SubmittedFormPdfService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

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

        string referenceNumber = isAppeal
            ? appeal?.Appeal_No ?? string.Empty
            : obj.Objection_No ?? string.Empty;

        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new InvalidOperationException("Reference number is empty. The PDF cannot be generated.");

        string submissionType = isAppeal ? "Appeal" : "Objection";

        string propertyDesc = isAppeal
            ? appeal?.A_Property_Desc ?? obj.Property_Desc ?? string.Empty
            : obj.Property_Desc ?? string.Empty;

        string category = isAppeal
            ? appeal?.A_Property_Type ?? obj.Property_Type ?? string.Empty
            : obj.Property_Type ?? string.Empty;

        string formType = NormalisePropertyType(category);

        var aggregate = new InquiryAggregate
        {
            Main = isAppeal
                ? appeal ?? throw new InvalidOperationException("Appeal model is required for Appeal PDF.")
                : obj
        };

        aggregate.Sections["Section1"] = obj1;
        aggregate.Sections["Section2"] = obj2;

        /*
         GV_Forms expects different section placement depending on form type.

         Form A / B:
           Section3 = Residential or Business details
           Section4 = Residential or Business building details

         Form C:
           Section3 = Agricultural details
           Section4 = Section5
           Section5 = Section6
           Section6 = Section7

         Form D:
           Uses multiple section slots for Multi category.
        */
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

        var wording = Wording.ForType(submissionType);

        byte[] pdfBytes = GenerateObjectionAppealPdfBytes(
            aggregate,
            wording,
            formType,_env);

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

        string propertyDesc = que.Property_Desc ?? obj6.Old_Property_Description ?? string.Empty;
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

        var wording = Wording.ForType("Query");

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

    private static byte[] GenerateObjectionAppealPdfBytes(
        InquiryAggregate aggregate,
        Wording wording,
        string formType, IWebHostEnvironment env)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return formType switch
        {
            "Res" => new FormADocument(aggregate, wording,env).GeneratePdf(),
            "Bus" => new FormBDocument(aggregate, wording,env).GeneratePdf(),
            "Agric" => new FormCDocument(aggregate, wording,env).GeneratePdf(),
            "Multi" => new FormDDocument(aggregate, wording,env).GeneratePdf(),

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

        /*
         GV_Forms currently has:
           - QueryFormBDocument
           - QueryFarmDocument

         There is no Query Form A or Query Form D class in the GV_Forms zip.
         So for now:
           - Agricultural uses QueryFarmDocument
           - Everything else uses QueryFormBDocument

         If you later add QueryFormADocument or QueryFormDDocument,
         only this switch needs to change.
        */
        return formType switch
        {
            "Agric" => new QueryFarmDocument(aggregate, wording).GeneratePdf(),
            _ => new QueryFormBDocument(aggregate, wording).GeneratePdf()
        };
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