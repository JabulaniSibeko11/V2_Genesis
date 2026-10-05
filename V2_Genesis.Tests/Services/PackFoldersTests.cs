using Microsoft.Extensions.Configuration;
using V2_Genesis.Services.Objection;
using Xunit;

namespace V2_Genesis.Tests.Services;

/// <summary>
/// Objection / Appeal Pack folder names.
/// </summary>
public class PackFoldersTests
{
    private static IConfiguration Config(Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();

    [Fact]
    public void Default_folder_names_are_used_when_appsettings_has_none()
    {
        var config = Config();

        Assert.Equal("Representative", PackFolders.Representative(config));
        Assert.Equal("Submitted Evidence", PackFolders.SubmittedEvidence(config));
        Assert.Equal("Section 51 Notice", PackFolders.Section51Notice(config));
        Assert.Equal("Section 51 Owner Evidence", PackFolders.Section51OwnerEvidence(config));
    }

    [Fact]
    public void Folder_name_from_appsettings_wins()
    {
        var config = Config(new() { ["PackFolders:SubmittedEvidence"] = "  Evidence  " });

        Assert.Equal("Evidence", PackFolders.SubmittedEvidence(config));
    }

    [Fact]
    public void Objection_pack_zip_is_named_after_the_objection()
        => Assert.Equal("GV23-Sup4-24 Objection Pack.zip", PackFolders.ObjectionPackZipName("GV23-Sup4-24"));

    [Fact]
    public void A_slash_in_a_reference_cannot_create_a_sub_folder()
        => Assert.Equal("GV23_24", PackFolders.SafeName("GV23/24"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_reference_becomes_Unknown(string? input)
        => Assert.Equal("Unknown", PackFolders.SafeName(input));
}
