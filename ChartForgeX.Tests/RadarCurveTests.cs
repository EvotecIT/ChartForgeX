using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects closed radar curves, source policy and identical geometry across static backends.</summary>
public sealed class RadarCurveTests {
    private const string Body = "radar-beta\naxis a[\"Coverage\"], b[\"Policy\"], c[\"Response\"], d[\"Recovery\"]\ncurve current[\"Current\"]{90, 45, 80, 60}\nmin 0\nmax 100\n";

    [Theory]
    [InlineData(-.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NativeAndHostOptionsRejectNonFiniteOrOutOfRangeTension(double tension) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartRadarOptions { CurveTension = tension });
        Assert.Throws<ArgumentOutOfRangeException>(() => new MermaidRadarRenderOptions { CurveTension = tension });
    }

    [Fact]
    public void CyclicTangentsIncludeTheClosingSegmentWithoutAnEndpointKink() {
        var points = new[] { new ChartPoint(0, -10), new ChartPoint(10, 0), new ChartPoint(0, 10), new ChartPoint(-10, 0) };
        var path = ChartPathBuilder.ClosedCurve(points, .25);
        Assert.Equal(5, path.Commands.Count);
        Assert.All(path.Commands.Skip(1), command => Assert.Equal(ChartPathCommandKind.CubicTo, command.Kind));
        var first = path.Commands[1]; var last = path.Commands[4];
        Assert.Equal((5d, -10d, 10d, -5d, 10d, 0d),
            (first.Control1X, first.Control1Y, first.Control2X, first.Control2Y, first.X, first.Y));
        Assert.Equal((-10d, -5d, -5d, -10d, 0d, -10d),
            (last.Control1X, last.Control1Y, last.Control2X, last.Control2Y, last.X, last.Y));
        Assert.Equal(new ChartRect(-10, -10, 20, 20), path.Bounds());
    }

    [Fact]
    public void CubicBoundsMeasureInteriorExtremaInsteadOfEndpointsOrTheControlHull() {
        var path = new ChartPath(new[] { ChartPathCommand.MoveTo(0, 0), ChartPathCommand.CubicTo(100, 100, -100, 100, 0, 0) });
        var bounds = path.Bounds();
        Assert.Equal(-100 / Math.Sqrt(12), bounds.Left, 8);
        Assert.Equal(100 / Math.Sqrt(12), bounds.Right, 8);
        Assert.Equal(0, bounds.Top); Assert.Equal(75, bounds.Bottom, 8);
    }

    [Theory]
    [InlineData(ChartLineAreaForm.Line)]
    [InlineData(ChartLineAreaForm.Area)]
    public void CurvesRetainObservationsAndChangeNativeInkForEitherForm(ChartLineAreaForm form) {
        var chart = Native(420, 360, form);
        var straight = Prepare(chart);
        chart.Series[0].ConfigureRadar(radar => radar.CurveTension = 0);
        Assert.Equal(straight.ToPng(), Prepare(chart).ToPng());
        chart.Series[0].Radar.CurveTension = .17;
        var curved = Prepare(chart);
        Assert.Equal(straight.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds),
            curved.Regions.Where(region => region.Role == "radar-point").Select(region => region.Bounds));
        Assert.All(Outline(curved).Commands.Skip(1), command => Assert.Equal(ChartPathCommandKind.CubicTo, command.Kind));
        Assert.True(Outline(curved).Close);
        if (form == ChartLineAreaForm.Area)
            Assert.Equal(Outline(curved).Commands, Assert.Single(curved.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-area").Commands);
        var before = RasterImageDecoder.Decode(straight.ToPng()); var after = RasterImageDecoder.Decode(curved.ToPng());
        Assert.True(before.Pixels.Where((value, index) => value != after.Pixels[index]).Count() > 100);
        Assert.Contains(" C", curved.ToSvg());
        var svg = curved.ToSvg(); var png = curved.ToPng();
        chart.Series[0].Radar.CurveTension = 1; chart.Series[0].Points.Clear();
        Assert.Equal(svg, curved.ToSvg()); Assert.Equal(png, curved.ToPng());
    }

    [Theory]
    [InlineData(320, 240)]
    [InlineData(480, 480)]
    public void MaximumTensionFitsWholeCurvesInsideThePlot(int width, int height) {
        var chart = Native(width, height, ChartLineAreaForm.Line);
        chart.Series[0].Points.Clear();
        for (var index = 1; index <= 4; index++) chart.Series[0].Points.Add(new ChartPoint(index, 100));
        chart.Series[0].Radar.CurveTension = 1;
        var prepared = Prepare(chart);
        var plot = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Last(group => group.Clip.HasValue).Clip!.Value;
        var outline = Outline(prepared);
        Assert.All(new ChartPath(outline.Commands).Flatten(1), point => {
            Assert.InRange(point.X, plot.Left + outline.StrokeWidth / 2, plot.Right - outline.StrokeWidth / 2);
            Assert.InRange(point.Y, plot.Top + outline.StrokeWidth / 2, plot.Bottom - outline.StrokeWidth / 2);
        });
    }

    [Theory]
    [InlineData("", .17)]
    [InlineData("graticule circle\n", .17)]
    [InlineData("graticule polygon\n", 0)]
    public void SourceGraticuleSelectsCurvedOrStraightPolicy(string statement, double tension) {
        var parsed = new MermaidParser().ParseRadar(Body + statement);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(tension, parsed.Document!.ToChart().Series[0].Radar.CurveTension);
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("2.5e-1", .25)]
    public void SourceTensionUsesCanonicalNumbersAndHostOverrideWins(string scalar, double tension) {
        var parsed = new MermaidParser().ParseRadar("---\nconfig:\n  radar:\n    curveTension: " + scalar + "\n---\n" + Body);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(tension, document.ToChart().Series[0].Radar.CurveTension);
        Assert.Equal(.4, document.ToChart(new MermaidRadarRenderOptions { CurveTension = .4 }).Series[0].Radar.CurveTension);
        document.Graticule = "polygon";
        Assert.Equal(0, document.ToChart().Series[0].Radar.CurveTension);
        Assert.Equal(.4, document.ToChart(new MermaidRadarRenderOptions { CurveTension = .4 }).Series[0].Radar.CurveTension);
        Assert.Contains(document.Configuration.Settings, setting => setting.Path == "radar.curveTension" && setting.Span.Line == 4);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.01")]
    [InlineData("1e309")]
    [InlineData("'0.17'")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("\n      nested: 1")]
    public void MalformedTensionProducesALocatedConfigurationError(string value) {
        var parsed = new MermaidParser().ParseRadar("---\nconfig:\n  radar:\n    curveTension: " + value + "\n---\n" + Body);
        Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidConfiguration && diagnostic.Span.Line == 4);
    }

    [Fact]
    public void DirectivePrecedenceAndOtherFamiliesKeepTheirExistingConfigurationContracts() {
        var parsed = new MermaidParser().ParseRadar("---\nconfig:\n  radar:\n    curveTension: 1\n---\n%%{init: {'radar': {'curveTension': 0.25}}}%%\n" + Body);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(.25, parsed.Document!.ToChart().Series[0].Radar.CurveTension);
        var other = new MermaidParser().ParseFlowchart("%%{init: {'radar': {'curveTension': 0.25}}}%%\nflowchart LR\nA --> B");
        Assert.False(other.HasErrors);
        Assert.Contains(other.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
    }

    [BrowserTheory]
    [InlineData(320, .17, false)]
    [InlineData(960, .17, false)]
    [InlineData(320, 1, false)]
    [InlineData(960, 1, false)]
    [InlineData(320, 1, true)]
    [InlineData(960, 1, true)]
    public async Task CurvedRadarRemainsStaticAndVisibleInResponsiveHosts(int width, double tension, bool polygon) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = "---\nconfig:\n  radar:\n    curveTension: " + tension.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n---\n" + Body + (polygon ? "graticule polygon\n" : "graticule circle\n");
        var artifact = new MermaidParser().ParseRadar(source).Document!.ToVisualArtifact();
        await using var session = await InteractiveChartBrowser.OpenAsync(artifact.ToHtmlPage(), width, 620);
        Assert.Equal(1, await session.Page.Locator("[data-cfx-role=radar-outline]").CountAsync());
        var path = await session.Page.Locator("[data-cfx-role=radar-outline]").GetAttributeAsync("d");
        if (polygon) Assert.DoesNotContain("C", path); else Assert.Contains("C", path);
        Assert.Equal(0, await session.Page.Locator("script").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "radar-curves-" + (polygon ? "polygon" : tension == 1 ? "maximum" : "default");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser-" + width + ".png"), FullPage = true });
            await File.WriteAllBytesAsync(Path.Combine(captures, name + "-native.png"), artifact.ToPng());
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }

    private static Chart Native(int width, int height, ChartLineAreaForm form) {
        var chart = Chart.Create().WithSize(width, height).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false).WithYAxisBounds(0, 100)
            .AddRadar("Current", new[] { new ChartPoint(1, 90), new ChartPoint(2, 45), new ChartPoint(3, 80), new ChartPoint(4, 60) });
        chart.Series[0].Radar.Form = form; chart.Series[0].Markers.Enabled = false;
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualScenePath Outline(PreparedVisual prepared) => Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-outline");
}
