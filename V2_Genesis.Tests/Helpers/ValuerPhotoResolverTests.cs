using V2_Genesis.Helpers;
using V2_Genesis.Models.Configuration;
using Xunit;

namespace V2_Genesis.Tests.Helpers;

/// <summary>
/// The valuer photo is found however the path was saved, and only image
/// files are ever returned.
/// </summary>
public sealed class ValuerPhotoResolverTests : IDisposable
{
    private readonly string _root;
    private readonly ValuerPhotoStorageSettings _settings;

    public ValuerPhotoResolverTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "genesis-photos-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "30092655.jpg"), "x");
        File.WriteAllText(Path.Combine(_root, "abc.png"), "x");
        File.WriteAllText(Path.Combine(_root, "notes.txt"), "x");
        _settings = new ValuerPhotoStorageSettings { RootFolder = _root };
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* ignore */ }
    }

    private string? Resolve(string? path, string? file = null, string? sap = null) =>
        ValuerPhotoResolver.Resolve(path, file, sap, _settings);

    [Fact]
    public void Full_path_is_used()
        => Assert.EndsWith("30092655.jpg", Resolve(Path.Combine(_root, "30092655.jpg")));

    [Fact]
    public void Path_saved_without_extension_is_found()
        => Assert.EndsWith("30092655.jpg", Resolve(Path.Combine(_root, "30092655")));

    [Fact]
    public void Web_style_path_uses_the_file_name_under_the_root()
        => Assert.EndsWith("abc.png", Resolve("/ValuerInspectionPhotos/abc.png"));

    [Fact]
    public void Only_a_file_name_is_found_under_the_root()
        => Assert.EndsWith("abc.png", Resolve(null, "abc.png"));

    [Fact]
    public void Sap_number_is_the_last_option()
        => Assert.EndsWith("30092655.jpg", Resolve(null, null, "30092655"));

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("../../etc/passwd")]
    [InlineData("missing.jpg")]
    public void Never_returns_a_non_image_or_missing_file(string path)
        => Assert.Null(Resolve(path));

    [Fact]
    public void Nothing_saved_returns_null()
        => Assert.Null(Resolve(null, null, null));

    [Theory]
    [InlineData("a.png", "image/png")]
    [InlineData("a.JPG", "image/jpeg")]
    [InlineData("a.jpeg", "image/jpeg")]
    public void Content_type_follows_the_extension(string file, string type)
        => Assert.Equal(type, ValuerPhotoResolver.ContentType(file));
}
