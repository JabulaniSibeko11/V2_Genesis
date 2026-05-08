namespace V2_Genesis.Services.Interfaces
{
    public interface INoticeService
    {
        /// <summary>
        /// Generates Section 49 PDF for a property.
        /// Returns (pdfBytes, fileName).
        /// </summary>
        Task<(byte[] Pdf, string FileName)> GenerateSection49Async(
            string rollSource,
            string unitKey,
            string valuationKey);
    }
}
