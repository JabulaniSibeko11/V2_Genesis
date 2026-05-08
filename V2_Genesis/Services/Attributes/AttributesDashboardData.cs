namespace V2_Genesis.Services.Attributes
{
    public class AttributesDashboardData
    {
        public List<AttributeLinkedProperty> LinkedProperties { get; set; } = new();
        public List<AttributeSubmission> Submissions { get; set; } = new();
        public List<AttributeAppointment> Appointments { get; set; } = new();

        public int LinkedCount => LinkedProperties.Count;
        public int SubmissionCount => Submissions.Count;
        public int AppointmentCount => Appointments.Count;
        public int PendingCount => Submissions.Count(s => s.Status == "Pending");
    }
}
