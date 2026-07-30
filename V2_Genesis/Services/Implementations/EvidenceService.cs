using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using V2_Genesis.Data;
using V2_Genesis.Models.Results.Atrributes;
using V2_Genesis.Models.Results.Evidence;
using V2_Genesis.Services.Evidence;
using V2_Genesis.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Models.Attributes;

namespace V2_Genesis.Services.Implementations;

public class EvidenceService : IEvidenceService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EvidenceService> _logger;
    private readonly IReadOnlyDictionary<string, EvidenceRollConfig> _registry;
    private readonly AttributesDbContext _attrDb;
    private const int MAX_FILES = 10;
    private const int MAX_FILE_MB = 3;
    private const int WINDOW_HOURS = 48;

    public EvidenceService(IConfiguration config,
        ILogger<EvidenceService> logger, AttributesDbContext attrDb)
    {
        _config = config;
        _logger = logger;
        _registry = EvidenceRollRegistry.Build(config);
        _attrDb = attrDb;
    }

    // ── Validate PIN + 48-hour window ─────────────────────────────────
    public async Task<EvidenceValidateResult> ValidateAsync(
        string rollSource, string objectionNo, string pin)
    {
        if (!_registry.TryGetValue(rollSource, out var cfg))
            return EvidenceValidateResult.Fail("Invalid roll source.");

        bool isAppeal = objectionNo.Trim().ToUpper().StartsWith("APP");

        try
        {
            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            await using var conn = new SqlConnection(connStr);

            // Step 1: validate objection + PIN via SP
            var rows = await conn.QueryAsync(
                cfg.ValidateSp,
                new { Objection_No = objectionNo.Trim() },
                commandType: CommandType.StoredProcedure);

            var record = rows.FirstOrDefault();
            if (record is null)
                return EvidenceValidateResult.Fail(
                    "Invalid objection number. Please check and try again.");

            string storedPin = ((IDictionary<string, object>)record)
                .TryGetValue("RandomPin", out var p) ? p?.ToString() ?? "" : "";

            if (!string.Equals(pin.Trim(), storedPin.Trim(),
                    StringComparison.Ordinal))
                return EvidenceValidateResult.Fail(
                    "Incorrect PIN. Please check and try again.");

            // Step 2: check the correct submission table and 48-hour window.
            dynamic? statusRow;

            if (isAppeal)
            {
                statusRow = await conn.QueryFirstOrDefaultAsync(
                    @"SELECT TOP 1
                          Appeal_Status AS Submission_Status,
                          Appeal_Start_DateTime AS Date_Submitted
                      FROM dbo.Obj_Property_Info_Appeal
                      WHERE LTRIM(RTRIM(Appeal_No)) =
                            LTRIM(RTRIM(@RefNo))",
                    new { RefNo = objectionNo.Trim() });
            }
            else
            {
                statusRow = await conn.QueryFirstOrDefaultAsync(
                    @"SELECT TOP 1
                          objection_Status AS Submission_Status,
                          COALESCE(Date_Submitted, Objection_Start_DateTime)
                              AS Date_Submitted
                      FROM dbo.Obj_Property_Info
                      WHERE LTRIM(RTRIM(Objection_No)) =
                            LTRIM(RTRIM(@RefNo))",
                    new { RefNo = objectionNo.Trim() });
            }

            if (statusRow is null)
            {
                return EvidenceValidateResult.Fail(
                    isAppeal
                        ? "Appeal record not found."
                        : "Objection record not found.");
            }

            string status =
                statusRow.Submission_Status?.ToString()?.Trim()
                ?? string.Empty;

            DateTime? submitted = null;

            object? submittedValue =
                statusRow.Date_Submitted;

            if (submittedValue is DateTime submittedDate)
            {
                submitted = submittedDate;
            }
            else
            {
                var submittedText =
                    Convert.ToString(
                        submittedValue,
                        CultureInfo.InvariantCulture);

                DateTime parsedDate = default;

                var parsedSuccessfully =
                    !string.IsNullOrWhiteSpace(submittedText)
                    && DateTime.TryParse(
                        submittedText,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AllowWhiteSpaces,
                        out parsedDate);

                if (parsedSuccessfully)
                    submitted = parsedDate;
            }

            var statusAllowsEvidence = isAppeal
                ? status.Equals(
                      "App-Lodging",
                      StringComparison.OrdinalIgnoreCase)
                  || status.Equals(
                      "App-Unallocated",
                      StringComparison.OrdinalIgnoreCase)
                : status.Equals(
                      "Obj-Lodging",
                      StringComparison.OrdinalIgnoreCase);

            bool withinWindow =
                statusAllowsEvidence
                && submitted.HasValue
                && DateTime.Now <=
                    submitted.Value.AddHours(WINDOW_HOURS);

            if (!withinWindow)
                return EvidenceValidateResult.Expired();

            // Step 3: get current file count
            var countRows = await conn.QueryAsync<dynamic>(
                cfg.EvidenceCountSp,
                new { Objection_No1 = objectionNo.Trim() },
                commandType: CommandType.StoredProcedure);

            int currentCount = 0;
            var countRow = countRows.FirstOrDefault();
            if (countRow is not null)
            {
                var d = (IDictionary<string, object>)countRow;
                if (d.TryGetValue("Evidence_count", out var ec))
                    currentCount = Convert.ToInt32(ec);
            }

            return EvidenceValidateResult.Ok(currentCount, isAppeal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Evidence] Validate failed for {ObjNo} on {Roll}",
                objectionNo, rollSource);
            return EvidenceValidateResult.Fail(
                "A system error occurred. Please try again.");
        }
    }

    // ── Upload files ──────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, int NewCount, List<string> FileNames)>
        UploadAsync(
            string rollSource, string objectionNo,
            bool isAppeal, int currentCount,
            List<IFormFile> files)
    {
        if (!_registry.TryGetValue(rollSource, out var cfg))
            return (false, "Invalid roll source.", currentCount, new());

        // Guard: file count
        int newCount = currentCount + files.Count;
        if (newCount > MAX_FILES)
            return (false,
                $"Cannot upload {files.Count} file(s). " +
                $"Maximum {MAX_FILES} allowed (you already have {currentCount}).",
                currentCount, new());

        string rootPath = isAppeal ? cfg.AppealRootPath : cfg.FileRootPath;
        string folder = Path.Combine(rootPath, objectionNo.Trim());
        Directory.CreateDirectory(folder);

        var savedNames = new List<string>();
        int fileIndex = currentCount;

        foreach (var file in files)
        {
            // Size guard
            if (file.Length > MAX_FILE_MB * 1024 * 1024)
                return (false,
                    $"File '{file.FileName}' exceeds {MAX_FILE_MB} MB limit.",
                    currentCount, new());

            fileIndex++;
            var fileName = Path.GetFileName(file.FileName);
            var path = Path.Combine(folder, fileName);

            await using (var stream = File.Create(path))
                await file.CopyToAsync(stream);

            savedNames.Add(fileName);
        }

        // Persist to DB
        try
        {
            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            await using var conn = new SqlConnection(connStr);

            int dbIndex = currentCount;
            foreach (var name in savedNames)
            {
                dbIndex++;
                await conn.ExecuteAsync(
                    cfg.UpdateFileSp,
                    new
                    {
                        FileName = name,
                        Objection_No = objectionNo.Trim(),
                        FileIndex = dbIndex
                    },
                    commandType: CommandType.StoredProcedure);
            }

            await conn.ExecuteAsync(
                cfg.UpdateCountSp,
                new { count = newCount, Objection_No = objectionNo.Trim() },
                commandType: CommandType.StoredProcedure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Evidence] DB update failed for {ObjNo}", objectionNo);
            return (false, $"Files saved but database update failed: {ex.Message}",
                currentCount, savedNames);
        }

        return (true, null, newCount, savedNames);
    }


    // ── ValidateAttributeAsync ───────────────────────────────────────

    public async Task<AttrEvidenceValidateResult> ValidateAttributeAsync(
        string attrNo, string pin)
    {
        try
        {
            // 1. Find declaration by Attr_No + EvidencePin
            var decl = await _attrDb.AttrDeclarations
                .FirstOrDefaultAsync(d =>
                    d.Attr_No == attrNo.Trim() &&
                    d.EvidencePin == pin.Trim());

            if (decl is null)
                return AttrEvidenceValidateResult.Fail(
                    "Invalid Attribute Number or PIN. Please check and try again.");

            // 2. Check PIN is still active and within window
            if (decl.PinIsActive != true ||
                decl.PinExpiryDateTime == null ||
                decl.PinExpiryDateTime <= DateTime.Now)
                return AttrEvidenceValidateResult.Expired();

            // 3. Get current file count + folder from AttrFiles
            var files = await _attrDb.AttrFiles
                .FirstOrDefaultAsync(f => f.Attr_No == attrNo.Trim());

            int currentCount = files?.Evidence_Count ?? 0;
            string? rootFolder = files?.RootFolder;

            if (currentCount >= 10)
                return AttrEvidenceValidateResult.Fail(
                    "This submission already has the maximum of 10 evidence files.");

            // 4. Get property description from AttrPropertyInfo
            var info = await _attrDb.AttrPropertyInfo
                .FirstOrDefaultAsync(p => p.Attr_No == attrNo.Trim());

            return AttrEvidenceValidateResult.Ok(
                currentCount,
                attrNo.Trim(),
                info?.Property_Desc,
                rootFolder,
                decl.PinExpiryDateTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[AttrEvidence] ValidateAttribute failed for {AttrNo}", attrNo);
            return AttrEvidenceValidateResult.Fail(
                "A system error occurred. Please try again.");
        }
    }


    // ── UploadAttributeEvidenceAsync ─────────────────────────────────

    public async Task<(bool Success, string? Error, int NewCount, List<string> FileNames)>
        UploadAttributeEvidenceAsync(
            string attrNo, int currentCount, List<IFormFile> files)
    {
        int newTotal = currentCount + files.Count;
        if (newTotal > 10)
            return (false,
                $"Cannot upload {files.Count} file(s). " +
                $"You have {currentCount} files and the maximum is 10.",
                currentCount, new());

        // Get root folder from AttrFiles
        var fileRecord = await _attrDb.AttrFiles
            .FirstOrDefaultAsync(f => f.Attr_No == attrNo.Trim());

        if (fileRecord is null)
            return (false, "Submission file record not found.", currentCount, new());

        // Save to \Attribute Lodged Evidence subfolder
        var evidenceFolder = Path.Combine(
            fileRecord.RootFolder ?? string.Empty,
            "Attribute Lodged Evidence");

        Directory.CreateDirectory(evidenceFolder);

        var savedNames = new List<string>();
        int slotIndex = currentCount;

        foreach (var file in files)
        {
            if (file.Length > MAX_FILE_MB * 1024 * 1024)
                return (false,
                    $"'{file.FileName}' exceeds {MAX_FILE_MB} MB limit.",
                    currentCount, new());

            slotIndex++;
            var ext = Path.GetExtension(file.FileName);
            var safeName = $"{attrNo}_Additional_Evidence_{slotIndex}" +
                           $"_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
            var path = Path.Combine(evidenceFolder, safeName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            savedNames.Add(safeName);
        }

        // Fill next available Files slots on AttrFiles record
        FillFileSlots(fileRecord, savedNames, currentCount);
        fileRecord.Evidence_Count = newTotal;

        // Update AttrPropertyInfo
        var info = await _attrDb.AttrPropertyInfo
            .FirstOrDefaultAsync(p => p.Attr_No == attrNo.Trim());

        if (info is not null)
        {
            info.Evidence_Count = newTotal;
            info.Has_Client_Evidence = true;
            info.Last_Evidence_Uploaded_DateTime = DateTime.Now;
        }

        await _attrDb.SaveChangesAsync();

        return (true, null, newTotal, savedNames);
    }

    private static void FillFileSlots(AttrFiles f, List<string> names, int startIndex)
    {
        int slot = startIndex;
        foreach (var name in names)
        {
            slot++;
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
}