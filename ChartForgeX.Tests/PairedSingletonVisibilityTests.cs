using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Isolated paired observations remain visible without inventing an interpolated interval.</summary>
public sealed class PairedSingletonVisibilityTests {
    [Theory]
    [InlineData(ChartSeriesKind.RangeBand, 390, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.RangeArea, 390, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.DifferenceArea, 390, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.RangeBand, 760, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.RangeArea, 760, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.DifferenceArea, 760, VisualThemeMode.Dark)]
    public void DefaultSingletonEndpointsUseVisibleContainedMarkersAndKeepTheirExactFacts(ChartSeriesKind kind, int width, VisualThemeMode mode) {
        var chart = Create(kind, new[] { (0d, 0d, 7d, false) });
        var prepared = Prepare(chart, width, mode);
        var markers = Markers(prepared).ToArray();
        Assert.Equal(2, markers.Length);
        Assert.All(markers, marker => {
            Assert.True(marker.Rx >= 3 && marker.Ry >= 3);
            Assert.InRange(marker.Cx - marker.Rx, 0, width); Assert.InRange(marker.Cx + marker.Rx, 0, width);
            Assert.InRange(marker.Cy - marker.Ry, 0, 320); Assert.InRange(marker.Cy + marker.Ry, 0, 320);
        });
        Assert.Equal(markers[0].Cx, markers[1].Cx, 8);
        Assert.NotEqual(markers[0].Cy, markers[1].Cy);
        var point = Assert.Single(XDocument.Parse(prepared.ToSvg()).Descendants(), element => (string?)element.Attribute("data-cfx-role") == "point");
        Assert.Equal("0", (string?)point.Attribute("data-cfx-x"));
        Assert.Equal("0", (string?)point.Attribute(kind == ChartSeriesKind.DifferenceArea ? "data-cfx-first" : "data-cfx-lower"));
        Assert.Equal("7", (string?)point.Attribute(kind == ChartSeriesKind.DifferenceArea ? "data-cfx-second" : "data-cfx-upper"));
        Assert.Equal("single", (string?)point.Attribute("data-cfx-observation-id"));
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var marker in markers) {
            var pixel = ((int)Math.Round(marker.Cy) * image.Width + (int)Math.Round(marker.Cx)) * 4;
            Assert.NotEqual(ChartColor.FromRgba(image.Pixels[0], image.Pixels[1], image.Pixels[2], image.Pixels[3]),
                ChartColor.FromRgba(image.Pixels[pixel], image.Pixels[pixel + 1], image.Pixels[pixel + 2], image.Pixels[pixel + 3]));
        }
        var svg = prepared.ToSvg(); var png = prepared.ToPng(new VisualRenderOptions(supersampling: 1));
        chart.Series[0].Markers.Enabled = false;
        Assert.Empty(Markers(Prepare(chart, width, mode)));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng(new VisualRenderOptions(supersampling: 1)));
        ScatterBubbleColorRenderingTests.Capture(prepared, $"paired-singleton-{kind}-{width}-{mode}");
    }

    [Theory]
    [InlineData(ChartSeriesKind.RangeBand)]
    [InlineData(ChartSeriesKind.RangeArea)]
    [InlineData(ChartSeriesKind.DifferenceArea)]
    public void OnlyDisconnectedSingleObservationRunsAcquireAutomaticEndpointMarkers(ChartSeriesKind kind) {
        var chart = Create(kind, new[] { (0d, 0d, 7d, false), (2d, 1d, 8d, true), (3d, 2d, 9d, false), (5d, 0d, 0d, true) });
        var prepared = Prepare(chart, 390, VisualThemeMode.Light);
        Assert.Equal(4, Markers(prepared).Count());
        var document = XDocument.Parse(prepared.ToSvg());
        var points = document.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(4, points.Length);
        Assert.Equal(new[] { 2, 0, 0, 2 }, points.Select(point => point.Descendants().Count(element =>
            (string?)element.Attribute("data-cfx-role") is "range-marker" or "difference-marker")));
        Assert.Equal(4, chart.Series[0].SourcePointCount);
        foreach (var role in kind == ChartSeriesKind.DifferenceArea ? new[] { "difference-first", "difference-second" } : new[] { "range-upper", "range-lower" }) {
            Assert.Equal(3, prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == role)
                .Sum(path => path.Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo)));
        }
        chart.Series[0].Markers.Enabled = true;
        Assert.Equal(8, Markers(Prepare(chart, 390, VisualThemeMode.Light)).Count());
        ScatterBubbleColorRenderingTests.Capture(prepared, "paired-isolated-runs-" + kind);
    }

    private static IEnumerable<VisualSceneEllipse> Markers(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>()
        .Where(node => node.Role is "range-marker" or "difference-marker");
    private static PreparedVisual Prepare(Chart chart, int width, VisualThemeMode mode) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(width, 320)), themeMode: mode, frame: new VisualFrame(chart.Title)));
    private static Chart Create(ChartSeriesKind kind, (double X, double First, double Second, bool Break)[] values) {
        var chart = Chart.Create().WithTitle("One observed pair");
        var bands = values.Select(value => new ChartRangeBand(value.X, value.First, value.Second, value.Break));
        if (kind == ChartSeriesKind.DifferenceArea) chart.AddDifferenceArea("Comparison", values.Select(value => new ChartDifferencePoint(value.X, value.First, value.Second, value.Break)));
        else if (kind == ChartSeriesKind.RangeArea) chart.AddRangeArea("Range", bands);
        else chart.AddRangeBand("Range", bands);
        chart.Series[0].WithObservationId(0, new ChartObservationId("single"));
        return chart;
    }
}
