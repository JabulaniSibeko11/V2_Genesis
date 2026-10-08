using V2_Genesis.Models.Configuration;

namespace V2_Genesis.Helpers;

/// <summary>
/// Finds the photo file of an authorised valuer
/// (AttrValuerInspectionDetails.PhotoPath / PhotoFileName).
///
/// Used by the secure inspection link and by the dashboard, so both find
/// the same photo. The values are written by the valuer administration, and
/// can be:
///   • a full path            C:\AIVS\ValuerInspectionPhotos\30092655.jpg  or \\server\share\x.jpg
///   • a path without .jpg    C:\AIVS\ValuerInspectionPhotos\30092655
///   • a web-style path       /ValuerInspectionPhotos/30092655.jpg  (file name is used)
///   • a relative path        Photos\30092655.png   (under RootFolder)
///   • only a file name       30092655.jpg          (PhotoFileName, under RootFolder)
/// As a last option RootFolder\{SAP number}.jpg|.jpeg|.png is tried.
///
/// Only image files (ValuerPhotoStorage:AllowedExtensions) are ever returned.
/// </summary>
public static class ValuerPhotoResolver
{
    public static string? Resolve(
        string? photoPath,
        string? photoFileName,
        string? sapNumber,
        ValuerPhotoStorageSettings settings)
    {
        var root = string.IsNullOrWhiteSpace(settings.RootFolder)
            ? @"C:\AIVS\ValuerInspectionPhotos"
            : settings.RootFolder.Trim();

        var allowed = (settings.AllowedExtensions?.Count > 0
                ? settings.AllowedExtensions
                : new List<string> { ".jpg", ".jpeg", ".png" })
            .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
            .ToList();

        foreach (var candidate in Candidates(photoPath, photoFileName, sapNumber, root))
        {
            var found = WithExtensions(candidate, allowed);
            if (found != null)
                return found;
        }

        return null;
    }

    public static string ContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

    private static IEnumerable<string> Candidates(
        string? photoPath, string? photoFileName, string? sapNumber, string root)
    {
        var path = photoPath?.Trim().Trim('"');

        if (!string.IsNullOrWhiteSpace(path))
        {
            var isWebPath = path.StartsWith('/') || path.StartsWith("~/") ||
                            path.StartsWith("http", StringComparison.OrdinalIgnoreCase);
            var isWindowsFull = (path.Length > 2 && path[1] == ':') ||
                                path.StartsWith(@"\\");

            if (isWindowsFull)
                yield return path;
            else if (!isWebPath)
                yield return Path.Combine(root, path.TrimStart('\\', '/'));

            var name = SafeFileName(path);
            if (name != null)
                yield return Path.Combine(root, name);
        }

        var fileName = SafeFileName(photoFileName);
        if (fileName != null)
            yield return Path.Combine(root, fileName);

        var sap = SafeFileName(sapNumber);
        if (sap != null)
            yield return Path.Combine(root, sap);
    }

    private static string? SafeFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var name = value.Trim().Replace('\\', '/');
        var slash = name.LastIndexOf('/');
        if (slash >= 0)
            name = name[(slash + 1)..];

        var q = name.IndexOfAny(new[] { '?', '#' });
        if (q >= 0)
            name = name[..q];

        if (string.IsNullOrWhiteSpace(name) ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return null;

        return name;
    }

    private static string? WithExtensions(string candidate, List<string> allowed)
    {
        try
        {
            var ext = Path.GetExtension(candidate).ToLowerInvariant();

            if (!string.IsNullOrEmpty(ext) && allowed.Contains(ext))
                return File.Exists(candidate) ? Path.GetFullPath(candidate) : null;

            // No (image) extension stored: try .jpg / .jpeg / .png.
            foreach (var e in allowed)
            {
                var withExt = candidate + e;
                if (File.Exists(withExt))
                    return Path.GetFullPath(withExt);
            }
        }
        catch (Exception)
        {
            // Unreadable / invalid path: try the next candidate.
        }

        return null;
    }
}
