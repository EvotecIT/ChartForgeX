using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserFact]
    public async Task ReadOnlyTargetResolutionIntersectsIndependentAggregateContributorsWithoutMutatingLinkedState() {
        if (!Enabled) return;
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("durations"), "Duration", new[] { .2, .4, 1.5 },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1, 2 }))
            .WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second"), new ChartObservationId("third") });
        await using var session = await OpenAsync(Chart.Create().WithSize(640, 360).AddSeries(series).ToInteractiveHtmlPage(options => {
            Fit(options); options.Highlight.DimOthersWhileSelected = true;
            options.Interaction.Enable(ChartInteractionFeatures.SynchronizedCharts);
            options.Interaction.GroupName = "target-resolution";
        }), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var result = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const controller=chartController,root=controller.root;
                controller.select({observationIds:['third']});
                controller.highlight({observationIds:['third']},{dim:true,source:'existing'});
                const events=[];
                for(const name of ['cfxselect','cfxselectionchange','cfxhighlight','cfxhighlightclear','cfxsync','cfxstate'])
                    root.addEventListener(name,()=>events.push(name));
                const observer=new MutationObserver(()=>{});observer.observe(root,{subtree:true,attributes:true,childList:true,characterData:true});
                const before=root.outerHTML,selection=controller.getSelection(),highlight=JSON.stringify(root._cfxHostHighlight);
                const all=controller.resolveTargets(),first=controller.resolveTargets({sourcePoints:[{seriesKey:'durations',index:0}]}),
                    second=controller.resolveTargets({sourcePoints:[{seriesKey:'durations',index:1}]}),
                    key=target=>JSON.stringify([target.targetKind,target.targetId,target.seriesKey]),keys=new Set(second.map(key)),
                    intersection=first.filter(target=>keys.has(key(target))),snapshot=JSON.parse(JSON.stringify(intersection[0]));
                first[0].observationIds[0]='mutated';first[0].sourcePoints.push(99);first[0].range[0]=-100;first[0].targetId='mutated';
                const fresh=controller.resolveTargets({observationIds:['first']}),empty=controller.resolveTargets({}),
                    missing=controller.resolveTargets({observationIds:['missing']}),unchanged=before===root.outerHTML,
                    sameSelection=JSON.stringify(selection)===JSON.stringify(controller.getSelection()),
                    sameHighlight=highlight===JSON.stringify(root._cfxHostHighlight),mutations=observer.takeRecords().length;
                observer.disconnect();
                const queryEvents=[...events];controller.highlight({targets:[snapshot]},{dim:true});
                return{all,snapshot,fresh,empty,missing,unchanged,sameSelection,sameHighlight,mutations,queryEvents,
                    highlighted:[...root.querySelectorAll('[data-cfx-highlighted=true]')].map(node=>node.dataset.cfxTargetId),
                    selected:controller.getSelection(),announcement:root.querySelector('[data-cfx-live]').textContent};
            }
            """);
        await CaptureAsync(page, "read-only-target-resolution-aggregate", result);
        Assert.Equal(2, result.GetProperty("all").GetArrayLength());
        var target = result.GetProperty("snapshot");
        Assert.Equal("point", target.GetProperty("targetKind").GetString());
        Assert.Equal("durations", target.GetProperty("seriesKey").GetString());
        Assert.Equal(new[] { 0, 1 }, target.GetProperty("sourcePoints").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(new[] { "first", "second" }, target.GetProperty("observationIds").EnumerateArray().Select(item => item.GetString()));
        Assert.False(target.TryGetProperty("observationId", out _));
        var fresh = Assert.Single(result.GetProperty("fresh").EnumerateArray());
        foreach (var property in new[] { "targetKind", "targetId", "id", "role", "label", "rangeLabel", "series", "seriesKey",
            "seriesScope", "observationId", "category", "point", "sourcePoint", "derived", "groupId", "legendTargetKind", "legendTargetId", "value", "kind" }) {
            var present = target.TryGetProperty(property, out var expected);
            Assert.Equal(present, fresh.TryGetProperty(property, out var actual));
            if (present) Assert.Equal(expected.GetString(), actual.GetString());
        }
        foreach (var property in new[] { "x", "numericValue", "groupIndex", "sourceCount" }) {
            var present = target.TryGetProperty(property, out var expected);
            Assert.Equal(present, fresh.TryGetProperty(property, out var actual));
            if (present) Assert.Equal(expected.GetDouble(), actual.GetDouble());
        }
        Assert.Equal(target.GetProperty("observationIds").EnumerateArray().Select(item => item.GetString()),
            fresh.GetProperty("observationIds").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(target.GetProperty("sourcePoints").EnumerateArray().Select(item => item.GetInt32()),
            fresh.GetProperty("sourcePoints").EnumerateArray().Select(item => item.GetInt32()));
        Assert.Equal(target.GetProperty("range").EnumerateArray().Select(item => item.GetDouble()),
            fresh.GetProperty("range").EnumerateArray().Select(item => item.GetDouble()));
        Assert.Empty(result.GetProperty("empty").EnumerateArray());
        Assert.Empty(result.GetProperty("missing").EnumerateArray());
        Assert.True(result.GetProperty("unchanged").GetBoolean());
        Assert.True(result.GetProperty("sameSelection").GetBoolean());
        Assert.True(result.GetProperty("sameHighlight").GetBoolean());
        Assert.Equal(0, result.GetProperty("mutations").GetInt32());
        Assert.Empty(result.GetProperty("queryEvents").EnumerateArray());
        Assert.Equal(target.GetProperty("targetId").GetString(), Assert.Single(result.GetProperty("highlighted").EnumerateArray()).GetString());
        Assert.Equal(new[] { "third" }, Assert.Single(result.GetProperty("selected").EnumerateArray())
            .GetProperty("observationIds").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("1 marks highlighted", result.GetProperty("announcement").GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task TargetResolutionWorksWithoutSelectionAndReadsTheCurrentMountedScene() {
        if (!Enabled) return;
        void Configure(HtmlChartInteractionOptions options) { Fit(options); options.Interaction.Disable(ChartInteractionFeatures.Selection); }
        await using var session = await OpenAsync(LinkedBars().ToInteractiveHtmlPage(Configure), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var initial = await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const root=chartController.root,before=root.outerHTML;
                const all=chartController.resolveTargets(),north=chartController.resolveTargets({observationIds:['north']});
                window.resolutionSnapshot=north[0];
                const invalid=[null,[],{targets:{}},{sourcePoints:0}].map(match=>{
                    try{chartController.resolveTargets(match);return false;}catch(error){return error instanceof TypeError;}});
                return{all,north,invalid,unchanged:before===root.outerHTML,selection:chartController.getSelection(),events:linkedEvents.length};
            }
            """);
        Assert.Equal(3, initial.GetProperty("all").GetArrayLength());
        Assert.Equal("north", Assert.Single(initial.GetProperty("north").EnumerateArray()).GetProperty("observationId").GetString());
        Assert.All(initial.GetProperty("invalid").EnumerateArray(), item => Assert.True(item.GetBoolean()));
        Assert.True(initial.GetProperty("unchanged").GetBoolean());
        Assert.Empty(initial.GetProperty("selection").EnumerateArray());
        Assert.Equal(0, initial.GetProperty("events").GetInt32());
        await UpdateAsync(page, HighlightLifecycleChart(true, true).ToInteractiveHtmlFragmentWithoutAssets(Configure));
        var current = await page.EvaluateAsync<JsonElement>("""
            ()=>({all:chartController.resolveTargets(),north:chartController.resolveTargets({observationIds:['north']})[0],old:resolutionSnapshot})
            """);
        Assert.Equal(2, current.GetProperty("all").GetArrayLength());
        Assert.Equal(99, current.GetProperty("north").GetProperty("numericValue").GetDouble());
        Assert.Equal(34, current.GetProperty("old").GetProperty("numericValue").GetDouble());
        await CaptureAsync(page, "read-only-target-resolution-no-selection", current);
        var lifetime = await page.EvaluateAsync<bool[]>("""
            ()=>{
                const root=chartController.root;
                const rejects=controller=>{try{controller.resolveTargets();return false;}catch(error){return error.message.includes('disposed');}};
                chartController.dispose();const disposed=rejects(chartController),mounted=ChartForgeXCharts.mount(root);
                const remounted=mounted.resolveTargets().length===2;root.remove();const detached=rejects(mounted);
                return[disposed,remounted,detached];
            }
            """);
        Assert.All(lifetime, item => Assert.True(item));
        AssertNoConsoleErrors(session);
    }
}
