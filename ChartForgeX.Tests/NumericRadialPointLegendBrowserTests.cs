using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NumericRadialPointLegendBrowserTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task FormattedCategoryLegendRetainsPhysicalSummaryAndKeyboardToggle(bool bars, bool dark) {
        if (!Enabled) return;
        var calls = new Dictionary<double, int>(); var chart = NumericRadialPointLegendTests.Create(bars, calls, false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight()).WithSize(dark ? 360 : 760, dark ? 360 : 480);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), dark ? 390 : 800, 560);
        var page = session.Page; var legend = page.Locator(Legend(0)).First;
        await legend.HoverAsync(); var tooltip = await TooltipTextAsync(page);
        var labels = await page.Locator("[data-cfx-role='legend-label']").AllTextContentsAsync();
        await legend.FocusAsync(); await page.Keyboard.PressAsync("Space");
        var muted = await page.Locator(".cfx-series-muted").CountAsync();
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var name = "formatted-category-" + (bars ? "bar-wide-light" : "column-compact-dark");
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png") });
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { tooltip, labels, calls, muted }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".console.json"), JsonSerializer.Serialize(session.ConsoleLog, new JsonSerializerOptions { WriteIndented = true }));
        }
        Assert.Contains("Group 1 pass 1", labels); Assert.Contains("Group 1 pass 1", tooltip); Assert.True(muted > 0);
        Assert.All(calls.Values, count => Assert.Equal(1, count)); AssertNoConsoleErrors(session);
    }
}
