using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveLegendTooltipBrowserTests {
    private static readonly string[] InternalFields = { "Role", "Kind", "legend item", "Series" };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegendTooltip_FocusAndHover_SummarizesSeriesWithoutInternalFields(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage());
        var page = session.Page;

        await page.Locator(Legend(1)).FocusAsync();
        AssertSummary(await TooltipTextAsync(page), "Warnings", "Latest (Sun)", "72");
        var colours = await page.EvaluateAsync<string[]>("() => [getComputedStyle(document.querySelector('.cfx-tooltip__swatch')).backgroundColor, getComputedStyle(document.querySelector('[data-cfx-role=\"series\"][data-cfx-series=\"1\"] [data-cfx-role=\"line\"]')).stroke]");
        Assert.Equal(colours[1], colours[0]);
        await page.Locator(Legend(1)).BlurAsync();

        // Moving across the legend must keep its summary instead of handing over to the plot crosshair.
        await MoveToAsync(page, Legend(2));
        await MoveToAsync(page, Legend(2), 3);
        AssertSummary(await TooltipTextAsync(page), "Failed", "Latest (Sun)", "11");
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegendTooltip_BarSeries_SummarizesTotal(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(SeverityBars(dark).ToInteractiveHtmlPage());
        var page = session.Page;
        await MoveToAsync(page, Legend(0));
        await MoveToAsync(page, Legend(0), 3);
        AssertSummary(await TooltipTextAsync(page), "Current", "Total", "450");
    }

    private static void AssertSummary(string tooltip, string name, string label, string value) {
        var lines = tooltip.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(new[] { name, label, value }, lines);
        foreach (var field in InternalFields) Assert.DoesNotContain(field, tooltip, StringComparison.Ordinal);
    }
}
