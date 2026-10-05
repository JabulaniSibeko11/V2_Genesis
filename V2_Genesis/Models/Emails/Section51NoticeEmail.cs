namespace V2_Genesis.Models.Emails
{
    /// <summary>
    /// Section 51 notice email sent to the property owner when a
    /// Third-Party objection is submitted.
    /// </summary>
    public sealed class Section51NoticeEmail
    {
        public string ObjectionNo { get; set; } = string.Empty;
        public string PropertyDescription { get; set; } = string.Empty;
        public string? ValuationKey { get; set; }
        public string RollName { get; set; } = string.Empty;
        public string? Section51Pin { get; set; }
        public DateTime SubmissionsCloseDate { get; set; }
        public string PortalUrl { get; set; } = string.Empty;

        /// Owner email from the roll's postal-address table.
        public string OwnerEmail { get; set; } = string.Empty;

        /// Where the email actually goes (the owner, or the test recipient).
        public string ToAddress { get; set; } = string.Empty;

        public string? CcAddress { get; set; }
        public bool IsTest { get; set; }

        public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
        public string PdfFileName { get; set; } = string.Empty;

        /// Folder and file name for the .eml copy.
        public string EmlFolderPath { get; set; } = string.Empty;
        public string EmlFileName { get; set; } = string.Empty;

        /// Other folders that get the same .eml copy (the Objection Pack).
        public List<string> EmlExtraFolders { get; set; } = new();
    }
}
