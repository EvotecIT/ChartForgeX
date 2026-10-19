using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects native progress-row identity through actual input and the public chart update controller.</summary>
public sealed class ProgressBarIdentityBrowserTests {
    private const string North = "[data-cfx-role=progress-row][data-cfx-observation-id=north]";

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProgressRowsKeepTheirAuthoredSelectionWhenThePublicControllerReordersAndUpdatesThem(bool dark) {
        if (!Enabled) return;
        var chart = Progress(dark, false);
        Assert.Contains("data-cfx-observation-id=\"north\"", chart.ToSvg(), StringComparison.Ordinal);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), 720, 540);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                window.progressController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));
                window.progressSelections=[];
                progressController.root.addEventListener('cfxselect',event=>progressSelections.push(event.detail));
            }
            """);
        var north = page.Locator(North);
        Assert.Equal("progress", await north.GetAttributeAsync("data-cfx-series-key"));
        Assert.Equal("40", await north.GetAttributeAsync("data-cfx-value"));
        await MoveToAsync(page, North + " [data-cfx-role=progress-fill]");
        Assert.Contains("North", await TooltipTextAsync(page), StringComparison.Ordinal);
        await north.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await north.GetAttributeAsync("aria-selected"));
        Assert.Equal("north", await page.EvaluateAsync<string>("()=>progressSelections.at(-1).target.observationId"));
        await page.EvaluateAsync("html=>progressController.update(html)", Progress(dark, true).ToInteractiveHtmlFragmentWithoutAssets(Options));
        north = page.Locator(North);
        Assert.Equal("1", await north.GetAttributeAsync("data-cfx-point"));
        Assert.Equal("75", await north.GetAttributeAsync("data-cfx-value"));
        Assert.Equal("true", await north.GetAttributeAsync("aria-selected"));
        Assert.Equal(new[] { "north" }, await page.EvaluateAsync<string[]>("()=>progressController.getSelection().map(target=>target.observationId)"));
        // The update restores this observation's focus while dismissing its old readout.
        // Focusing the already active node again is a no-op; new keyboard input inspects the new scene.
        Assert.Equal("north", await page.EvaluateAsync<string>("()=>document.activeElement?.getAttribute('data-cfx-observation-id')"));
        Assert.True(await page.Locator(".cfx-tooltip").EvaluateAsync<bool>("tip=>tip.hidden"));
        await page.Keyboard.PressAsync("ArrowLeft");
        Assert.Contains("South", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Contains("20", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("north", await page.EvaluateAsync<string>("()=>document.activeElement?.getAttribute('data-cfx-observation-id')"));
        Assert.Contains("North", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Contains("75", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture); var stem = Path.Combine(capture, "progress-identity-" + dark);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".png", FullPage = true });
            await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("()=>JSON.stringify({selection:progressController.getSelection(),events:progressSelections},null,2)"));
        }
    }

    private static Chart Progress(bool dark, bool updated) {
        var points = updated ? new[] { new ChartPoint(2, 20), new ChartPoint(1, 75) } : new[] { new ChartPoint(1, 40), new ChartPoint(2, 20) };
        var series = new ChartSeries(new ChartSeriesId("progress"), "Tasks", ChartSeriesKind.ProgressBar, points);
        series.WithObservationId(updated ? 1 : 0, new ChartObservationId("north"))
            .WithObservationId(updated ? 0 : 1, new ChartObservationId("south"));
        return Chart.Create().WithSize(640, 360).WithTitle("Progress by region").WithXLabels("North", "South")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.PinOnSelect = false;
    }
}
