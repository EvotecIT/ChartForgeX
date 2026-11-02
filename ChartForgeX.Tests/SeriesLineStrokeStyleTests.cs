using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class SeriesLineStrokeStyleTests {
    private static readonly ChartColor Stroke = ChartColor.FromArgb(128, 220, 20, 80);
    private static readonly ChartColor Fill = ChartColor.FromHex("#16A34A");

    [Theory]
    [InlineData(ChartSeriesKind.Line, "line")]
    [InlineData(ChartSeriesKind.StepLine, "line")]
    [InlineData(ChartSeriesKind.Area, "line")]
    [InlineData(ChartSeriesKind.StepArea, "line")]
    [InlineData(ChartSeriesKind.StackedArea, "line")]
    [InlineData(ChartSeriesKind.RangeBand, "range-upper,range-lower")]
    [InlineData(ChartSeriesKind.RangeArea, "range-upper,range-lower,range-midline")]
    [InlineData(ChartSeriesKind.DifferenceArea, "difference-first,difference-second")]
    [InlineData(ChartSeriesKind.Radar, "radar-outline")]
    [InlineData(ChartSeriesKind.Polar, "polar-line")]
    [InlineData(ChartSeriesKind.Slope, "slope-line")]
    [InlineData(ChartSeriesKind.TrendLine, "trend-line")]
    [InlineData(ChartSeriesKind.Dumbbell, "dumbbell-connector")]
    [InlineData(ChartSeriesKind.Lollipop, "lollipop-stem")]
    [InlineData(ChartSeriesKind.ErrorBar, "error-range,error-cap,error-range-x,error-cap-x")]
    [InlineData(ChartSeriesKind.BoxPlot, "boxplot-whisker,boxplot-cap,boxplot-median,boxplot-body")]
    public void SeriesStrokeReachesEveryFamilyWithoutRecoloringFills(ChartSeriesKind kind, string roles) {
        var chart = Create(kind);
        var before = Prepare(chart);
        var series = chart.Series.Single();
        var dash = new[] { 7d, 13d };
        Assert.Same(series, series.WithStrokeStyle(new ChartStrokeStyle(Stroke, 4, .5, dash)));
        dash[0] = 99;
        var prepared = Prepare(chart);
        var svg = XDocument.Parse(prepared.ToSvg());
        var expected = Stroke.WithAlpha(64);
        foreach (var role in roles.Split(',')) {
            var marks = prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(mark => mark.Role == role).ToArray();
            Assert.NotEmpty(marks);
            Assert.All(marks, mark => {
                Assert.Equal(expected, mark.Stroke);
                Assert.Equal(4, mark.StrokeWidth);
                Assert.Equal(new[] { 7d, 13d }, mark is VisualSceneLine line ? line.Dash : Assert.IsType<VisualScenePath>(mark).Dash);
            });
            var elements = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).ToArray();
            Assert.NotEmpty(elements);
            Assert.All(elements, element => {
                Assert.Equal("4", (string?)element.Attribute("stroke-width"));
                Assert.Equal("7 13", (string?)element.Attribute("stroke-dasharray"));
                Assert.Equal(expected.ToCss(), (string?)element.Attribute("stroke"));
            });
        }
        Assert.Equal(before.Scene.Nodes.OfType<VisualSceneMark>().Where(mark => mark.Fill.HasValue).Select(mark => (mark.Role, mark.Fill)),
            prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(mark => mark.Fill.HasValue).Select(mark => (mark.Role, mark.Fill)));
        var dashed = RasterImageDecoder.Decode(prepared.ToPng());
        series.WithStrokeStyle(new ChartStrokeStyle(Stroke, 4, .5, Array.Empty<double>()));
        var solid = RasterImageDecoder.Decode(Prepare(chart).ToPng());
        Assert.False(dashed.Pixels.SequenceEqual(solid.Pixels), "Native PNG must paint the authored dash/gap pattern.");
        Assert.Equal(new[] { 7d, 13d }, prepared.Scene.Nodes.OfType<VisualScenePath>().FirstOrDefault(mark => mark.Role == roles.Split(',')[0])?.Dash
            ?? prepared.Scene.Nodes.OfType<VisualSceneLine>().First(mark => mark.Role == roles.Split(',')[0]).Dash);
    }

    [Fact]
    public void StyleWidthWinsRegardlessOfSetterOrderAndClearingRestoresExplicitLegacyWidth() {
        var chart = Create(ChartSeriesKind.Line);
        var series = chart.Series[0];
        series.WithStrokeWidth(9).WithStrokeStyle(new ChartStrokeStyle(width: 4));
        Assert.Equal(4, Foreground(Prepare(chart), "line").StrokeWidth);
        series.WithStrokeWidth(12);
        Assert.Equal(4, Foreground(Prepare(chart), "line").StrokeWidth);
        series.WithStrokeStyle(new ChartStrokeStyle(dash: new[] { 7d, 13d }));
        Assert.Equal(12, Foreground(Prepare(chart), "line").StrokeWidth);
        series.WithStrokeStyle(null);
        Assert.Equal(12, Foreground(Prepare(chart), "line").StrokeWidth);
        Assert.Null(series.StrokeStyle);
    }

    [Fact]
    public void StaticReflowRetainsCapturedStrokeAfterAuthoredStyleChanges() {
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, 60), new ChartPoint(3, 40) };
        var series = ChartSeries.CreateLine(new ChartSeriesId("signal"), "Signal", points, Fill)
            .WithStrokeWidth(9).WithStrokeStyle(new ChartStrokeStyle(Stroke, 4, .5, new[] { 7d, 13d }));
        var chart = Chart.Create().WithSize(420, 300).WithHeader(false).WithLegend(false).WithDataLabels(false).AddSeries(series)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("other"), "Other", points, Fill));
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.PrepareVisibility(context, new ChartVisibilityView(new[] { ChartVisibilityTarget.Series(new ChartSeriesId("other")) }, layout: ChartVisibilityLayout.Reflow));
        var line = Assert.IsType<VisualScenePath>(Foreground(prepared, "line"));
        Assert.Equal(Stroke.WithAlpha(64), line.Stroke);
        Assert.Equal(4, line.StrokeWidth);
        Assert.Equal(new[] { 7d, 13d }, line.Dash);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.WithStrokeStyle(new ChartStrokeStyle(width: 0));
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        Assert.NotEqual(png, chart.PrepareVisibility(context,
            new ChartVisibilityView(new[] { ChartVisibilityTarget.Series(new ChartSeriesId("other")) }, layout: ChartVisibilityLayout.Reflow)).ToPng());
    }

    [Fact]
    public void ZeroWidthHidesAllLineLayersAndFamilyOutlineWinsOverSeriesDefaults() {
        var chart = Create(ChartSeriesKind.Radar).WithLineVisualStyle(ChartLineVisualStyle.Luminous());
        var series = chart.Series[0];
        series.WithStrokeStyle(new ChartStrokeStyle(Stroke, 0));
        var hidden = Prepare(chart);
        Assert.DoesNotContain(hidden.Scene.Nodes, node => node.Role?.StartsWith("radar-outline", StringComparison.Ordinal) == true);
        Assert.Contains(hidden.Scene.Nodes, node => node.Role == "radar-area");
        series.ConfigureRadar(options => options.OutlineStroke = new ChartStrokeStyle(Fill, 6, dash: Array.Empty<double>()));
        var outline = Assert.IsType<VisualScenePath>(Foreground(Prepare(chart), "radar-outline"));
        Assert.Equal(Fill, outline.Stroke);
        Assert.Equal(6, outline.StrokeWidth);
        Assert.Empty(outline.Dash!);
    }

    [Fact]
    public void DecodedNativePngAndSvgRasterAgreeOnTranslucentDashCoverageAndGaps() {
        var chart = Chart.Create().WithSize(420, 90).WithSparkline().WithYAxisBounds(0, 100).WithLineVisualStyle(ChartLineVisualStyle.Plain())
            .AddLine("Signal", new[] { new ChartPoint(0, 50), new ChartPoint(100, 50) }, Fill);
        var series = chart.Series[0].WithMarkerRadius(0).WithStrokeStyle(new ChartStrokeStyle(Stroke, 4, .5, new[] { 9d, 15d }));
        var prepared = Prepare(chart);
        var path = Assert.IsType<VisualScenePath>(Foreground(prepared, "line"));
        var start = path.Commands.First(); var end = path.Commands.Last();
        var native = RasterImageDecoder.Decode(prepared.ToPng());
        var svg = SvgRasterizer.ToImage(prepared.ToSvg());
        series.WithStrokeStyle(new ChartStrokeStyle(width: 0));
        var hidden = Prepare(chart);
        var nativeBackground = RasterImageDecoder.Decode(hidden.ToPng());
        var svgBackground = SvgRasterizer.ToImage(hidden.ToSvg());
        var nativeProfiles = new List<double>(); var svgProfiles = new List<double>();
        for (var x = (int)Math.Ceiling(start.X) + 3; x < (int)Math.Floor(end.X) - 3; x++) {
            nativeProfiles.Add(Coverage(native, nativeBackground, x, start.Y));
            svgProfiles.Add(Coverage(svg, svgBackground, x, start.Y));
        }
        // Explicit Y bounds place the 4px line at integer Y=45, clear of the plot clip.
        // Its full cross-section is four complete output pixels at alpha round(128 * .5) = 64.
        const double expectedCoverage = 4 * 64 / 255d;
        const double alphaQuantum = 1 / 255d;
        Assert.True(nativeProfiles.Count(value => Math.Abs(value - expectedCoverage) < alphaQuantum) > 50, "PNG must contain full translucent dashes.");
        Assert.True(nativeProfiles.Count(value => value < alphaQuantum) > 50, "PNG must contain unpainted gaps.");
        Assert.Equal(expectedCoverage, nativeProfiles.Max(), 12);
        Assert.Equal(expectedCoverage, svgProfiles.Max(), 12);
        // Each partially covered edge row can lose less than one alpha byte during downsampling.
        Assert.InRange(nativeProfiles.Zip(svgProfiles, (a, b) => Math.Abs(a - b)).Average(), 0, 2 * alphaQuantum);
    }

    private static double Coverage(RgbaImage image, RgbaImage background, int x, double centerY) {
        var result = 0d;
        for (var y = Math.Max(0, (int)centerY - 7); y <= Math.Min(image.Height - 1, (int)centerY + 7); y++) {
            var offset = (y * image.Width + x) * 4 + 1;
            // Sparkline exports have a transparent background. Compare both straight-alpha images over white.
            var green = OverWhite(image.Pixels[offset], image.Pixels[offset + 2]);
            var backdrop = OverWhite(background.Pixels[offset], background.Pixels[offset + 2]);
            result += (backdrop - green) / (backdrop - Stroke.G);
        }
        return result;

        static double OverWhite(byte channel, byte alpha) => channel * (alpha / 255d) + 255 - alpha;
    }

    private static VisualSceneMark Foreground(PreparedVisual prepared, string role) =>
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneMark>(), mark => mark.Role == role);

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);

    private static Chart Create(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(420, 300).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithLineVisualStyle(ChartLineVisualStyle.Plain());
        var points = new[] { new ChartPoint(1, 30), new ChartPoint(2, 60), new ChartPoint(3, 40), new ChartPoint(4, 70) };
        var ranges = new[] { new ChartRangeBand(1, 20, 50), new ChartRangeBand(2, 35, 70), new ChartRangeBand(3, 25, 60) };
        switch (kind) {
            case ChartSeriesKind.Line: chart.AddLine("Series", points, Fill); break;
            case ChartSeriesKind.StepLine: chart.AddStepLine("Series", points, Fill); break;
            case ChartSeriesKind.Area: chart.AddArea("Series", points, Fill); break;
            case ChartSeriesKind.StepArea: chart.AddStepArea("Series", points, Fill); break;
            case ChartSeriesKind.StackedArea: chart.AddStackedArea("Series", points, Fill); break;
            case ChartSeriesKind.RangeBand: chart.AddRangeBand("Series", ranges, Fill); break;
            case ChartSeriesKind.RangeArea: chart.AddRangeArea("Series", ranges, Fill); break;
            case ChartSeriesKind.DifferenceArea:
                chart.AddDifferenceArea("Series", new[] { new ChartDifferencePoint(1, 30, 50), new ChartDifferencePoint(2, 60, 40), new ChartDifferencePoint(3, 40, 70) }, Fill); break;
            case ChartSeriesKind.Radar: chart.AddRadarArea("Series", points, Fill); break;
            case ChartSeriesKind.Polar: chart.AddPolar("Series", points, Fill); break;
            case ChartSeriesKind.Slope: chart.AddSlope("Series", points, Fill); break;
            case ChartSeriesKind.TrendLine: chart.AddTrendLine("Series", points, Fill); break;
            case ChartSeriesKind.Dumbbell: chart.AddDumbbell("Series", new[] { new ChartDumbbell(1, 20, 65), new ChartDumbbell(2, 35, 80) }, Fill); break;
            case ChartSeriesKind.Lollipop: chart.AddLollipop("Series", points, Fill); break;
            case ChartSeriesKind.ErrorBar: chart.AddErrorBar("Series", new[] { new ChartErrorBar(2, 50, new ChartErrorBounds(1, 3), new ChartErrorBounds(30, 70)) }, Fill); break;
            case ChartSeriesKind.BoxPlot: chart.AddBoxPlot("Series", new[] { new ChartBoxPlot(1, 10, 25, 45, 60, 80) }, Fill); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        return chart;
    }
}
