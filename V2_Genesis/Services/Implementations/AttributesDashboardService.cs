using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Data;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Models.ViewModels.Dashboard;
using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public class AttributesDashboardService : IAttributesDashboardService
{
    private readonly string _connString;
    private readonly ILogger<AttributesDashboardService> _logger;
    private readonly ValuerPhotoStorageSettings _valuerPhotoStorageSettings;
    public AttributesDashboardService(
        IConfiguration config,
        ILogger<AttributesDashboardService> logger, IOptions<ValuerPhotoStorageSettings> valuerPhotoStorageOptions)
    {
        _connString = config.GetConnectionString("AttributesConnection")!;
        _logger = logger;
        _valuerPhotoStorageSettings = valuerPhotoStorageOptions.Value;
    }

    public async Task<AttributesDashboardData> GetDashboardDataAsync(string userId)
    {
        var data = new AttributesDashboardData();

        if (string.IsNullOrWhiteSpace(userId))
            return data;

        try
        {
            await using var conn = new SqlConnection(_connString);

            try
            {
                var linked = await conn.QueryAsync<AttributeLinkedProperty>(
                    "Attr_DashboardLinked",
                    // SQL procedure expects @userName (not @UserId).
                    // The value is still the ASP.NET Identity user id used by
                    // AttrLinkedProperties.UserID.
                    new { userName = userId },
                    commandType: CommandType.StoredProcedure);

                data.LinkedProperties = linked.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Attributes] Attr_DashboardLinked failed for {User}", userId);

                data.LinkedProperties = new List<AttributeLinkedProperty>();
            }

            try
            {
                var subs = await conn.QueryAsync<AttributeSubmission>(
                    "Attr_DashboardSubmissions",
                    new { UserName = userId },
                    commandType: CommandType.StoredProcedure);

                data.Submissions = subs
                    .OrderByDescending(x => x.SubmittedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Attributes] Attr_DashboardSubmissions failed for {User}", userId);

                data.Submissions = new List<AttributeSubmission>();
            }

            try
            {
                var appointmentRows = await conn.QueryAsync<AttributeAppointment>(
                    "Attr_DashboardAppointments",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);

                data.Appointments = appointmentRows.ToList();

                var slotRows = await conn.QueryAsync<AttributeAppointmentSlot>(
                    "Attr_DashboardAppointmentSlots",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);

                var slots = slotRows.ToList();

                foreach (var appointment in data.Appointments)
                {
                    appointment.Slots = slots
                        .Where(x => x.InspectionRequestId == appointment.Id)
                        .OrderBy(x => x.SlotNo)
                        .ToList();

                    appointment.Status = NormalizeAppointmentStatus(appointment);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Attributes] Appointment loading failed for {User}", userId);

                data.Appointments = new List<AttributeAppointment>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Attributes] Dashboard connection failed for {User}", userId);
        }

        return data;
    }

    private static string NormalizeAppointmentStatus(
        AttributeAppointment appointment)
    {
        var statusKey = new string((appointment.Status ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());

        return statusKey switch
        {
            "PENDINGCLIENTRESPONSE" => "PendingClientResponse",
            "CONFIRMED" => "Confirmed",
            "INSPECTIONDETAILSSENT" => "InspectionDetailsSent",
            "EXPIRED" => "Expired",

            // Some versions of Attr_DashboardAppointments return the parent
            // Attribute status instead of AttrInspectionRequests.Status.
            // An unconfirmed request with offered slots is still awaiting the
            // client's selection and must expose the Select Date action.
            "INSPECTIONREQUIRED"
                when appointment.ConfirmedDateTime == null &&
                     appointment.Slots.Any() => "PendingClientResponse",

            _ => appointment.Status?.Trim() ?? string.Empty
        };
    }

    public async Task RespondToInspectionAppointmentAsync(
        InspectionAppointmentResponseVm vm,
        string userId,
        string userEmail)
    {
        if (vm.InspectionRequestId <= 0)
            throw new InvalidOperationException("Invalid inspection request.");

        if (vm.SelectedSlotId <= 0)
            throw new InvalidOperationException("Please select one inspection date.");

        await using var conn = new SqlConnection(_connString);

        await conn.ExecuteAsync(
            "Attr_RespondToInspectionAppointment",
            new
            {
                InspectionRequestId = vm.InspectionRequestId,
                SelectedSlotId = vm.SelectedSlotId,
                UserId = userId,
                UserEmail = userEmail,
                ClientResponseComment = vm.ClientResponseComment
            },
            commandType: CommandType.StoredProcedure);
    }
    public async Task<AppointmentValuerDetailsVm> VerifyInspectionPinAsync(
        VerifyInspectionPinVm vm,
        string userId,
        string userEmail,
        string? ipAddress,
        string? userAgent)
    {
        if (vm.InspectionRequestId <= 0)
        {
            return new AppointmentValuerDetailsVm
            {
                Success = false,
                ErrorMessage = "Invalid inspection appointment."
            };
        }

        var cleanPin = vm.Pin.Trim();

        if (cleanPin.Length != 4 || !cleanPin.All(char.IsDigit))
        {
            return new AppointmentValuerDetailsVm
            {
                Success = false,
                ErrorMessage = "Please enter the 4-digit inspection PIN."
            };
        }

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connString);

        var result = await connection.QueryFirstOrDefaultAsync<AppointmentValuerDetailsVm>(
            "dbo.Attr_VerifyInspectionPin",
            new
            {
                InspectionRequestId = vm.InspectionRequestId,
                UserId = userId,
                UserEmail = userEmail,
                Pin = vm.Pin.Trim(),
                IpAddress = ipAddress,
                UserAgent = userAgent
            },
            commandType: CommandType.StoredProcedure);

        return result ?? new AppointmentValuerDetailsVm
        {
            Success = false,
            ErrorMessage = "Unable to verify inspection PIN."
        };
    }
    public async Task<VerifiedValuerPhotoVm?> GetVerifiedValuerPhotoAsync(
    long inspectionRequestId,
    string userId)
    {
        if (inspectionRequestId <= 0)
            return null;

        if (string.IsNullOrWhiteSpace(userId))
            return null;

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connString);

        var photoResult = await connection.QueryFirstOrDefaultAsync<VerifiedValuerPhotoPathResult>(
            "dbo.Attr_GetVerifiedValuerPhoto",
            new
            {
                InspectionRequestId = inspectionRequestId,
                UserId = userId
            },
            commandType: CommandType.StoredProcedure);

        if (photoResult == null)
            return null;

        var finalPhotoPath = ResolveValuerPhotoPath(
            photoResult.PhotoPath,
            photoResult.PhotoFileName);

        if (string.IsNullOrWhiteSpace(finalPhotoPath))
            return null;

        if (!File.Exists(finalPhotoPath))
            return null;

        var ext = Path.GetExtension(finalPhotoPath).ToLowerInvariant();

        if (!_valuerPhotoStorageSettings.AllowedExtensions
                .Select(x => x.ToLowerInvariant())
                .Contains(ext))
        {
            return null;
        }

        var contentType = ext switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

        return new VerifiedValuerPhotoVm
        {
            Bytes = await File.ReadAllBytesAsync(finalPhotoPath),
            ContentType = contentType
        };
    }
    private string? ResolveValuerPhotoPath(
    string? photoPath,
    string? photoFileName)
    {
        if (!string.IsNullOrWhiteSpace(photoPath))
        {
            var cleanedPath = photoPath.Trim();

            if (File.Exists(cleanedPath))
                return cleanedPath;

            var pathExt = Path.GetExtension(cleanedPath);

            if (string.IsNullOrWhiteSpace(pathExt))
            {
                foreach (var ext in _valuerPhotoStorageSettings.AllowedExtensions)
                {
                    var candidate = cleanedPath + ext;

                    if (File.Exists(candidate))
                        return candidate;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(photoFileName))
            return null;

        var safeFileName = Path.GetFileName(photoFileName.Trim());

        if (string.IsNullOrWhiteSpace(safeFileName))
            return null;

        var directPath = Path.Combine(
            _valuerPhotoStorageSettings.RootFolder,
            safeFileName);

        if (File.Exists(directPath))
            return directPath;

        var directExt = Path.GetExtension(directPath);

        if (string.IsNullOrWhiteSpace(directExt))
        {
            foreach (var ext in _valuerPhotoStorageSettings.AllowedExtensions)
            {
                var candidate = directPath + ext;

                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }
    public async Task ResubmitReturnedAttributeAsync(
    ResubmitReturnedAttributeVm vm,
    string userId,
    string userEmail)
    {
        if (vm.AttrId <= 0)
            throw new InvalidOperationException("Invalid attribute submission.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Your session could not be verified.");

        if (string.IsNullOrWhiteSpace(vm.RevisionComment))
            throw new InvalidOperationException("Please enter what you corrected before resubmitting.");

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connString);

        await connection.ExecuteAsync(
            "dbo.Attr_ResubmitReturnedSubmission",
            new
            {
                AttrId = vm.AttrId,
                UserId = userId,
                UserEmail = userEmail,
                RevisionComment = vm.RevisionComment.Trim()
            },
            commandType: System.Data.CommandType.StoredProcedure);
    }
    private sealed class VerifiedValuerPhotoPathResult
    {
        public string? PhotoPath { get; set; }

        public string? PhotoFileName { get; set; }
    }
}
