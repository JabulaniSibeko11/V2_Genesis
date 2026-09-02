namespace V2_Genesis.Models.ViewModels.Attributes;

public sealed class PublicInspectionLinkVm
{
    public Guid Token { get; set; }
    public long InspectionRequestId { get; set; }
    public string AttrNo { get; set; } = string.Empty;
    public string PropertyDescription { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RequestComment { get; set; }

    public DateTime? ConfirmedDateTime { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsExpired { get; set; }
    public bool CanSelectDate { get; set; }

    public bool ValuerDetailsAvailable { get; set; }
    public bool ValuerDetailsReleased { get; set; }
    public bool PinVerified { get; set; }
    public bool RequiresPinVerification { get; set; }
    public DateTime? PinValidFrom { get; set; }
    public DateTime? PinValidUntil { get; set; }

    public string? Message { get; set; }

    // For new calendar-based requests these are generated dynamically from
    // the assigned processor's AIVS calendar. Nothing is persisted until
    // the client confirms one date/time.
    public List<PublicInspectionSlotVm> Slots { get; set; } = new();

    public PublicValuerDetailsVm? Valuer { get; set; }
}

public sealed class PublicInspectionSlotVm
{
    public long Id { get; set; }
    public int SlotNo { get; set; }

    public DateTime ProposedDateTime { get; set; }

    public string Status { get; set; } = string.Empty;
}

public sealed class PublicValuerDetailsVm
{
    public string ValuerName { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public string? CellNumber { get; set; }
    public string? VehicleRegistration { get; set; }
    public string? VehicleMake { get; set; }
    public string? VehicleColour { get; set; }
    public bool HasPhoto { get; set; }
}
