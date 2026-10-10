namespace V2_Genesis.Services.Section51;

/// <summary>
/// Who receives the Section 51 notice e-mail.
///
///   Owner has an email   → To: owner,           CC: Valuation Enquiries, BCC: tracking mailbox
///   Owner has no email   → To: tracking mailbox, CC: Valuation Enquiries
///                          (the PDF must still be printed and posted to the owner)
///   Test mode            → To: test recipient,  CC: Valuation Enquiries, BCC: tracking mailbox
///
/// The tracking mailbox is never a visible copy: it is either the direct
/// recipient (no owner email) or a blind copy (BCC).
///
/// The tracking mailbox is per roll (Section51Rolls:{roll}:TrackingEmail,
/// e.g. GV23Supp4@joburg.org.za) so every Section 51 notice of the roll can be
/// followed up from one place.
/// </summary>
public static class Section51Recipients
{
    public sealed record Plan(string? To, IReadOnlyList<string> Cc, IReadOnlyList<string> Bcc, bool OwnerHasEmail)
    {
        public bool CanSend => !string.IsNullOrWhiteSpace(To);

        /// The owner (not just the tracking mailbox) gets the notice.
        public bool ReachesOwner(bool testMode) => CanSend && OwnerHasEmail && !testMode;
    }

    public static Plan Resolve(
        string? ownerEmail,
        bool testMode,
        string? testRecipient,
        string? ccAddress,
        string? trackingEmail)
    {
        var owner = Clean(ownerEmail);
        var tracking = Clean(trackingEmail);
        var cc = Clean(ccAddress);

        string? to;
        string? bcc;

        if (testMode)
        {
            to = Clean(testRecipient);
            bcc = tracking;
        }
        else if (owner is not null)
        {
            to = owner;
            bcc = tracking;
        }
        else
        {
            to = tracking;   // straight to the tracking mailbox
            bcc = null;
        }

        bool NotTo(string x) => to is null || !x.Equals(to, StringComparison.OrdinalIgnoreCase);

        var ccList = cc is not null && NotTo(cc) ? new List<string> { cc } : new List<string>();

        var bccList = bcc is not null && NotTo(bcc)
                      && !ccList.Contains(bcc, StringComparer.OrdinalIgnoreCase)
            ? new List<string> { bcc }
            : new List<string>();

        return new Plan(to, ccList, bccList, owner is not null);
    }

    private static string? Clean(string? email)
    {
        var value = email?.Trim();
        return string.IsNullOrWhiteSpace(value) || !value.Contains('@') ? null : value;
    }
}
