using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposalClearsComparisonBrushAndResetPresentationBeforeFreshMount(bool brushSelection) {
        if (!Enabled) return;
        var html = Readings(dark: brushSelection).ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush);
        });
        await using var session = await OpenAsync(html, brushSelection ? 420 : 820, 680);
        var page = session.Page;
        await BindControllerAsync(page);
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]").ClickAsync();
        if (brushSelection) {
            await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
            var mark = await BoxAsync(page, North);
            var centerX = mark.X + mark.Width / 2; var centerY = mark.Y + mark.Height / 2;
            await page.Mouse.MoveAsync((float)(centerX - 18), (float)(centerY - 18));
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync((float)(centerX + 18), (float)(centerY + 18), new MouseMoveOptions { Steps = 6 });
            await page.Mouse.UpAsync();
        } else await SelectAsync(page, North);
        Assert.Equal("true", await page.Locator(North).GetAttributeAsync("aria-selected"));
        var before = await DisposalPresentationAsync(page);
        Assert.False(before.GetProperty("compareHidden").GetBoolean());
        Assert.True(before.GetProperty("compareChildren").GetInt32() > 0);
        Assert.False(before.GetProperty("resetHidden").GetBoolean());
        if (brushSelection) Assert.False(before.GetProperty("brushHidden").GetBoolean());
        var variant = brushSelection ? "brush" : "selection";
        await CaptureAsync(page, "review-disposal-" + variant + "-before", before);
        await page.EvaluateAsync("()=>chartController.dispose()");
        Assert.True(await page.Locator(Root).EvaluateAsync<bool>("root=>root.isConnected"));
        var disposed = await DisposalPresentationAsync(page);
        await CaptureAsync(page, "review-disposal-" + variant + "-disposed", new { before, disposed });
        await page.EvaluateAsync("()=>chartController=ChartForgeXCharts.mount(chartController.root)");
        var remounted = await DisposalPresentationAsync(page);
        await CaptureAsync(page, "review-disposal-" + variant + "-remounted", new { before, disposed, remounted });
        Assert.True(ClearedDisposalPresentation(disposed), "Disposed chart retained browser UI: " + disposed.GetRawText());
        Assert.True(ClearedDisposalPresentation(remounted), "Fresh mount retained browser UI: " + remounted.GetRawText());
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> DisposalPresentationAsync(IPage page) => page.Locator(Root).EvaluateAsync<JsonElement>("""
        root=>{
            const compare=root.querySelector('[data-cfx-compare-tray]'),brush=root.querySelector('.cfx-brush-box'),reset=root.querySelector('[data-cfx-reset]');
            return {compareHidden:compare.hidden,compareChildren:compare.childElementCount,
                hasCompareCount:root.hasAttribute('data-cfx-compare-count'),compareCount:root.dataset.cfxCompareCount||'',
                brushHidden:brush.hidden,brushBounds:root.dataset.cfxBrush||'',resetHidden:reset.hidden,
                selected:root.querySelectorAll('[aria-selected=true]').length,muted:root.querySelectorAll('[data-cfx-muted=true],.cfx-series-muted').length};
        }
        """);

    private static bool ClearedDisposalPresentation(JsonElement receipt) => receipt.GetProperty("compareHidden").GetBoolean()
        && receipt.GetProperty("compareChildren").GetInt32() == 0 && !receipt.GetProperty("hasCompareCount").GetBoolean()
        && receipt.GetProperty("brushHidden").GetBoolean() && receipt.GetProperty("resetHidden").GetBoolean()
        && receipt.GetProperty("selected").GetInt32() == 0 && receipt.GetProperty("muted").GetInt32() == 0;
}
