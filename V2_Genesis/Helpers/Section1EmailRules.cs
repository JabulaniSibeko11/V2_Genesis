namespace V2_Genesis.Helpers;

/// <summary>
/// Section 1 e-mail rules.
///
/// A Representative must not type their own e-mail in the Owner details:
/// the owner then never receives the acknowledgement, and the owner's
/// contact details are lost. The browser blocks it (genesis-form-guard.js);
/// the server checks again in case the browser check was skipped.
/// </summary>
public static class Section1EmailRules
{
    public const string OwnerUsesRepEmailMessage =
        "The owner's e-mail address cannot be the same as the representative's e-mail address. " +
        "Please enter the property owner's own e-mail address in Section 1.";

    public static bool OwnerUsesRepEmail(string? ownerEmail, string? repEmail)
    {
        var owner = ownerEmail?.Trim();
        var rep = repEmail?.Trim();

        return !string.IsNullOrWhiteSpace(owner)
               && !string.IsNullOrWhiteSpace(rep)
               && owner.Equals(rep, StringComparison.OrdinalIgnoreCase);
    }
}
