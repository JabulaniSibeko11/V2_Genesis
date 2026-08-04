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

    private const int MAX_FILES = 10;
    private const int MAX_FILE_MB = 3;

    public Section51Service(IConfiguration config,
        ILogger<Section51Service> logger)
    {
        _config = config;
        _logger = logger;
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

            // Step 2 — check if already uploaded OR past deadline
            bool pastDeadline = DateTime.UtcNow > cfg.DeadlineUtc;

            var checkRows = await conn.QueryAsync(
                cfg.CheckSp,
                new { Objection_No = objectionNo.Trim() },
                commandType: CommandType.StoredProcedure);

            bool alreadyDone = checkRows.Any();

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

        // Build folder: FileRootPath\{ObjNo}\Section 51 Evidence
        var baseFolder = Path.Combine(
            cfg.FileRootPath, objectionNo.Trim());
        var evidenceFolder = Path.Combine(baseFolder, "Section 51 Evidence");
        Directory.CreateDirectory(evidenceFolder);

        var savedNames = new List<string>();

        foreach (var file in files)
        {
            if (file.Length > MAX_FILE_MB * 1024 * 1024)
                return (false,
                    $"File '{file.FileName}' exceeds {MAX_FILE_MB} MB limit.",
                    0, new());

            var fileName = Path.GetFileName(file.FileName);
            var path = Path.Combine(evidenceFolder, fileName);
            await using var stream = File.Create(path);
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
            return (false,
                $"Files saved but database update failed: {ex.Message}",
                savedNames.Count, savedNames);
        }

        return (true, null, savedNames.Count, savedNames);
    }
}
