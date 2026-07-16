namespace V2_Genesis.Models.ViewModels.Attributes
{
    public class InspectionAppointmentResponseVm
    {
        public long InspectionRequestId { get; set; }

        public long SelectedSlotId { get; set; }

        public string? ClientResponseComment { get; set; }
    }
}
