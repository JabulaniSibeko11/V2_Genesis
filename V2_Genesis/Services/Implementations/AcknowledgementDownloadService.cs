using V2_Genesis.Models.Results.Acknowledgement;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations;

public sealed class AcknowledgementDownloadService
    : IAcknowledgementDownloadService
{
    private readonly INoticeService _noticeService;
    private readonly IObjectionFormService _objectionFormService;
    private readonly ISection78Service _section78Service;

    public AcknowledgementDownloadService(
        INoticeService noticeService,
        IObjectionFormService objectionFormService,
        ISection78Service section78Service)
    {
        _noticeService = noticeService;
        _objectionFormService = objectionFormService;
        _section78Service = section78Service;
    }

    public async Task<GeneratedAcknowledgementResult> GenerateAsync(
        string referenceNumber,
        string? rollSource,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceNumber))
        {
            throw new ArgumentException(
                "Reference number is required.",
                nameof(referenceNumber));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "The current user could not be identified.");
        }

        var reference = referenceNumber.Trim();

        // Section 78 Query or Review.
        if (reference.StartsWith(
                "QUE-",
                StringComparison.OrdinalIgnoreCase))
        {
            return await _section78Service
                .GenerateAcknowledgementFromDatabaseAsync(
                    reference,
                    userId,
                    cancellationToken);
        }

        // Objection or Appeal.
        if (reference.StartsWith(
                "OBJ-",
                StringComparison.OrdinalIgnoreCase)
            ||
            reference.StartsWith(
                "APP-",
                StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(rollSource))
            {
                throw new ArgumentException(
                    "Roll source is required for objection and appeal acknowledgements.",
                    nameof(rollSource));
            }

            var data = await _objectionFormService
                .GetAcknowledgementDataAsync(
                    rollSource.Trim(),
                    reference);

            if (data is null)
            {
                throw new KeyNotFoundException(
                    $"Submission '{reference}' was not found.");
            }

            var generated = await _noticeService
                .GenerateAcknowledgementAsync(data);

            if (generated.Pdf is null || generated.Pdf.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Acknowledgement generation returned an empty PDF for '{reference}'.");
            }

            return new GeneratedAcknowledgementResult
            {
                ReferenceNumber = reference,
                FileName = generated.FileName,
                PdfBytes = generated.Pdf,
                SubmissionType = reference.StartsWith(
                    "APP-",
                    StringComparison.OrdinalIgnoreCase)
                        ? "Appeal"
                        : "Objection"
            };
        }

        throw new NotSupportedException(
            $"Acknowledgement generation is not configured for reference '{reference}'.");
    }
}