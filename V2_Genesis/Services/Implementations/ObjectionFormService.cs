using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using System.Net.Mime;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Emails;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Notice;
using V2_Genesis.Services.Objection;
using Dapper;

namespace V2_Genesis.Services.Implementations;

public class ObjectionFormService : IObjectionFormService
{
    private readonly ApplicationDbContext _db;
    private readonly ObjectionRollSettings _rollSettings;
    private readonly IConfiguration _config;
    private readonly ILogger<ObjectionFormService> _logger;
    private readonly INoticeService _noticeService;
    private readonly IEmailService _emailService;
    private readonly ISubmittedFormPdfService _submittedFormPdfService;
  

    public ObjectionFormService(
        ApplicationDbContext db,
        IOptions<ObjectionRollSettings> rollOpts,
        IConfiguration config,
        ILogger<ObjectionFormService> logger,INoticeService noticeService,IEmailService emailService , ISubmittedFormPdfService submittedFormPdfService)
    {
        _db = db;
        _rollSettings = rollOpts.Value;
        _config = config;
        _noticeService = noticeService;
        _logger = logger;
        _emailService = emailService;
        _submittedFormPdfService = submittedFormPdfService;
    }

    public async Task<ObjectionSubmitResult> SubmitAsync(
 string rollSource,
 string userId,
 string appealStat,
 string? objAppeal,
 string? propertyFrom,
 Obj_Property_InfoModel obj,
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
 Obj_Files objFile,
 List<IFormFile> files,
 List<IFormFile> fileR,
 Obj_Property_Info_AppealModel appeal)
    {
        rollSource = NormalizeRollSource(rollSource);

        propertyFrom = NormalizePropertyFrom(propertyFrom, rollSource);

        var cfg = _rollSettings.For(rollSource);

        bool isApp = appealStat == "True";
        bool isMulti = obj.Property_Type?.Trim().Equals("Multi", StringComparison.OrdinalIgnoreCase) == true;

        try
        {
            await using var db = CreateDbContextForRoll(rollSource);

            if (isApp)
            {
                return await SubmitAppealAsync(
                    db,
                    rollSource,
                    cfg,
                    userId,
                    objAppeal,
                    propertyFrom,
                    obj,
                    obj1,
                    obj2,
                    objR3,
                    objB3,
                    objA3,
                    objB4,
                    objR4,
                    obj5,
                    obj6,
                    obj7,
                    objFile,
                    files,
                    fileR,
                    appeal,
                    isMulti);
            }

            return await SubmitObjectionAsync(
                db,
                rollSource,
                 propertyFrom,
                cfg,
                userId,

                obj,
               
                obj1,
                obj2,
                objR3,
                objB3,
                objA3,
                objB4,
                objR4,
                obj5,
                obj6,
                obj7,
                objFile,
                files,
                fileR,
                isMulti);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ObjectionFormService] Submission failed for roll {RollSource}",
                rollSource);

            return new ObjectionSubmitResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    private static string NormalizePropertyFrom(string? propertyFrom, string rollSource)
    {
        if (string.IsNullOrWhiteSpace(propertyFrom))
            return RollSourceToSourceTableValue(rollSource);

        var value = propertyFrom.Trim();

        if (value.Equals("LIS", StringComparison.OrdinalIgnoreCase))
            return "LIS";

        if (value.Equals("Omission", StringComparison.OrdinalIgnoreCase))
            return "Omission";

        if (value.Equals("Omitted", StringComparison.OrdinalIgnoreCase))
            return "Omission";

        return value;
    }

    private static string RollSourceToSourceTableValue(string rollSource)
    {
        rollSource = NormalizeRollSource(rollSource);

        return rollSource switch
        {
            "Objection_Supp5" => "GV23-SUP5",
            "Objection_Supp4" => "GV23-SUP4",
            "Objection_Supp3" => "GV23-SUP3",
            "Objection_Supp2" => "GV23-SUP2",
            "Objection_Supp1" => "GV23-SUP1",
            "Objection" => "GV23",
            _ => rollSource
        };
    }
    private ApplicationDbContext CreateDbContextForRoll(string rollSource)
    {
        rollSource = NormalizeRollSource(rollSource);

        var connectionKey = GetConnectionKeyFromRollSource(rollSource);

        var connectionString = _config.GetConnectionString(connectionKey);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string '{connectionKey}' was not found.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string NormalizeRollSource(string? rollSource)
    {
        if (string.IsNullOrWhiteSpace(rollSource))
            return "Objection_Supp3";

        return rollSource.Trim() switch
        {
            "GV23" => "Objection",
            "GV23-SUP1" => "Objection_Supp1",
            "GV23-SUP2" => "Objection_Supp2",
            "GV23-SUP3" => "Objection_Supp3",
            "GV23-SUP4" => "Objection_Supp4",
            "GV23-SUP5" => "Objection_Supp5",

            "Sup1" => "Objection_Supp1",
            "Sup2" => "Objection_Supp2",
            "Sup3" => "Objection_Supp3",
            "Sup4" => "Objection_Supp4",
            "Sup5" => "Objection_Supp5",

            "SUP1" => "Objection_Supp1",
            "SUP2" => "Objection_Supp2",
            "SUP3" => "Objection_Supp3",
            "SUP4" => "Objection_Supp4",
            "SUP5" => "Objection_Supp5",

            _ => rollSource.Trim()
        };
    }

    private static string GetConnectionKeyFromRollSource(string rollSource)
    {
        rollSource = NormalizeRollSource(rollSource);

        return rollSource switch
        {
            "Objection_Supp5" => "Sup5Connection",
            "Objection_Supp4" => "Sup4Connection",
            "Objection_Supp3" => "Sup3Connection",
            "Objection_Supp2" => "Sup2Connection",
            "Objection_Supp1" => "Sup1Connection",
            "Objection" => "DefaultConnection",
            _ => "DefaultConnection"
        };
    }

    // ── OBJECTION ────────────────────────────────────────────────────
    private async Task<ObjectionSubmitResult> SubmitObjectionAsync(
     ApplicationDbContext db,
     string rollSource,
     string propertyFrom,
     ObjectionRollEntry cfg,
     string userId,
     Obj_Property_InfoModel obj,
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
     Obj_Files objFile,
     List<IFormFile> files,
     List<IFormFile> fileR,
     bool isMulti)
    {
        // ── 1. Property Info ─────────────────────────────────────────
        obj.PropertyFrom = propertyFrom;
        obj.UserID = userId;
        obj.objection_Status = "Obj-Lodging";

        obj.Property_Type = NormalizePropertyType(obj.Property_Type, isMulti);

        db.Obj_Property_Info.Add(obj);
        await db.SaveChangesAsync();

        int objId = Convert.ToInt32(obj.Objection_ID);

        // If Objection_No is generated by DB trigger, reload it.
        await db.Entry(obj).ReloadAsync();

        var objRef = obj.Objection_No;

        // Fallback only if DB trigger did not generate Objection_No.
        if (string.IsNullOrWhiteSpace(objRef))
        {
            objRef = $"{cfg.ObjPrefix}-{objId}";

            obj.Objection_No = objRef;
            db.Obj_Property_Info.Update(obj);
            await db.SaveChangesAsync();
        }

        // ── 2. Sections 1–7 ──────────────────────────────────────────
        obj1.Ref = objId;
        obj1.Objection_Ref_S1 = objRef;
        db.Obj_Section1.Add(obj1);

        obj2.Ref = objId;
        obj2.Objection_Ref_S2 = objRef;
        db.Obj_Section2.Add(obj2);

        objA3.Ref = objId;
        objA3.Objection_Ref_SA3 = objRef;
        db.Obj_Section3Agri.Add(objA3);

        objB3.Ref = objId;
        objB3.Objection_Ref_SB3 = objRef;
        db.Obj_Section3Bus.Add(objB3);

        objR3.Ref = objId;
        objR3.Objection_Ref_SR3 = objRef;
        db.Obj_Section3Res.Add(objR3);

        objB4.Ref = objId;
        objB4.Objection_Ref_SB4 = objRef;
        db.Obj_Section4Bus.Add(objB4);

        objR4.Ref = objId;
        objR4.Objection_Ref_SR4 = objRef;
        db.Obj_Section4Res.Add(objR4);

        obj5.Ref = objId;
        obj5.Objection_Ref_S5 = objRef;
        db.Obj_Section5.Add(obj5);


        NormaliseSection6MoneyForDb(obj6);

        obj6.Ref = objId;
        obj6.Objection_Ref_S6 = objRef;
        db.Obj_Section6.Add(obj6);

        obj7.Ref = objId;
        obj7.Objection_Ref_S7 = objRef;
        obj7.RandomPin = GeneratePin();
        obj7.Section51Pin = GeneratePin();
        db.Obj_Section7.Add(obj7);

        await db.SaveChangesAsync();

        // ── 3. File upload / evidence folder ─────────────────────────
        int count = await SaveFilesAsync(
            cfg.FileRootPath,
            objRef,
            files ?? new List<IFormFile>(),
            fileR ?? new List<IFormFile>(),
            objFile);

        objFile.Ref = objId;
        objFile.Objection_Ref_files = objRef;
        objFile.Evidence_count = count;

        db.Obj_Files.Add(objFile);
        await db.SaveChangesAsync();

        // ── 4. Generate acknowledgement + populated form + send email ─
        await GeneratePdfAndSendEmailAsync(
            rollSource: rollSource,
            cfg: cfg,
            referenceNo: objRef,
            pin: obj7.RandomPin,
            isAppeal: false,
            isMulti: isMulti,
            obj: obj,
            appeal: null,
            obj1: obj1,
            obj2: obj2,
            objR3: objR3,
            objB3: objB3,
            objA3: objA3,
            objB4: objB4,
            objR4: objR4,
            obj5: obj5,
            obj6: obj6,
            obj7: obj7,
            fileCount: count, objFile: objFile);

        return new ObjectionSubmitResult
        {
            Success = true,
            ObjectionNo = objRef,
            Pin = obj7.RandomPin,
            IsMulti = isMulti,
            IsAppeal = false
        };
    }

    private static void NormaliseSection6MoneyForDb(Obj_Section6Model obj6)
    {
        obj6.Old_Market_Value = MoneyToPlainNumber(obj6.Old_Market_Value);
        obj6.New_Market_Value = MoneyToPlainNumber(obj6.New_Market_Value);

        obj6.Old2_Market_Value = MoneyToPlainNumber(obj6.Old2_Market_Value);
        obj6.New2_Market_Value = MoneyToPlainNumber(obj6.New2_Market_Value);

        obj6.Old3_Market_Value = MoneyToPlainNumber(obj6.Old3_Market_Value);
        obj6.New3_Market_Value = MoneyToPlainNumber(obj6.New3_Market_Value);
    }

    private static string? MoneyToPlainNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var digits = new string(value.Where(char.IsDigit).ToArray());

        return string.IsNullOrWhiteSpace(digits)
            ? null
            : digits;
    }
    // ── APPEAL ───────────────────────────────────────────────────────
    private async Task<ObjectionSubmitResult> SubmitAppealAsync(
        ApplicationDbContext db,
        string rollSource,
        ObjectionRollEntry cfg,
        string userId,
        string propertyFrom,
        string? objAppeal,
        Obj_Property_InfoModel obj,
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
        Obj_Files objFile,
        List<IFormFile> files,
        List<IFormFile> fileR,
        Obj_Property_Info_AppealModel appeal,
        bool isMulti)
    {
        // ── 1. Appeal Info ───────────────────────────────────────────
        var appealPropertyType = NormalizePropertyType(obj.Property_Type, isMulti);

       
        appeal.A_UserID = userId;
        appeal.Appeal_Status = "App-Lodging";
        appeal.Obj_Ref = objAppeal;

        // Important: set these BEFORE first SaveChangesAsync().
        appeal.Appeal_Type = obj.Objector_Type;
        appeal.A_Property_Type = appealPropertyType;
        appeal.A_Property_Desc = obj.Property_Desc;
        appeal.A_Premise_id = obj.Premise_id;
        appeal.A_Unit_key = obj.Unit_key;
        appeal.A_Property_id = obj.Property_id;
        appeal.A_Valuation_Key = obj.Valuation_Key;
        appeal.A_Sector = obj.Sector;

        db.Obj_Property_Info_Appeal.Add(appeal);
        await db.SaveChangesAsync();

        int appId = Convert.ToInt32(appeal.Appeal_ID);

        // If Appeal_No is generated by DB trigger in future, reload first.
        await db.Entry(appeal).ReloadAsync();

        var appNo = appeal.Appeal_No;

        // Current fallback: generate Appeal_No from configured prefix.
        if (string.IsNullOrWhiteSpace(appNo))
        {
            appNo = $"{cfg.AppealPrefix}-{appId}";

            appeal.Appeal_No = appNo;
            db.Obj_Property_Info_Appeal.Update(appeal);
            await db.SaveChangesAsync();
        }

        // Keep obj in sync because the PDF service uses obj + appeal.
        obj.Property_Type = appealPropertyType;

        // ── 2. Sections 1–7 ──────────────────────────────────────────
        obj1.Ref = appId;
        obj1.Appeal_Ref_S1 = appId;
        obj1.Objection_Ref_S1 = appNo;
        db.Obj_Section1.Add(obj1);

        obj2.Ref = appId;
        obj2.Appeal_Ref_S2 = appId;
        obj2.Objection_Ref_S2 = appNo;
        db.Obj_Section2.Add(obj2);

        objA3.Ref = appId;
        objA3.Appeal_Ref_SA3 = appId;
        objA3.Objection_Ref_SA3 = appNo;
        db.Obj_Section3Agri.Add(objA3);

        objB3.Ref = appId;
        objB3.Appeal_Ref_SB3 = appId;
        objB3.Objection_Ref_SB3 = appNo;
        db.Obj_Section3Bus.Add(objB3);

        objR3.Ref = appId;
        objR3.Appeal_Ref_SR3 = appId;
        objR3.Objection_Ref_SR3 = appNo;
        db.Obj_Section3Res.Add(objR3);

        objB4.Ref = appId;
        objB4.Appeal_Ref_SB4 = appId;
        objB4.Objection_Ref_SB4 = appNo;
        db.Obj_Section4Bus.Add(objB4);

        objR4.Ref = appId;
        objR4.Appeal_Ref_SR4 = appId;
        objR4.Objection_Ref_SR4 = appNo;
        db.Obj_Section4Res.Add(objR4);

        obj5.Ref = appId;
        obj5.Appeal_Ref_S5 = appId;
        obj5.Objection_Ref_S5 = appNo;
        db.Obj_Section5.Add(obj5);

        obj6.Ref = appId;
        obj6.Appeal_Ref_S6 = appId;
        obj6.Objection_Ref_S6 = appNo;
        db.Obj_Section6.Add(obj6);

        obj7.Ref = appId;
        obj7.Appeal_Ref_S7 = appId;
        obj7.Objection_Ref_S7 = appNo;
        obj7.RandomPin = GeneratePin();
        obj7.Section51Pin = GeneratePin();
        db.Obj_Section7.Add(obj7);

        await db.SaveChangesAsync();

        // ── 3. File upload / evidence folder ─────────────────────────
        int count = await SaveFilesAsync(
            cfg.AppealRootPath,
            appNo,
            files ?? new List<IFormFile>(),
            fileR ?? new List<IFormFile>(),
            objFile);

        objFile.Ref = appId;
        objFile.Objection_Ref_files = appNo;
        objFile.Evidence_count = count;
        objFile.Appeal_Ref_files = 1;

        db.Obj_Files.Add(objFile);
        await db.SaveChangesAsync();

        // ── 4. Generate acknowledgement + populated form + send email ─
        await GeneratePdfAndSendEmailAsync(
            rollSource: rollSource,
            cfg: cfg,
            referenceNo: appNo,
            pin: obj7.RandomPin,
            isAppeal: true,
            isMulti: isMulti,
            obj: obj,
            appeal: appeal,
            obj1: obj1,
            obj2: obj2,
            objR3: objR3,
            objB3: objB3,
            objA3: objA3,
            objB4: objB4,
            objR4: objR4,
            obj5: obj5,
            obj6: obj6,
            obj7: obj7,
            fileCount: count, objFile: objFile);

        return new ObjectionSubmitResult
        {
            Success = true,
            ObjectionNo = appNo,
            Pin = obj7.RandomPin,
            IsMulti = isMulti,
            IsAppeal = true
        };
    }

    private async Task GeneratePdfAndSendEmailAsync(
        string rollSource,
        ObjectionRollEntry cfg,
        string referenceNo,
        string pin,
        bool isAppeal,
        bool isMulti,
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
        int fileCount, Obj_Files objFile)
    {
        try
        {
            var folderPath = isAppeal
                ? Path.Combine(cfg.AppealRootPath, referenceNo)
                : Path.Combine(cfg.FileRootPath, referenceNo);

            Directory.CreateDirectory(folderPath);

            var acknowledgementData = new AcknowledgementData
            {
                ObjectionNo = pin,
                ObjectionRef = referenceNo,
                RollSource = rollSource,
                SubmissionTime = DateTime.Now.ToString("dd MMMM yyyy HH:mm"),
                IsMulti = isMulti,
                IsAppeal = isAppeal,
                FileCount = fileCount,
                ObjectionReason = obj6.Objection_Reasons,

                Old_PropertyDescription = obj6.Old_Property_Description,
                Old_Category = obj6.Old_Category,
                Old_Address = obj6.Old_Address,
                Old_Extent = obj6.Old_Extent,
                Old_MarketValue = obj6.Old_Market_Value,
                Old_Owner = obj6.Old_Owner,

                New_PropertyDescription = obj6.New_Property_Description,
                New_Category = obj6.New_Category,
                New_Address = obj6.New_Address,
                New_Extent = obj6.New_Extent,
                New_MarketValue = obj6.New_Market_Value,
                New_Owner = obj6.New_Owner,

                Old2_Category = obj6.Old2_Category,
                Old2_Extent = obj6.Old2_Extent,
                Old2_MarketValue = obj6.Old2_Market_Value,

                New2_Category = obj6.New2_Category,
                New2_Extent = obj6.New2_Extent,
                New2_MarketValue = obj6.New2_Market_Value,

                Old3_Category = obj6.Old3_Category,
                Old3_Extent = obj6.Old3_Extent,
                Old3_MarketValue = obj6.Old3_Market_Value,

                New3_Category = obj6.New3_Category,
                New3_Extent = obj6.New3_Extent,
                New3_MarketValue = obj6.New3_Market_Value,

                ValuationKey = isAppeal
                    ? appeal?.A_Valuation_Key ?? obj.Valuation_Key
                    : obj.Valuation_Key,

                    UploadedDocumentNames = GetUploadedDocumentNames(objFile),
            };

            // 1. Generate acknowledgement PDF
            var (ackPdfBytes, ackFileName) = await _noticeService
      .GenerateAcknowledgementAsync(acknowledgementData);
            if (ackPdfBytes == null || ackPdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Acknowledgement PDF is empty for {referenceNo}.");
            }

            // 2. Save acknowledgement PDF in evidence folder
            var ackPath = Path.Combine(folderPath, ackFileName);

            await File.WriteAllBytesAsync(ackPath, ackPdfBytes);

            await File.WriteAllBytesAsync(ackPath, ackPdfBytes);

            _logger.LogInformation(
                "[ObjectionFormService] Acknowledgement PDF saved for {ReferenceNo}. Path: {Path}",
                referenceNo,
                ackPath);

            // 3. Generate populated Form A/B/C/D PDF
            SubmittedFormPdfResult submittedFormPdf;

            try
            {
                // Preferred method: use the stored procedures from GV_Forms
                submittedFormPdf = await _submittedFormPdfService
                    .GenerateObjectionOrAppealFormFromDbAsync(
                        rollSource,
                        isAppeal,
                        referenceNo,
                        folderPath,
                        DateTime.Now);
            }
            catch (Exception dbPdfEx)
            {
                _logger.LogError(
                    dbPdfEx,
                    "[ObjectionFormService] Stored-procedure PDF failed for {ReferenceNo}. Trying posted-model fallback.",
                    referenceNo);

                // Fallback method: use the posted models
                submittedFormPdf = await _submittedFormPdfService
                    .GenerateObjectionOrAppealFormAsync(
                        isAppeal,
                        folderPath,
                        obj,
                        appeal,
                        obj1,
                        obj2,
                        objR3,
                        objB3,
                        objA3,
                        objB4,
                        objR4,
                        obj5,
                        obj6,
                        obj7,
                        DateTime.Now);
            }

            if (submittedFormPdf.PdfBytes == null || submittedFormPdf.PdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Submitted form PDF is empty for {referenceNo}.");
            }

            if (!File.Exists(submittedFormPdf.FilePath))
            {
                await File.WriteAllBytesAsync(
                    submittedFormPdf.FilePath,
                    submittedFormPdf.PdfBytes);
            }

            _logger.LogInformation(
                "[ObjectionFormService] Submitted form PDF ready for {ReferenceNo}. File: {FileName}. Size: {Size} bytes. Path: {Path}",
                referenceNo,
                submittedFormPdf.FileName,
                submittedFormPdf.PdfBytes.Length,
                submittedFormPdf.FilePath);

            // 4. Attach populated form PDF to email
            var extraAttachments = new List<EmailAttachment>
        {
            new EmailAttachment
            {
                FileName = submittedFormPdf.FileName,
                FileBytes = submittedFormPdf.PdfBytes,
                ContentType = MediaTypeNames.Application.Pdf
            }
        };

            // 5. Send email once only.
            // The acknowledgement PDF is passed separately.
            // The populated Form A/B/C/D is passed as extraAttachments.
            await _emailService.SendObjectionAcknowledgementAsync(
                referenceNo,
                rollSource,
                isAppeal,
                ackPdfBytes,
                folderPath,
                extraAttachments);

            _logger.LogInformation(
                "[ObjectionFormService] PDFs and email completed for {ReferenceNo}. Folder: {Folder}",
                referenceNo,
                folderPath);
        }
        catch (Exception ex)
        {
            // Submission already saved, so we only log the PDF/email issue.
            _logger.LogError(
                ex,
                "[ObjectionFormService] Failed to generate PDFs/send email for {ReferenceNo}",
                referenceNo);
        }
    }
    private static string NormalizePropertyType(string? propertyType, bool isMulti)
    {
        if (isMulti)
            return "Multi";

        if (string.IsNullOrWhiteSpace(propertyType))
            return "";

        var value = propertyType.Trim();

        if (value.Equals("Residential", StringComparison.OrdinalIgnoreCase))
            return "Res";

        if (value.Equals("Business", StringComparison.OrdinalIgnoreCase))
            return "Bus";

        if (value.Equals("Commercial", StringComparison.OrdinalIgnoreCase))
            return "Bus";

        if (value.Equals("Agricultural", StringComparison.OrdinalIgnoreCase))
            return "Agric";

        if (value.Equals("Agriculture", StringComparison.OrdinalIgnoreCase))
            return "Agric";

        if (value.Equals("Agric", StringComparison.OrdinalIgnoreCase))
            return "Agric";

        if (value.Equals("Res", StringComparison.OrdinalIgnoreCase))
            return "Res";

        if (value.Equals("Bus", StringComparison.OrdinalIgnoreCase))
            return "Bus";

        if (value.Equals("Multi", StringComparison.OrdinalIgnoreCase))
            return "Multi";

        return value;
    }
    // ── Helpers ───────────────────────────────────────────────────────
    private static string GeneratePin()
        => new Random().Next(100000, 999999).ToString();

    private static async Task<int> SaveFilesAsync(
        string rootPath,
        string folder,
        List<IFormFile> files,
        List<IFormFile> fileR,
        Obj_Files objFile)
    {
        string dir = Path.Combine(rootPath, folder);
        Directory.CreateDirectory(dir);

        // Representative letter
        foreach (var f in fileR)
        {
            if (f == null || f.Length == 0)
                continue;

            string repDir = Path.Combine(dir, "Representative Letter");
            Directory.CreateDirectory(repDir);

            string name = Path.GetFileName(f.FileName);

            await using var stream = File.Create(Path.Combine(repDir, name));
            await f.CopyToAsync(stream);

            objFile.Rep_letter = name;
        }

        // Evidence files
        int count = 0;

        foreach (var f in files)
        {
            if (f == null || f.Length == 0)
                continue;

            count++;

            string name = Path.GetFileName(f.FileName);

            await using var stream = File.Create(Path.Combine(dir, name));
            await f.CopyToAsync(stream);

            SetFileSlot(objFile, count, name);
        }

        return count;
    }

    private static void SetFileSlot(Obj_Files f, int slot, string name)
    {
        switch (slot)
        {
            case 1: f.Files1 = name; break;
            case 2: f.Files2 = name; break;
            case 3: f.Files3 = name; break;
            case 4: f.Files4 = name; break;
            case 5: f.Files5 = name; break;
            case 6: f.Files6 = name; break;
            case 7: f.Files7 = name; break;
            case 8: f.Files8 = name; break;
            case 9: f.Files9 = name; break;
            case 10: f.Files10 = name; break;
        }
    }
    public async Task<(bool Success, string? Error)> WithdrawAsync(
  string objectionNo,
  string withdrawType,
  string rollSource,
  string userId)
    {
        if (string.IsNullOrWhiteSpace(objectionNo))
            return (false, "Objection / reference number is required.");

        objectionNo = objectionNo.Trim();
        withdrawType = withdrawType?.Trim() ?? string.Empty;
        rollSource = NormalizeRollSource(rollSource);

        bool isAppeal = withdrawType.Contains("Appeal", StringComparison.OrdinalIgnoreCase);

        bool isReview = withdrawType.Contains("Review", StringComparison.OrdinalIgnoreCase);

        bool isQuery =
            withdrawType.Equals("Query", StringComparison.OrdinalIgnoreCase)
            || withdrawType.Equals("Section78", StringComparison.OrdinalIgnoreCase)
            || withdrawType.Contains("Section78", StringComparison.OrdinalIgnoreCase)
            || isReview;

        string submissionType = isAppeal
            ? "Appeal"
            : isReview
                ? "Review"
                : isQuery
                    ? "Query"
                    : "Objection";

        // ── Resolve connection ─────────────────────────────────────
        string connKey = isQuery
            ? "QueryConnection"
            : GetConnectionKeyFromRollSource(rollSource);

        string connStr = _config.GetConnectionString(connKey)
                      ?? _config.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException($"Connection string '{connKey}' was not found.");

        // ── Stored procedure name ──────────────────────────────────
        string spName = isAppeal
            ? "Obj_Withdraw_Appeal"
            : isQuery
                ? "Que_Withdraw"
                : "Obj_Withdraw";

        try
        {
            await using var conn = new SqlConnection(connStr);

            // 1. Execute withdrawal stored procedure
            await conn.ExecuteAsync(
                spName,
                new { Objection_No = objectionNo },
                commandType: CommandType.StoredProcedure);

            // 2. Save withdrawal audit record
            if (isQuery)
            {
                _db.Que_Withdrawals.Add(new Que_WithdrawalsModel
                {
                    Query_Withdrawn = objectionNo,
                    User = userId,
                });
            }
            else
            {
                _db.Obj_Withdrawals.Add(new Obj_WithdrawalsModel
                {
                    Objection_Withdrawn = objectionNo,
                    User = userId,
                });
            }

            await _db.SaveChangesAsync();

            // 3. Send withdrawal email to client
            try
            {
                var recipients = await ResolveWithdrawalRecipientsAsync(
                    connStr,
                    objectionNo,
                    isAppeal,
                    isQuery);

                if (!recipients.Any())
                {
                    _logger.LogWarning(
                        "[Withdrawal Email] No client email found for {Ref}. Type: {Type}",
                        objectionNo,
                        submissionType);
                }
                else
                {
                    var subject = $"City of Johannesburg — {submissionType} Withdrawal Confirmation: {objectionNo}";

                    foreach (var recipient in recipients)
                    {
                        var body = BuildWithdrawalEmailBody(
                            referenceNo: objectionNo,
                            submissionType: submissionType,
                            recipientName: recipient.Name);

                        await _emailService.SendEmailWithAttachmentsAsync(
                            toEmail: recipient.Address,
                            subject: subject,
                            body: body,
                            attachments: new List<EmailAttachment>(),
                            isHtml: true);

                        _logger.LogInformation(
                            "[Withdrawal Email] Sent {Type} withdrawal confirmation for {Ref} to {Email}",
                            submissionType,
                            objectionNo,
                            recipient.Address);
                    }
                }
            }
            catch (Exception emailEx)
            {
                // Withdrawal must remain successful even if email fails.
                _logger.LogError(
                    emailEx,
                    "[Withdrawal Email] Failed to send withdrawal email for {Ref}. Type: {Type}",
                    objectionNo,
                    submissionType);
            }

            _logger.LogInformation(
                "Withdrew {Type} {Ref} (roll: {Roll}) for user {User}.",
                submissionType,
                objectionNo,
                rollSource,
                userId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error withdrawing {Ref} (type: {Type}, roll: {Roll}).",
                objectionNo,
                withdrawType,
                rollSource);

            return (false, "An error occurred while processing the withdrawal. Please try again.");
        }
    }
    private async Task<List<EmailRecipient>> ResolveWithdrawalRecipientsAsync(
    string connStr,
    string referenceNo,
    bool isAppeal,
    bool isQuery)
    {
        await using var conn = new SqlConnection(connStr);

        string sql;

        if (isQuery)
        {
            sql = @"
            SELECT TOP 1
                s1.Owner_Name,
                s1.Owner_Email,
                s1.Objector_Name,
                s1.Objector_Email,
                s1.Representative_name,
                s1.Rep_Email,
                COALESCE(q.Objector_Type, q.Query_Type, q.Submission_Type, '') AS Objector_Type
            FROM dbo.Obj_Section1 s1
            LEFT JOIN dbo.QUE_Property_Info q
                   ON LTRIM(RTRIM(q.Query_No)) = LTRIM(RTRIM(@Ref))
            WHERE LTRIM(RTRIM(s1.Objection_Ref_S1)) = LTRIM(RTRIM(@Ref));";
        }
        else if (isAppeal)
        {
            sql = @"
            SELECT TOP 1
                s1.Owner_Name,
                s1.Owner_Email,
                s1.Objector_Name,
                s1.Objector_Email,
                s1.Representative_name,
                s1.Rep_Email,
                COALESCE(opi.Objector_Type, opia.Appeal_Type, '') AS Objector_Type
            FROM dbo.Obj_Section1 s1
            LEFT JOIN dbo.Obj_Property_Info_Appeal opia
                   ON LTRIM(RTRIM(opia.Appeal_No)) = LTRIM(RTRIM(@Ref))
            LEFT JOIN dbo.Obj_Property_Info opi
                   ON LTRIM(RTRIM(opi.Objection_No)) = LTRIM(RTRIM(opia.Obj_Ref))
            WHERE LTRIM(RTRIM(s1.Objection_Ref_S1)) = LTRIM(RTRIM(@Ref));";
        }
        else
        {
            sql = @"
            SELECT TOP 1
                s1.Owner_Name,
                s1.Owner_Email,
                s1.Objector_Name,
                s1.Objector_Email,
                s1.Representative_name,
                s1.Rep_Email,
                COALESCE(opi.Objector_Type, '') AS Objector_Type
            FROM dbo.Obj_Section1 s1
            LEFT JOIN dbo.Obj_Property_Info opi
                   ON LTRIM(RTRIM(opi.Objection_No)) = LTRIM(RTRIM(@Ref))
            WHERE LTRIM(RTRIM(s1.Objection_Ref_S1)) = LTRIM(RTRIM(@Ref));";
        }

        var row = await conn.QueryFirstOrDefaultAsync(sql, new { Ref = referenceNo });

        if (row is null)
        {
            _logger.LogWarning(
                "[Withdrawal Email] No Obj_Section1 recipient data found for {Ref}",
                referenceNo);

            return new List<EmailRecipient>();
        }

        var objectorType = row.Objector_Type?.ToString()?.Trim() ?? string.Empty;
        var recipients = new List<EmailRecipient>();

        if (objectorType.Equals("Owner", StringComparison.OrdinalIgnoreCase))
        {
            TryAddEmailRecipient(
                recipients,
                row.Owner_Name?.ToString(),
                row.Owner_Email?.ToString(),
                "Owner");
        }
        else if (objectorType.Equals("Representative", StringComparison.OrdinalIgnoreCase))
        {
            TryAddEmailRecipient(
                recipients,
                row.Owner_Name?.ToString(),
                row.Owner_Email?.ToString(),
                "Owner");

            TryAddEmailRecipient(
                recipients,
                row.Representative_name?.ToString(),
                row.Rep_Email?.ToString(),
                "Representative");
        }
        else if (
            objectorType.Equals("Third_Party", StringComparison.OrdinalIgnoreCase)
            || objectorType.Equals("Third Party", StringComparison.OrdinalIgnoreCase))
        {
            TryAddEmailRecipient(
                recipients,
                row.Objector_Name?.ToString(),
                row.Objector_Email?.ToString(),
                "Third Party");
        }
        else
        {
            // Safe fallback
            TryAddEmailRecipient(
                recipients,
                row.Owner_Name?.ToString(),
                row.Owner_Email?.ToString(),
                "Owner");

            if (!recipients.Any())
            {
                TryAddEmailRecipient(
                    recipients,
                    row.Objector_Name?.ToString(),
                    row.Objector_Email?.ToString(),
                    "Client");
            }
        }

        return recipients;
    }

    private static void TryAddEmailRecipient(
    List<EmailRecipient> list,
    string? name,
    string? email,
    string recipientType)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return;

        var cleanEmail = email.Trim();

        if (list.Any(x => x.Address.Equals(cleanEmail, StringComparison.OrdinalIgnoreCase)))
            return;

        list.Add(new EmailRecipient(
            name?.Trim() ?? cleanEmail,
            cleanEmail,
            recipientType));
    }

    private static string BuildWithdrawalEmailBody(
    string referenceNo,
    string submissionType,
    string recipientName)
    {
        var safeName = string.IsNullOrWhiteSpace(recipientName)
            ? "Valued Ratepayer"
            : recipientName.Trim();

        var date = DateTime.Now.ToString("dd MMMM yyyy HH:mm");

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
</head>
<body style='font-family:Arial,Helvetica,sans-serif;background:#f5f5f5;margin:0;padding:24px;'>
    <div style='max-width:760px;margin:0 auto;background:#ffffff;border-radius:10px;
                border:1px solid #ddd;overflow:hidden;'>

        <div style='background:#1a2e35;color:#ffffff;padding:18px 24px;
                    border-bottom:4px solid #e6b000;'>
            <h2 style='margin:0;font-size:20px;'>City of Johannesburg</h2>
            <div style='font-size:13px;color:#e6b000;font-weight:bold;margin-top:4px;'>
                Valuation Services Department
            </div>
        </div>

        <div style='padding:24px;color:#333;'>
            <p>Dear {safeName},</p>

            <p>
                This email confirms that your <strong>{submissionType}</strong>
                with reference number
                <strong>{referenceNo}</strong>
                has been successfully withdrawn.
            </p>

            <table style='width:100%;border-collapse:collapse;margin:18px 0;'>
                <tr>
                    <td style='padding:10px;border:1px solid #ddd;background:#f8f8f8;font-weight:bold;width:35%;'>
                        Reference Number
                    </td>
                    <td style='padding:10px;border:1px solid #ddd;'>
                        {referenceNo}
                    </td>
                </tr>
                <tr>
                    <td style='padding:10px;border:1px solid #ddd;background:#f8f8f8;font-weight:bold;'>
                        Submission Type
                    </td>
                    <td style='padding:10px;border:1px solid #ddd;'>
                        {submissionType}
                    </td>
                </tr>
                <tr>
                    <td style='padding:10px;border:1px solid #ddd;background:#f8f8f8;font-weight:bold;'>
                        Withdrawn Date
                    </td>
                    <td style='padding:10px;border:1px solid #ddd;'>
                        {date}
                    </td>
                </tr>
            </table>

            <p>
                No further processing will continue on this withdrawn submission.
            </p>

            <p style='margin-top:24px;'>
                Regards,<br/>
                <strong>City of Johannesburg<br/>Valuation Services Department</strong>
            </p>
        </div>

        <div style='background:#1a2e35;color:#ffffff;padding:12px 24px;
                    font-size:12px;text-align:center;'>
            This is an automated notification. Please do not reply to this email.
        </div>
    </div>
</body>
</html>";
    }
    // ══════════════════════════════════════════════════════════════
    //  UNLINK — remove a saved / linked property
    //
    //  Uses the LinkedProperties table in the DefaultConnection DB.
    //  Security: checks userId so a user cannot unlink another's property.
    // ══════════════════════════════════════════════════════════════
    public async Task<(bool Success, string? Error)> UnlinkPropertyAsync(
        long linkedId,
        string userId)
    {
        try
        {
            // Re-enable this DbSet in ApplicationDbContext if it is still
            // commented out:
            //   public DbSet<LinkedProperties> LinkedProperties { get; set; }
            var record = await _db.LinkedProperties
                .FirstOrDefaultAsync(p => p.ID == linkedId && p.UserID == userId);

            if (record is null)
                return (false, "Property not found or you do not have permission to unlink it.");

            _db.LinkedProperties.Remove(record);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "User {User} unlinked property record {Id}.", userId, linkedId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error unlinking property {Id} for user {User}.", linkedId, userId);
            return (false, "An error occurred while removing the property. Please try again.");
        }
    }
    private static List<string> GetUploadedDocumentNames(Obj_Files objFile)
    {
        var docs = new List<string>();

        void Add(string? name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                docs.Add(name.Trim());
        }

        Add(objFile.Files1);
        Add(objFile.Files2);
        Add(objFile.Files3);
        Add(objFile.Files4);
        Add(objFile.Files5);
        Add(objFile.Files6);
        Add(objFile.Files7);
        Add(objFile.Files8);
        Add(objFile.Files9);
        Add(objFile.Files10);

        if (!string.IsNullOrWhiteSpace(objFile.Rep_letter))
            docs.Add("Representative Letter: " + objFile.Rep_letter.Trim());

        return docs;
    }
}

