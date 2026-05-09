using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
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
            var connStr = _config.GetConnectionString(cfg.ConnectionKey)!;
            await using var conn = new SqlConnection(connStr);
            await conn.ExecuteAsync(
                @"INSERT INTO [dbo].[Obj_Section_51_Uploads]
                    (Objection_Ref_51, Files1, Files2, Files3, Files4, Files5,
                     Files6, Files7, Files8, Files9, Files10, Evidence_count)
                  VALUES
                    (@Ref, @F1, @F2, @F3, @F4, @F5,
                     @F6, @F7, @F8, @F9, @F10, @Count)",
                new
                {
                    Ref = objectionNo.Trim(),
                    F1 = savedNames.ElementAtOrDefault(0),
                    F2 = savedNames.ElementAtOrDefault(1),
                    F3 = savedNames.ElementAtOrDefault(2),
                    F4 = savedNames.ElementAtOrDefault(3),
                    F5 = savedNames.ElementAtOrDefault(4),
                    F6 = savedNames.ElementAtOrDefault(5),
                    F7 = savedNames.ElementAtOrDefault(6),
                    F8 = savedNames.ElementAtOrDefault(7),
                    F9 = savedNames.ElementAtOrDefault(8),
                    F10 = savedNames.ElementAtOrDefault(9),
                    Count = savedNames.Count
                });
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