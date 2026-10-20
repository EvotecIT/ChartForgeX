using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Complete-chart acceptance keeps difficult interval and paired data honest after frame and axis layout.</summary>
public sealed class ExistingFamilyDataStateAcceptanceTests {
    [Theory]
    [InlineData("dense", 390, VisualThemeMode.Light)]
    [InlineData("dense-horizontal", 390, VisualThemeMode.Dark)]
    [InlineData("dense", 760, VisualThemeMode.Dark)]
    [InlineData("dense-horizontal", 760, VisualThemeMode.Light)]
    [InlineData("sparse-catalog", 390, VisualThemeMode.Light)]
    [InlineData("sparse-catalog", 760, VisualThemeMode.Dark)]
    public void CompleteRangeBarFramesRetainEverySignedReversedZeroAndSparseInterval(string variant, int width, VisualThemeMode mode) {
        var chart = RangeBarRecipe.Create(variant); var series = Assert.Single(chart.Series);
        var source = series.Intervals.ToArray();
        for (var index = 0; index < source.Length; index++) series.WithObservationId(index, new ChartObservationId("interval-" + index));
        var prepared = Prepare(chart, width, mode); var points = Points(prepared);
        Assert.Equal(variant == "sparse-catalog" ? 3 : 24, points.Length);
        for (var index = 0; index < source.Length; index++) {
            Assert.Equal(source[index].X, Number(points[index], "data-cfx-x"));
            Assert.Equal(source[index].Start, Number(points[index], "data-cfx-start"));
            Assert.Equal(source[index].End, Number(points[index], "data-cfx-end"));
            Assert.Equal("interval-" + index, (string?)points[index].Attribute("data-cfx-observation-id"));
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), (string?)points[index].Attribute("data-cfx-source-point"));
        }
        Assert.Equal(source, series.Intervals); Assert.Equal(source.Length, series.SourcePointCount);
        Assert.Equal(source.Length * 2, prepared.Scene.Nodes.Count(node => node.Role == "range-bar-cap"));
        Assert.Contains(source, item => item.Start == 0 && item.End == 0);
        Assert.Contains(source, item => item.End < item.Start);
        AssertNumericAxisCaptions(prepared, variant.Contains("horizontal", StringComparison.Ordinal) ? "axis-x-label" : "axis-y-label");
        var facts = source.Select(item => Math.Min(item.Start, item.End).ToString("0.##", CultureInfo.InvariantCulture) + "–"
            + Math.Max(item.Start, item.End).ToString("0.##", CultureInfo.InvariantCulture)).ToArray();
        var values = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.NotEmpty(values);
        Assert.All(values, value => Assert.Contains(string.Join(" ", value.Text.Lines.Select(line => line.Text)), facts));
        if (variant == "sparse-catalog") Assert.Equal(new[] { 1d, 4, 8 }, source.Select(item => item.X));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"rangebar-{variant}-{width}-{mode}");
    }

    [Theory]
    [InlineData(ChartSeriesKind.RangeBand, 390, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.RangeBand, 760, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.DifferenceArea, 390, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.DifferenceArea, 760, VisualThemeMode.Light)]
    public void DenseCompletePairedFramesRetainRawPairsTiesAndBothSidesOfTheirExplicitGap(ChartSeriesKind kind, int width, VisualThemeMode mode) {
        var chart = kind == ChartSeriesKind.RangeBand ? RangeBandRecipe.Create("dense") : DifferenceAreaRecipe.Create("dense");
        var series = Assert.Single(chart.Series);
        for (var index = 0; index < 32; index++) series.WithObservationId(index, new ChartObservationId("pair-" + index));
        var prepared = Prepare(chart, width, mode); var points = Points(prepared);
        Assert.Equal(32, points.Length); Assert.Equal(32, series.SourcePointCount);
        for (var index = 0; index < points.Length; index++) {
            var first = kind == ChartSeriesKind.RangeBand ? series.Ranges[index].Lower : series.DifferencePoints[index].First;
            var second = kind == ChartSeriesKind.RangeBand ? series.Ranges[index].Upper : series.DifferencePoints[index].Second;
            Assert.Equal(first, Number(points[index], kind == ChartSeriesKind.RangeBand ? "data-cfx-lower" : "data-cfx-first"));
            Assert.Equal(second, Number(points[index], kind == ChartSeriesKind.RangeBand ? "data-cfx-upper" : "data-cfx-second"));
            Assert.Equal("pair-" + index, (string?)points[index].Attribute("data-cfx-observation-id"));
            Assert.Equal(index == 16 ? "true" : "false", (string?)points[index].Attribute("data-cfx-break-before"));
        }
        var roles = kind == ChartSeriesKind.RangeBand ? new[] { "range-upper", "range-lower" } : new[] { "difference-first", "difference-second" };
        foreach (var role in roles) Assert.Equal(2, prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == role)
            .Sum(path => path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo)));
        if (kind == ChartSeriesKind.DifferenceArea) {
            Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-positive");
            Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-negative");
        }
        AssertNumericAxisCaptions(prepared, "axis-x-label"); AssertNumericAxisCaptions(prepared, "axis-y-label");
        ScatterBubbleColorRenderingTests.Capture(prepared, $"paired-dense-{kind}-{width}-{mode}");
    }

    private static XElement[] Points(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "point").ToArray();
    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
    private static PreparedVisual Prepare(Chart chart, int width, VisualThemeMode mode) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(width, 320)), themeMode: mode, frame: new VisualFrame(chart.Title)));
    private static void AssertNumericAxisCaptions(PreparedVisual prepared, string role) {
        var captions = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == role).ToArray();
        Assert.NotEmpty(captions);
        Assert.All(captions, caption => {
            Assert.DoesNotContain("…", string.Join(" ", caption.Text.Lines.Select(line => line.Text)), StringComparison.Ordinal);
            Assert.Equal(12, caption.Text.Size);
        });
    }
}
