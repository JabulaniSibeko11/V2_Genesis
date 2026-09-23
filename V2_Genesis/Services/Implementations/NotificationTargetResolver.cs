using Microsoft.AspNetCore.WebUtilities;
using V2_Genesis.Models.Notifications;

namespace V2_Genesis.Services.Implementations
{
    /// <summary>
    /// Works out where a notification should take the person who clicks it.
    ///
    /// Client notifications point at the dedicated dashboard pages:
    ///   /dashboard/roll-detail/{roll}?ref=OBJ-123&amp;section=objections
    ///   /dashboard/rebates-detail?ref=...
    ///   /dashboard/attributes-detail?ref=...
    /// The detail page then opens the right section and highlights the row.
    ///
    /// Older rows saved "/Dashboard?openRoll=X", which the new landing page
    /// ignores, so those are translated here as well.
    /// </summary>
    public static class NotificationTargetResolver
    {
        private const string ClientHome = "/dashboard";
        private const string AdminHome = "/admin";

        public static string Resolve(Notifications n, bool isAdmin)
        {
            var url = n.Url?.Trim() ?? string.Empty;

            if (isAdmin)
                return ResolveAdmin(n, url);

            // Anything that is already a specific local page (profile,
            // an inspection, a detail page, ...) is used as-is.
            if (IsLocal(url) && !IsGenericDashboardUrl(url))
                return url;

            var target = ResolveRollOrService(n, url);

            if (target is null)
                return ClientHome;

            var query = new Dictionary<string, string?>();

            if (!string.IsNullOrWhiteSpace(n.ReferenceNumber))
                query["ref"] = n.ReferenceNumber.Trim();

            var section = GuessSection(n, target);
            if (!string.IsNullOrWhiteSpace(section))
                query["section"] = section;

            return QueryHelpers.AddQueryString(target, query);
        }

        /// <summary>
        /// Builds the URL to store on a new client notification.
        /// </summary>
        public static string BuildClientUrl(
            string? rollSource,
            string? referenceNumber = null,
            string? section = null)
        {
            var target = ServicePathFor(rollSource) ?? ClientHome;

            var query = new Dictionary<string, string?>();

            if (!string.IsNullOrWhiteSpace(referenceNumber))
                query["ref"] = referenceNumber.Trim();

            if (!string.IsNullOrWhiteSpace(section))
                query["section"] = section;

            return QueryHelpers.AddQueryString(target, query);
        }

        /// <summary>
        /// Builds the URL to store on a new admin notification: Search
        /// Enquiries with the reference (and roll) filled in and searched.
        /// </summary>
        public static string BuildAdminUrl(
            string? referenceNumber,
            string? rollSource = null)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                return string.IsNullOrWhiteSpace(rollSource)
                    ? AdminHome
                    : QueryHelpers.AddQueryString(AdminHome, "openRoll", rollSource);
            }

            var query = new Dictionary<string, string?>
            {
                ["reference"] = referenceNumber.Trim()
            };

            // The reference search only accepts valuation rolls, not Query.
            if (!string.IsNullOrWhiteSpace(rollSource) &&
                !rollSource.Equals("Objection_Query", StringComparison.OrdinalIgnoreCase))
            {
                query["rollSource"] = rollSource;
            }

            return QueryHelpers.AddQueryString("/admin/search", query);
        }

        private static string ResolveAdmin(Notifications n, string url)
        {
            // Specific admin pages other than the old search link are kept.
            if (IsLocal(url) &&
                !url.StartsWith("/admin/search", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            var reference = n.ReferenceNumber;

            if (string.IsNullOrWhiteSpace(reference) && url.Contains('?'))
            {
                var parsed = QueryHelpers.ParseQuery(url[url.IndexOf('?')..]);
                if (parsed.TryGetValue("reference", out var value))
                    reference = value.ToString();
            }

            return BuildAdminUrl(reference, n.RollSource);
        }

        // ── helpers ───────────────────────────────────────────────────

        private static string? ResolveRollOrService(Notifications n, string url)
        {
            string? openRoll = null;

            var queryStart = url.IndexOf('?');
            if (queryStart >= 0)
            {
                var parsed = QueryHelpers.ParseQuery(url[queryStart..]);
                if (parsed.TryGetValue("openRoll", out var value))
                    openRoll = value.ToString();
            }

            return ServicePathFor(openRoll)
                   ?? ServicePathFor(n.RollSource)
                   ?? ServicePathFor(SourceFromTable(n.SourceTable));
        }

        private static string? ServicePathFor(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            var value = source.Trim();

            if (value.Equals("attributes", StringComparison.OrdinalIgnoreCase))
                return "/dashboard/attributes-detail";

            if (value.Equals("rebates", StringComparison.OrdinalIgnoreCase))
                return "/dashboard/rebates-detail";

            if (value.StartsWith("Objection", StringComparison.OrdinalIgnoreCase))
                return "/dashboard/roll-detail/" + Uri.EscapeDataString(value);

            return null;
        }

        private static string? SourceFromTable(string? sourceTable)
        {
            if (string.IsNullOrWhiteSpace(sourceTable))
                return null;

            var t = sourceTable.ToUpperInvariant();

            if (t.Contains("ATTRIBUTE")) return "attributes";
            if (t.Contains("REBATE")) return "rebates";

            return null;
        }

        private static string? GuessSection(Notifications n, string target)
        {
            var text = $"{n.Title} {n.ReferenceNumber}".ToUpperInvariant();

            if (target.StartsWith("/dashboard/roll-detail/", StringComparison.OrdinalIgnoreCase))
            {
                if (text.Contains("UNLINK") || text.Contains("LINKED"))
                    return "linked";

                if (text.Contains("APPEAL") || text.StartsWith("APP-"))
                    return "appeals";

                if (text.Contains("REVIEW"))
                    return "reviews";

                if (text.Contains("QUERY"))
                    return "queries";

                if (text.Contains("OBJECTION"))
                    return "objections";

                return string.IsNullOrWhiteSpace(n.ReferenceNumber)
                    ? "linked"
                    : null;
            }

            if (target.StartsWith("/dashboard/attributes-detail", StringComparison.OrdinalIgnoreCase))
            {
                if (text.Contains("INSPECTION") || text.Contains("APPOINTMENT"))
                    return "appointments";

                return string.IsNullOrWhiteSpace(n.ReferenceNumber)
                    ? "linked"
                    : "submissions";
            }

            if (target.StartsWith("/dashboard/rebates-detail", StringComparison.OrdinalIgnoreCase))
                return "applications";

            return null;
        }

        private static bool IsGenericDashboardUrl(string url)
        {
            var path = url.Split('?', '#')[0].TrimEnd('/');

            return path.Equals("/dashboard", StringComparison.OrdinalIgnoreCase)
                   || path.Equals("/dashboard/index", StringComparison.OrdinalIgnoreCase)
                   || path.Length == 0;
        }

        private static bool IsLocal(string url) =>
            !string.IsNullOrWhiteSpace(url)
            && url.StartsWith('/')
            && !url.StartsWith("//")
            && !url.StartsWith("/\\");
    }
}