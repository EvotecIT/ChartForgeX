using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class InteractivePyramidSelectionDockTests {
    [Theory]
    [InlineData(false, ChartOrientation.Vertical, HtmlChartResponsiveLayout.Fit, 340, 560)]
    [InlineData(true, ChartOrientation.Horizontal, HtmlChartResponsiveLayout.Fit, 340, 360)]
    [InlineData(true, ChartOrientation.Vertical, HtmlChartResponsiveLayout.Readable, 700, 460)]
    public async Task PyramidSelectionControlsPreserveTheNativeLabelsAndViewport(bool dark, ChartOrientation orientation,
        HtmlChartResponsiveLayout layout, int width, int height) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTitle("Work allocation").WithDataLabels()
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXLabels("Services", "Platform", "Support")
            .AddPyramid("Allocation", new[] { new ChartPoint(1, 50), new ChartPoint(2, 30), new ChartPoint(3, 20) })
            .WithPyramid(options => { options.Orientation = orientation; options.Reversed = true; options.ValueEncoding = ChartPyramidValueEncoding.Area; });
        await using var session = await InteractiveChartBrowser.OpenAsync(chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = layout), width, height);
        var page = session.Page;
        var before = await InteractiveChartBrowser.BoxAsync(page, ".cfx-stage svg");
        for (var point = 0; point < 3; point++) {
            await page.Locator("g[data-cfx-role='pyramid-stage'][data-cfx-point='" + point + "']").FocusAsync();
            await page.Keyboard.PressAsync("Space");
        }
        Assert.Equal("3", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-compare-count"));
        var dock = await InteractiveChartBrowser.BoxAsync(page, "[data-cfx-compare-tray]");
        var stage = await InteractiveChartBrowser.BoxAsync(page, ".cfx-stage");
        var after = await InteractiveChartBrowser.BoxAsync(page, ".cfx-stage svg");
        Assert.True(dock.Y >= stage.Y + stage.Height, "Selected pyramid stages must not cover labels or the chart viewport.");
        Assert.Equal(before.Width, after.Width, 1);
        Assert.Equal(before.Height, after.Height, 1);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            var name = "pyramid-dock-" + orientation + "-" + layout + "-" + (dark ? "dark" : "light");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, name + ".png"), FullPage = true });
            await File.WriteAllTextAsync(Path.Combine(capture, name + ".json"), JsonSerializer.Serialize(new { sourceValues = new[] { 50, 30, 20 }, stage, dock, before, after }));
        }
        await page.Locator("[data-cfx-compare-clear]").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        Assert.False(await page.Locator("[data-cfx-compare-tray]").IsVisibleAsync());
        Assert.Equal("cfx-interactive-chart", await page.EvaluateAsync<string>("() => document.activeElement.className"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
