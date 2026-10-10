using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RoundedRadialSeriesTests {
    [Theory]
    [InlineData(ChartSeriesKind.RadialBar)]
    [InlineData(ChartSeriesKind.RadialColumn)]
    [InlineData(ChartSeriesKind.Sunburst)]
    public void DefaultAndExplicitZeroKeepTheSameSharpSceneAndNativePixels(ChartSeriesKind kind) {
        var chart = V2GalleryModels.Create(kind).WithSize(400, 300).WithHeader(false).WithAxes(false).WithGrid(false).WithLegend(false).WithDataLabels(false);
        UseFixtureFont(chart);
        var original = Prepare(chart); var options = chart.Options.RadialGeometry;
        Assert.Equal(0, kind == ChartSeriesKind.Sunburst ? chart.Options.Sunburst.CornerRadius : options.CornerRadius);
        if (kind == ChartSeriesKind.Sunburst) chart.ConfigureSunburst(sunburst => sunburst.CornerRadius = 0);
        else chart.WithRadialGeometry(new(options.StartAngleDegrees, options.EndAngleDegrees, options.InnerRadiusRatio, options.CategorySpacing, options.SeriesSpacing, cornerRadius: 0));
        var explicitZero = Prepare(chart);
        Assert.Equal(original.ToSvg(), explicitZero.ToSvg());
        Assert.Equal(original.ToPng(), explicitZero.ToPng());
        Assert.DoesNotContain(explicitZero.Scene.Nodes, node => node is VisualScenePath && node.Role is "radial-bar" or "radial-column" or "sunburst-segment-mark");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SignedReversedClippedStacksRoundEveryPaintedSegmentWithoutChangingSourceSemantics(bool bars) {
        var chart = NumericRadialSeriesTests.Add(Frame(), bars, "First", new ChartPoint(1, 200), new ChartPoint(2, -150));
        NumericRadialSeriesTests.Add(chart, bars, "Second", new ChartPoint(1, 300), new ChartPoint(2, -50));
        foreach (var series in chart.Series) series.WithStackGroup("work").WithNormalization(100);
        chart.WithYAxisBounds(-100, 75);
        chart.Options.XAxis.Reversed = true; chart.Options.YAxis.Reversed = true;
        chart.WithRadialGeometry(new(-75, 195, .2, .2, .1));
        chart.Series[1].WithPointFillPattern(0, ChartFillPattern.DiagonalForward);
        var sharp = Prepare(chart); var sharpSvg = XDocument.Parse(sharp.ToSvg());
        chart.WithRadialGeometry(new(-75, 195, .2, .2, .1, cornerRadius: 6));
        var rounded = Prepare(chart); var roundedSvg = XDocument.Parse(rounded.ToSvg());
        Assert.Equal(SourceFacts(sharpSvg, "point"), SourceFacts(roundedSvg, "point"));
        Assert.Equal(4, rounded.Scene.Nodes.OfType<VisualScenePath>().Count(mark => mark.Role is "radial-bar" or "radial-column"));
        Assert.DoesNotContain(rounded.Scene.Nodes, node => node is VisualSceneSlice && node.Role is "radial-bar" or "radial-column");
        Assert.Contains(rounded.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Id == "series-1-point-0" && group.Metadata["data-cfx-clipped"] == "true");
        Assert.Equal(sharp.Regions.Select(region => region.Label), rounded.Regions.Select(region => region.Label));
        Assert.NotEqual(sharp.ToPng(), rounded.ToPng());
        var expected = rounded.ToSvg();
        chart.WithRadialGeometry(new(cornerRadius: 100));
        Assert.Equal(expected, rounded.ToSvg());
    }

    [Theory]
    [InlineData(ChartSeriesKind.RadialBar, false)]
    [InlineData(ChartSeriesKind.RadialColumn, false)]
    [InlineData(ChartSeriesKind.RadialBar, true)]
    [InlineData(ChartSeriesKind.RadialColumn, true)]
    public void RoundedGalleryInsideLabelsStayInTheirOwnPaintedOutline(ChartSeriesKind kind, bool compact) {
        var chart = V2GalleryModels.Create(kind, compact ? "compact-options" : "options");
        chart.WithSize(compact ? 360 : 800, compact ? 360 : 440).WithHeader(false).WithLegend(false);
        UseFixtureFont(chart);
        var scene = Prepare(chart).Scene;
        var marks = scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role is "radial-bar" or "radial-column").ToDictionary(mark => mark.Id!);
        var labels = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "radial-data-label").ToArray();
        if (labels.Length == 0) Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.label-overflow");
        foreach (var label in labels) {
            var mark = marks[label.Id!.Replace("-label", "-mark", StringComparison.Ordinal)];
            var bounds = new ChartRect(label.X, label.Baseline - label.Text.Ascent, label.Text.Metrics.Width, label.Text.Metrics.Height);
            Assert.True(new LabelMarkShape(VisualSceneGeometry.Flatten(mark, 8), true, 0).Contains(bounds), label.Id);
        }
        Assert.Equal(8, scene.Regions.Count(region => region.Role == "point"));
        Assert.Equal(8, scene.Regions.Count(region => region.Role == "radial-data-label"));
        Assert.Contains(scene.Regions, region => region.Id == "series-0-point-0-label" && region.Label == "1200");
    }

    [Fact]
    public void RoundedSunburstPreservesAuthoredTotalsRemaindersColorsAndFullLabelFacts() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Sunburst, "authored-total").WithSize(800, 440).WithLegend(false);
        UseFixtureFont(chart);
        var rounded = Prepare(chart); var roundedSvg = XDocument.Parse(rounded.ToSvg());
        chart.ConfigureSunburst(options => options.CornerRadius = 0);
        var sharp = Prepare(chart); var sharpSvg = XDocument.Parse(sharp.ToSvg());
        Assert.Equal(SourceFacts(sharpSvg, "sunburst-segment"), SourceFacts(roundedSvg, "sunburst-segment"));
        Assert.Equal(sharp.Regions.Select(region => region.Label), rounded.Regions.Select(region => region.Label));
        Assert.Contains(rounded.Scene.Nodes.OfType<VisualScenePath>(), mark => mark.Role == "sunburst-segment-mark");
        Assert.Single(rounded.Scene.Nodes.OfType<VisualSceneSlice>(), mark => mark.Role == "sunburst-segment-mark");
        Assert.Contains(rounded.Scene.Nodes.OfType<VisualSceneGroup>(), group => group.Role == "sunburst-segment"
            && group.Metadata.TryGetValue("data-cfx-remainder-value", out var remainder) && remainder == "20");
        Assert.NotEqual(sharp.ToPng(), rounded.ToPng());
    }

    internal static Chart Frame() {
        var chart = Chart.Create().WithSize(600, 440).WithHeader(false).WithAxes(false).WithGrid(false).WithLegend(false).WithDataLabels(false);
        UseFixtureFont(chart); return chart;
    }
    internal static void UseFixtureFont(Chart chart) {
        chart.WithFontFamily("Carlito");
        chart.Options.PngFontPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static string[] SourceFacts(XDocument svg, string role) => svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == role)
        .Select(node => string.Join(";", node.Attributes().Where(attribute => attribute.Name.LocalName.StartsWith("data-cfx-", StringComparison.Ordinal))
            .OrderBy(attribute => attribute.Name.LocalName).Select(attribute => attribute.Name.LocalName + "=" + attribute.Value))).ToArray();
}
