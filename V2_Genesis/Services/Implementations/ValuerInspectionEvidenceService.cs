using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;
using V2_Genesis.Data;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.ViewModels.ValuerInspectionEvidence;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class ValuerInspectionEvidenceService : IValuerInspectionEvidenceService
    {
        private readonly string _connString;
        private readonly AttributesDbContext _db;
        private readonly AttributeStorageSettings _storageSettings;
        private readonly ILogger<ValuerInspectionEvidenceService> _logger;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png"
        };

        private const int MaximumEvidenceFiles = 10;
        private const long MaximumEvidenceFileSize = 10 * 1024 * 1024;

        public ValuerInspectionEvidenceService(
            IConfiguration config,
            AttributesDbContext db,
            IOptions<AttributeStorageSettings> storageOptions,
            ILogger<ValuerInspectionEvidenceService> logger)
        {
            _connString = config.GetConnectionString("AttributesConnection")
                ?? throw new InvalidOperationException("AttributesConnection is missing.");

            _db = db;
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

        public async Task<UploadValuerInspectionEvidenceVm> GetInspectionForUploadAsync(
            long inspectionRequestId,
            string sapNumber)
        {
            if (inspectionRequestId <= 0)
                throw new InvalidOperationException("Invalid inspection.");

            if (string.IsNullOrWhiteSpace(sapNumber))
                throw new InvalidOperationException("SAP number is required.");

            var cleanSap = sapNumber.Trim();
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var inspection = await (
                from request in _db.AttrInspectionRequests.AsNoTracking()
                join property in _db.AttrPropertyInfo.AsNoTracking()
                    on request.Attr_ID equals property.Attr_ID
                join valuer in _db.AttrValuerInspectionDetails.AsNoTracking()
                    on request.ValuerSapNumber equals valuer.SapNumber
                where request.Id == inspectionRequestId
                      && request.ValuerSapNumber == cleanSap
                      && request.ConfirmedDateTime >= today
                      && request.ConfirmedDateTime < tomorrow
                      && request.Status == "InspectionDetailsSent"
                      && property.IsActive
                      && valuer.IsActive
                select new UploadValuerInspectionEvidenceVm
                {
                    InspectionRequestId = request.Id,
                    SapNumber = cleanSap,
                    AttrNo = request.Attr_No,
                    PropertyDescription = property.Property_Desc,
                    InspectionAddress = property.Inspection_Address,
                    ConfirmedDateTime = request.ConfirmedDateTime,
                    ValuerName = valuer.ValuerName
                }).FirstOrDefaultAsync();

            if (inspection is null)
            {
                throw new InvalidOperationException(
                    "This inspection is not available for evidence upload today.");
            }

            return inspection;
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

            if (vm.EvidenceFiles.Count > MaximumEvidenceFiles)
            {
                throw new InvalidOperationException(
                    $"A maximum of {MaximumEvidenceFiles} evidence photos may be uploaded at once.");
            }

            if (string.IsNullOrWhiteSpace(vm.InspectionOutcome))
                throw new InvalidOperationException("Please select the inspection outcome.");

            var cleanSap = vm.SapNumber.Trim();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var inspection = await (
                from request in _db.AttrInspectionRequests.AsNoTracking()
                join property in _db.AttrPropertyInfo.AsNoTracking()
                    on request.Attr_ID equals property.Attr_ID
                join valuer in _db.AttrValuerInspectionDetails.AsNoTracking()
                    on request.ValuerSapNumber equals valuer.SapNumber
                where request.Id == vm.InspectionRequestId
                      && request.ValuerSapNumber == cleanSap
                      && request.ConfirmedDateTime >= today
                      && request.ConfirmedDateTime < tomorrow
                      && request.Status == "InspectionDetailsSent"
                      && property.IsActive
                      && valuer.IsActive
                select new ValuerInspectionItemVm
                {
                    InspectionRequestId = request.Id,
                    AttrId = request.Attr_ID,
                    AttrNo = request.Attr_No,
                    PropertyDescription = property.Property_Desc,
                    ConfirmedDateTime = request.ConfirmedDateTime,
                    Status = request.Status,
                    ValuerName = valuer.ValuerName
                }).FirstOrDefaultAsync();

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

                if (file.Length > MaximumEvidenceFileSize)
                {
                    throw new InvalidOperationException(
                        $"{Path.GetFileName(file.FileName)} is larger than 10 MB.");
                }

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

            var evidenceRows = savedFiles.Select(saved => new AttrInspectionEvidence
            {
                Attr_ID = inspection.AttrId,
                Attr_No = inspection.AttrNo,
                InspectionRequestId = inspection.InspectionRequestId,
                FileName = saved.FileName,
                FilePath = saved.FilePath,
                ContentType = saved.ContentType,
                FileSizeBytes = saved.Size,
                UploadedBySapNumber = cleanSap,
                UploadedByUserId = uploadedByUserId,
                UploadedByName = uploadedByName ?? inspection.ValuerName,
                CaptureSource = "CameraOrFileUpload",
                EvidenceComment = vm.InspectionOutcomeComment,
                UploadedAt = DateTime.Now,
                IsActive = true
            });

            await _db.AttrInspectionEvidence.AddRangeAsync(evidenceRows);
            await _db.SaveChangesAsync();

            await using var conn = new SqlConnection(_connString);

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
