namespace V2_Genesis.Services.Attributes
{
    public class AttributeAppointment
    {
        public int Id { get; set; }
        public string? AppointmentRef { get; set; }
        public string? PropertyDesc { get; set; }
        public string? ValuerName { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string? TimeSlot { get; set; }
        public string? Status { get; set; }  // Scheduled / Completed / Cancelled
        public string? Notes { get; set; }
    }
}
