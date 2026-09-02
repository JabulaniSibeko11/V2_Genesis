using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using V2_Genesis.Data;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.ViewModels.Attributes;
using V2_Genesis.Services.Attributes;

namespace V2_Genesis.Controllers;

[AllowAnonymous]
[Route("attributes/inspection")]
public sealed class AttributeInspectionLinkController : Controller
{
    private readonly AttributesDbContext _db;
    private readonly ValuerPhotoStorageSettings _photoSettings;
    private readonly ILogger<AttributeInspectionLinkController> _logger;
    private readonly IAttributeInspectionCalendarService _calendarService;

    public AttributeInspectionLinkController(
        AttributesDbContext db,
        IOptions<ValuerPhotoStorageSettings> photoSettings,
        ILogger<AttributeInspectionLinkController> logger,
        IAttributeInspectionCalendarService calendarService)
    {
        _db = db;
        _photoSettings = photoSettings.Value;
        _logger = logger;
        _calendarService = calendarService;
    }

    [HttpGet("{token:guid}")]
    public async Task<IActionResult> Index(Guid token, string? view = null)
    {
        ApplySecureLinkHeaders();
        var model = await BuildModelAsync(token);

        if (model == null)
            return View("Invalid");

        if (string.Equals(view, "valuer", StringComparison.OrdinalIgnoreCase) &&
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
            var expired = await BuildModelAsync(token);

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
            var current = await BuildModelAsync(token);

            if (current != null)
                current.Message =
                    "This inspection request has already been responded to.";

            return View("Index", current);
        }

        // Normalise browser input, then validate the client-supplied value.
        selectedDateTime = new DateTime(
            selectedDateTime.Year,
            selectedDateTime.Month,
            selectedDateTime.Day,
            selectedDateTime.Hour,
            selectedDateTime.Minute,
            0);

        if (selectedDateTime <= DateTime.Now)
        {
            var invalidPast = await BuildModelAsync(token);

            if (invalidPast != null)
                invalidPast.Message =
                    "Please select a future inspection date and time.";

            return View("Index", invalidPast);
        }

        var horizonEnd = DateTime.Today.AddMonths(2);

        if (request.EmailTokenExpiresAt.HasValue &&
            request.EmailTokenExpiresAt.Value < horizonEnd)
        {
            horizonEnd = request.EmailTokenExpiresAt.Value;
        }

        if (selectedDateTime > horizonEnd)
        {
            var invalid = await BuildModelAsync(token);

            if (invalid != null)
                invalid.Message =
                    "The selected inspection date is outside the available booking period.";

            return View("Index", invalid);
        }

        // Critical revalidation:
        // the calendar displayed to the client may now be stale because another
        // client could have booked the same processor while this page was open.
        var stillAvailable =
            await _calendarService.IsSlotAvailableAsync(
                request.RequestedByUserId,
                selectedDateTime,
                request.Id);

        if (!stillAvailable)
        {
            var unavailable = await BuildModelAsync(token);

            if (unavailable != null)
                unavailable.Message =
                    "That inspection time is no longer available. Please select another date and time.";

            return View("Index", unavailable);
        }

        await using var tx =
            await _db.Database.BeginTransactionAsync();

        try
        {
            // Recheck inside the transaction before saving.
            stillAvailable =
                await _calendarService.IsSlotAvailableAsync(
                    request.RequestedByUserId,
                    selectedDateTime,
                    request.Id);

            if (!stillAvailable)
                throw new InvalidOperationException(
                    "The selected inspection time has just been booked. Please select another slot.");

            // We save only the client's selected appointment.
            // We do NOT persist all available calendar slots.
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
                "Inspection date and time selected from the assigned processor's AIVS inspection calendar.";
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
                    review.ReviewStatus =
                        "InspectionConfirmed";
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

            var refreshed = await BuildModelAsync(token);

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

            var failed = await BuildModelAsync(token);

            if (failed != null)
                failed.Message =
                    "The appointment could not be confirmed. Please try again.";

            return View("Index", failed);
        }

        TempData["InspectionLinkSuccess"] =
            "Inspection date and time confirmed successfully.";

        return RedirectToAction(
            nameof(Index),
            new { token });
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
        BuildModelAsync(Guid token)
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

        List<PublicInspectionSlotVm> slots;

        if (!expired &&
            string.Equals(
                request.Status,
                "PendingClientResponse",
                StringComparison.OrdinalIgnoreCase))
        {
            var from = DateTime.Now;
            var to = DateTime.Today.AddMonths(2);

            if (request.EmailTokenExpiresAt.HasValue &&
                request.EmailTokenExpiresAt.Value < to)
            {
                to = request.EmailTokenExpiresAt.Value;
            }

            var available =
                await _calendarService.GetAvailableSlotsAsync(
                    request.RequestedByUserId,
                    from,
                    to);

            slots = available
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
            // Historical/confirmed requests may still contain the old
            // AttrInspectionRequestSlot records. Keep displaying them.
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
    // Secure GUID email-link responses must never be cached, framed or leak referrers.
    private void ApplySecureLinkHeaders()
    {
        Response.Headers.CacheControl =
            "no-store, no-cache, must-revalidate, max-age=0";

        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
    }

}
