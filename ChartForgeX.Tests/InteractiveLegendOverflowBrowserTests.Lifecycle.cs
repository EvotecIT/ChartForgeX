using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveLegendOverflowBrowserTests {
    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task OverflowCommandsFollowDurableIdentityAndReleaseDetachedPagesAcrossUpdateAndDisposal() {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options) => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        await using var session = await OpenAsync(DenseSeries(false).ToInteractiveHtmlPage(Configure), 390, 900);
        var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart');
                window.legendController=ChartForgeXCharts.get(root);window.lifecycleLegendEvents=[];
                root.addEventListener('cfxseries',event=>lifecycleLegendEvents.push(event.detail));
            }
            """);
        await page.Locator(Panel + " summary").ClickAsync();
        await page.Locator("[data-cfx-legend-page=next]").ClickAsync();
        var target = page.Locator(Rows + " [data-cfx-legend-action=mute]").Last;
        await FocusWithKeyboardAsync(page, target); await page.Keyboard.PressAsync("Space");
        Assert.Contains("Service 12", await target.InnerTextAsync());
        Assert.Equal("true", await target.GetAttributeAsync("aria-pressed"));
        await target.EvaluateAsync("button=>window.oldLegendAction=button");
        await page.Locator("[data-cfx-legend-page=previous]").EvaluateAsync("button=>window.oldLegendPage=button");
        await page.Locator("[data-cfx-legend-page=next]").ClickAsync();
        await page.EvaluateAsync("()=>{oldLegendAction.click();oldLegendPage.click()}");
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>lifecycleLegendEvents.length"));
        Assert.Equal("13–13 of 13", await page.Locator(Panel + " .cfx-legend-overflow__range").InnerTextAsync());
        await page.Locator("[data-cfx-legend-page=previous]").ClickAsync();
        await FocusWithKeyboardAsync(page, target);
        await page.EvaluateAsync("html=>legendController.update(html)", DenseSeries(false, reverse: true).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        Assert.True(await page.Locator(Panel).EvaluateAsync<bool>("node=>node.open"));
        Assert.Equal("1–6 of 13", await page.Locator(Panel + " .cfx-legend-overflow__range").InnerTextAsync());
        Assert.Contains("Service 12", await page.EvaluateAsync<string>("()=>document.activeElement.textContent"));
        Assert.Equal("true", await page.EvaluateAsync<string>("()=>document.activeElement.getAttribute('aria-pressed')"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>lifecycleLegendEvents.length"));
        await page.EvaluateAsync("()=>{oldLegendAction.click();oldLegendPage.click()}");
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>lifecycleLegendEvents.length"));
        Assert.Equal("1–6 of 13", await page.Locator(Panel + " .cfx-legend-overflow__range").InnerTextAsync());
        await page.EvaluateAsync("html=>legendController.update(html)", DenseSeries(false, count: 5).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        Assert.Equal(5, await page.Locator(Rows).CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-legend-page]").CountAsync());
        Assert.Equal(0, await page.Locator(".cfx-stage .cfx-series-muted").CountAsync());
        Assert.True(await page.Locator(Panel + " summary").EvaluateAsync<bool>("node=>node===document.activeElement"));
        await CaptureAsync(page, "legend-overflow-updated-clamped");
        await page.Locator(Rows + " [data-cfx-legend-action=mute]").First.EvaluateAsync("button=>window.disposedLegendAction=button");
        await page.EvaluateAsync("()=>{legendController.dispose();disposedLegendAction.click()}");
        Assert.Equal(0, await page.Locator(Panel).CountAsync());
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>lifecycleLegendEvents.length"));
        await page.EvaluateAsync("()=>window.legendController=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'))");
        Assert.Equal(1, await page.Locator(Panel).CountAsync());
        await page.Locator(Panel + " summary").ClickAsync();
        await page.Locator(Rows + " [data-cfx-legend-action=mute]").First.ClickAsync();
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>lifecycleLegendEvents.length"));
        await page.EvaluateAsync("()=>{window.removedLegendRoot=legendController.root;removedLegendRoot.remove()}");
        await page.WaitForFunctionAsync("()=>removedLegendRoot.querySelector('.cfx-legend-overflow')===null");
        await page.EvaluateAsync("()=>{document.body.appendChild(removedLegendRoot);legendController=ChartForgeXCharts.mount(removedLegendRoot)}");
        Assert.Equal(1, await page.Locator(Panel).CountAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task AdaptiveOverflowRestoresAnIndividuallyMutedKeyAndKeepsDisclosurePreferenceWhenTheWideLegendFits() {
        if (!Enabled) return;
        var labels = new[] { "Service request measures North", "Service request measures South", "Service request measures West" };
        var chart = Chart.Create().WithSize(1100, 360).WithTitle("Adaptive legend access").WithLegend()
            .WithLegendBudget(maximumRows: 1).WithTheme(VisualTheme.Graphite());
        for (var index = 0; index < labels.Length; index++) chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("area-" + index), labels[index],
            new[] { new ChartPoint(0, index + 2), new ChartPoint(1, index + 4) }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive), 1440, 900);
        var page = session.Page;
        Assert.Equal(0, await page.Locator(Panel).CountAsync());
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series='2']").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.SetViewportSizeAsync(390, 900);
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing==='narrow'");
        await page.Locator(Panel + " summary").ClickAsync();
        var target = page.Locator(Rows + " [data-cfx-legend-action=mute]").Last;
        Assert.Equal("true", await target.GetAttributeAsync("aria-pressed"));
        await target.ClickAsync();
        Assert.Equal("false", await target.GetAttributeAsync("aria-pressed"));
        await page.SetViewportSizeAsync(1440, 900);
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing==='wide'");
        Assert.Equal(0, await page.Locator(Panel).CountAsync());
        Assert.NotEqual("true", await page.Locator("[data-cfx-role=legend-item][data-cfx-series='2']").GetAttributeAsync("data-cfx-muted"));
        await page.SetViewportSizeAsync(390, 900);
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxAdaptiveDrawing==='narrow'");
        Assert.True(await page.Locator(Panel).EvaluateAsync<bool>("node=>node.open"));
        await CaptureAsync(page, "legend-overflow-adaptive-return");
        AssertNoConsoleErrors(session);
    }
}
