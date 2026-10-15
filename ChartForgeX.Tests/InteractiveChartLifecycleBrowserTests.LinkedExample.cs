using System.Text.Json;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 1440)]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    public async Task LinkedExampleFiltersPreparedTotalsAndHighlightsThroughPublicControllers(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(LinkedChartViewsExamples.CreatePage(dark), width, 1000);
        var page = session.Page;
        await page.EvaluateAsync("""
            () => {
                window.linkedEvents=[];
                document.body.addEventListener('cfxselectionchange',event=>linkedEvents.push(event.detail));
                const time=document.querySelector('[data-cfx-chart-id="linked-time"]');
                time.dispatchEvent(new CustomEvent('cfxbrush',{detail:{
                    range:{x:['2026-03-03T00:00:00.000Z','2026-03-04T00:00:00.000Z']}
                }}));
            }
            """);
        Assert.Contains("Showing 2 days", await page.Locator("#linked-status").InnerTextAsync());
        const string api = "[data-cfx-chart-id=linked-totals] g[data-cfx-role=point][data-cfx-observation-id=api-total]";
        await page.Locator(api).ClickAsync();
        var selection = await page.EvaluateAsync<JsonElement>("""
            () => ChartForgeXCharts.get(document.querySelector('[data-cfx-chart-id="linked-totals"]')).getSelection()
            """);
        var target = Assert.Single(selection.EnumerateArray());
        Assert.Equal("API", target.GetProperty("category").GetString());
        Assert.Equal(58, target.GetProperty("numericValue").GetDouble());
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>linkedEvents.length"));
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const time=document.querySelector('[data-cfx-chart-id="linked-time"]');
                const api=[...time.querySelectorAll('g[data-cfx-role=point][data-cfx-series-key=api]')];
                const worker=[...time.querySelectorAll('g[data-cfx-role=point][data-cfx-series-key=worker]')];
                return api.length===4 && worker.length===4
                    && api.every(mark=>mark.dataset.cfxHighlighted==='true')
                    && worker.every(mark=>Number(getComputedStyle(mark).opacity)<=.35);
            }
            """));
        await CaptureAsync(page, "linked-example-brushed-" + width + "-" + (dark ? "dark" : "light"), selection);
        await page.Locator("#linked-reset").ClickAsync();
        Assert.Contains("Showing every day", await page.Locator("#linked-status").InnerTextAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('.cfx-interactive-chart')].every(root=>
                ChartForgeXCharts.get(root).getSelection().length===0
                && !root.querySelector('[data-cfx-highlighted=true]'))
            """));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>linkedEvents.length"));
        if (width == 390) {
            Assert.True(await page.EvaluateAsync<bool>("""
                () => [...document.querySelectorAll('.cfx-stage')].every(stage=>stage.scrollWidth<=stage.clientWidth+1)
                """));
        }
        await CaptureAsync(page, "linked-example-cleared-" + width + "-" + (dark ? "dark" : "light"));
        AssertNoConsoleErrors(session);
    }
}
