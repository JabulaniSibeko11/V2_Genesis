using V2_Genesis.Services.Section51;
using Xunit;

namespace V2_Genesis.Tests.Services;

/// Section 51 notice: a split block is printed only when it has values,
/// so a multipurpose form on a single property stays a single notice.
public class Section51SplitTests
{
    [Fact]
    public void Empty_split_is_not_printed() =>
        Assert.False(Section51PdfBuilder.HasSplit(null, "", "  ", null, null, null));

    [Fact]
    public void Zero_values_count_as_empty() =>
        Assert.False(Section51PdfBuilder.HasSplit("", "0", "R 0", "", "0.00", "0"));

    [Fact]
    public void Roll_side_value_prints_the_split() =>
        Assert.True(Section51PdfBuilder.HasSplit("Business and Commercial", null, null, null, null, null));

    [Fact]
    public void Objector_side_value_prints_the_split() =>
        Assert.True(Section51PdfBuilder.HasSplit(null, null, null, null, null, "R 1 200 000"));

    [Fact]
    public void Extent_only_prints_the_split() =>
        Assert.True(Section51PdfBuilder.HasSplit(null, "476", null, null, null, null));
}
