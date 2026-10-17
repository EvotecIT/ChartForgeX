using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData("open")]
    [InlineData("closed")]
    public async Task DeferredShadowCrosshairKeepsLocalTransportAndPeerSeriesEmphasisCoherent(string shadowMode) {
        if (!Enabled) return;
        var html = new[] { StatusLines(false), StatusLines(false) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Interaction.GroupName = "shadow-crosshair-coherence";
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Crosshair
                | ChartInteractionFeatures.SynchronizedCharts;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        });
        await using var session = await OpenAsync(html, 900, 1100);
        var page = session.Page;
        await page.EvaluateAsync("""
            mode => {
                const roots=[...document.querySelectorAll('.cfx-interactive-chart')];
                window.shadowSyncSource=roots[0];window.shadowSyncPeer=roots[1];
                window.shadowSyncEvents=[];window.shadowSyncEchoes=[];
                roots[0].addEventListener('cfxsync',event=>shadowSyncEvents.push(event.detail));
                roots[1].addEventListener('cfxsync',event=>shadowSyncEchoes.push(event.detail));
                const style=document.querySelector('style[data-cfx-interactive-assets]')||document.querySelector('style');
                const host=document.createElement('div');host.id='shadow-sync-host';roots[0].before(host);
                host.attachShadow({mode}).append(style.cloneNode(true),roots[0]);
            }
            """, shadowMode);
        var position = await page.EvaluateAsync<double[]>("""
            selector => {
                const box=shadowSyncSource.querySelector(selector).getBoundingClientRect();
                return [box.x+box.width/2,box.y+box.height/2];
            }
            """, Point(1, 3));
        await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
        await page.WaitForFunctionAsync("()=>shadowSyncEvents.some(event=>event.action==='crosshair')", null,
            new PageWaitForFunctionOptions { Timeout = 5000 });
        var state = await page.EvaluateAsync<JsonElement>("""
            () => {
                const read=root=>({mode:root.dataset.cfxHoverMode,
                    opacity:[...root.querySelectorAll('[data-cfx-role="series"]')].map(node=>getComputedStyle(node).opacity)});
                return {source:read(shadowSyncSource),peer:read(shadowSyncPeer),events:shadowSyncEvents,echoes:shadowSyncEchoes};
            }
            """);
        await CaptureAsync(page, "shadow-crosshair-coherence-" + shadowMode, state);
        Assert.Equal("series", state.GetProperty("source").GetProperty("mode").GetString());
        var crosshairs = state.GetProperty("events").EnumerateArray().Where(item => item.GetProperty("action").GetString() == "crosshair").ToArray();
        Assert.NotEmpty(crosshairs);
        Assert.All(crosshairs, item => Assert.Equal("series", item.GetProperty("mode").GetString()));
        Assert.Equal("series", state.GetProperty("peer").GetProperty("mode").GetString());
        foreach (var chart in new[] { "source", "peer" })
            Assert.Equal(new[] { "0.3", "1", "0.3" }, state.GetProperty(chart).GetProperty("opacity").EnumerateArray().Select(item => item.GetString()));
        Assert.Empty(state.GetProperty("echoes").EnumerateArray());
        AssertNoConsoleErrors(session);
    }
}
