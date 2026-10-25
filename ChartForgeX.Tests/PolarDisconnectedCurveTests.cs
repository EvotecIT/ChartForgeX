using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Source-ordered runs share native paint, curve fitting and both exports without crossing a declared break.</summary>
public sealed class PolarDisconnectedCurveTests {
    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void FilledSmoothRunsRetainEveryRawFactAndDetachedNativeExports(int width, bool dark) {
        var chart = Create(width, dark); var series = chart.Series[0];
        var prepared = PolarDepthContractTests.Prepare(chart);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "polar-line").ToArray();
        Assert.Equal(2, paths.Length); Assert.All(paths, path => { Assert.True(path.Close); Assert.Equal(3, path.Commands.Count(command => command.Kind == ChartPathCommandKind.CubicTo)); });
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "polar-area"));
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "legend-area");
        var sources = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "polar-point-source").ToArray();
        Assert.Equal(6, sources.Length);
        for (var index = 0; index < sources.Length; index++) {
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), sources[index].Metadata["data-cfx-point"]);
            Assert.Equal(series.Points[index].X.ToString("R", CultureInfo.InvariantCulture), sources[index].Metadata["data-cfx-angle"]);
            Assert.Equal("observation-" + index, sources[index].Metadata["data-cfx-observation-id"]);
            Assert.Equal(index == 3, sources[index].Metadata.ContainsKey("data-cfx-break-before"));
        }
        var svg = prepared.ToSvg(); var png = prepared.ToPng(); var decoded = RasterImageDecoder.Decode(png);
        Assert.Equal(width, decoded.Width); Assert.Equal(380, decoded.Height); Assert.DoesNotContain("<script", svg);
        Assert.Equal(6, XDocument.Parse(svg).Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "polar-point-source"));
        Capture("polar-filled-runs-" + width + "-" + (dark ? "dark" : "light"), svg, png);
        series.Points.Clear(); series.Polar.Form = ChartLineAreaForm.Line; chart.Options.PolarRotationDegrees = 140;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void OpenCurvesDoNotBorrowTangentsAcrossTheAuthoredBreak() {
        var chart = Create(800, false); chart.Series[0].ConfigurePolar(options => { options.Form = ChartLineAreaForm.Line; options.FillOpacity = null; options.CurveTension = .6; });
        var actual = PolarDepthContractTests.Prepare(chart);
        var paths = actual.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "polar-line").ToArray();
        Assert.Equal(2, paths.Length); Assert.All(paths, path => Assert.False(path.Close));
        foreach (var pair in new[] { (Path: paths[0], Start: 0), (Path: paths[1], Start: 3) }) {
            var points = Enumerable.Range(pair.Start, 3).Select(index => {
                var bounds = actual.Regions.Single(region => region.Id == "series-0-point-" + index).Bounds;
                return new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
            }).ToArray();
            Assert.Equal(ChartPathBuilder.OpenCurve(points, .6).Commands, pair.Path.Commands);
        }
    }

    [Fact]
    public void SelectedSecondaryScaleAndReversalRetainAuthoredRadiiAndBreakIdentity() {
        var chart = Create(600, false).WithSecondaryYAxisBounds(0, 1000);
        var primary = PolarDepthContractTests.Prepare(chart);
        var series = chart.Series[0]; series.UseSecondaryValueAxis();
        for (var index = 0; index < series.Points.Count; index++) {
            var point = series.Points[index]; series.Points[index] = new ChartPoint(point.X, point.Y * 10, point.BreakBefore);
        }
        var secondary = PolarDepthContractTests.Prepare(chart);
        Assert.Equal(primary.Regions.Where(region => region.Role == "polar-point").Select(region => region.Bounds),
            secondary.Regions.Where(region => region.Role == "polar-point").Select(region => region.Bounds));
        chart.Options.SecondaryYAxis.WithReversal(); var reversed = PolarDepthContractTests.Prepare(chart);
        Assert.NotEqual(secondary.ToPng(), reversed.ToPng());
        Assert.Equal(6, reversed.Scene.Nodes.OfType<VisualSceneGroup>().Count(group => group.Role == "polar-point-source" && group.Metadata["data-cfx-axis"] == "secondary"));
        Assert.True(series.Points[3].BreakBefore); Assert.Equal(600, series.Points[3].Y);
    }

    [Fact]
    public void ExplicitAreaPaintReachesNativePixelsWithSourceAlphaAndOpacityAppliedOnce() {
        var points = Enumerable.Range(0, 3).Select(index => new ChartPoint(index * Math.PI * 2 / 3, 80)).ToArray();
        var chart = Chart.Create().WithSize(400, 320).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).WithYAxisBounds(0, 100).AddPolar("Signal", points, ChartColor.FromRgba(36, 104, 172, 128));
        chart.Options.TransparentBackground = true; chart.Options.ShowPlotBackground = false; chart.Options.ShowCard = false;
        chart.Series[0].ConfigurePolar(options => { options.Form = ChartLineAreaForm.Area; options.FillOpacity = .5; options.OutlineStroke = new ChartStrokeStyle(width: 0); });
        chart.Series[0].Markers.Enabled = false;
        var prepared = PolarDepthContractTests.Prepare(chart);
        var fill = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-area").Fill!.Value;
        Assert.Equal(64, fill.A); Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "polar-line");
        var targets = prepared.Regions.Where(region => region.Role == "polar-point").Select(region => region.Bounds).ToArray();
        var cx = targets.Average(bounds => bounds.Left + bounds.Width / 2); var cy = targets.Average(bounds => bounds.Top + bounds.Height / 2);
        var image = RasterImageDecoder.Decode(prepared.ToPng()); var pixel = image.Pixels.Skip(((int)cy * image.Width + (int)cx) * 4).Take(4).ToArray();
        Assert.Equal(64, pixel[3]);
        for (var channel = 0; channel < 3; channel++) Assert.InRange(Math.Abs(pixel[channel] - new[] { fill.R, fill.G, fill.B }[channel]), 0, 1);
        Assert.Contains(XDocument.Parse(prepared.ToSvg()).Descendants(), node => (string?)node.Attribute("data-cfx-role") == "polar-area");
    }

    [Fact]
    public void RepeatedAndMultiTurnAnglesRemainDistinctAcrossDisconnectedSourceRuns() {
        var points = new[] { new ChartPoint(0, 30), new ChartPoint(Math.PI * 2, 40), new ChartPoint(-Math.PI / 2, 50, true), new ChartPoint(Math.PI * 4, 60) };
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithDataLabels(false).WithYAxisBounds(0, 100).AddPolar("Signal", points);
        chart.Series[0].ConfigurePolar(options => options.CurveTension = .4);
        var prepared = PolarDepthContractTests.Prepare(chart);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "polar-line"));
        var sources = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Role == "polar-point-source").ToArray();
        Assert.Equal(4, sources.Length); Assert.Equal(4, sources.Select(source => source.Id).Distinct().Count());
        Assert.Equal(points.Select(point => point.X.ToString("R", CultureInfo.InvariantCulture)), sources.Select(source => source.Metadata["data-cfx-angle"]));
        Assert.Equal("true", sources[2].Metadata["data-cfx-break-before"]); Assert.Equal(points, chart.Series[0].Points);
    }

    [Theory]
    [InlineData(ChartLineAreaForm.Line)] [InlineData(ChartLineAreaForm.Area)]
    public void MaximumTensionFitsActualCurveExtremaInsideTheNativeViewport(ChartLineAreaForm form) {
        var points = new[] { new ChartPoint(0, 100), new ChartPoint(.1, 20), new ChartPoint(2.4, 100), new ChartPoint(5, 70) };
        var chart = Chart.Create().WithSize(360, 320).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).WithYAxisBounds(0, 100).AddPolar("Signal", points);
        chart.Series[0].ConfigurePolar(options => { options.Form = form; options.ClosePath = true; options.CurveTension = 1; });
        var prepared = PolarDepthContractTests.Prepare(chart);
        var outline = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "polar-line");
        var bounds = new ChartPath(outline.Commands).Bounds();
        var plot = prepared.Scene.Nodes.OfType<VisualSceneGroup>().First(group => group.Clip.HasValue).Clip!.Value;
        Assert.InRange(bounds.Left, plot.Left + outline.StrokeWidth / 2, plot.Right);
        Assert.InRange(bounds.Right, plot.Left, plot.Right - outline.StrokeWidth / 2);
        Assert.InRange(bounds.Top, plot.Top + outline.StrokeWidth / 2, plot.Bottom);
        Assert.InRange(bounds.Bottom, plot.Top, plot.Bottom - outline.StrokeWidth / 2);
        Assert.Equal(points, chart.Series[0].Points);
    }

    internal static Chart Create(int width, bool dark) {
        var points = new[] { new ChartPoint(0, 30), new ChartPoint(.8, 80), new ChartPoint(1.7, 55),
            new ChartPoint(3.1, 60, true), new ChartPoint(3.9, 40), new ChartPoint(4.8, 90) };
        var series = new ChartSeries(new ChartSeriesId("signal"), "Signal", ChartSeriesKind.Polar, points)
            .ConfigurePolar(options => { options.Form = ChartLineAreaForm.Area; options.CurveTension = .2; options.FillOpacity = .25;
                options.OutlineStroke = new ChartStrokeStyle(width: 2, dash: new[] { 5d, 3d }); });
        for (var index = 0; index < points.Length; index++) series.WithObservationId(index, new ChartObservationId("observation-" + index));
        var chart = Chart.Create().WithSize(width, 380).WithHeader(false).WithLegend().WithDataLabels(false).WithYAxisBounds(0, 100)
            .WithAxes(false).WithGrid(false).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
        chart.Options.PolarRotationDegrees = 25; return chart;
    }

    private static void Capture(string name, string svg, byte[] png) {
        var directory = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), svg); File.WriteAllBytes(Path.Combine(directory, name + ".png"), png);
    }
}
