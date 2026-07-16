namespace V2_Genesis.Services.Attributes
{
    public class AttributeAppointment
    {
        public long Id { get; set; }

        public long AttrId { get; set; }

        public string? AppointmentRef { get; set; }

        public string? PropertyDesc { get; set; }

        public string? ValuerName { get; set; }

        public string? Status { get; set; }

        public string? Notes { get; set; }

        public string? RequestComment { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? AppointmentDate { get; set; }

        public string? TimeSlot { get; set; }

        public DateTime? ConfirmedDateTime { get; set; }

        public List<AttributeAppointmentSlot> Slots { get; set; } = new();
    }

    public class AttributeAppointmentSlot
    {
        public long Id { get; set; }

        public long InspectionRequestId { get; set; }

        public int SlotNo { get; set; }

        public DateTime ProposedDateTime { get; set; }

        public string? SlotStatus { get; set; }
    }
}
