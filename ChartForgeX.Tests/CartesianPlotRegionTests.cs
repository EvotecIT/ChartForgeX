using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class CartesianPlotRegionTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ArbitraryPivotKeepsFourPaintedRegionsAndRawBoundsUnderReversal(bool reverseX, bool reverseY) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Observed", new[] { new ChartPoint(1, 1), new ChartPoint(9, 9) });
        chart.Options.XAxis.Reversed = reverseX; chart.Options.YAxis.Reversed = reverseY;
        var colors = new[] { new ChartColor(180, 40, 40), new ChartColor(40, 130, 60), new ChartColor(40, 80, 180), new ChartColor(150, 90, 30) };
        chart.AddPlotRegion(new(colors[0], xMaximum: 4, yMaximum: 6, opacity: 1, label: "Low/low", showLabel: false))
            .AddPlotRegion(new(colors[1], xMinimum: 4, yMaximum: 6, opacity: 1, label: "High/low", showLabel: false))
            .AddPlotRegion(new(colors[2], xMaximum: 4, yMinimum: 6, opacity: 1, label: "Low/high", showLabel: false))
            .AddPlotRegion(new(colors[3], xMinimum: 4, yMinimum: 6, opacity: 1, label: "High/high", showLabel: false));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regions = prepared.Regions.Where(region => region.Role == "plot-region").ToArray(); Assert.Equal(4, regions.Length);
        var left = regions.Min(r => r.Bounds.Left); var top = regions.Min(r => r.Bounds.Top);
        var width = regions.Max(r => r.Bounds.Right) - left; var height = regions.Max(r => r.Bounds.Bottom) - top;
        Assert.Equal(width * .4, regions[0].Bounds.Width, 6); Assert.Equal(height * .6, regions[0].Bounds.Height, 6);
        Assert.Equal(left + (reverseX ? width * .6 : 0), regions[0].Bounds.Left, 6);
        Assert.Equal(top + (reverseY ? 0 : height * .4), regions[0].Bounds.Top, 6);
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        for (var i = 0; i < regions.Length; i++) {
            var bounds = regions[i].Bounds;
            var offset = ((int)(bounds.Top + bounds.Height / 2) * image.Width + (int)(bounds.Left + bounds.Width / 2)) * 4;
            Assert.Equal(colors[i].R, image.Pixels[offset]); Assert.Equal(colors[i].G, image.Pixels[offset + 1]); Assert.Equal(colors[i].B, image.Pixels[offset + 2]);
        }
        var metadata = XDocument.Parse(prepared.ToSvg()).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "plot-region").ToArray();
        Assert.Equal("4", (string?)metadata[0].Attribute("data-cfx-x-maximum")); Assert.Equal("6", (string?)metadata[0].Attribute("data-cfx-y-maximum"));
        Assert.Equal("Low/low", (string?)metadata[0].Attribute("data-cfx-label"));
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
        Capture(prepared.ToSvg(), prepared.ToPng(), "quadrants-" + reverseX + "-" + reverseY);
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); chart.PlotRegions.Clear(); chart.ClearSeries();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void BackgroundBoundsDoNotExpandAutomaticDomainsAndEmptyIntersectionRetainsFacts() {
        var chart = Bare().AddScatter("Observed", new[] { new ChartPoint(1, 2), new ChartPoint(3, 4) });
        var before = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.AddPlotRegion(new(new ChartColor(200, 40, 40), xMinimum: 100, xMaximum: 200, label: "Outside"));
        var after = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(before.Regions.Where(r => r.Role == "point").Select(r => r.Bounds), after.Regions.Where(r => r.Role == "point").Select(r => r.Bounds));
        Assert.Equal(0, Assert.Single(after.Regions, r => r.Role == "plot-region").Bounds.Width);
        Assert.DoesNotContain(after.Scene.Nodes, n => n.Role is "plot-region-fill" or "plot-region-label");
        var group = XDocument.Parse(after.ToSvg()).Descendants().Single(e => (string?)e.Attribute("data-cfx-role") == "plot-region");
        Assert.Equal("100", (string?)group.Attribute("data-cfx-x-minimum")); Assert.Equal("Outside", (string?)group.Attribute("data-cfx-label"));
    }

    [Fact]
    public void OverlappingBackgroundsCompositeAuthoredAlphaAndOpacityInInsertionOrder() {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddPlotRegion(new(new ChartColor(200, 0, 0), opacity: 1))
            .AddPlotRegion(new(new ChartColor(0, 0, 200, 128), xMinimum: 5, opacity: .5));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var bounds = prepared.Regions.Single(r => r.Id == "plot-region-1").Bounds;
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        var offset = ((int)(bounds.Top + bounds.Height / 2) * image.Width + (int)(bounds.Left + bounds.Width / 2)) * 4;
        Assert.InRange(image.Pixels[offset], 148, 151); Assert.Equal(0, image.Pixels[offset + 1]); Assert.InRange(image.Pixels[offset + 2], 49, 52);
        Assert.Equal(new[] { "plot-region-0", "plot-region-1" }, prepared.Regions.Where(r => r.Role == "plot-region").Select(r => r.Id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverlappingBackgroundCaptionsKeepOpaqueReadableInkWithinTheirRegion(bool dark) {
        var chart = Bare().WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddPlotRegion(new(new ChartColor(0, 0, 0), opacity: 1))
            .AddPlotRegion(new(new ChartColor(255, 255, 255, 128), label: "Review", opacity: .14));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plate = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), n => n.Role == "plot-region-label-backplate");
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), n => n.Role == "plot-region-label");
        Assert.Equal(255, plate.Fill!.Value.A);
        var contrast = (Math.Max(Luminance(plate.Fill.Value), Luminance(label.Color)) + .05)
            / (Math.Min(Luminance(plate.Fill.Value), Luminance(label.Color)) + .05);
        Assert.True(contrast >= 4.5, "Caption ink must remain readable independently of underlying overlapping regions.");
        var region = prepared.Regions.Single(r => r.Id == "plot-region-1").Bounds;
        Assert.True(plate.Bounds.Left >= region.Left && plate.Bounds.Right <= region.Right && plate.Bounds.Top >= region.Top && plate.Bounds.Bottom <= region.Bottom);
        var image = RasterImageDecoder.Decode(prepared.ToPng());
        var offset = ((int)(plate.Bounds.Top + plate.Bounds.Height / 2) * image.Width + (int)(plate.Bounds.Left + 2)) * 4;
        Assert.Equal(plate.Fill.Value.R, image.Pixels[offset]); Assert.Equal(plate.Fill.Value.G, image.Pixels[offset + 1]); Assert.Equal(plate.Fill.Value.B, image.Pixels[offset + 2]);
        Assert.Contains("plot-region-label-backplate", prepared.ToSvg());
        Capture(prepared.ToSvg(), prepared.ToPng(), "overlapping-region-caption-" + (dark ? "dark" : "light"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalChartsUsePhysicalValueXAndLinearCategorySlotsDespiteStoredCategoryScale(bool reversed) {
        var chart = Bare().WithXAxisBounds(0, 10).WithYAxisBounds(.5, 4.5)
            .AddHorizontalBar("Observed", new[] { new ChartPoint(1, 2), new ChartPoint(3, 9) })
            .AddPlotRegion(new(new ChartColor(0, 0, 0), opacity: 0))
            .AddPlotRegion(new(new ChartColor(60, 120, 170), xMaximum: 4, yMinimum: -2, yMaximum: 2.5, showLabel: false));
        chart.Options.YAxis.Scale = ChartScaleKind.Logarithmic;
        chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = prepared.Regions.Single(r => r.Id == "plot-region-0").Bounds;
        var region = prepared.Regions.Single(r => r.Id == "plot-region-1").Bounds;
        Assert.Equal(plot.Width * .4, region.Width, 6); Assert.Equal(plot.Height * .5, region.Height, 6);
        Assert.Equal(plot.Left + (reversed ? plot.Width * .6 : 0), region.Left, 6);
        // Named horizontal categories read top-down, so the first category band sits at the top unless reversed.
        Assert.Equal(plot.Top + (reversed ? plot.Height * .5 : 0), region.Top, 6);
        Assert.Contains("data-cfx-y-minimum=\"-2\"", prepared.ToSvg());
        Assert.Equal(2, prepared.Regions.Count(r => r.Role == "point"));
        Capture(prepared.ToSvg(), prepared.ToPng(), "horizontal-region-" + reversed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DatedXAndSecondaryLogarithmicYUseTheSelectedFinalDomain(bool reversed) {
        var start = new DateTime(2026, 3, 1).ToOADate();
        var chart = Bare().WithXAxisBounds(start, start + 10).WithYAxisBounds(0, 10)
            .AddScatter("Primary", new[] { new ChartPoint(start, 1), new ChartPoint(start + 10, 9) })
            .AddScatter("Secondary", new[] { new ChartPoint(start, 1), new ChartPoint(start + 10, 100) });
        chart.Series[1].UseSecondaryValueAxis(); chart.Options.XAxis.Scale = ChartScaleKind.Time;
        chart.Options.SecondaryYAxis.Scale = ChartScaleKind.Logarithmic;
        chart.Options.SecondaryYAxis.Minimum = 1; chart.Options.SecondaryYAxis.Maximum = 100;
        chart.Options.XAxis.Reversed = reversed; chart.Options.SecondaryYAxis.Reversed = reversed;
        chart.AddPlotRegion(new(new ChartColor(0, 0, 0), opacity: 0))
            .AddPlotRegion(new(new ChartColor(50, 150, 90), xMinimum: start + 4, xMaximum: start + 20, yMinimum: 10,
                yAxis: ChartAxisSide.Secondary, showLabel: false));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regions = prepared.Regions.Where(r => r.Role == "plot-region").ToArray(); var plot = regions[0].Bounds; var selected = regions[1].Bounds;
        Assert.Equal(plot.Width * .6, selected.Width, 6); Assert.Equal(plot.Height * .5, selected.Height, 6);
        Assert.Equal(plot.Left + (reversed ? 0 : plot.Width * .4), selected.Left, 6);
        Assert.Equal(plot.Top + (reversed ? plot.Height * .5 : 0), selected.Top, 6);
        Assert.Contains("data-cfx-y-axis=\"secondary\"", prepared.ToSvg());
    }

    [Theory]
    [InlineData(360)]
    [InlineData(800)]
    public void CaptionFitsInsideItsRegionOrReportsOverflowWithOriginalTextRetained(int width) {
        var chart = Bare().WithSize(width, 300).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Observed", new[] { new ChartPoint(2, 2), new ChartPoint(8, 8) })
            .AddPlotRegion(new(new ChartColor(120, 170, 80), xMinimum: 3, xMaximum: 7, yMinimum: 3, yMaximum: 7, label: "Review"))
            .AddPlotRegion(new(new ChartColor(220, 80, 40), xMinimum: 9.9, label: "A caption that cannot fit"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var region = prepared.Regions.Single(r => r.Id == "plot-region-0").Bounds;
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), n => n.Role == "plot-region-label");
        Assert.Equal("Review", Assert.Single(label.Text.Lines).Text);
        Assert.True(label.X >= region.Left && label.X + label.Text.Metrics.Width <= region.Right);
        Assert.True(label.Baseline - label.Text.Ascent >= region.Top && label.Baseline <= region.Bottom);
        Assert.Contains(prepared.Diagnostics, d => d.Code == "cartesian.plot-region-label-overflow");
        Assert.Contains("A caption that cannot fit", prepared.ToSvg());
        Capture(prepared.ToSvg(), prepared.ToPng(), "region-caption-" + width);
    }

    [Fact]
    public void InvalidRegionsAndUnsupportedFamiliesRejectClearly() {
        var color = new ChartColor(1, 2, 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPlotRegion(color, xMinimum: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPlotRegion(color, xMinimum: 2, xMaximum: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPlotRegion(color, yMinimum: 2, yMaximum: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPlotRegion(color, opacity: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPlotRegion(color, yAxis: (ChartAxisSide)99));
        Assert.Throws<ArgumentNullException>(() => Bare().AddPlotRegion(null!));
        var pie = Bare().AddPie("Parts", new[] { new ChartPoint(1, 2) }).AddPlotRegion(new(color));
        Assert.Throws<NotSupportedException>(() => pie.ToSvg());
        var chart = Bare().AddPlotRegion(new(color, yAxis: ChartAxisSide.Secondary));
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        chart.PlotRegions.Clear(); chart.Options.XAxis.Scale = ChartScaleKind.Logarithmic;
        chart.AddPlotRegion(new(color, xMinimum: 0)); Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
    }

    private static Chart Bare() => Chart.Create().WithSize(600, 360).WithHeader(false).WithLegend(false)
        .WithAxes(false).WithGrid(false).WithDataLabels(false).WithTheme(VisualTheme.Graphite());
    private static double Luminance(ChartColor color) {
        double Channel(byte value) { var n = value / 255d; return n <= .04045 ? n / 12.92 : Math.Pow((n + .055) / 1.055, 2.4); }
        return .2126 * Channel(color.R) + .7152 * Channel(color.G) + .0722 * Channel(color.B);
    }
    private static void Capture(string svg, byte[] png, string name) {
        var path = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(path, name + ".png"), png);
    }
}
