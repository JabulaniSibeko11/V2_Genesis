namespace V2_Genesis.Models.Results.Acknowledgement
{
    public sealed class GeneratedAcknowledgementResult
    {
        public required string ReferenceNumber { get; init; }

        public required string FileName { get; init; }

        public required byte[] PdfBytes { get; init; }

        public required string SubmissionType { get; init; }
    }
}
