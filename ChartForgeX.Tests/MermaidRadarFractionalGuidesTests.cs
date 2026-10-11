using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidRadarFractionalGuidesTests {
    [Theory]
    [InlineData(false, .5)]
    [InlineData(false, 2.5)]
    [InlineData(true, .5)]
    [InlineData(true, 2.5)]
    public void HiddenFractionalGuidesPreserveTheDataPlot(bool polar, double count) {
        var chart = Chart.Create().WithSize(520, 380).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithYAxisBounds(0, 100);
        var values = new[] { new ChartPoint(1, 17), new ChartPoint(2, 31), new ChartPoint(3, 73) };
        if (polar) chart.AddPolar("Signal", values);
        else chart.AddRadarLine("Signal", values);
        chart.Options.ShowGrid = false;
        chart.Options.ShowAxes = false;
        var original = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.Options.PolarGridRingCount = count;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Equal(original.Regions.Select(region => region.Bounds), prepared.Regions.Select(region => region.Bounds));
        Assert.Equal(original.ToPng(), prepared.ToPng());
    }

    [Theory]
    [InlineData("circle", .5, 320)]
    [InlineData("polygon", .5, 960)]
    [InlineData("circle", 2.5, 960)]
    [InlineData("polygon", 2.5, 320)]
    public async Task FractionalGuidesRetainTheAuthoredSpacingDivisorAndFitTheViewport(string shape, double count, int width) {
        var countText = count.ToString(CultureInfo.InvariantCulture);
        var source = "radar-beta\naxis A,B,C\ncurve signal{17,31,73}\nmax 100\ngraticule " + shape + "\nticks " + countText;
        var parsed = new MermaidParser().ParseRadar(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(count, Convert.ToDouble(document.Ticks, CultureInfo.InvariantCulture));
        var options = new MermaidRadarRenderOptions { Width = width, Height = 380 };
        var artifact = document.ToVisualArtifact(options);
        Assert.Equal(countText, artifact.Metadata["mermaid.ticks"]);
        var chart = document.ToChart(options);
        chart.Options.TransparentBackground = true;
        chart.Options.ShowPlotBackground = false;
        chart.Options.ShowCard = false;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var spoke = prepared.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "radar-spoke");
        var radius = Distance(spoke.Start.X, spoke.Start.Y, spoke.End.X, spoke.End.Y);
        var rings = prepared.Scene.Nodes.Where(node => node.Role == "radar-ring").ToArray();
        Assert.Equal((int)Math.Ceiling(count), rings.Length);
        for (var index = 0; index < rings.Length; index++) {
            var ring = rings[index];
            var r = ring is VisualSceneEllipse ellipse ? ellipse.Rx
                : Distance(spoke.Start.X, spoke.Start.Y, Assert.IsType<VisualScenePath>(ring).Commands[0].X, Assert.IsType<VisualScenePath>(ring).Commands[0].Y);
            Assert.Equal((index + 1d) / count, r / radius, 7);
            Assert.InRange(spoke.Start.X - r, 0, width);
            Assert.InRange(spoke.Start.X + r, 0, width);
            Assert.InRange(spoke.Start.Y - r, 0, 380);
            Assert.InRange(spoke.Start.Y + r, 0, 380);
        }
        Assert.Equal(new[] { 17d, 31d, 73d }, document.Curves[0].OrderedValues);
        Assert.Equal(3, prepared.Regions.Count(region => region.Role == "radar-point"));
        var fenced = new MermaidVisualMarkupParser(new MermaidRenderOptions { Radar = options }).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        Assert.Equal(artifact.ToSvg(), Assert.Single(fenced.Artifacts).ToSvg());
        var png = prepared.ToPng();
        AssertGuidePixels(png, spoke.Start.X, spoke.Start.Y, radius * Math.Ceiling(count) / count, shape == "polygon");
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", width, 380);
        Assert.Equal(rings.Length, await session.Page.Locator("[data-cfx-role=radar-ring]").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => [...document.querySelectorAll('[data-cfx-role=radar-ring]')].every(n=>{const b=n.getBoundingClientRect();return b.left>=0&&b.top>=0&&b.right<=innerWidth&&b.bottom<=innerHeight;})"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(captures)) return;
        Directory.CreateDirectory(captures);
        var name = "radar-fraction-" + shape + "-" + countText + "-" + width;
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + "-browser.png") });
        File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
    }

    [Theory]
    [InlineData(ChartScaleKind.Linear, false)]
    [InlineData(ChartScaleKind.Linear, true)]
    [InlineData(ChartScaleKind.Logarithmic, false)]
    [InlineData(ChartScaleKind.Logarithmic, true)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, false)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, true)]
    public void NativeFractionalIntervalsPreserveTheScaleAndFitTheExtendedGuide(ChartScaleKind scale, bool reversed) {
        var chart = Chart.Create().WithSize(520, 380).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithYAxisBounds(10, 1000).WithPolarGridShape(ChartPolarGridShape.Circle)
            .AddRadarLine("Signal", new[] { new ChartPoint(1, 10), new ChartPoint(2, 100), new ChartPoint(3, 1000) });
        chart.Options.YAxis.Scale = scale;
        chart.Options.YAxis.Reversed = reversed;
        chart.Options.TransparentBackground = true;
        chart.Options.ShowPlotBackground = false;
        chart.Options.ShowCard = false;
        var original = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.Options.PolarGridRingCount = 2.5;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var spoke = prepared.Scene.Nodes.OfType<VisualSceneLine>().First(node => node.Role == "radar-spoke");
        var radius = Distance(spoke.Start.X, spoke.Start.Y, spoke.End.X, spoke.End.Y);
        var rings = prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(node => node.Role == "radar-ring").ToArray();
        Assert.Equal(3, rings.Length);
        for (var index = 0; index < rings.Length; index++) Assert.Equal((index + 1d) / 2.5, rings[index].Rx / radius, 7);
        var before = original.Regions.Where(region => region.Role == "radar-point").ToArray();
        var after = prepared.Regions.Where(region => region.Role == "radar-point").ToArray();
        Assert.Equal(before.Length, after.Length);
        for (var index = 0; index < before.Length; index++) {
            Assert.Equal((before[index].Bounds.Left + before[index].Bounds.Width / 2 - spoke.Start.X) * 2.5 / 3,
                after[index].Bounds.Left + after[index].Bounds.Width / 2 - spoke.Start.X, 7);
            Assert.Equal((before[index].Bounds.Top + before[index].Bounds.Height / 2 - spoke.Start.Y) * 2.5 / 3,
                after[index].Bounds.Top + after[index].Bounds.Height / 2 - spoke.Start.Y, 7);
        }
        AssertGuidePixels(prepared.ToPng(), spoke.Start.X, spoke.Start.Y, radius * 3 / 2.5, false);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarGridRingCount = double.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.Options.PolarGridRingCount = double.PositiveInfinity);
    }

    [Theory]
    [InlineData("02.50", 2.5, false)]
    [InlineData("0.01", .01, false)]
    [InlineData("32.5", 32.5, true)]
    [InlineData("2.00001", 2.00001, false)]
    public void DecimalSpellingsAndTheCapMatchPinnedMermaidOptions(string spelling, double expected, bool capped) {
        var result = new MermaidParser().ParseRadar("radar-beta\naxis A,B,C\ncurve c{1,2,3}\nticks " + spelling);
        Assert.False(result.HasErrors);
        Assert.Equal(expected, result.Document!.Ticks);
        Assert.Equal(expected.ToString(CultureInfo.InvariantCulture), result.Document.ToVisualArtifact().Metadata["mermaid.ticks"]);
        var chart = result.Document.ToChart();
        Assert.Equal(Math.Min(expected, 32), chart.Options.PolarGridRingCount);
        Assert.Equal((int)Math.Ceiling(Math.Min(expected, 32)), chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.Count(node => node.Role == "radar-ring"));
        Assert.Equal(capped ? 1 : 0, result.Diagnostics.Count);
        if (capped) Assert.Equal(MermaidDiagnosticSeverity.Warning, result.Diagnostics[0].Severity);
    }

    [Theory]
    [InlineData(".5")]
    [InlineData("2.")]
    [InlineData("+2")]
    [InlineData("1e2")]
    [InlineData("-0")]
    public void TickNumeralsRejectSpellingsOutsideThePinnedGrammar(string spelling) {
        var result = MermaidRenderer.Render("radar-beta\naxis A,B,C\ncurve c{1,2,3}\nticks " + spelling);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Equal(4, Assert.Single(result.Diagnostics).Span.Line);
    }

    private static void AssertGuidePixels(byte[] png, double cx, double cy, double radius, bool polygon) {
        var image = RasterImageDecoder.Decode(png);
        // Halfway between the first two axes, the triangular guide is at half
        // the circular radius. Both positions are beyond the source data here.
        var circle = new ChartPoint(cx + Math.Cos(-Math.PI / 6) * radius, cy + Math.Sin(-Math.PI / 6) * radius);
        var edge = new ChartPoint(cx + (circle.X - cx) / 2, cy + (circle.Y - cy) / 2);
        Assert.True(HasPaint(polygon ? edge : circle), "The authored outer guide must paint its native PNG geometry.");
        Assert.False(HasPaint(polygon ? circle : edge), "The other guide shape must not replace the authored geometry.");

        bool HasPaint(ChartPoint point) {
            var x = (int)Math.Round(point.X); var y = (int)Math.Round(point.Y);
            for (var dy = -2; dy <= 2; dy++) for (var dx = -2; dx <= 2; dx++) {
                var px = x + dx; var py = y + dy;
                if (px >= 0 && px < image.Width && py >= 0 && py < image.Height && image.Pixels[(py * image.Width + px) * 4 + 3] > 0) return true;
            }
            return false;
        }
    }

    private static double Distance(double x1, double y1, double x2, double y2) => Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
}
