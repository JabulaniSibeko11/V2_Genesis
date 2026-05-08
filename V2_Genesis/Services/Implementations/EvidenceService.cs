using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

using V2_Genesis.Models.Results.Evidence;
using V2_Genesis.Services.Evidence;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class EvidenceService : IEvidenceService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EvidenceService> _logger;
    private readonly IReadOnlyDictionary<string, EvidenceRollConfig> _registry;

    private const int MAX_FILES = 10;
    private const int MAX_FILE_MB = 3;
    private const int WINDOW_HOURS = 48;

    public EvidenceService(IConfiguration config,
        ILogger<EvidenceService> logger)
    {
        _config = config;
        _logger = logger;
        _registry = EvidenceRollRegistry.Build(config);
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

            // Step 2: check 48-hour window via status
            var statusRow = await conn.QueryFirstOrDefaultAsync(
                @"SELECT TOP 1 Objection_Status, Date_Submitted
                  FROM dbo.Obj_Property_Info
                  WHERE Objection_No = @ObjNo",
                new { ObjNo = objectionNo.Trim() });

            if (statusRow is null)
                return EvidenceValidateResult.Fail("Objection record not found.");

            string status = statusRow.Objection_Status?.ToString() ?? "";
            DateTime? submitted = statusRow.Date_Submitted as DateTime?;

            // Must be Obj-Lodging AND within 48 hours
            bool withinWindow =
                status == "Obj-Lodging" &&
                submitted.HasValue &&
                submitted.Value.AddHours(WINDOW_HOURS) > DateTime.Now;

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
}