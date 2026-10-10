using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects the area legend's source pattern when optional marker glyphs are hidden.</summary>
public sealed class AreaMarkerLegendPatternTests {
    [Theory]
    [InlineData(ChartSeriesKind.Area, "area-pattern")]
    [InlineData(ChartSeriesKind.StepArea, "area-pattern")]
    [InlineData(ChartSeriesKind.StackedArea, "area-pattern")]
    [InlineData(ChartSeriesKind.RangeArea, "range-area-pattern")]
    [InlineData(ChartSeriesKind.RangeBand, "range-band-pattern")]
    [InlineData(ChartSeriesKind.Radar, "radar-pattern")]
    public void HiddenGlyphsKeepTheAreaPatternInBothLegendExporters(ChartSeriesKind kind, string plotRole) {
        foreach (var zeroRadius in new[] { false, true }) {
            var chart = Family(kind);
            var series = chart.Series[0];
            series.FillPattern = ChartFillPattern.Crosshatch;
            if (zeroRadius) series.Markers.Radius = 0;
            else series.Markers.Enabled = false;
            var prepared = Prepare(chart);
            Assert.Contains(prepared.Scene.Nodes, node => node.Role == plotRole);
            Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneMark>(), node => node.Role is "marker" or "range-marker" or "radar-point");
            var swatch = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "legend-area").Bounds;
            Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "legend-area-pattern");
            var svg = XDocument.Parse(prepared.ToSvg());
            var hatches = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "legend-area-pattern").ToArray();
            Assert.NotEmpty(hatches);
            Assert.All(hatches, hatch => Assert.Equal("line", hatch.Name.LocalName));

            var patternedInk = SwatchPixels(RasterImageDecoder.Decode(prepared.ToPng()), swatch);
            series.FillPattern = ChartFillPattern.None;
            var plainInk = SwatchPixels(RasterImageDecoder.Decode(Prepare(chart).ToPng()), swatch);
            Assert.False(patternedInk.SequenceEqual(plainInk), "The native legend must retain visible source hatching when marker glyphs are hidden.");
        }
    }

    private static Chart Family(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(320, 220).WithHeader(false).WithLegend().WithDataLabels(false).WithYAxisBounds(0, 100);
        chart.Options.ShowAxes = false; chart.Options.ShowGrid = false;
        var points = new[] { new ChartPoint(1, 20), new ChartPoint(3, 70), new ChartPoint(5, 40) };
        var ranges = new[] { new ChartRangeBand(1, 10, 30), new ChartRangeBand(3, 20, 60), new ChartRangeBand(5, 30, 80) };
        return kind switch {
            ChartSeriesKind.Area => chart.AddArea("Samples", points),
            ChartSeriesKind.StepArea => chart.AddStepArea("Samples", points),
            ChartSeriesKind.StackedArea => chart.AddStackedArea("Samples", points),
            ChartSeriesKind.RangeArea => chart.AddRangeArea("Samples", ranges, smooth: false),
            ChartSeriesKind.RangeBand => chart.AddRangeBand("Samples", ranges),
            ChartSeriesKind.Radar => chart.AddRadarArea("Samples", points),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private static PreparedVisual Prepare(Chart chart) {
        var request = VisualExportRequest.ForChart(chart).Context;
        return chart.Prepare(new VisualRenderContext(request.Layout, request.Theme, request.ThemeMode,
            new VisualFrame(showLegend: true, transparentBackground: true), request.Font));
    }

    private static byte[] SwatchPixels(RgbaImage image, ChartRect bounds) {
        var pixels = new List<byte>();
        for (var y = (int)Math.Ceiling(bounds.Top); y < bounds.Bottom; y++)
            for (var x = (int)Math.Ceiling(bounds.Left); x < bounds.Right; x++)
                pixels.AddRange(image.Pixels.Skip((y * image.Width + x) * 4).Take(4));
        return pixels.ToArray();
    }
}
