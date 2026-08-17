namespace V2_Genesis.Models.ViewModels.Attributes;

public sealed class InspectionDateModalVm
{
    public string InstanceKey { get; set; } = string.Empty;
    public string AppointmentRef { get; set; } = string.Empty;
    public string FormAction { get; set; } = string.Empty;
    public string SlotFieldName { get; set; } = "SelectedSlotId";
    public string? RequestComment { get; set; }
    public string? CommentFieldName { get; set; }
    public long? InspectionRequestId { get; set; }
    public bool AutoOpen { get; set; }
    public List<InspectionDateModalSlotVm> Slots { get; set; } = new();
}

public sealed class InspectionDateModalSlotVm
{
    public long Id { get; set; }
    public int SlotNo { get; set; }
    public DateTime ProposedDateTime { get; set; }
}

public sealed class InspectionPinModalVm
{
    public string InstanceKey { get; set; } = string.Empty;
    public string AppointmentRef { get; set; } = string.Empty;
    public string FormAction { get; set; } = string.Empty;
    public string PinFieldName { get; set; } = "Pin";
    public long? InspectionRequestId { get; set; }
    public DateTime? ConfirmedDateTime { get; set; }
    public string? ErrorMessage { get; set; }
    public bool AutoOpen { get; set; }
}
