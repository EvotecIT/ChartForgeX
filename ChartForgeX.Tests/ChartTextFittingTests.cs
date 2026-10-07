using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartTextFittingTests {
    [Fact]
    public void LongSourceFitsWithoutShapingItsWholeValueOrEveryTrimmedSuffix() {
        var source = new string('t', 65_536);
        var measured = new List<int>();
        var fitted = ChartTextFitting.FitEnd(source, 40, text => { measured.Add(text.Length); return text.Length; }, "…");
        Assert.Equal(new string('t', 39) + "…", fitted);
        Assert.All(measured, length => Assert.InRange(length, 1, 65));
        Assert.InRange(measured.Count, 1, 30);
        Assert.Equal(new string('t', 40), ChartTextFitting.FitEnd(source, 40, text => text.Length, ""));
    }

    [Theory]
    [InlineData("A\u0301B", 2, "A\u0301")]
    [InlineData("\U0001F469\u200D\U0001F4BBX", 5, "\U0001F469\u200D\U0001F4BB")]
    [InlineData("\U0001F600X", 1, "")]
    public void PrefixFitPreservesCombiningMarksSurrogatePairsAndJoinedEmoji(string source, int width, string expected) {
        Assert.Equal(expected, ChartTextFitting.FitEnd(source, width, text => text.Length, ""));
    }

    [Fact]
    public void ExistingEllipsisContractRetainsFittingValuesAndSuffixBudget() {
        Assert.Equal("whole", ChartTextFitting.TrimEnd("whole", 12, 5, (text, _) => text.Length));
        Assert.Equal("lo...", ChartTextFitting.TrimEnd("long value", 12, 5, (text, _) => text.Length));
        Assert.Equal("", ChartTextFitting.TrimEnd("long value", 12, 2, (text, _) => text.Length));
    }
}
