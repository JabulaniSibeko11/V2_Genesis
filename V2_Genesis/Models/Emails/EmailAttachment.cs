namespace V2_Genesis.Models.Emails
{
    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty;

        public byte[] FileBytes { get; set; } = Array.Empty<byte>();

        public string ContentType { get; set; } = "application/pdf";
    }
}
