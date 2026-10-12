using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadarCurveReversalIntegrationTests {
    [Theory]
    [InlineData(320, 240)]
    [InlineData(800, 440)]
    public async Task ReversedAsymmetricCurvesFitTheWholePlot(int width, int height) {
        var values = new[] { 100d, 5, 90, 10, 80 };
        var chart = Chart.Create().WithSize(width, height).WithHeader(false).WithLegend(false)
            .WithAxes(false).WithGrid(false).WithDataLabels(false).WithYAxisBounds(0, 100)
            .AddRadarLine("Observed", values.Select((value, index) => new ChartPoint(index + 1, value)));
        chart.Options.XAxis.Reversed = true;
        chart.Series[0].Radar.CurveTension = 1;
        chart.Series[0].Markers.Enabled = false;
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var plot = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Last(group => group.Clip.HasValue).Clip!.Value;
        var outline = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "radar-outline");
        Assert.All(new ChartPath(outline.Commands).Flatten(1), point => {
            Assert.InRange(point.X, plot.Left + outline.StrokeWidth / 2, plot.Right - outline.StrokeWidth / 2);
            Assert.InRange(point.Y, plot.Top + outline.StrokeWidth / 2, plot.Bottom - outline.StrokeWidth / 2);
        });
        Assert.Equal(values, chart.Series[0].Points.Select(point => point.Y));
        Assert.Equal(5, prepared.Regions.Count(region => region.Role == "radar-point"));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, $"reversed-asymmetric-{width}.native.png"), prepared.ToPng());
            File.WriteAllText(Path.Combine(directory, $"reversed-asymmetric-{width}.svg"), prepared.ToSvg());
        }
        if (!InteractiveChartBrowser.Enabled) return;
        await using var session = await InteractiveChartBrowser.OpenAsync("<!doctype html><html><body style='margin:0'>" + prepared.ToSvg() + "</body></html>", width, height);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Equal(1, await session.Page.Locator("[data-cfx-role=radar-outline]").CountAsync());
        if (!string.IsNullOrWhiteSpace(directory))
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, $"reversed-asymmetric-{width}.browser.png"), FullPage = true });
    }
}
