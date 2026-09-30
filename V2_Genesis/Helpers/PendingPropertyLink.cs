using System.Text.Json;

namespace V2_Genesis.Helpers;

/// <summary>
/// Remembers the property a signed-out visitor wanted to link, so that the
/// link can be completed (or offered again) as soon as they sign in —
/// even when they first have to register and confirm their email address.
///
/// Stored in a short-lived HttpOnly cookie. Only a local
/// "/property/save?..." URL is ever accepted back.
/// </summary>
public sealed class PendingPropertyLink
{
    public const string CookieName = "Genesis.PendingLink";
    private const string SavePath = "/property/save";
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(2);

    public string Url { get; set; } = string.Empty;
    public string? PropertyDescription { get; set; }
    public string? RollSource { get; set; }
    public string? RollName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static bool IsSaveUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url)
        && url.StartsWith(SavePath + "?", StringComparison.OrdinalIgnoreCase)
        && !url.Contains("//")
        && !url.Contains('\\');

    public static void Save(HttpContext context, PendingPropertyLink link)
    {
        if (!IsSaveUrl(link.Url))
            return;

        var json = JsonSerializer.Serialize(link);

        context.Response.Cookies.Append(
            CookieName,
            Uri.EscapeDataString(json),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Expires = DateTimeOffset.Now.Add(Lifetime)
            });
    }

    public static PendingPropertyLink? Read(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var raw) ||
            string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var link = JsonSerializer.Deserialize<PendingPropertyLink>(
                Uri.UnescapeDataString(raw));

            return link is not null && IsSaveUrl(link.Url)
                ? link
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(CookieName);
}
