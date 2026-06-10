using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Emails;
using V2_Genesis.Models.Results;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Notice;
using V2_Genesis.Services.Objection;

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
        obj.PropertyFrom = cfg.ObjPrefix;
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
            fileCount: count);

        return new ObjectionSubmitResult
        {
            Success = true,
            ObjectionNo = objRef,
            Pin = obj7.RandomPin,
            IsMulti = isMulti,
            IsAppeal = false
        };
    }

    // ── APPEAL ───────────────────────────────────────────────────────
    private async Task<ObjectionSubmitResult> SubmitAppealAsync(
        ApplicationDbContext db,
        string rollSource,
        ObjectionRollEntry cfg,
        string userId,
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
            fileCount: count);

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
        int fileCount)
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
                    : obj.Valuation_Key
            };

            // 1. Generate acknowledgement PDF
            var (ackPdfBytes, _) = await _noticeService
                .GenerateAcknowledgementAsync(acknowledgementData);

            if (ackPdfBytes == null || ackPdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Acknowledgement PDF is empty for {referenceNo}.");
            }

            // 2. Save acknowledgement PDF in evidence folder
            var ackFileName = $"{referenceNo}_Acknowledgement.pdf";
            var ackPath = Path.Combine(folderPath, ackFileName);

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
}