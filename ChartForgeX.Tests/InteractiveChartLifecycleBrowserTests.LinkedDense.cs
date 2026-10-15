using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserFact]
    public async Task DenseLassoPublishesOneCompleteSelectionWithoutRebuildingTheSceneOrThousandsOfReadouts() {
        if (!Enabled) return;
        var points = Enumerable.Range(0, 5000).Select(index => new ChartPoint(index % 100 + 1, index / 100 + 1));
        var chart = Chart.Create().WithSize(800, 500).WithXAxisBounds(0, 101).WithYAxisBounds(0, 51)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("dense"), "Readings", points));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush); options.Tooltip.PinOnSelect = false;
        }), 860, 680);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var positions = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>{
                window.denseSvg=root.querySelector('.cfx-stage svg');window.denseRevision=chartController.sceneRevision;
                const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                const screen=(x,y)=>{const point=new DOMPoint(plot.x+x*plot.width,plot.y+y*plot.height).matrixTransform(denseSvg.getScreenCTM());return{x:point.x,y:point.y}};
                return{from:screen(-.02,-.02),to:screen(1.02,1.02)};
            }
            """);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("from").GetProperty("x").GetDouble(), (float)positions.GetProperty("from").GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await page.EvaluateAsync("""
            ()=>{window.denseReadouts=0;window.denseObserver=new MutationObserver(records=>denseReadouts+=records.filter(record=>record.type==='childList').length);
                denseObserver.observe(chartController.root.querySelector('.cfx-tooltip'),{childList:true,subtree:true})}
            """);
        await page.Mouse.MoveAsync((float)positions.GetProperty("to").GetProperty("x").GetDouble(), (float)positions.GetProperty("to").GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 5 });
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>linkedEvents.length"));
        await page.Mouse.UpAsync();
        var result = await page.EvaluateAsync<JsonElement>("""
            ()=>{denseObserver.disconnect();return{sameSvg:denseSvg===chartController.root.querySelector('.cfx-stage svg'),sameRevision:denseRevision===chartController.sceneRevision,
                events:linkedEvents.length,selected:linkedEvents[0].detail.targets.length,readouts:denseReadouts,
                wrappers:linkedEvents[0].detail.targets.filter(target=>target.targetKind==='series').length,chips:chartController.root.querySelectorAll('.cfx-compare-chip').length}}
            """);
        Assert.True(result.GetProperty("sameSvg").GetBoolean());
        Assert.True(result.GetProperty("sameRevision").GetBoolean());
        Assert.Equal(1, result.GetProperty("events").GetInt32());
        Assert.Equal(5000, result.GetProperty("selected").GetInt32());
        Assert.Equal(0, result.GetProperty("readouts").GetInt32());
        Assert.Equal(0, result.GetProperty("wrappers").GetInt32());
        Assert.Equal(6, result.GetProperty("chips").GetInt32());
        await CaptureAsync(page, "linked-dense-lasso", result);
        AssertNoConsoleErrors(session);
    }
}
