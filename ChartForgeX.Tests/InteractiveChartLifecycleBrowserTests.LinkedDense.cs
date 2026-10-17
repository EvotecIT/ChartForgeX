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

    [BrowserFact]
    public async Task DenseSynchronizedToggleReconcilesOneChangedIdentityWithBoundedNativeQueries() {
        if (!Enabled) return;
        var observations = Enumerable.Range(0, 5000).Select(index => (point: new ChartPoint(index % 100 + 1, index / 100 + 1), id: new ChartObservationId("dense-" + index))).ToArray();
        Chart Create(bool reverse) {
            var ordered = reverse ? observations.AsEnumerable().Reverse().ToArray() : observations;
            return Chart.Create().WithSize(800, 500).WithXAxisBounds(0, 101).WithYAxisBounds(0, 51).WithLegend(false)
                .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("dense"), "Readings", ordered.Select(item => item.point))
                    .WithObservationIds(ordered.Select(item => item.id)));
        }
        var html = new[] { Create(false), Create(true) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.GroupName = "dense-scoped-selection";
            options.Interaction.Features = ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation
                | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.StateBookmarks;
        });
        await using var session = await OpenAsync(html, 900, 1300);
        var page = session.Page;
        var prepared = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                window.densePeers=[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>ChartForgeXCharts.get(root));
                const peer=densePeers[1],first=peer.root.querySelector('[data-cfx-role=point]');
                peer.select({seriesKeys:['dense']});const sample=peer.getSelection()[0];
                const apply=targets=>peer.root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:{selectedTargets:targets},emit:false,sync:false}}));
                apply([{...sample,seriesKey:'unrelated'},{targetKind:'point',targetId:'missing'}]);const rejected=peer.getSelection().length;
                const second=peer.root.querySelector('[data-cfx-role=point][data-cfx-point="1"]');
                apply([sample,sample,{seriesKey:'dense',point:second.dataset.cfxPoint}]);const duplicateAndLegacy=peer.getSelection().map(target=>target.observationId);
                densePeers.forEach(controller=>controller.select({seriesKeys:['dense']}));
                const counts=densePeers.map(controller=>controller.getSelection().length);
                window.densePeerQueries=[0,0];window.densePeerChanges=[];window.densePeerSelections=[];
                window.densePeerSvgs=densePeers.map(controller=>controller.root.querySelector('.cfx-stage svg'));
                densePeers.forEach((controller,index)=>{
                    const original=controller.root.querySelectorAll.bind(controller.root);
                    controller.root.querySelectorAll=selector=>{const result=original(selector);if(result.length>=1000)densePeerQueries[index]++;return result};
                });
                document.body.addEventListener('cfxselectionchange',event=>densePeerChanges.push({root:event.target,detail:event.detail}));
                document.body.addEventListener('cfxselect',event=>densePeerSelections.push(event.detail));
                return {counts,rejected,duplicateAndLegacy,firstPeer:sample.observationId,firstSource:densePeers[0].getSelection()[0].observationId};
            }
            """);
        Assert.Equal(new[] { 5000, 5000 }, prepared.GetProperty("counts").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(0, prepared.GetProperty("rejected").GetInt32());
        Assert.Equal(new[] { "dense-4999", "dense-4998" }, prepared.GetProperty("duplicateAndLegacy").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("dense-4999", prepared.GetProperty("firstPeer").GetString());
        Assert.Equal("dense-0", prepared.GetProperty("firstSource").GetString());
        await page.Locator(Root).First.Locator("[data-cfx-observation-id=dense-0]").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        var result = await page.EvaluateAsync<JsonElement>("""
            ()=>({counts:densePeers.map(controller=>controller.getSelection().length),queries:densePeerQueries,
                missing:densePeers.map(controller=>!controller.getSelection().some(target=>target.observationId==='dense-0')),
                sameSvg:densePeers.every((controller,index)=>controller.root.querySelector('.cfx-stage svg')===densePeerSvgs[index]),
                changes:densePeerChanges.length,selections:densePeerSelections.length,
                sourceOnly:densePeerChanges.every(event=>event.root===densePeers[0].root),
                targetCount:densePeerChanges.at(-1).detail.targets.length,readouts:document.querySelectorAll('.cfx-tooltip:not([hidden])').length})
            """);
        Assert.Equal(new[] { 4999, 4999 }, result.GetProperty("counts").EnumerateArray().Select(item => item.GetInt32()));
        Assert.All(result.GetProperty("queries").EnumerateArray(), value => Assert.InRange(value.GetInt32(), 1, 100));
        Assert.All(result.GetProperty("missing").EnumerateArray(), value => Assert.True(value.GetBoolean()));
        Assert.True(result.GetProperty("sameSvg").GetBoolean());
        Assert.Equal(1, result.GetProperty("changes").GetInt32());
        Assert.Equal(1, result.GetProperty("selections").GetInt32());
        Assert.True(result.GetProperty("sourceOnly").GetBoolean());
        Assert.Equal(4999, result.GetProperty("targetCount").GetInt32());
        Assert.Equal(0, result.GetProperty("readouts").GetInt32());
        await CaptureAsync(page, "linked-dense-synchronized-toggle", new { prepared, result });
        AssertNoConsoleErrors(session);
    }
}
