using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class LabelPlacementGalleryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DenseGalleryPanelsHaveNoUnintendedTextCollisionsInEitherRenderer(bool dark) {
        foreach (var (name, chart) in LabelPlacementExamples.Charts(dark)) {
            var font = new FontSpec { Family = chart.Options.Theme.FontFamily };
            var svg = chart.ToSvg();
            var pngText = new SvgChartRenderer().RenderLabelScene(chart).ToSvg();
            AssertClear(name + " SVG", svg, font); AssertClear(name + " PNG", pngText, font);
            Assert.Equal(Placements(svg), Placements(pngText));
            var raster = new Raster.PngChartRenderer().RenderImage(chart);
            Assert.Equal(chart.Options.Size.Width * chart.Options.PngOutputScale, raster.Width);
        }
        var grid = LabelPlacementExamples.Scorecard(dark);
        AssertClear("scorecards", grid.ToSvg(), FontSpec.SystemSans());
        Assert.NotEmpty(grid.ToPng());
        var topology = LabelPlacementExamples.Topology(dark);
        var options = new TopologyRenderOptions { IncludeLegend = false };
        AssertClear("topology", topology.ToSvg(options), new FontSpec { Family = topology.Theme!.FontFamily });
        Assert.True(ChartLabelScene.Inspect(topology.ToSvg(options), new FontSpec { Family = topology.Theme.FontFamily }).Contained >= topology.Nodes.Count * 2,
            "The checker must include node bodies and deliberately contained node captions.");
        Assert.NotEmpty(topology.ToPng(options));
    }

    [Fact]
    public void NativePngBulletMarksUseTheGeometryCheckedBySharedLabels() {
        var chart = LabelPlacementExamples.Charts(false).Single(panel => panel.Name == "bullet").Chart.WithPngOutputScale(2);
        var document = XDocument.Parse(chart.ToSvg());
        var image = new Raster.PngChartRenderer().RenderImage(chart);
        foreach (var mark in document.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "bullet-value")) {
            double Number(string name) => double.Parse(mark.Attribute(name)!.Value, System.Globalization.CultureInfo.InvariantCulture);
            var x = (int)Math.Round((Number("x") + Number("width") / 2) * 2);
            var y = (int)Math.Round((Number("y") + Number("height") / 2) * 2);
            var offset = (y * image.Width + x) * 4;
            var pixel = new Primitives.ChartColor(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
            var color = chart.Options.Theme.Palette[int.Parse(mark.Attribute("data-cfx-series")!.Value)];
            Assert.True(Math.Abs(pixel.R - color.R) + Math.Abs(pixel.G - color.G) + Math.Abs(pixel.B - color.B) < 100,
                "The native mark must occupy the measured SVG row instead of using a separate label or legend reserve.");
        }
    }

    [Fact]
    public void FunnelLabelsAndRatiosRemainInTheirOwnMeasuredStage() {
        var chart = LabelPlacementExamples.Charts(false).Single(panel => panel.Name == "funnel").Chart;
        var document = XDocument.Parse(chart.ToSvg());
        var labels = document.Descendants().Where(e => (string?)e.Attribute("data-cfx-role") is "funnel-label" or "funnel-value" or "funnel-retention" or "funnel-dropoff").ToArray();
        Assert.Equal(18, labels.Length);
        foreach (var label in labels) {
            Assert.Equal("placed", (string?)label.Attribute("data-cfx-label-status"));
            var mark = Assert.Single(document.Descendants(), e => (string?)e.Attribute("data-cfx-role") == "funnel-segment"
                && (string?)e.Attribute("data-cfx-point") == (string?)label.Attribute("data-cfx-point"));
            Assert.Equal((string?)mark.Attribute("data-cfx-mark-key"), (string?)label.Attribute("data-cfx-label-mark"));
            Assert.Null(label.Attribute("transform"));
        }
        Assert.Equal(10, ChartLabelScene.Inspect(document.ToString(), FontSpec.SystemSans()).Contained);
    }

    [Fact]
    public void DroppedLabelsRetainTheirValueAndAssociatedMarkAccessibility() {
        var chart = Chart.Create().WithSize(260, 180).WithDataLabels().WithLegend(false)
            .AddScatter("Measurements", Enumerable.Range(0, 80).Select(i => new Primitives.ChartPoint(i % 5, i % 7)));
        var document = XDocument.Parse(chart.ToSvg());
        var dropped = document.Descendants().Where(e => (string?)e.Attribute("data-cfx-label-status") == "dropped").ToArray();
        Assert.NotEmpty(dropped);
        foreach (var label in dropped) {
            Assert.False(string.IsNullOrWhiteSpace((string?)label.Attribute("data-cfx-label-original")));
            if (label.Attribute("data-cfx-label-mark") is not { } key) continue;
            var mark = Assert.Single(document.Descendants(), e => (string?)e.Attribute("data-cfx-mark-key") == key.Value);
            Assert.Equal((string?)label.Attribute("data-cfx-series"), (string?)mark.Attribute("data-cfx-series"));
            Assert.Equal((string?)label.Attribute("data-cfx-point"), (string?)mark.Attribute("data-cfx-point"));
            Assert.Contains(label.Attribute("data-cfx-label-original")!.Value, mark.Attribute("aria-label")!.Value);
            Assert.NotNull(mark.Attribute("data-cfx-label-text"));
        }
    }

    private static void AssertClear(string name, string svg, FontSpec font) {
        var report = ChartLabelScene.Inspect(svg, font);
        Assert.True(report.Visible > 2, name + " must retain useful labels.");
        Assert.True(report.LabelLabel == 0 && report.LabelMark == 0, name + ": " + report.LabelLabel + " label pairs; " + report.LabelMark + " mark pairs. " + report.Details);
    }
    private static string[] Placements(string svg) => XDocument.Parse(svg).Descendants()
        .Where(e => e.Attribute("data-cfx-label-status") != null)
        .Select(e => string.Join("|", new[] { "data-cfx-label-original", "data-cfx-label-status", "data-cfx-label-x", "data-cfx-label-y", "data-cfx-label-width", "data-cfx-label-height" }.Select(a => (string?)e.Attribute(a)))).ToArray();
}
