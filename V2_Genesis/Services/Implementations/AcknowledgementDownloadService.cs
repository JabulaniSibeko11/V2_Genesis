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
        bool allowAdministrativeAccess = false,
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
        if (IsSection78Reference(reference, rollSource))
        {
            return await _section78Service
                .GenerateAcknowledgementFromDatabaseAsync(
                    reference,
                    userId,
                    allowAdministrativeAccess,
                    cancellationToken);
        }

        // Objection or Appeal. The form service detects the roll from the
        // reference when the caller did not supply rollSource.
        var data = await _objectionFormService
            .GetAcknowledgementDataAsync(
                rollSource?.Trim() ?? string.Empty,
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

        var isAppeal = reference.StartsWith(
            "APP-",
            StringComparison.OrdinalIgnoreCase);

        return new GeneratedAcknowledgementResult
        {
            ReferenceNumber = reference,
            FileName = generated.FileName,
            PdfBytes = generated.Pdf,
            SubmissionType = isAppeal ? "Appeal" : "Objection"
        };
    }

    private static bool IsSection78Reference(
        string reference,
        string? rollSource)
    {
        if (string.Equals(rollSource?.Trim(), "Objection_Query",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rollSource?.Trim(), "Query",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var value = reference.Trim().ToUpperInvariant();
        return value.StartsWith("QUE-") ||
               value.StartsWith("QUERY-") ||
               value.Contains("-QUE-") ||
               value.Contains("-QUERY-") ||
               value.EndsWith("-R");
    }
}
