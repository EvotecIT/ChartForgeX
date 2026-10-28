using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Area-normalized authored coordinates retain physical bounds, source facts and native paint independently of scale.</summary>
public sealed class CustomMarkerBoundsTests {
    [Theory]
    [InlineData(1e-5, false)] [InlineData(1e-150, false)] [InlineData(1e-200, false)]
    [InlineData(1e-5, true)] [InlineData(1e-150, true)] [InlineData(1e-200, true)]
    public void ScaledAreaCurvesRetainActualSemanticBoundsFrameAndPixels(double scale, bool labels) {
        var expected = Prepare(Bubbles(Lens(1), labels, automatic: false));
        var actual = Prepare(Bubbles(Lens(scale), labels, automatic: false));
        ScatterBubbleColorRenderingTests.Capture(actual, "custom-marker-bounds-" + scale.ToString("0e0", System.Globalization.CultureInfo.InvariantCulture) + "-labels-" + labels);
        Assert.Equal(expected.Scene.Size, actual.Scene.Size);
        Assert.Equal(expected.Regions.Select(region => (region.Id, region.Role)), actual.Regions.Select(region => (region.Id, region.Role)));
        for (int index = 0; index < expected.Regions.Count; index++) SameBounds(expected.Regions[index].Bounds, actual.Regions[index].Bounds);
        Assert.All(actual.Regions, region => { Assert.InRange(region.Bounds.Width, 0, 400); Assert.InRange(region.Bounds.Height, 0, 300); });
        var before = RasterImageDecoder.Decode(expected.ToPng(new VisualRenderOptions(supersampling: 2)));
        var after = RasterImageDecoder.Decode(actual.ToPng(new VisualRenderOptions(supersampling: 2)));
        Assert.Equal(before.Width, after.Width); Assert.Equal(before.Height, after.Height); Assert.Equal(before.Pixels, after.Pixels);
        var facts = Assert.Single(actual.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point");
        Assert.Equal("100", facts.Metadata["data-cfx-size"]);
        if (labels) Assert.Contains(actual.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
    }

    [Theory]
    [InlineData(360, false)] [InlineData(800, true)]
    public void AutomaticRangesFitPhysicalCustomGeometryAcrossCompactWideAndBothThemes(int width, bool dark) {
        var original = Bubbles(Lens(1), false, automatic: true).WithSize(width, 300).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var scaled = Bubbles(Lens(1e-200), false, automatic: true).WithSize(width, 300).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var expected = Prepare(original); var actual = Prepare(scaled);
        ScatterBubbleColorRenderingTests.Capture(actual, "custom-marker-automatic-bounds-" + width + "-" + (dark ? "dark" : "light"));
        var first = expected.Regions.Single(region => region.Role == "point"); var second = actual.Regions.Single(region => region.Role == "point");
        SameBounds(first.Bounds, second.Bounds); Assert.InRange(second.Bounds.Width, 20, 100); Assert.InRange(second.Bounds.Height, 20, 100);
        Assert.Equal(RasterImageDecoder.Decode(expected.ToPng()).Pixels, RasterImageDecoder.Decode(actual.ToPng()).Pixels);
    }

    [Fact]
    public void AsymmetricCubicExtremaAndEndpointOverridesResolveThroughOneGeometryOwner() {
        var chart = Chart.Create().AddDumbbell("Endpoints", new[] { new ChartDumbbell(1, 20, 40) });
        var right = ChartMarkerPath.Start(.2, -.25).LineTo(.6, -.25).LineTo(.6, .5).LineTo(.2, .5);
        chart.Series[0].ConfigureMarkers(options => { options.CustomPath = right; options.Radius = 20; options.StrokeWidth = 2; });
        chart.Series[0].ConfigureDumbbell(options => { options.StartMarker.Radius = 10; options.EndMarker.StrokeWidth = 4; });
        var series = chart.Series[0]; var start = series.ResolveDumbbellMarker(true); var end = series.ResolveDumbbellMarker(false);
        SameBounds(new ChartRect(101, 96.5, 6, 9.5), VisualMarkerScene.Bounds(series, 100, 100, 10, start));
        SameBounds(new ChartRect(102, 93, 12, 19), VisualMarkerScene.Bounds(series, 100, 100, 20, end));
        Assert.Equal(7, VisualMarkerScene.Extent(series, 10, start)); Assert.Equal(14, VisualMarkerScene.Extent(series, 20, end));
        // Cubic controls at +/-1 overshoot the real extrema at +/-0.75; reuse the existing exact path bound owner.
        var lens = Lens(1); SameBounds(new ChartRect(-.75, -1, 1.5, 2), lens.Bounds);
    }

    private static Chart Bubbles(ChartMarkerPath shape, bool labels, bool automatic) {
        var chart = Chart.Create().WithSize(400, 300).WithHeader(false).WithLegend(false).WithDataLabels(labels).WithAxes(false).WithGrid(false)
            .WithDataLabelPlacement(ChartDataLabelPlacement.Above).AddBubble("Area", new[] { new ChartBubble(5, 5, 100) })
            .ConfigureBubble(options => { options.WithSizeDomain(0, 100); options.MinimumRadius = 0; options.MaximumRadius = 20; options.SizeEncoding = ChartBubbleSizeEncoding.Area; });
        if (!automatic) chart.WithXAxisBounds(0, 10).WithYAxisBounds(0, 10);
        chart.Series[0].WithPointLabel(0, "Area").ConfigureMarkers(options => { options.CustomPath = shape; options.Fill = ChartColor.Black; options.StrokeWidth = 0; });
        chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false; chart.Options.TransparentBackground = true; return chart;
    }
    private static ChartMarkerPath Lens(double scale) => ChartMarkerPath.Start(0, -scale).CubicTo(scale, -scale, scale, scale, 0, scale)
        .CubicTo(-scale, scale, -scale, -scale, 0, -scale);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static void SameBounds(ChartRect expected, ChartRect actual) {
        Assert.Equal(expected.X, actual.X, 7); Assert.Equal(expected.Y, actual.Y, 7);
        Assert.Equal(expected.Width, actual.Width, 7); Assert.Equal(expected.Height, actual.Height, 7);
    }
}
