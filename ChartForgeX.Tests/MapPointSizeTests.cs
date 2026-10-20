using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MapPointSizeTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AuthoredWeightDomainClampsOnlyRadiusAndRetainsIndependentRawColor(bool reversed, bool fixedRadius) {
        var chart = Chart.Create().WithSize(640, 460).WithLegend(true).WithMapViewport(ChartMapViewport.Europe())
            .AddDottedMap("Traffic", new[] {
                new ChartMapPoint("Zero", 0, 50, 0).WithColorValue(-20), new ChartMapPoint("Quarter", 5, 52, 25).WithColorValue(-10),
                new ChartMapPoint("Full", 10, 54, 100).WithColorValue(30), new ChartMapPoint("Beyond", 15, 56, 200).WithColorValue(80)
            });
        var series = chart.Series[0].WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#DBEAFE"), ChartColor.FromHex("#1D4ED8")).WithValueRange(-20, 40));
        series.ConfigureMapPointSize(options => { options.WithSizeDomain(0, 100); options.MinimumRadius = 2; options.MaximumRadius = 12; options.Reversed = reversed; });
        if (fixedRadius) series.WithMarkerRadius(7);
        var prepared = Prepare(chart);
        var radii = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").Select(node => node.Rx).ToArray();
        Assert.Equal(fixedRadius ? new[] { 7d, 7, 7, 7 } : reversed ? new[] { 12d, 9.5, 2, 2 } : new[] { 2d, 4.5, 12, 12 }, radii);
        var points = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "dotted-map-point-source").ToArray();
        Assert.Equal(new[] { "0", "25", "100", "200" }, points.Select(node => node.Metadata["data-cfx-value"]));
        Assert.Equal(new[] { "-20", "-10", "30", "80" }, points.Select(node => node.Metadata["data-cfx-color-value"]));
        Assert.All(points, node => Assert.Equal(fixedRadius ? "fixed-radius" : "linear-radius-range", node.Metadata["data-cfx-size-mapping"]));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.ConfigureMapPointSize(options => options.WithSizeDomain(0, 200));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public void OptionalMapSizeKeyUsesTheSameMeasuredReferenceCirclesAsMarks(int width, VisualThemeMode mode) {
        var chart = GeographicMapCompositionTests.Fixture(ChartMapGeoJsonProjection.Equirectangular, width, mode).WithSize(width, 600);
        var sites = chart.Series[1];
        var calls = new Dictionary<double, int>();
        sites.ConfigureMapPointSize(options => {
            options.WithSizeDomain(0, 100); options.MinimumRadius = 3; options.MaximumRadius = 11;
            options.SizeDimension.Name = "Traffic"; options.SizeDimension.ShowLegend = true;
            options.SizeDimension.ValueFormat = ChartValueFormat.Custom(value => {
                calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
                return value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " trips";
            });
        });
        var prepared = Prepare(chart);
        var samples = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "map-size-key-1-sample").ToArray();
        Assert.Equal(new[] { "0", "100" }, samples.Select(node => node.Metadata["data-cfx-size-value"]));
        Assert.Equal(new[] { "3", "11" }, samples.Select(node => node.Metadata["data-cfx-radius"]));
        Assert.Equal(new[] { 3d, 11 }, prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "dotted-map-point").Select(node => node.Rx));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.Equal(new[] { "West gateway 0 trips", "North gateway 100 trips" }, prepared.Scene.Nodes.OfType<VisualSceneText>()
            .Where(node => node.Role == "dotted-map-data-label").Select(node => string.Join(" ", node.Text.Lines.Select(line => line.Text))));
        Assert.Contains(prepared.Regions, region => region.Role == "map-size-key-1" && region.Label!.Contains("Traffic"));
        Capture(prepared, "map-size-key-" + width + "-" + mode);
        var original = prepared.ToSvg();
        Assert.Throws<InvalidOperationException>(() => sites.ConfigureMapPointSize(options => { options.MinimumRadius = 9; throw new InvalidOperationException(); }));
        Assert.Equal(original, Prepare(chart).ToSvg());
        chart.WithLegend(false);
        Assert.DoesNotContain(Prepare(chart).Scene.Nodes, node => node.Role == "map-size-key-1");
    }

    [Fact]
    public void ConfiguredSizeOptionsKeepEmptyAndUnweightedPointsDistinctAndRejectWrongFamilies() {
        var series = ChartSeries.CreateDottedMap(new ChartSeriesId("sites"), "Sites", new[] { new ChartMapPoint("Unweighted", 1, 2) });
        series.ConfigureMapPointSize(options => options.SizeDimension.ShowLegend = true);
        var chart = Chart.Create().AddSeries(series);
        Assert.DoesNotContain(Prepare(chart).Scene.Nodes, node => node.Role == "map-size-key-0");
        series.ReplaceMapPoints(Array.Empty<ChartMapPoint>());
        Assert.Contains(Prepare(chart).Scene.Diagnostics, diagnostic => diagnostic.Code == "map.no-data");
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2) }).Series[0];
        Assert.Throws<InvalidOperationException>(() => line.ConfigureMapPointSize(options => options.MinimumRadius = 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartMapPointSizeOptions().WithSizeDomain(10, 10));
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static void Capture(PreparedVisual prepared, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), prepared.ToPng());
    }
}
