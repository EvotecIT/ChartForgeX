using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserFact]
    public async Task FullReplacementClearsChartLocalAndPositionalHighlightMatches() {
        if (!Enabled) return;
        var initial = HighlightLifecycleChart(false, false);
        var replacement = HighlightLifecycleChart(false, true);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Fit), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var result = await page.EvaluateAsync<JsonElement>("""
            ({initial,next})=>{
                const results=[];
                for(const form of ['series','target','source','observation','range']) {
                    chartController.update(initial);
                    const key=JSON.parse(chartController.root.dataset.cfxPreparedChart).series[0].key;
                    chartController.select({seriesKeys:[key]});const target=chartController.getSelection()[0];chartController.clearSelection();
                    const match=form==='series'?{seriesKeys:[key]}:form==='target'?{targets:[target]}
                        :form==='source'?{sourcePoints:[{seriesKey:key,index:0}]}:form==='observation'?{observationIds:['north'],seriesKey:key}
                        :{ranges:[[0,0]],seriesKey:key};
                    chartController.highlight(match,{dim:true});
                    const before=chartController.root.querySelectorAll('.cfx-highlighted').length;
                    chartController.update(next);
                    results.push({form,before,after:chartController.root.querySelectorAll('.cfx-highlighted,.cfx-linked-dim').length});
                }
                return{results,events:linkedEvents.length,selection:chartController.getSelection().length};
            }
            """, new { initial = initial.ToInteractiveHtmlFragmentWithoutAssets(Fit), next = replacement.ToInteractiveHtmlFragmentWithoutAssets(Fit) });
        foreach (var item in result.GetProperty("results").EnumerateArray()) {
            Assert.True(item.GetProperty("before").GetInt32() > 0, item.GetProperty("form").GetString());
            Assert.Equal(0, item.GetProperty("after").GetInt32());
        }
        Assert.Equal(0, result.GetProperty("events").GetInt32());
        Assert.Equal(0, result.GetProperty("selection").GetInt32());
        await CaptureAsync(page, "linked-highlight-local-replacement", result);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task FullReplacementRetainsAuthoredHighlightIdentitiesAndValueFilterIntent() {
        if (!Enabled) return;
        var initial = HighlightLifecycleChart(true, false);
        var replacement = HighlightLifecycleChart(true, true);
        await using var session = await OpenAsync(initial.ToInteractiveHtmlPage(Fit), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var result = await page.EvaluateAsync<JsonElement>("""
            ({initial,next})=>{
                const results=[];
                for(const form of ['target','observation','series','category','range','source']) {
                    chartController.update(initial);chartController.select({observationIds:['north']});
                    const target=chartController.getSelection()[0];chartController.clearSelection();
                    const match=form==='target'?{targets:[target]}:form==='observation'?{observationIds:['north'],seriesKey:'orders'}
                        :form==='series'?{seriesKeys:['orders']}:form==='category'?{categories:['North']}
                        :form==='range'?{ranges:[[.5,1.5]]}:{sourcePoints:[{seriesKey:'orders',index:0}]};
                    chartController.highlight(match,{dim:true});chartController.update(next);
                    const marks=[...chartController.root.querySelectorAll('.cfx-highlighted')];
                    results.push({form,ids:marks.map(node=>node.dataset.cfxObservationId),values:marks.map(node=>Number(node.dataset.cfxValue??node.dataset.cfxY))});
                }
                return{results,events:linkedEvents.length,selection:chartController.getSelection().length};
            }
            """, new { initial = initial.ToInteractiveHtmlFragmentWithoutAssets(Fit), next = replacement.ToInteractiveHtmlFragmentWithoutAssets(Fit) });
        foreach (var item in result.GetProperty("results").EnumerateArray()) {
            var form = item.GetProperty("form").GetString();
            var ids = item.GetProperty("ids").EnumerateArray().Select(value => value.GetString()).ToArray();
            Assert.Equal(form == "source" ? Array.Empty<string>() : form == "series" ? new[] { "north", "south" } : new[] { "north" }, ids);
            if (ids.Length == 1) Assert.Equal(99, item.GetProperty("values")[0].GetDouble());
        }
        Assert.Equal(0, result.GetProperty("events").GetInt32());
        Assert.Equal(0, result.GetProperty("selection").GetInt32());
        await CaptureAsync(page, "linked-highlight-authored-replacement", result);
        AssertNoConsoleErrors(session);
    }

    private static Chart HighlightLifecycleChart(bool shared, bool updated) {
        var points = new[] { new ChartPoint(updated ? 1 : 0, updated ? 99 : 34), new ChartPoint(updated ? 0 : 1, updated ? 77 : 52) };
        var chart = Chart.Create().WithSize(640, 360).WithXLabels(new[] {
            new ChartAxisLabel(0, updated ? "South" : "North"), new ChartAxisLabel(1, updated ? "North" : "South") });
        if (shared) chart.AddSeries(ChartSeries.CreateBar(new ChartSeriesId("orders"), "Orders", points));
        else chart.AddBar(updated ? "Unrelated readings" : "Original readings", points);
        chart.Series[0].WithObservationIds(new[] { new ChartObservationId("north"), new ChartObservationId("south") });
        return chart;
    }
}
