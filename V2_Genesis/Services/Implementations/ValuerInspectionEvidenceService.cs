using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.ViewModels.ValuerInspectionEvidence;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class ValuerInspectionEvidenceService : IValuerInspectionEvidenceService
    {
        private readonly string _connString;
        private readonly AttributeStorageSettings _storageSettings;
        private readonly ILogger<ValuerInspectionEvidenceService> _logger;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png"
        };

        public ValuerInspectionEvidenceService(
            IConfiguration config,
            IOptions<AttributeStorageSettings> storageOptions,
            ILogger<ValuerInspectionEvidenceService> logger)
        {
            _connString = config.GetConnectionString("AttributesConnection")
                ?? throw new InvalidOperationException("AttributesConnection is missing.");

            _storageSettings = storageOptions.Value;
            _logger = logger;
        }

        public async Task<ValuerInspectionTodayVm> GetTodayInspectionsAsync(string sapNumber)
        {
            if (string.IsNullOrWhiteSpace(sapNumber))
                throw new InvalidOperationException("Please enter your SAP number.");

            var cleanSap = sapNumber.Trim();

            await using var conn = new SqlConnection(_connString);

            var inspections = (await conn.QueryAsync<ValuerInspectionItemVm>(
                "dbo.Attr_GetTodayValuerInspections",
                new { SapNumber = cleanSap },
                commandType: CommandType.StoredProcedure)).ToList();

            return new ValuerInspectionTodayVm
            {
                SapNumber = cleanSap,
                ValuerName = inspections.FirstOrDefault()?.ValuerName,
                Inspections = inspections
            };
        }

        public async Task UploadEvidenceAsync(
            UploadValuerInspectionEvidenceVm vm,
            string? uploadedByUserId,
            string? uploadedByName)
        {
            if (vm.InspectionRequestId <= 0)
                throw new InvalidOperationException("Invalid inspection.");

            if (string.IsNullOrWhiteSpace(vm.SapNumber))
                throw new InvalidOperationException("SAP number is required.");

            if (vm.EvidenceFiles == null || !vm.EvidenceFiles.Any())
                throw new InvalidOperationException("Please upload at least one evidence photo.");

            if (string.IsNullOrWhiteSpace(vm.InspectionOutcome))
                throw new InvalidOperationException("Please select the inspection outcome.");

            var cleanSap = vm.SapNumber.Trim();

            await using var conn = new SqlConnection(_connString);

            var inspection = await conn.QueryFirstOrDefaultAsync<ValuerInspectionItemVm>(
                """
                SELECT TOP (1)
                    r.Id AS InspectionRequestId,
                    r.Attr_ID AS AttrId,
                    r.Attr_No AS AttrNo,
                    p.Property_Desc AS PropertyDescription,
                    r.ConfirmedDateTime,
                    r.Status,
                    d.ValuerName
                FROM dbo.AttrInspectionRequests r
                INNER JOIN dbo.Attr_Property_Info p
                    ON r.Attr_ID = p.Attr_ID
                INNER JOIN dbo.AttrValuerInspectionDetails d
                    ON d.SapNumber = r.ValuerSapNumber
                    AND d.IsActive = 1
                WHERE
                    r.Id = @InspectionRequestId
                    AND r.ValuerSapNumber = @SapNumber
                    AND CAST(r.ConfirmedDateTime AS DATE) = CAST(GETDATE() AS DATE)
                    AND r.Status = 'InspectionDetailsSent'
                    AND ISNULL(p.IsActive, 1) = 1
                """,
                new
                {
                    vm.InspectionRequestId,
                    SapNumber = cleanSap
                });

            if (inspection == null)
                throw new InvalidOperationException("This inspection is not available for upload today.");

            var attrNo = inspection.AttrNo ?? $"ATTR-{inspection.AttrId}";

            var evidenceFolder = Path.Combine(
                _storageSettings.BasePath,
                attrNo,
                _storageSettings.ValuerInspectionEvidenceFolderName);

            Directory.CreateDirectory(evidenceFolder);

            var savedFiles = new List<(string FileName, string FilePath, string ContentType, long Size)>();

            foreach (var file in vm.EvidenceFiles)
            {
                if (file.Length <= 0)
                    continue;

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(ext))
                    throw new InvalidOperationException("Only JPG and PNG evidence photos are allowed.");

                var safeName =
                    $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}{ext}";

                var fullPath = Path.Combine(evidenceFolder, safeName);

                await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
                {
                    await file.CopyToAsync(stream);
                }

                savedFiles.Add((
                    safeName,
                    fullPath,
                    file.ContentType,
                    file.Length));
            }

            if (!savedFiles.Any())
                throw new InvalidOperationException("No valid evidence files were uploaded.");

            foreach (var saved in savedFiles)
            {
                await conn.ExecuteAsync(
                    """
                    INSERT INTO dbo.AttrInspectionEvidence
                    (
                        Attr_ID,
                        Attr_No,
                        InspectionRequestId,
                        FileName,
                        FilePath,
                        ContentType,
                        FileSizeBytes,
                        UploadedBySapNumber,
                        UploadedByUserId,
                        UploadedByName,
                        CaptureSource,
                        EvidenceComment,
                        UploadedAt,
                        IsActive
                    )
                    VALUES
                    (
                        @AttrId,
                        @AttrNo,
                        @InspectionRequestId,
                        @FileName,
                        @FilePath,
                        @ContentType,
                        @FileSizeBytes,
                        @UploadedBySapNumber,
                        @UploadedByUserId,
                        @UploadedByName,
                        @CaptureSource,
                        @EvidenceComment,
                        GETDATE(),
                        1
                    )
                    """,
                    new
                    {
                        AttrId = inspection.AttrId,
                        AttrNo = inspection.AttrNo,
                        InspectionRequestId = inspection.InspectionRequestId,
                        saved.FileName,
                        saved.FilePath,
                        saved.ContentType,
                        FileSizeBytes = saved.Size,
                        UploadedBySapNumber = cleanSap,
                        UploadedByUserId = uploadedByUserId,
                        UploadedByName = uploadedByName ?? inspection.ValuerName,
                        CaptureSource = "CameraOrFileUpload",
                        EvidenceComment = vm.InspectionOutcomeComment
                    });
            }

            await conn.ExecuteAsync(
                "dbo.Attr_CompleteValuerInspection",
                new
                {
                    InspectionRequestId = vm.InspectionRequestId,
                    SapNumber = cleanSap,
                    InspectionOutcome = vm.InspectionOutcome,
                    InspectionOutcomeComment = vm.InspectionOutcomeComment,
                    EvidenceFolderPath = evidenceFolder
                },
                commandType: CommandType.StoredProcedure);
        }
    }
}