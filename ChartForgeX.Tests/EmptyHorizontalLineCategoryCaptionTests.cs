using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class EmptyHorizontalLineCategoryCaptionTests {
    [Trait("Category", "Browser")]
    [Theory]
    [InlineData(false, true, 360, true)]
    [InlineData(true, true, 800, false)]
    [InlineData(false, false, 800, false)]
    [InlineData(true, false, 360, true)]
    public async Task EmptyLineCompanionsPreserveBarCategoryCaptionsAndPixels(bool step, bool legacy, int width, bool dark) {
        var chart = Chart.Create().WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddHorizontalBar("Readings", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40), new ChartPoint(3, 60) });
        chart.WithSize(width, 320);
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        var labels = legacy ? chart.Options.XAxis.Labels : chart.Options.YAxis.Labels;
        labels.Add(new ChartAxisLabel(1, "Alpha")); labels.Add(new ChartAxisLabel(2, "Beta")); labels.Add(new ChartAxisLabel(3, "Gamma"));
        var before = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var expectedPng = before.ToPng();
        if (step) chart.AddStepLine("Empty", Array.Empty<ChartPoint>());
        else chart.AddLine("Empty", Array.Empty<ChartPoint>());
        var after = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = after.ToSvg(); var png = after.ToPng();
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        string? stem = null;
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            stem = Path.Combine(capture, $"empty-line-captions-{step}-{legacy}-{width}");
            File.WriteAllText(stem + ".svg", svg); File.WriteAllBytes(stem + "-before.png", expectedPng);
            File.WriteAllBytes(stem + "-native.png", png);
        }
        if (InteractiveChartBrowser.Enabled) {
            var html = chart.ToInteractiveHtmlPage();
            await using var session = await InteractiveChartBrowser.OpenAsync(html, width + 32, 440);
            if (stem != null) {
                File.WriteAllText(stem + ".html", html);
                await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + "-browser.png", FullPage = true });
            }
            Assert.Equal("Alpha", await session.Page.Locator(InteractiveChartBrowser.Point(0, 0)).GetAttributeAsync("data-cfx-x-label"));
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
        }
        foreach (var caption in new[] { "Alpha", "Beta", "Gamma" }) Assert.Contains(">" + caption + "<", svg);
        Assert.Equal(expectedPng, png);
    }
}
