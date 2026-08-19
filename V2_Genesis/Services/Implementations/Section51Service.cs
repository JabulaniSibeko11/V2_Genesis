using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Section51;
using V2_Genesis.Services.Interfaces;
using V2_Genesis.Services.Section51;

namespace V2_Genesis.Services.Implementations;

public class Section51Service : ISection51Service
{
    private readonly IConfiguration _config;
    private readonly ILogger<Section51Service> _logger;
    private readonly IReadOnlyDictionary<string, Section51RollConfig> _registry;
    private readonly IWebHostEnvironment _environment;

    private const int MAX_FILES = 10;
    private const int MAX_FILE_MB = 3;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png"
        };

    public Section51Service(
        IConfiguration config,
        ILogger<Section51Service> logger,
        IWebHostEnvironment environment)
    {
        _config = config;
        _logger = logger;
        _environment = environment;
        _registry = Section51RollRegistry.Build(config);
    }

    // ── Validate PIN + limit check ────────────────────────────────────
    public async Task<Section51ValidateResult> ValidateAsync(
        string rollSource, string objectionNo, string pin)
    {
        if (!_registry.TryGetValue(rollSource, out var cfg))
            return Section51ValidateResult.Fail("Invalid roll source.");

        try
        {
            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            await using var conn = new SqlConnection(connStr);

            // Step 1 — validate objection + PIN
            var rows = await conn.QueryAsync(
                cfg.ValidateSp,
                new { Objection_No = objectionNo.Trim(), Pin = pin.Trim() },
                commandType: CommandType.StoredProcedure);

            if (!rows.Any())
                return Section51ValidateResult.Fail(
                    "Invalid objection number or PIN. Please check and try again.");

            // Step 2 — check if evidence was already submitted.
            //
            // Do not use checkRows.Any() here. Some legacy Section51Check
            // procedures return a status row even when the answer is "No",
            // which caused Genesis to treat every reference as already done.
            var alreadyDone = await conn.ExecuteScalarAsync<int?>(
                @"SELECT TOP (1) 1
                  FROM dbo.Obj_Section_51_Uploads
                  WHERE LTRIM(RTRIM(Objection_Ref_51)) = @Objection_No;",
                new { Objection_No = objectionNo.Trim() }) == 1;

            // UAT needs to exercise the full Section 51 workflow even when the
            // statutory production deadline has passed. The bypass is explicit
            // and is never honoured in Production.
            var bypassDeadline =
                _config.GetValue<bool>("Section51:BypassDeadline")
                && !_environment.IsProduction();

            var pastDeadline =
                !bypassDeadline &&
                DateTime.UtcNow > cfg.DeadlineUtc;

            if (bypassDeadline)
            {
                _logger.LogWarning(
                    "[Section51] Deadline bypass is enabled in {Environment} for {ObjNo} on {Roll}.",
                    _environment.EnvironmentName,
                    objectionNo,
                    rollSource);
            }

            if (alreadyDone || pastDeadline)
                return Section51ValidateResult.Limit(alreadyDone, pastDeadline);

            return Section51ValidateResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Section51] Validate failed for {ObjNo} on {Roll}",
                objectionNo, rollSource);
            return Section51ValidateResult.Fail(
                "A system error occurred. Please try again.");
        }
    }

    // ── Upload files ──────────────────────────────────────────────────
    public async Task<(bool Success, string? Error, int FileCount, List<string> FileNames)>
        UploadAsync(string rollSource, string objectionNo, List<IFormFile> files)
    {
        if (!_registry.TryGetValue(rollSource, out var cfg))
            return (false, "Invalid roll source.", 0, new());

        if (files.Count > MAX_FILES)
            return (false,
                $"Maximum {MAX_FILES} files allowed.", 0, new());

        if (files.Any(f => f is null || f.Length == 0))
            return (false, "One or more selected files are empty.", 0, new());

        // Validate every file before creating anything on disk.
        foreach (var file in files)
        {
            if (file.Length > MAX_FILE_MB * 1024 * 1024)
                return (false,
                    $"File '{file.FileName}' exceeds {MAX_FILE_MB} MB limit.",
                    0, new());

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
                return (false,
                    $"File '{file.FileName}' is not an allowed file type. Allowed: PDF, JPG, JPEG, PNG.",
                    0, new());
        }

        // Build folder: FileRootPath\{ObjNo}\Section 51 Evidence
        var baseFolder = Path.Combine(
            cfg.FileRootPath, objectionNo.Trim());
        var evidenceFolder = Path.Combine(baseFolder, "Section 51 Evidence");
        Directory.CreateDirectory(evidenceFolder);

        var savedNames = new List<string>();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.FileName);
            var path = Path.Combine(evidenceFolder, fileName);

            if (System.IO.File.Exists(path))
                return (false,
                    $"A file named '{fileName}' has already been uploaded for this objection.",
                    savedNames.Count, savedNames);

            await using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

            await file.CopyToAsync(stream);
            savedNames.Add(fileName);
        }

        // Save record to DB
        try
        {
            var connStr = _config.GetConnectionString(cfg.ConnectionKey)
                ?? throw new InvalidOperationException(
                    $"Connection string '{cfg.ConnectionKey}' was not found.");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connStr)
                .Options;

            await using var db = new ApplicationDbContext(options);

            var upload = new Obj_Section_51_Uploads
            {
                Objection_Ref_51 = objectionNo.Trim(),
                Files1 = savedNames.ElementAtOrDefault(0),
                Files2 = savedNames.ElementAtOrDefault(1),
                Files3 = savedNames.ElementAtOrDefault(2),
                Files4 = savedNames.ElementAtOrDefault(3),
                Files5 = savedNames.ElementAtOrDefault(4),
                Files6 = savedNames.ElementAtOrDefault(5),
                Files7 = savedNames.ElementAtOrDefault(6),
                Files8 = savedNames.ElementAtOrDefault(7),
                Files9 = savedNames.ElementAtOrDefault(8),
                Files10 = savedNames.ElementAtOrDefault(9),
                Evidence_count = savedNames.Count
            };

            await db.Obj_Section_51_Uploads.AddAsync(upload);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Section51] DB insert failed for {ObjNo}", objectionNo);

            // Keep the database and physical evidence in sync.
            foreach (var savedName in savedNames)
            {
                try
                {
                    var savedPath = Path.Combine(evidenceFolder, savedName);
                    if (System.IO.File.Exists(savedPath))
                        System.IO.File.Delete(savedPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(
                        cleanupEx,
                        "[Section51] Could not clean up {FileName} after DB failure for {ObjNo}.",
                        savedName,
                        objectionNo);
                }
            }

            return (false,
                "The Section 51 submission could not be recorded. Please try again.",
                0, new());
        }

        return (true, null, savedNames.Count, savedNames);
    }
}
