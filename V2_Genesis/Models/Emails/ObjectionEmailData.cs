namespace V2_Genesis.Models.Emails
{
    public class ObjectionEmailData
    {
        public string ObjectionRef { get; set; } = string.Empty;
        public string RollTitle { get; set; } = string.Empty;
        public bool IsAppeal { get; set; }
        public string SubmittedDate { get; set; } = string.Empty;
        public string ObjectorType { get; set; } = string.Empty;  // Owner | Third_Party | Representative

        // Recipients resolved from Obj_Section1
        public List<EmailRecipient> Recipients { get; set; } = new();

        // Paths
        public string FolderPath { get; set; } = string.Empty;

        // PDF bytes of the acknowledgement to attach
        public byte[]? AcknowledgementPdf { get; set; }
    }
}
