using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveHoverEmphasisBrowserTests {
    private const string Lines = "[data-cfx-role=\"line\"]";
    private const string Legends = "[data-cfx-role=\"legend-item\"]";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PointHover_PointedSeries_StaysFullWhileOtherSeriesDim(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage());
        var page = session.Page;

        await MoveToAsync(page, Point(1, 3));
        Assert.Equal(new[] { "0.3", "1", "0.3" }, await OpacitiesAsync(page, Lines));
        Assert.Equal(new[] { "0.3", "1", "0.3" }, await OpacitiesAsync(page, Legends));
        var marker = await page.Locator(Point(1, 3)).EvaluateAsync<string[]>("node => { const s = getComputedStyle(node); return [s.opacity, s.r, s.strokeWidth]; }");
        Assert.Equal(new[] { "1", "4px", "2px" }, marker);
        Assert.StartsWith("Thu", await TooltipTextAsync(page), StringComparison.Ordinal);

        // Between the Passed and Warnings lines at Thursday: the shared readout keeps every series at full strength.
        var warnings = await BoxAsync(page, Point(1, 3));
        var passed = await BoxAsync(page, Point(0, 3));
        await page.Mouse.MoveAsync((float)(warnings.X + warnings.Width / 2 + 6), (float)((warnings.Y + passed.Y) / 2 + 40), new Microsoft.Playwright.MouseMoveOptions { Steps = 4 });
        Assert.Equal("shared", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-mode"));
        Assert.Equal(new[] { "1", "1", "1" }, await OpacitiesAsync(page, Lines));
        Assert.Equal(new[] { "1", "1", "1" }, await OpacitiesAsync(page, Legends));
        Assert.Equal(new[] { "1", "1", "1" }, await OpacitiesAsync(page, "circle.cfx-hover-column"));
        var tooltip = await TooltipTextAsync(page);
        Assert.Matches("^Thu\\s+Failed\\s+31\\s+Warnings\\s+112\\s+Passed\\s+1,?041$", tooltip);

        await MoveAwayAsync(page);
        Assert.Equal(new[] { "1", "1", "1" }, await OpacitiesAsync(page, Lines));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeyboardFocus_ChartTargets_KeepsFocusRingAndArrowTraversal(bool dark) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(dark).ToInteractiveHtmlPage());
        var page = session.Page;

        await page.Keyboard.PressAsync("Tab");
        var first = await page.EvaluateAsync<string[]>("() => { const node = document.activeElement; const s = getComputedStyle(node); return [node.dataset.cfxTargetId || '', s.outlineStyle, s.outlineWidth]; }");
        Assert.NotEqual(string.Empty, first[0]);
        Assert.Equal(new[] { "solid", "2px" }, first.Skip(1).ToArray());

        await page.Keyboard.PressAsync("ArrowRight");
        var next = await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId || ''");
        Assert.NotEqual(first[0], next);
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
    }
}
