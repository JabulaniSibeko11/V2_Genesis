using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using V2_Genesis.Data;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Services.Attributes;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Controllers;

[AllowAnonymous]
[Route("attributes/inspection")]
public sealed class AttributeInspectionLinkController : Controller
{
    private const int OnlineBookingMonthCount = 3;
    private const string AdministrationAssistanceEmail =
        "AdministrationEnquiries@Joburg.org.za";

    private readonly AttributesDbContext _db;
    private readonly ValuerPhotoStorageSettings _photoSettings;
    private readonly ILogger<AttributeInspectionLinkController> _logger;
    private readonly IAttributeInspectionCalendarService _calendarService;
    private readonly IEmailService _emailService;

    public AttributeInspectionLinkController(
        AttributesDbContext db,
        IOptions<ValuerPhotoStorageSettings> photoSettings,
        ILogger<AttributeInspectionLinkController> logger,
        IAttributeInspectionCalendarService calendarService,
        IEmailService emailService)
    {
        _db = db;
        _photoSettings = photoSettings.Value;
        _logger = logger;
        _calendarService = calendarService;
        _emailService = emailService;
    }

    [HttpGet("{token:guid}")]
    public async Task<IActionResult> Index(
        Guid token,
        string? view = null,
        int? year = null,
        int? month = null)
    {
        ApplySecureLinkHeaders();

        var selectedMonth = ResolveRequestedMonth(year, month);
        var model = await BuildModelAsync(token, selectedMonth);

        if (model == null)
            return View("Invalid");

        if (string.Equals(
                view,
                "valuer",
                StringComparison.OrdinalIgnoreCase) &&
            !model.IsExpired)
        {
            if (model.RequiresPinVerification)
            {
                model.Message =
                    "Enter the inspection PIN from the City email to view the authorised valuer and vehicle details.";
            }
            else if (!model.ValuerDetailsReleased)
            {
                model.Message =
                    "The appointment is confirmed. The authorised valuer details will appear here as soon as the valuer releases them.";
            }
        }

        return View(model);
    }

    [HttpPost("{token:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(
        Guid token,
        DateTime selectedDateTime)
    {
        ApplySecureLinkHeaders();

        var request = await _db.AttrInspectionRequests
            .FirstOrDefaultAsync(x => x.EmailToken == token);

        if (request == null)
            return View("Invalid");

        var property = await _db.AttrPropertyInfo
            .FirstOrDefaultAsync(x =>
                x.Attr_ID == request.Attr_ID &&
                x.IsActive);

        if (property == null)
            return View("Invalid");

        if (request.EmailTokenExpiresAt.HasValue &&
            request.EmailTokenExpiresAt.Value < DateTime.Now)
        {
            var expired = await BuildModelAsync(
                token,
                ResolveRequestedMonth(null, null));

            if (expired != null)
                expired.Message =
                    "This secure appointment link has expired.";

            return View("Index", expired);
        }

        if (!string.Equals(
                request.Status,
                "PendingClientResponse",
                StringComparison.OrdinalIgnoreCase))
        {
            var current = await BuildModelAsync(
                token,
                ResolveRequestedMonth(null, null));

            if (current != null)
                current.Message =
                    "This inspection request has already been responded to.";

            return View("Index", current);
        }

        selectedDateTime = new DateTime(
            selectedDateTime.Year,
            selectedDateTime.Month,
            selectedDateTime.Day,
            selectedDateTime.Hour,
            selectedDateTime.Minute,
            0);

        var selectedMonth =
            new DateTime(
                selectedDateTime.Year,
                selectedDateTime.Month,
                1);

        if (selectedDateTime <= DateTime.Now)
        {
            var invalidPast =
                await BuildModelAsync(token, selectedMonth);

            if (invalidPast != null)
                invalidPast.Message =
                    "Please select a future inspection date and time.";

            return View("Index", invalidPast);
        }

        var minMonth = GetMinimumMonth();
        var maxMonth = GetMaximumMonth();
        var bookingEndExclusive = maxMonth.AddMonths(1);

        if (selectedMonth < minMonth ||
            selectedMonth > maxMonth ||
            selectedDateTime >= bookingEndExclusive)
        {
            var invalid =
                await BuildModelAsync(token, selectedMonth);

            if (invalid != null)
                invalid.Message =
                    "The selected inspection date is outside the available online booking period.";

            return View("Index", invalid);
        }

        if (request.EmailTokenExpiresAt.HasValue &&
            selectedDateTime > request.EmailTokenExpiresAt.Value)
        {
            var expiredSelection =
                await BuildModelAsync(token, selectedMonth);

            if (expiredSelection != null)
                expiredSelection.Message =
                    "The secure appointment link expires before the selected date. Please request Administration assistance.";

            return View("Index", expiredSelection);
        }

        var stillAvailable =
            await _calendarService.IsSlotAvailableAsync(
                request.RequestedByUserId,
                selectedDateTime,
                request.Id);

        if (!stillAvailable)
        {
            var unavailable =
                await BuildModelAsync(token, selectedMonth);

            if (unavailable != null)
                unavailable.Message =
                    "That inspection time is no longer available. Please select another date and time.";

            return View("Index", unavailable);
        }

        await using var tx =
            await _db.Database.BeginTransactionAsync();

        try
        {
            stillAvailable =
                await _calendarService.IsSlotAvailableAsync(
                    request.RequestedByUserId,
                    selectedDateTime,
                    request.Id);

            if (!stillAvailable)
            {
                throw new InvalidOperationException(
                    "The selected inspection time has just been booked. Please select another slot.");
            }

            var selectedSlot =
                new AttrInspectionRequestSlot
                {
                    InspectionRequestId = request.Id,
                    Attr_ID = request.Attr_ID,
                    Attr_No = request.Attr_No,
                    SlotNo = 1,
                    ProposedDateTime = selectedDateTime,
                    SlotStatus = "Confirmed",
                    CreatedBy = "GenesisSecureEmailLink",
                    CreatedDate = DateTime.Now
                };

            _db.AttrInspectionRequestSlots.Add(selectedSlot);
            await _db.SaveChangesAsync();

            request.Status = "Confirmed";
            request.ConfirmedSlotId = selectedSlot.Id;
            request.ConfirmedDateTime = selectedDateTime;
            request.ClientResponseChannel =
                "GenesisInspectionCalendar";
            request.ClientResponseComment =
                AppendClientResponseNote(
                    request.ClientResponseComment,
                    $"Appointment confirmed for {selectedDateTime:dd MMM yyyy HH:mm}.");
            request.ClientRespondedAt = DateTime.Now;
            request.UpdatedBy = "GenesisSecureEmailLink";
            request.UpdatedDate = DateTime.Now;

            property.Attr_Status = "InspectionConfirmed";
            property.Physical_Inspection_Status =
                "InspectionConfirmed";
            property.Inspection_Scheduled_Date =
                selectedDateTime.Date;
            property.Inspection_Scheduled_Time =
                selectedDateTime.TimeOfDay;
            property.UpdatedBy =
                "GenesisSecureEmailLink";
            property.UpdatedDate = DateTime.Now;

            if (request.ReviewId.HasValue)
            {
                var review = await _db.AttrValuerReviews
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.ReviewId.Value &&
                        x.Attr_ID == property.Attr_ID);

                if (review != null)
                {
                    review.ReviewStatus =
                        "InspectionConfirmed";
                }
            }

            _db.AttrPropertyInfoAuditTrail.Add(
                new AttrPropertyInfoAuditTrail
                {
                    Attr_ID = property.Attr_ID,
                    Attr_No = property.Attr_No,
                    Action = "Inspection Date Confirmed",
                    OldStatus = "InspectionRequired",
                    NewStatus = "InspectionConfirmed",
                    ActionByUserId =
                        request.ClientEmail ?? "Client",
                    ActionByName =
                        request.ClientName ?? "Client",
                    ActionRole =
                        "Client - Secure Email Link",
                    Comment =
                        $"Client selected inspection date {selectedDateTime:dd MMM yyyy HH:mm} from the AIVS inspection calendar.",
                    ActionDateTime = DateTime.Now
                });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync();

            _logger.LogWarning(
                "Inspection appointment confirmation could not be completed for request {InspectionRequestId}: {Reason}",
                request.Id,
                ex.Message);

            var refreshed =
                await BuildModelAsync(token, selectedMonth);

            if (refreshed != null)
                refreshed.Message = ex.Message;

            return View("Index", refreshed);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();

            _logger.LogError(
                ex,
                "Inspection appointment confirmation failed for request {InspectionRequestId}.",
                request.Id);

            var failed =
                await BuildModelAsync(token, selectedMonth);

            if (failed != null)
                failed.Message =
                    "The appointment could not be confirmed. Please try again.";

            return View("Index", failed);
        }

        TempData["InspectionLinkSuccess"] =
            "Inspection date and time confirmed successfully.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                token,
                year = selectedMonth.Year,
                month = selectedMonth.Month
            });
    }

    [HttpPost("{token:guid}/unavailable-this-month")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnavailableThisMonth(
        Guid token,
        int year,
        int month)
    {
        ApplySecureLinkHeaders();

        var request = await _db.AttrInspectionRequests
            .FirstOrDefaultAsync(x => x.EmailToken == token);

        if (request == null)
            return View("Invalid");

        var property = await _db.AttrPropertyInfo
            .FirstOrDefaultAsync(x =>
                x.Attr_ID == request.Attr_ID &&
                x.IsActive);

        if (property == null)
            return View("Invalid");

        if (!CanClientStillBook(request))
        {
            TempData["InspectionLinkSuccess"] =
                "This inspection request can no longer be changed.";

            return RedirectToAction(
                nameof(Index),
                new { token });
        }

        var viewedMonth =
            ResolveRequestedMonth(year, month);

        var now = DateTime.Now;

        request.ClientResponseChannel =
            "GenesisInspectionCalendar";

        request.ClientResponseComment =
            AppendClientResponseNote(
                request.ClientResponseComment,
                $"Client indicated they are unavailable for {viewedMonth:MMMM yyyy}.");

        request.UpdatedBy =
            request.ClientEmail ??
            "GenesisSecureEmailLink";

        request.UpdatedDate = now;

        _db.AttrPropertyInfoAuditTrail.Add(
            new AttrPropertyInfoAuditTrail
            {
                Attr_ID = property.Attr_ID,
                Attr_No = property.Attr_No,
                Action = "Client Unavailable This Month",
                OldStatus = property.Attr_Status,
                NewStatus = property.Attr_Status,
                ActionByUserId =
                    request.ClientEmail ?? "Client",
                ActionByName =
                    request.ClientName ?? "Client",
                ActionRole =
                    "Client - Secure Email Link",
                Comment =
                    $"Client indicated that they are unavailable for the whole of {viewedMonth:MMMM yyyy}. The inspection request remains open.",
                ActionDateTime = now
            });

        await _db.SaveChangesAsync();

        if (viewedMonth < GetMaximumMonth())
        {
            var nextMonth = viewedMonth.AddMonths(1);

            TempData["InspectionLinkSuccess"] =
                $"No problem. Showing available appointments for {nextMonth:MMMM yyyy}.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    token,
                    year = nextMonth.Year,
                    month = nextMonth.Month
                });
        }

        TempData["InspectionLinkSuccess"] =
            "You have reached the final online booking month. Please use Request Administration Assistance below.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                token,
                year = viewedMonth.Year,
                month = viewedMonth.Month
            });
    }

    [HttpPost("{token:guid}/request-administration-assistance")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAdministrationAssistance(
        Guid token,
        int year,
        int month,
        string? reason)
    {
        ApplySecureLinkHeaders();

        var request = await _db.AttrInspectionRequests
            .FirstOrDefaultAsync(x => x.EmailToken == token);

        if (request == null)
            return View("Invalid");

        var property = await _db.AttrPropertyInfo
            .FirstOrDefaultAsync(x =>
                x.Attr_ID == request.Attr_ID &&
                x.IsActive);

        if (property == null)
            return View("Invalid");

        if (!CanClientStillBook(request))
        {
            TempData["InspectionLinkSuccess"] =
                "This inspection request can no longer be changed.";

            return RedirectToAction(
                nameof(Index),
                new { token });
        }

        var viewedMonth =
            ResolveRequestedMonth(year, month);

        if (string.Equals(
                request.ClientResponseChannel,
                "AdministrationAssistanceRequested",
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["InspectionLinkSuccess"] =
                "Administration assistance has already been requested for this inspection.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    token,
                    year = viewedMonth.Year,
                    month = viewedMonth.Month
                });
        }

        var safeReason =
            string.IsNullOrWhiteSpace(reason)
                ? "Client could not find a suitable online inspection appointment."
                : reason.Trim();

        if (safeReason.Length > 500)
            safeReason = safeReason[..500];

        var now = DateTime.Now;

        request.ClientResponseChannel =
            "AdministrationAssistanceRequested";

        request.ClientResponseComment =
            AppendClientResponseNote(
                request.ClientResponseComment,
                $"Administration assistance requested while viewing {viewedMonth:MMMM yyyy}. Reason: {safeReason}");

        request.UpdatedBy =
            request.ClientEmail ??
            "GenesisSecureEmailLink";

        request.UpdatedDate = now;

        _db.AttrPropertyInfoAuditTrail.Add(
            new AttrPropertyInfoAuditTrail
            {
                Attr_ID = property.Attr_ID,
                Attr_No = property.Attr_No,
                Action = "Inspection Administration Assistance Requested",
                OldStatus = property.Attr_Status,
                NewStatus = property.Attr_Status,
                ActionByUserId =
                    request.ClientEmail ?? "Client",
                ActionByName =
                    request.ClientName ?? "Client",
                ActionRole =
                    "Client - Secure Email Link",
                Comment =
                    $"Client requested Administration assistance for inspection scheduling. Viewed month: {viewedMonth:MMMM yyyy}. Reason: {safeReason}",
                ActionDateTime = now
            });

        await _db.SaveChangesAsync();

        try
        {
            var subject =
                $"Inspection scheduling assistance required - {request.Attr_No ?? property.Attr_No ?? "-"}";

            var body = $@"
<p>Good day,</p>

<p>A client has requested assistance with scheduling a physical property attribute inspection.</p>

<table style='border-collapse:collapse;width:100%;max-width:700px'>
<tr>
<td style='border:1px solid #ddd;padding:8px;font-weight:bold'>Reference</td>
<td style='border:1px solid #ddd;padding:8px'>{WebUtility.HtmlEncode(request.Attr_No ?? property.Attr_No ?? "-")}</td>
</tr>
<tr>
<td style='border:1px solid #ddd;padding:8px;font-weight:bold'>Property</td>
<td style='border:1px solid #ddd;padding:8px'>{WebUtility.HtmlEncode(property.Property_Desc ?? "-")}</td>
</tr>
<tr>
<td style='border:1px solid #ddd;padding:8px;font-weight:bold'>Client</td>
<td style='border:1px solid #ddd;padding:8px'>{WebUtility.HtmlEncode(request.ClientName ?? "Client")}</td>
</tr>
<tr>
<td style='border:1px solid #ddd;padding:8px;font-weight:bold'>Client Email</td>
<td style='border:1px solid #ddd;padding:8px'>{WebUtility.HtmlEncode(request.ClientEmail ?? "-")}</td>
</tr>
<tr>
<td style='border:1px solid #ddd;padding:8px;font-weight:bold'>Month Viewed</td>
<td style='border:1px solid #ddd;padding:8px'>{viewedMonth:MMMM yyyy}</td>
</tr>
<tr>
<td style='border:1px solid #ddd;padding:8px;font-weight:bold'>Reason</td>
<td style='border:1px solid #ddd;padding:8px'>{WebUtility.HtmlEncode(safeReason)}</td>
</tr>
</table>

<p>
The inspection request remains open in AIVS/Genesis.
Please contact the client and assist with arranging a suitable inspection appointment.
</p>";

            await _emailService.SendEmailAsync(
                AdministrationAssistanceEmail,
                subject,
                body);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Administration assistance request was recorded but email delivery failed for inspection request {InspectionRequestId}.",
                request.Id);
        }

        TempData["InspectionLinkSuccess"] =
            "Your request has been sent to Valuation Administration. They will assist you with arranging a suitable inspection date.";

        return RedirectToAction(
            nameof(Index),
            new
            {
                token,
                year = viewedMonth.Year,
                month = viewedMonth.Month
            });
    }

    [HttpPost("{token:guid}/verify-pin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPin(
        Guid token,
        string? inspectionPin)
    {
        ApplySecureLinkHeaders();

        var request = await _db.AttrInspectionRequests
            .FirstOrDefaultAsync(x => x.EmailToken == token);

        if (request == null)
            return View("Invalid");

        var property = await _db.AttrPropertyInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Attr_ID == request.Attr_ID &&
                x.IsActive);

        if (property == null)
            return View("Invalid");

        if (request.EmailTokenExpiresAt.HasValue &&
            request.EmailTokenExpiresAt.Value < DateTime.Now)
        {
            return RedirectToAction(
                nameof(Index),
                new { token, view = "valuer" });
        }

        if (!request.ValuerDetailsSent ||
            string.IsNullOrWhiteSpace(request.InspectionPin))
        {
            TempData["InspectionPinError"] =
                "The authorised valuer details have not been released yet.";

            return RedirectToAction(
                nameof(Index),
                new { token, view = "valuer" });
        }

        var now = DateTime.Now;

        if (request.PinValidFrom.HasValue &&
            now < request.PinValidFrom.Value)
        {
            TempData["InspectionPinError"] =
                $"The inspection PIN will be valid from {request.PinValidFrom.Value:dd MMM yyyy HH:mm}.";

            return RedirectToAction(
                nameof(Index),
                new { token, view = "valuer" });
        }

        if (request.PinValidUntil.HasValue &&
            now > request.PinValidUntil.Value)
        {
            TempData["InspectionPinError"] =
                "The inspection PIN has expired. Please contact Valuation Administration.";

            return RedirectToAction(
                nameof(Index),
                new { token, view = "valuer" });
        }

        var suppliedPin =
            (inspectionPin ?? string.Empty).Trim();

        var expectedPin =
            (request.InspectionPin ?? string.Empty).Trim();

        if (!string.Equals(
                suppliedPin,
                expectedPin,
                StringComparison.OrdinalIgnoreCase))
        {
            request.PinFailedAttempts += 1;
            request.UpdatedBy =
                request.ClientEmail ??
                "GenesisSecureEmailLink";
            request.UpdatedDate = now;

            await _db.SaveChangesAsync();

            TempData["InspectionPinError"] =
                "The inspection PIN is incorrect. Please use the PIN from the City email.";

            return RedirectToAction(
                nameof(Index),
                new { token, view = "valuer" });
        }

        request.PinVerifiedAt = now;
        request.PinVerifiedByEmail =
            request.ClientEmail;
        request.PinUsedAt = now;
        request.PinUsedByEmail =
            request.ClientEmail;
        request.PinUsedIpAddress =
            HttpContext.Connection.RemoteIpAddress?.ToString();
        request.PinUsedUserAgent =
            Request.Headers.UserAgent.ToString();
        request.UpdatedBy =
            request.ClientEmail ??
            "GenesisSecureEmailLink";
        request.UpdatedDate = now;

        await _db.SaveChangesAsync();

        HttpContext.Session.SetString(
            PinSessionKey(token),
            "1");

        TempData["InspectionLinkSuccess"] =
            "PIN verified. You can now view the authorised valuer details.";

        return RedirectToAction(
            nameof(Index),
            new { token, view = "valuer" });
    }

    [HttpGet("{token:guid}/valuer-photo")]
    public async Task<IActionResult> ValuerPhoto(Guid token)
    {
        ApplySecureLinkHeaders();

        var context =
            await ResolveSecureRequestAsync(token);

        if (context == null ||
            !context.Value.Request.ValuerDetailsSent ||
            !IsPinSessionVerified(token))
        {
            return NotFound();
        }

        var request = context.Value.Request;

        if (string.IsNullOrWhiteSpace(
                request.ValuerSapNumber))
        {
            return NotFound();
        }

        var valuer =
            await _db.AttrValuerInspectionDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SapNumber ==
                        request.ValuerSapNumber &&
                    x.IsActive);

        if (valuer == null)
            return NotFound();

        var path = ResolvePhotoPath(valuer);

        if (path == null ||
            !System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var ext =
            Path.GetExtension(path)
                .ToLowerInvariant();

        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        return PhysicalFile(
            path,
            contentType);
    }

    private async Task<PublicInspectionLinkVm?>
        BuildModelAsync(
            Guid token,
            DateTime requestedMonth)
    {
        var context =
            await ResolveSecureRequestAsync(token);

        if (context == null)
            return null;

        var request = context.Value.Request;
        var property = context.Value.Property;

        var now = DateTime.Now;

        var expired =
            request.EmailTokenExpiresAt.HasValue &&
            request.EmailTokenExpiresAt.Value < now;

        var minMonth = GetMinimumMonth();
        var maxMonth = GetMaximumMonth();

        requestedMonth =
            ClampMonth(requestedMonth, minMonth, maxMonth);

        List<PublicInspectionSlotVm> slots;

        if (!expired &&
            string.Equals(
                request.Status,
                "PendingClientResponse",
                StringComparison.OrdinalIgnoreCase))
        {
            var from =
                requestedMonth == minMonth
                    ? now
                    : requestedMonth;

            var to =
                requestedMonth.AddMonths(1);

            if (request.EmailTokenExpiresAt.HasValue &&
                request.EmailTokenExpiresAt.Value < to)
            {
                to = request.EmailTokenExpiresAt.Value;
            }

            if (to > from)
            {
                var available =
                    await _calendarService.GetAvailableSlotsAsync(
                        request.RequestedByUserId,
                        from,
                        to);

                slots = available
                    .Where(x =>
                        x.Year == requestedMonth.Year &&
                        x.Month == requestedMonth.Month)
                    .Select((dateTime, index) =>
                        new PublicInspectionSlotVm
                        {
                            Id = 0,
                            SlotNo = index + 1,
                            ProposedDateTime = dateTime,
                            Status = "Available"
                        })
                    .ToList();
            }
            else
            {
                slots = new List<PublicInspectionSlotVm>();
            }
        }
        else
        {
            slots =
                await _db.AttrInspectionRequestSlots
                    .AsNoTracking()
                    .Where(x =>
                        x.InspectionRequestId ==
                        request.Id)
                    .OrderBy(x => x.SlotNo)
                    .Select(x =>
                        new PublicInspectionSlotVm
                        {
                            Id = x.Id,
                            SlotNo = x.SlotNo,
                            ProposedDateTime =
                                x.ProposedDateTime,
                            Status =
                                x.SlotStatus ??
                                string.Empty
                        })
                    .ToListAsync();
        }

        var pinVerified =
            IsPinSessionVerified(token);

        var valuerDetailsReleased =
            request.ValuerDetailsSent &&
            !string.IsNullOrWhiteSpace(
                request.InspectionPin);

        PublicValuerDetailsVm? valuerVm = null;

        if (valuerDetailsReleased &&
            pinVerified &&
            !string.IsNullOrWhiteSpace(
                request.ValuerSapNumber))
        {
            var valuer =
                await _db.AttrValuerInspectionDetails
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.SapNumber ==
                            request.ValuerSapNumber &&
                        x.IsActive);

            if (valuer != null)
            {
                valuerVm =
                    new PublicValuerDetailsVm
                    {
                        ValuerName =
                            valuer.ValuerName,
                        EmailAddress =
                            valuer.EmailAddress,
                        CellNumber =
                            valuer.CellNumber,
                        VehicleRegistration =
                            valuer.VehicleRegistration,
                        VehicleMake =
                            valuer.VehicleMake,
                        VehicleColour =
                            valuer.VehicleColour,
                        HasPhoto =
                            ResolvePhotoPath(valuer)
                                is string p &&
                            System.IO.File.Exists(p)
                    };
            }
        }

        return new PublicInspectionLinkVm
        {
            Token = token,
            InspectionRequestId =
                request.Id,
            AttrNo =
                request.Attr_No ??
                property.Attr_No ??
                "-",
            PropertyDescription =
                property.Property_Desc ??
                "-",
            ClientName =
                request.ClientName ??
                "Client",
            Status =
                request.Status ??
                string.Empty,
            RequestComment =
                request.RequestComment,
            ConfirmedDateTime =
                request.ConfirmedDateTime,
            ExpiresAt =
                request.EmailTokenExpiresAt,
            IsExpired =
                expired,
            CanSelectDate =
                !expired &&
                string.Equals(
                    request.Status,
                    "PendingClientResponse",
                    StringComparison.OrdinalIgnoreCase),
            CurrentMonth =
                requestedMonth,
            MinimumMonth =
                minMonth,
            MaximumMonth =
                maxMonth,
            HasPreviousMonth =
                requestedMonth > minMonth,
            HasNextMonth =
                requestedMonth < maxMonth,
            PreviousMonth =
                requestedMonth > minMonth
                    ? requestedMonth.AddMonths(-1)
                    : null,
            NextMonth =
                requestedMonth < maxMonth
                    ? requestedMonth.AddMonths(1)
                    : null,
            AdministrationAssistanceRequested =
                string.Equals(
                    request.ClientResponseChannel,
                    "AdministrationAssistanceRequested",
                    StringComparison.OrdinalIgnoreCase),
            ValuerDetailsReleased =
                !expired &&
                valuerDetailsReleased,
            PinVerified =
                pinVerified,
            RequiresPinVerification =
                !expired &&
                valuerDetailsReleased &&
                !pinVerified,
            PinValidFrom =
                request.PinValidFrom,
            PinValidUntil =
                request.PinValidUntil,
            ValuerDetailsAvailable =
                !expired &&
                pinVerified &&
                valuerVm != null,
            Slots = slots,
            Valuer = valuerVm
        };
    }

    private async Task<(
        AttrInspectionRequest Request,
        AttrPropertyInfo Property)?>
        ResolveSecureRequestAsync(Guid token)
    {
        if (token == Guid.Empty)
            return null;

        var request =
            await _db.AttrInspectionRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmailToken == token);

        if (request == null)
            return null;

        var property =
            await _db.AttrPropertyInfo
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID ==
                        request.Attr_ID &&
                    x.IsActive);

        if (property == null)
            return null;

        return (request, property);
    }

    private static bool CanClientStillBook(
        AttrInspectionRequest request)
    {
        if (!string.Equals(
                request.Status,
                "PendingClientResponse",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !request.EmailTokenExpiresAt.HasValue ||
               request.EmailTokenExpiresAt.Value >=
               DateTime.Now;
    }

    private static DateTime GetMinimumMonth()
    {
        var today = DateTime.Today;

        return new DateTime(
            today.Year,
            today.Month,
            1);
    }

    private static DateTime GetMaximumMonth() =>
        GetMinimumMonth()
            .AddMonths(
                OnlineBookingMonthCount - 1);

    private static DateTime ResolveRequestedMonth(
        int? year,
        int? month)
    {
        var minMonth = GetMinimumMonth();
        var maxMonth = GetMaximumMonth();

        if (!year.HasValue ||
            !month.HasValue ||
            month.Value < 1 ||
            month.Value > 12 ||
            year.Value < 2000 ||
            year.Value > 2100)
        {
            return minMonth;
        }

        var requested =
            new DateTime(
                year.Value,
                month.Value,
                1);

        return ClampMonth(
            requested,
            minMonth,
            maxMonth);
    }

    private static DateTime ClampMonth(
        DateTime requested,
        DateTime minMonth,
        DateTime maxMonth)
    {
        var month =
            new DateTime(
                requested.Year,
                requested.Month,
                1);

        if (month < minMonth)
            return minMonth;

        if (month > maxMonth)
            return maxMonth;

        return month;
    }

    private static string AppendClientResponseNote(
        string? existing,
        string note)
    {
        var entry =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {note}";

        if (string.IsNullOrWhiteSpace(existing))
            return entry;

        var combined =
            existing.Trim() +
            Environment.NewLine +
            entry;

        const int maxLength = 3900;

        return combined.Length <= maxLength
            ? combined
            : combined[^maxLength..];
    }

    private static string PinSessionKey(Guid token) =>
        $"attribute-inspection-pin:{token:D}";

    private bool IsPinSessionVerified(Guid token) =>
        string.Equals(
            HttpContext.Session.GetString(
                PinSessionKey(token)),
            "1",
            StringComparison.Ordinal);

    private string? ResolvePhotoPath(
        AttrValuerInspectionDetail valuer)
    {
        var root =
            Path.GetFullPath(
                _photoSettings.RootFolder ??
                @"C:\AIVS\ValuerInspectionPhotos");

        string? candidate = null;

        if (!string.IsNullOrWhiteSpace(
                valuer.PhotoPath))
        {
            candidate =
                Path.IsPathRooted(
                    valuer.PhotoPath)
                    ? valuer.PhotoPath
                    : Path.Combine(
                        root,
                        valuer.PhotoPath);
        }
        else if (!string.IsNullOrWhiteSpace(
                     valuer.PhotoFileName))
        {
            candidate =
                Path.Combine(
                    root,
                    Path.GetFileName(
                        valuer.PhotoFileName));
        }

        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        var full =
            Path.GetFullPath(candidate);

        if (!full.StartsWith(
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return full;
    }

    private void ApplySecureLinkHeaders()
    {
        Response.Headers.CacheControl =
            "no-store, no-cache, must-revalidate, max-age=0";

        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        Response.Headers["Referrer-Policy"] =
            "no-referrer";
        Response.Headers["X-Content-Type-Options"] =
            "nosniff";
        Response.Headers["X-Frame-Options"] =
            "DENY";
        Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    }
}
