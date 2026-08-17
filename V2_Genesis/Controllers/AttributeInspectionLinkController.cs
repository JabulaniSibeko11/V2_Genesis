using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using V2_Genesis.Data;
using V2_Genesis.Models.Attributes;
using V2_Genesis.Models.Configuration;
using V2_Genesis.Models.ViewModels.Attributes;

namespace V2_Genesis.Controllers;

[AllowAnonymous]
[Route("attributes/inspection")]
public sealed class AttributeInspectionLinkController : Controller
{
    private readonly AttributesDbContext _db;
    private readonly ValuerPhotoStorageSettings _photoSettings;
    private readonly ILogger<AttributeInspectionLinkController> _logger;

    public AttributeInspectionLinkController(
        AttributesDbContext db,
        IOptions<ValuerPhotoStorageSettings> photoSettings,
        ILogger<AttributeInspectionLinkController> logger)
    {
        _db = db;
        _photoSettings = photoSettings.Value;
        _logger = logger;
    }

    [HttpGet("{token:guid}")]
    public async Task<IActionResult> Index(Guid token, string? view = null)
    {
        var model = await BuildModelAsync(token);
        if (model == null)
            return View("Invalid");

        if (string.Equals(view, "valuer", StringComparison.OrdinalIgnoreCase) && !model.IsExpired)
        {
            if (model.RequiresPinVerification)
            {
                model.Message = "Enter the inspection PIN from the City email to view the authorised valuer and vehicle details.";
            }
            else if (!model.ValuerDetailsReleased)
            {
                model.Message = "The appointment is confirmed. The authorised valuer details will appear here as soon as the valuer releases them.";
            }
        }

        return View(model);
    }

    [HttpPost("{token:guid}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid token, long slotId)
    {
        var request = await _db.AttrInspectionRequests
            .FirstOrDefaultAsync(x => x.EmailToken == token);

        if (request == null)
            return View("Invalid");

        var property = await _db.AttrPropertyInfo
            .FirstOrDefaultAsync(x => x.Attr_ID == request.Attr_ID && x.IsActive);

        // The unguessable GUID email token authorises this client appointment action.
        // Valuer identity remains protected by the separate inspection PIN.
        if (property == null)
            return View("Invalid");

        if (request.EmailTokenExpiresAt.HasValue && request.EmailTokenExpiresAt.Value < DateTime.Now)
        {
            var expired = await BuildModelAsync(token);
            if (expired != null) expired.Message = "This secure appointment link has expired.";
            return View("Index", expired);
        }

        if (!string.Equals(request.Status, "PendingClientResponse", StringComparison.OrdinalIgnoreCase))
        {
            var current = await BuildModelAsync(token);
            if (current != null) current.Message = "This inspection request has already been responded to.";
            return View("Index", current);
        }

        var slots = await _db.AttrInspectionRequestSlots
            .Where(x => x.InspectionRequestId == request.Id)
            .OrderBy(x => x.SlotNo)
            .ToListAsync();

        var selected = slots.FirstOrDefault(x => x.Id == slotId &&
            string.Equals(x.SlotStatus, "Offered", StringComparison.OrdinalIgnoreCase));

        if (selected == null)
        {
            var invalid = await BuildModelAsync(token);
            if (invalid != null) invalid.Message = "Please select one of the available inspection dates.";
            return View("Index", invalid);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var slot in slots)
                slot.SlotStatus = slot.Id == selected.Id ? "Confirmed" : "Declined";

            request.Status = "Confirmed";
            request.ConfirmedSlotId = selected.Id;
            request.ConfirmedDateTime = selected.ProposedDateTime;
            request.ClientResponseChannel = "GenesisSecureEmailLink";
            request.ClientResponseComment = "Inspection date selected using the secure GUID email link.";
            request.ClientRespondedAt = DateTime.Now;
            request.UpdatedBy = "GenesisSecureEmailLink";
            request.UpdatedDate = DateTime.Now;

            property.Attr_Status = "InspectionConfirmed";
            property.Physical_Inspection_Status = "InspectionConfirmed";
            property.Inspection_Scheduled_Date = selected.ProposedDateTime.Date;
            property.Inspection_Scheduled_Time = selected.ProposedDateTime.TimeOfDay;
            property.UpdatedBy = "GenesisSecureEmailLink";
            property.UpdatedDate = DateTime.Now;

            if (request.ReviewId.HasValue)
            {
                var review = await _db.AttrValuerReviews
                    .FirstOrDefaultAsync(x => x.Id == request.ReviewId.Value && x.Attr_ID == property.Attr_ID);
                if (review != null)
                    review.ReviewStatus = "InspectionConfirmed";
            }

            _db.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = property.Attr_ID,
                Attr_No = property.Attr_No,
                Action = "Inspection Date Confirmed",
                OldStatus = "InspectionRequired",
                NewStatus = "InspectionConfirmed",
                ActionByUserId = request.ClientEmail ?? "Client",
                ActionByName = request.ClientName ?? "Client",
                ActionRole = "Client - Secure Email Link",
                Comment = $"Client selected inspection date {selected.ProposedDateTime:dd MMM yyyy HH:mm} using the secure email link.",
                ActionDateTime = DateTime.Now
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        TempData["InspectionLinkSuccess"] = "Inspection date confirmed successfully.";
        return RedirectToAction(nameof(Index), new { token });
    }

    [HttpPost("{token:guid}/verify-pin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPin(Guid token, string? inspectionPin)
    {
        var request = await _db.AttrInspectionRequests
            .FirstOrDefaultAsync(x => x.EmailToken == token);

        if (request == null)
            return View("Invalid");

        var property = await _db.AttrPropertyInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Attr_ID == request.Attr_ID && x.IsActive);

        if (property == null)
            return View("Invalid");

        if (request.EmailTokenExpiresAt.HasValue && request.EmailTokenExpiresAt.Value < DateTime.Now)
            return RedirectToAction(nameof(Index), new { token, view = "valuer" });

        if (!request.ValuerDetailsSent || string.IsNullOrWhiteSpace(request.InspectionPin))
        {
            TempData["InspectionPinError"] = "The authorised valuer details have not been released yet.";
            return RedirectToAction(nameof(Index), new { token, view = "valuer" });
        }

        var now = DateTime.Now;

        if (request.PinValidFrom.HasValue && now < request.PinValidFrom.Value)
        {
            TempData["InspectionPinError"] =
                $"The inspection PIN will be valid from {request.PinValidFrom.Value:dd MMM yyyy HH:mm}.";
            return RedirectToAction(nameof(Index), new { token, view = "valuer" });
        }

        if (request.PinValidUntil.HasValue && now > request.PinValidUntil.Value)
        {
            TempData["InspectionPinError"] = "The inspection PIN has expired. Please contact Valuation Administration.";
            return RedirectToAction(nameof(Index), new { token, view = "valuer" });
        }

        var suppliedPin = (inspectionPin ?? string.Empty).Trim();
        var expectedPin = (request.InspectionPin ?? string.Empty).Trim();

        if (!string.Equals(suppliedPin, expectedPin, StringComparison.OrdinalIgnoreCase))
        {
            request.PinFailedAttempts += 1;
            request.UpdatedBy = request.ClientEmail ?? "GenesisSecureEmailLink";
            request.UpdatedDate = now;
            await _db.SaveChangesAsync();

            TempData["InspectionPinError"] = "The inspection PIN is incorrect. Please use the PIN from the City email.";
            return RedirectToAction(nameof(Index), new { token, view = "valuer" });
        }

        request.PinVerifiedAt = now;
        request.PinVerifiedByEmail = request.ClientEmail;
        request.PinUsedAt = now;
        request.PinUsedByEmail = request.ClientEmail;
        request.PinUsedIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        request.PinUsedUserAgent = Request.Headers.UserAgent.ToString();
        request.UpdatedBy = request.ClientEmail ?? "GenesisSecureEmailLink";
        request.UpdatedDate = now;

        await _db.SaveChangesAsync();

        HttpContext.Session.SetString(PinSessionKey(token), "1");
        TempData["InspectionLinkSuccess"] = "PIN verified. You can now view the authorised valuer details.";

        return RedirectToAction(nameof(Index), new { token, view = "valuer" });
    }

    [HttpGet("{token:guid}/valuer-photo")]
    public async Task<IActionResult> ValuerPhoto(Guid token)
    {
        var context = await ResolveSecureRequestAsync(token);
        if (context == null || !context.Value.Request.ValuerDetailsSent || !IsPinSessionVerified(token))
            return NotFound();

        var request = context.Value.Request;
        if (string.IsNullOrWhiteSpace(request.ValuerSapNumber))
            return NotFound();

        var valuer = await _db.AttrValuerInspectionDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SapNumber == request.ValuerSapNumber && x.IsActive);

        if (valuer == null) return NotFound();

        var path = ResolvePhotoPath(valuer);
        if (path == null || !System.IO.File.Exists(path))
            return NotFound();

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        return PhysicalFile(path, contentType);
    }

    private async Task<PublicInspectionLinkVm?> BuildModelAsync(Guid token)
    {
        var context = await ResolveSecureRequestAsync(token);
        if (context == null) return null;

        var request = context.Value.Request;
        var property = context.Value.Property;
        var now = DateTime.Now;
        var expired = request.EmailTokenExpiresAt.HasValue && request.EmailTokenExpiresAt.Value < now;

        var slots = await _db.AttrInspectionRequestSlots
            .AsNoTracking()
            .Where(x => x.InspectionRequestId == request.Id)
            .OrderBy(x => x.SlotNo)
            .ToListAsync();

        var pinVerified = IsPinSessionVerified(token);
        var valuerDetailsReleased = request.ValuerDetailsSent && !string.IsNullOrWhiteSpace(request.InspectionPin);

        PublicValuerDetailsVm? valuerVm = null;
        if (valuerDetailsReleased && pinVerified && !string.IsNullOrWhiteSpace(request.ValuerSapNumber))
        {
            var valuer = await _db.AttrValuerInspectionDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SapNumber == request.ValuerSapNumber && x.IsActive);

            if (valuer != null)
            {
                valuerVm = new PublicValuerDetailsVm
                {
                    ValuerName = valuer.ValuerName,
                    EmailAddress = valuer.EmailAddress,
                    CellNumber = valuer.CellNumber,
                    VehicleRegistration = valuer.VehicleRegistration,
                    VehicleMake = valuer.VehicleMake,
                    VehicleColour = valuer.VehicleColour,
                    HasPhoto = ResolvePhotoPath(valuer) is string p && System.IO.File.Exists(p)
                };
            }
        }

        return new PublicInspectionLinkVm
        {
            Token = token,
            InspectionRequestId = request.Id,
            AttrNo = request.Attr_No ?? property.Attr_No ?? "-",
            PropertyDescription = property.Property_Desc ?? "-",
            ClientName = request.ClientName ?? "Client",
            Status = request.Status ?? string.Empty,
            RequestComment = request.RequestComment,
            ConfirmedDateTime = request.ConfirmedDateTime,
            ExpiresAt = request.EmailTokenExpiresAt,
            IsExpired = expired,
            CanSelectDate = !expired && string.Equals(request.Status, "PendingClientResponse", StringComparison.OrdinalIgnoreCase),
            ValuerDetailsReleased = !expired && valuerDetailsReleased,
            PinVerified = pinVerified,
            RequiresPinVerification = !expired && valuerDetailsReleased && !pinVerified,
            PinValidFrom = request.PinValidFrom,
            PinValidUntil = request.PinValidUntil,
            ValuerDetailsAvailable = !expired && pinVerified && valuerVm != null,
            Slots = slots.Select(x => new PublicInspectionSlotVm
            {
                Id = x.Id,
                SlotNo = x.SlotNo,
                ProposedDateTime = x.ProposedDateTime,
                Status = x.SlotStatus ?? string.Empty
            }).ToList(),
            Valuer = valuerVm
        };
    }

    private async Task<(AttrInspectionRequest Request, AttrPropertyInfo Property)?> ResolveSecureRequestAsync(Guid token)
    {
        var request = await _db.AttrInspectionRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EmailToken == token);
        if (request == null) return null;

        var property = await _db.AttrPropertyInfo
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Attr_ID == request.Attr_ID && x.IsActive);

        if (property == null)
            return null;

        return (request, property);
    }

    private static string PinSessionKey(Guid token) =>
        $"attribute-inspection-pin:{token:D}";

    private bool IsPinSessionVerified(Guid token) =>
        string.Equals(
            HttpContext.Session.GetString(PinSessionKey(token)),
            "1",
            StringComparison.Ordinal);

    private string? ResolvePhotoPath(AttrValuerInspectionDetail valuer)
    {
        var root = Path.GetFullPath(_photoSettings.RootFolder ?? @"C:\AIVS\ValuerInspectionPhotos");
        string? candidate = null;

        if (!string.IsNullOrWhiteSpace(valuer.PhotoPath))
            candidate = Path.IsPathRooted(valuer.PhotoPath)
                ? valuer.PhotoPath
                : Path.Combine(root, valuer.PhotoPath);
        else if (!string.IsNullOrWhiteSpace(valuer.PhotoFileName))
            candidate = Path.Combine(root, Path.GetFileName(valuer.PhotoFileName));

        if (string.IsNullOrWhiteSpace(candidate)) return null;

        var full = Path.GetFullPath(candidate);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return null;

        return full;
    }
}
