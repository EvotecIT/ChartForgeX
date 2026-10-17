using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserFact]
    public async Task DisposedConnectedPeerIgnoresSynchronizedSelectionUntilFreshMount() {
        if (!Enabled) return;
        var html = new[] { Readings().WithTitle("Selection source"), Readings().WithTitle("Selection peer") }
            .ToInteractiveHtmlDashboardPage(options => {
                options.Columns = 1;
                options.Interaction.GroupName = "chart-lifecycle-peers";
                options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                    | ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.SynchronizedCharts;
            });
        await using var session = await OpenAsync(html, 800, 1000);
        var page = session.Page;
        var roots = page.Locator(Root); var source = roots.Nth(0); var peer = roots.Nth(1);
        await peer.EvaluateAsync("root=>{window.disposedPeerController=ChartForgeXCharts.mount(root);disposedPeerController.dispose();}");
        Assert.True(await peer.EvaluateAsync<bool>("root=>root.isConnected"));
        await source.Locator(North).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await source.Locator(North).GetAttributeAsync("aria-selected"));
        Assert.Equal(0, await peer.Locator("[aria-selected=true]").CountAsync());
        await CaptureAsync(page, "synchronized-peer-disposed");
        Assert.True(await peer.EvaluateAsync<bool>("""
            root=>{
                const fresh=ChartForgeXCharts.mount(root);
                window.peerSelectionEvents=[];
                root.addEventListener('cfxselect',event=>peerSelectionEvents.push(event.detail));
                return fresh!==disposedPeerController;
            }
            """));
        const string south = "g[data-cfx-role=point][data-cfx-observation-id=south]";
        await source.Locator(south).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await peer.Locator(south).GetAttributeAsync("aria-selected"));
        // A fresh mount receives the current whole set, including the selection made while it was disposed.
        Assert.Equal("true", await peer.Locator(North).GetAttributeAsync("aria-selected"));
        Assert.Equal(new[] { "north", "south" }, await peer.EvaluateAsync<string[]>(
            "root=>ChartForgeXCharts.get(root).getSelection().map(target=>target.observationId)"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>peerSelectionEvents.length"));
        await peer.Locator(south).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>peerSelectionEvents.length"));
        Assert.Equal("south", await page.EvaluateAsync<string>("()=>peerSelectionEvents[0].target.observationId"));
        Assert.Equal(new[] { "north" }, await peer.EvaluateAsync<string[]>(
            "root=>ChartForgeXCharts.get(root).getSelection().map(target=>target.observationId)"));
        await CaptureAsync(page, "synchronized-peer-remounted");
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthoredObservationIdentitySurvivesOnlyWithinAnExplicitSeriesIdentity(bool explicitSeries) {
        if (!Enabled) return;
        Chart Create(bool updated) {
            var values = new[] { new ChartBubble(1, updated ? 17 : 5, 25), new ChartBubble(2, 9, 9) };
            var chart = Chart.Create().WithSize(640, 360).WithTitle("Series identity provenance").WithLegend();
            if (explicitSeries) chart.AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("series-1"), "Samples", values));
            else chart.AddBubble("Samples", values);
            chart.Series[0].WithObservationId(0, new ChartObservationId("north"));
            return chart;
        }
        var initial = Create(false); var updated = Create(true);
        // Independently authored charts can have the same automatic ordinal; metadata still marks it local.
        Assert.Equal(initial.Series[0].Id, updated.Series[0].Id);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage());
        var page = session.Page;
        await BindControllerAsync(page);
        await SelectAsync(page, North);
        await page.Locator(Legend(0)).ClickAsync();
        await UpdateAsync(page, updated.ToInteractiveHtmlFragmentWithoutAssets());
        Assert.Equal("17", await page.Locator(North).GetAttributeAsync("data-cfx-y"));
        Assert.Equal(explicitSeries ? "shared" : "local", await page.Locator(North).GetAttributeAsync("data-cfx-series-scope"));
        Assert.Equal(explicitSeries, await page.Locator(North).EvaluateAsync<bool>("node=>node.getAttribute('aria-selected')==='true'"));
        Assert.Equal(explicitSeries, await page.Locator(Legend(0)).EvaluateAsync<bool>("node=>node.dataset.cfxMuted==='true'"));
        Assert.NotEqual("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await CaptureAsync(page, "series-provenance-" + (explicitSeries ? "explicit" : "automatic"));
        AssertNoConsoleErrors(session);
    }
}
