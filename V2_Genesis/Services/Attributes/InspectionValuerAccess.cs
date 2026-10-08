using System.Security.Cryptography;
using System.Text;

namespace V2_Genesis.Services.Attributes;

/// <summary>
/// Who may see the authorised valuer on the secure inspection page
/// (AttributeInspectionLinkController). Kept free of web / database code so
/// the rules can be unit-tested (V2_Genesis.Tests/Services/InspectionValuerAccessTests).
///
/// Result, checked in this order:
///   Hidden       the secure link has expired
///   NotReleased  the valuer has not released the details (no PIN yet)
///   SignIn       visitor is not signed in
///   NotOwner     signed in with an account that did not submit the attributes
///   Locked       too many incorrect PINs
///   NotYetValid  before PinValidFrom
///   Ended        after PinValidUntil
///   Pin          the PIN still has to be entered
///   Visible      show the valuer
/// </summary>
public static class InspectionValuerAccess
{
    public const int MaxPinAttempts = 5;

    public const string Hidden = "Hidden";
    public const string NotReleased = "NotReleased";
    public const string SignIn = "SignIn";
    public const string NotOwner = "NotOwner";
    public const string Locked = "Locked";
    public const string NotYetValid = "NotYetValid";
    public const string Ended = "Ended";
    public const string Pin = "Pin";
    public const string Visible = "Visible";

    public static string Decide(
        DateTime now,
        DateTime? linkExpiresAt,
        bool valuerDetailsSent,
        string? inspectionPin,
        string? signedInUserId,
        string? submittedByUserId,
        int pinFailedAttempts,
        DateTime? pinValidFrom,
        DateTime? pinValidUntil,
        bool pinVerifiedInSession)
    {
        if (linkExpiresAt.HasValue && linkExpiresAt.Value < now)
            return Hidden;

        if (!valuerDetailsSent || string.IsNullOrWhiteSpace(inspectionPin))
            return NotReleased;

        if (string.IsNullOrWhiteSpace(signedInUserId))
            return SignIn;

        if (!IsOwner(submittedByUserId, signedInUserId))
            return NotOwner;

        if (pinFailedAttempts >= MaxPinAttempts)
            return Locked;

        if (pinValidFrom.HasValue && now < pinValidFrom.Value)
            return NotYetValid;

        if (pinValidUntil.HasValue && now > pinValidUntil.Value)
            return Ended;

        return pinVerifiedInSession ? Visible : Pin;
    }

    public static bool IsOwner(string? submittedByUserId, string? userId) =>
        !string.IsNullOrWhiteSpace(submittedByUserId) &&
        !string.IsNullOrWhiteSpace(userId) &&
        string.Equals(submittedByUserId.Trim(), userId.Trim(), StringComparison.OrdinalIgnoreCase);

    /// Constant-time compare (no timing hint about how many digits were right).
    public static bool PinMatches(string? supplied, string? expected)
    {
        var a = (supplied ?? string.Empty).Trim();
        var b = (expected ?? string.Empty).Trim();

        if (a.Length == 0 || b.Length == 0)
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a.ToUpperInvariant()),
            Encoding.UTF8.GetBytes(b.ToUpperInvariant()));
    }

    public static int AttemptsLeft(int pinFailedAttempts) =>
        Math.Max(0, MaxPinAttempts - pinFailedAttempts);
}
