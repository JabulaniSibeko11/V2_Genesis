namespace V2_Genesis.Models.ViewModels.Dashboard
{
    public class AppointmentValuerDetailsVm
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public long InspectionRequestId { get; set; }

        public string? AttrNo { get; set; }

        public string? PropertyDescription { get; set; }

        public DateTime? ConfirmedDateTime { get; set; }

        public string? ValuerName { get; set; }

        public string? EmailAddress { get; set; }

        public string? CellNumber { get; set; }

        public string? VehicleRegistration { get; set; }

        public string? VehicleMake { get; set; }

        public string? VehicleColour { get; set; }

        public bool HasPhoto { get; set; }

        public DateTime? PinVerifiedAt { get; set; }

        public DateTime? PinUsedAt { get; set; }
    }
}
