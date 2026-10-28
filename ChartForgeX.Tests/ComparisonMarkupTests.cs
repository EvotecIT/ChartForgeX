using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ComparisonMarkupTests {
    [Theory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1200)]
    [InlineData(true, 1200)]
    public void DirectAndFencedComparisonsShareTheSameNativeSvgPngAndAuthoredPairs(bool dark, int width) {
        foreach (var dumbbell in new[] { false, true }) foreach (var horizontal in new[] { false, true }) {
            var kind = dumbbell ? "dumbbell" : "lollipop"; var orientation = horizontal ? "horizontal" : "vertical";
            var values = dumbbell ? "20 -10 -5 15" : "20 -5";
            var source = "```chartforgex chart v1\ntype " + kind + "\ntitle Changes\nsize " + width + "x360\nlabels North South\nseries Comparison\nvalues " + values + "\noption " + kind + "Orientation " + orientation + "\n```";
            var parsed = new MarkupChartParser().Parse(source); Assert.Empty(parsed.Diagnostics);
            var direct = Chart.Create().WithTitle("Changes").WithSize(width, 360).WithXLabels("North", "South");
            if (dumbbell) direct.AddDumbbell("Comparison", new[] { new ChartDumbbell(1, 20, -10), new ChartDumbbell(2, -5, 15) });
            else direct.AddLollipop("Comparison", new[] { new ChartPoint(1, 20), new ChartPoint(2, -5) });
            direct.Options.LollipopOrientation = direct.Options.DumbbellOrientation = horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical;
            var context = new VisualRenderContext(layout: new VisualLayoutOptions(new VisualSize(width, 360)),
                theme: VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), themeMode: dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
            var expected = direct.Prepare(context); var actual = parsed.Document!.Chart.Prepare(context);
            Assert.Equal(expected.ToSvg("comparison-parity"), actual.ToSvg("comparison-parity")); Assert.Equal(expected.ToPng(), actual.ToPng());
            Assert.Equal(horizontal ? ChartOrientation.Horizontal : ChartOrientation.Vertical, actual.CartesianOrientation);
            var image = RasterImageDecoder.Decode(actual.ToPng()); Assert.Equal(width, image.Width); Assert.Equal(360, image.Height);
            Assert.Equal(2, parsed.Document.Chart.Series[0].ObservationCount);
            if (dumbbell) {
                Assert.Equal(new[] { 20d, -5 }, parsed.Document.Chart.Series[0].Dumbbells.Select(value => value.Start));
                Assert.Equal(new[] { -10d, 15 }, parsed.Document.Chart.Series[0].Dumbbells.Select(value => value.End));
            }
            ComparisonOrientationTests.Save(kind + "-" + orientation + "-" + width + "-" + (dark ? "dark" : "light"), actual);
        }
    }

    [Theory]
    [InlineData("type dumbbell\nvalues 20 10 5")]
    [InlineData("series Comparison type dumbbell values 20 10 5")]
    [InlineData("type dumbbell\nvalues 20 10\noption dumbbellOrientation diagonal")]
    [InlineData("type lollipop\nvalues 20 10\noption lollipopOrientation 42")]
    public void IncompletePairsAndInvalidOrientationsProduceDiagnosticsWithoutPartialCharts(string source) {
        var parsed = new MarkupChartParser().Parse("```chartforgex chart v1\n" + source + "\n```");
        Assert.True(parsed.HasErrors); Assert.Null(parsed.Document);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MarkupDiagnosticSeverity.Error);
    }
}
