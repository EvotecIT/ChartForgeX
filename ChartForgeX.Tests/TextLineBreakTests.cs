using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the line-break opportunities that sequence captions wrap at.</summary>
public sealed class TextLineBreakTests {
    [Fact]
    public void ALatinWordAfterCjkClosingPunctuationStartsItsOwnRun() =>
        Assert.Equal(new[] { "漢。", "abcdefgh" }, TextLineBreak.Runs("漢。abcdefgh"));

    [Fact]
    public void NoBreakSpacesKeepTheirCjkNeighboursInOneRun() {
        Assert.Equal(new[] { "前", "漢 字" }, TextLineBreak.Runs("前漢 字"));
        Assert.Equal(new[] { "前", "漢 字" }, TextLineBreak.Runs("前漢 字"));
        Assert.Equal(new[] { "前", "漢⁠字" }, TextLineBreak.Runs("前漢⁠字"));
    }
}
