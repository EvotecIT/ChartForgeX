using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AcceptedNativeResetPublishesTheCommittedSelectionAndClearsSynchronizedPeers(bool dark, bool selected) {
        if (!Enabled) return;
        var chart = NativeLinkedChart(dark);
        var initial = NativeLinkedSection(chart, new ChartXViewport(2, 8));
        var full = NativeLinkedSection(chart);
        var peer = NativeLinkedSection(chart, id: "linked-native-peer");
        await using var session = await OpenAsync(NativeLinkedPage(initial, peer, dark), 820, 1000);
        var page = session.Page;
        await BindNativeLinkedProbeAsync(page, selected);
        await page.Locator("#native-source [data-cfx-reset]").ClickAsync();
        await page.WaitForFunctionAsync("()=>nativeControlled.length===1");
        Assert.Equal(selected ? 1 : 0, await page.EvaluateAsync<int>("()=>chartController.getSelection().length"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>peerController.getSelection().length"));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>nativeReceipts.filter(item=>item.name==='cfxreset').length"));
        await page.EvaluateAsync("html=>nativeControlled[0].resolve(html)", full);
        await page.WaitForFunctionAsync("()=>nativeReceipts.some(item=>item.name==='cfxviewportchange')");
        var result = await NativeLinkedResultAsync(page);
        Assert.Equal(0, result.GetProperty("selected").GetInt32());
        Assert.Equal(0, result.GetProperty("peerSelected").GetInt32());
        Assert.Equal(0, result.GetProperty("linkedFilter").GetInt32());
        Assert.Equal(0, result.GetProperty("candidates").GetInt32());
        Assert.False(result.GetProperty("aborted").GetBoolean());
        var events = result.GetProperty("events").EnumerateArray().ToArray();
        Assert.Single(events, item => item.GetProperty("name").GetString() == "cfxreset");
        var selectionEvents = events.Where(item => item.GetProperty("name").GetString() == "cfxselectionchange").ToArray();
        Assert.Equal(selected ? 2 : 0, selectionEvents.Length);
        if (selected) {
            Assert.Equal("reset", selectionEvents[^1].GetProperty("detail").GetProperty("reason").GetString());
            Assert.Empty(selectionEvents[^1].GetProperty("detail").GetProperty("targets").EnumerateArray());
        }
        await CaptureAsync(page, "linked-native-reset-" + dark + "-" + selected, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("rejected")]
    [InlineData("invalid-adaptive")]
    [InlineData("canceled")]
    public async Task FailedOrCanceledNativeResetRetainsSelectionAndSettlesTheHostCandidate(string outcome) {
        if (!Enabled) return;
        var chart = NativeLinkedChart(false);
        await using var session = await OpenAsync(NativeLinkedPage(NativeLinkedSection(chart, new ChartXViewport(2, 8)),
            NativeLinkedSection(chart, id: "linked-native-peer"), false), 820, 1000);
        var page = session.Page;
        await BindNativeLinkedProbeAsync(page, true);
        var revision = await page.EvaluateAsync<string>("()=>chartController.sceneRevision");
        await page.Locator("#native-source [data-cfx-reset]").ClickAsync();
        await page.WaitForFunctionAsync("()=>nativeControlled.length===1");
        if (outcome == "rejected") {
            await page.EvaluateAsync("()=>nativeControlled[0].reject(new Error('Host rejected this candidate'))");
        } else if (outcome == "invalid-adaptive") {
            await page.EvaluateAsync("""
                html=>{const template=document.createElement('template');template.innerHTML=html;
                    template.content.firstElementChild.querySelector('template[data-cfx-adaptive-narrow]').content.firstElementChild.removeAttribute('data-cfx-prepared-chart');
                    nativeControlled[0].resolve(template.content.firstElementChild.outerHTML)}
                """, NativeLinkedSection(chart, adaptive: true));
        } else {
            await page.EvaluateAsync("html=>{chartController.connectXViewport(null);nativeControlled[0].resolve(html)}", NativeLinkedSection(chart));
        }
        if (outcome != "canceled") await page.WaitForFunctionAsync("()=>nativeReceipts.some(item=>item.name==='cfxviewporterror')");
        else await page.WaitForFunctionAsync("()=>nativeControlled[0].signal.aborted&&nativeCandidates.size===0");
        var result = await NativeLinkedResultAsync(page);
        Assert.Equal(1, result.GetProperty("selected").GetInt32());
        Assert.Equal(1, result.GetProperty("peerSelected").GetInt32());
        Assert.Equal(1, result.GetProperty("linkedFilter").GetInt32());
        Assert.Equal(0, result.GetProperty("candidates").GetInt32());
        Assert.Equal(revision, await page.EvaluateAsync<string>("()=>chartController.sceneRevision"));
        Assert.Equal(outcome == "canceled", result.GetProperty("aborted").GetBoolean());
        var events = result.GetProperty("events").EnumerateArray().ToArray();
        Assert.Single(events, item => item.GetProperty("name").GetString() == "cfxselectionchange");
        Assert.DoesNotContain(events, item => item.GetProperty("name").GetString() is "cfxreset" or "cfxviewportchange");
        Assert.Equal(outcome == "canceled" ? 0 : 1, events.Count(item => item.GetProperty("name").GetString() == "cfxviewporterror"));
        await CaptureAsync(page, "linked-native-reset-" + outcome, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ReplacementRestorationHonorsSingleSelectionForOrdinaryAndNativeCommits(bool dark, bool native) {
        if (!Enabled) return;
        var chart = NativeLinkedChart(dark);
        await using var session = await OpenAsync(NativeLinkedPage(NativeLinkedSection(chart, native ? new ChartXViewport(2, 8) : null), null, dark), 820, 700);
        var page = session.Page;
        await BindNativeLinkedProbeAsync(page, false);
        var ids = native ? new[] { "middle", "last" } : new[] { "first", "middle", "last" };
        await page.EvaluateAsync("ids=>{chartController.select({observationIds:ids});window.nativeSelectionSnapshot=chartController.getSelection()}", ids);
        Assert.Equal(ids.Length, await page.EvaluateAsync<int>("()=>chartController.getSelection().length"));
        var replacement = NativeLinkedSection(NativeLinkedChart(dark, removeFirst: !native), native ? new ChartXViewport(2.6, 7.4) : null,
            ChartSelectionMode.Single);
        if (native) {
            await page.Locator("#native-source [data-cfx-zoom=in]").ClickAsync();
            await page.WaitForFunctionAsync("()=>nativeControlled.length===1");
            await page.EvaluateAsync("html=>nativeControlled[0].resolve(html)", replacement);
            await page.WaitForFunctionAsync("()=>nativeReceipts.some(item=>item.name==='cfxviewportchange')");
        } else await page.EvaluateAsync("html=>chartController.update(html)", replacement);
        var result = await page.EvaluateAsync<JsonElement>("""
            ()=>{const selected=chartController.getSelection();return{count:selected.length,id:selected[0]?.observationId,
                mode:chartController.root.dataset.cfxSelectionMode,selectionEvents:nativeReceipts.filter(item=>item.name==='cfxselectionchange').length}}
            """);
        Assert.Equal("single", result.GetProperty("mode").GetString());
        Assert.Equal(1, result.GetProperty("count").GetInt32());
        Assert.Equal("middle", result.GetProperty("id").GetString());
        Assert.Equal(0, result.GetProperty("selectionEvents").GetInt32());
        await CaptureAsync(page, "linked-single-restoration-" + dark + "-" + native, result);
        // The same owner must enforce the limit when a bookmark supplies several surviving targets.
        await page.EvaluateAsync("""
            ()=>chartController.root.dispatchEvent(new CustomEvent('cfx-apply-state',{
                detail:{snapshot:{selectedTargets:nativeSelectionSnapshot},emit:false,sync:false}}))
            """);
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>chartController.getSelection().length"));
        AssertNoConsoleErrors(session);
    }

    private static Chart NativeLinkedChart(bool dark, bool removeFirst = false) {
        var points = new[] { new ChartPoint(0, 2), new ChartPoint(4, 5), new ChartPoint(10, 3) };
        var ids = new[] { "first", "middle", "last" };
        var series = ChartSeries.CreateLine(new ChartSeriesId("readings"), "Readings", removeFirst ? points.Skip(1) : points)
            .WithObservationIds((removeFirst ? ids.Skip(1) : ids).Select(id => new ChartObservationId(id)));
        series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; });
        return Chart.Create().WithSize(640, 360).WithXAxisBounds(0, 10).WithYAxisBounds(0, 8)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).AddSeries(series);
    }

    private static string NativeLinkedSection(Chart chart, ChartXViewport? viewport = null,
        ChartSelectionMode mode = ChartSelectionMode.Toggle, bool adaptive = false, string id = "linked-native-source") =>
        new HtmlInteractiveChartRenderer().PrepareSection(chart, viewport == null ? null : new ChartCartesianViewport(x: viewport), options => {
            Fit(options); options.Interaction.ChartId = id; options.Interaction.GroupName = "linked-native-contract";
            options.Interaction.SelectionMode = mode;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Selection
                | ChartInteractionFeatures.SynchronizedCharts | ChartInteractionFeatures.StateBookmarks);
            options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
            options.Highlight.DimOthersWhileSelected = true;
            if (adaptive) options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
        }).Html;

    private static string NativeLinkedPage(string source, string? peer, bool dark) =>
        "<!doctype html><html><head><style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style></head><body style='background:"
        + (dark ? "#0b0d0f" : "#f3f4f6") + "'><output id='native-filter' style='color:" + (dark ? "white" : "black")
        + "'></output><div id='native-source'>" + source + "</div>" + (peer == null ? "" : "<div id='native-peer'>" + peer + "</div>")
        + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script></body></html>";

    private static Task BindNativeLinkedProbeAsync(Microsoft.Playwright.IPage page, bool selected) => page.EvaluateAsync("""
        selected=>{
            window.chartController=ChartForgeXCharts.get(document.querySelector('#native-source .cfx-interactive-chart'));
            window.peerController=ChartForgeXCharts.get(document.querySelector('#native-peer .cfx-interactive-chart'));
            window.nativeControlled=[];window.nativeReceipts=[];window.nativeCandidates=new Set();window.nativeLinkedFilter=0;
            for(const name of ['cfxselectionchange','cfxreset','cfxviewportchange','cfxviewporterror'])document.body.addEventListener(name,event=>{
                if(event.detail.chartId!=='linked-native-source')return;
                nativeReceipts.push({name,detail:event.detail});
                if(name==='cfxselectionchange'){nativeLinkedFilter=event.detail.targets.length;document.querySelector('#native-filter').textContent='Linked filter targets: '+nativeLinkedFilter;}
                if(name==='cfxviewportchange'||name==='cfxviewporterror')nativeCandidates.delete(event.detail.requestId);
            });
            chartController.connectXViewport((request,signal)=>new Promise((resolve,reject)=>{
                nativeCandidates.add(request.requestId);signal.addEventListener('abort',()=>nativeCandidates.delete(request.requestId));
                nativeControlled.push({request,signal,resolve,reject});
            }));
            if(peerController)peerController.select({observationIds:['middle']});
            if(selected)chartController.select({observationIds:['middle']},{emit:true});
        }
        """, selected);

    private static Task<JsonElement> NativeLinkedResultAsync(Microsoft.Playwright.IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>JSON.parse(JSON.stringify({selected:chartController.getSelection().length,peerSelected:peerController?.getSelection().length??0,
            linkedFilter:nativeLinkedFilter,events:nativeReceipts,candidates:nativeCandidates.size,aborted:nativeControlled[0]?.signal.aborted??false}))
        """);
}
