using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class MermaidXYOrientationBrowserTests {
    [BrowserTheory]
    [InlineData("bar", 900, false)]
    [InlineData("bar", 360, true)]
    [InlineData("line", 900, true)]
    [InlineData("line", 360, false)]
    [InlineData("mixed", 900, false)]
    [InlineData("mixed", 360, true)]
    public async Task HorizontalCategoriesAndValuesAgreeAcrossVisibleAxesPointerAndKeyboard(string kind, int width, bool dark) {
        if (!Enabled) return;
        var series = kind == "mixed" ? "bar [20,50,80]\nline [30,60,90]" : kind + " [20,50,80]";
        var parsed = new MermaidParser().ParseXYChart("xychart-beta horizontal\ntitle Monthly count\nx-axis Month [Jan,Feb,Mar]\ny-axis Count 0 --> 100\n" + series);
        Assert.False(parsed.HasErrors);
        var chart = parsed.Document!.ToChart(new MermaidXYChartRenderOptions { Width = width - 32, Height = 420 });
        chart.WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithLineMarkers(ChartLineMarkerMode.All);
        if (kind == "bar") chart.WithLegend().WithPointLegend();
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage();
        await using var session = await OpenAsync(html, width, 580);
        var page = session.Page;
        await page.WaitForSelectorAsync(Point(0, 0));
        if (kind == "bar") Assert.Equal(new[] { "Jan", "Feb", "Mar" }, await page.Locator("[data-cfx-role='legend-label']").AllTextContentsAsync());
        var points = new[] { await BoxAsync(page, Point(0, 0)), await BoxAsync(page, Point(0, 1)), await BoxAsync(page, Point(0, 2)) };
        Assert.True(points[0].Y > points[1].Y && points[1].Y > points[2].Y);
        Assert.True(points[0].X + points[0].Width < points[1].X + points[1].Width && points[1].X + points[1].Width < points[2].X + points[2].Width);
        Assert.Equal("Jan", await page.Locator(Point(0, 0)).GetAttributeAsync("data-cfx-x-label"));
        await MoveToAsync(page, Point(0, 0));
        await page.WaitForFunctionAsync("() => { const tip=document.querySelector('.cfx-tooltip');return tip && !tip.hidden; }");
        var tooltip = await TooltipTextAsync(page);
        Assert.Contains("20", tooltip);
        await MoveAwayAsync(page);
        await page.Locator(Point(0, 0)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Point(0, 0)).GetAttributeAsync("aria-selected"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("2", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxX"));
        AssertNoConsoleErrors(session);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "xy-horizontal-" + kind + "-" + width + (dark ? "-dark" : "-light");
            File.WriteAllText(Path.Combine(capture, name + ".html"), html);
            File.WriteAllBytes(Path.Combine(capture, name + ".native.png"), prepared.ToPng());
            File.WriteAllText(Path.Combine(capture, name + ".json"), JsonSerializer.Serialize(new { kind, width, dark, points, tooltip, ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error) }));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png"), FullPage = true });
        }
    }
}
