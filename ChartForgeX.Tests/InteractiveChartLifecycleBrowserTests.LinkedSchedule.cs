using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Timeline)]
    [InlineData(ChartSeriesKind.Gantt)]
    [InlineData(ChartSeriesKind.StateTimeline)]
    [InlineData(ChartSeriesKind.GanttLane)]
    public async Task ScheduleBrushUsesItsPreparedTimeMapAndLeavesOutFictionalLaneValues(ChartSeriesKind kind) {
        if (!Enabled) return;
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc); var end = start.AddDays(10);
        var chart = LinkedSchedule(kind, start, end);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush); options.Interaction.BrushAxis = ChartBrushAxis.X;
        }), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var positions = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>{
                window.scheduleBrush=[];document.body.addEventListener('cfxbrush',event=>scheduleBrush.push(event.detail));
                const metadata=JSON.parse(root.dataset.cfxPreparedChart),svg=root.querySelector('.cfx-stage svg'),p=metadata.dataProjection.plotBounds;
                const screen=(x,y)=>{const point=new DOMPoint(p.x+x*p.width,p.y+y*p.height).matrixTransform(svg.getScreenCTM());return{x:point.x,y:point.y}};
                return {from:screen(.2,.4),to:screen(.8,.6),hasY:metadata.dataProjection.y!==undefined};
            }
            """);
        Assert.False(positions.GetProperty("hasY").GetBoolean());
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("from").GetProperty("x").GetDouble(), (float)positions.GetProperty("from").GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("to").GetProperty("x").GetDouble(), (float)positions.GetProperty("to").GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 4 });
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(resolve))");
        var expected = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>{
                const projection=JSON.parse(root.dataset.cfxPreparedChart).dataProjection,axis=projection.x,p=projection.plotBounds;
                const inverse=root.querySelector('.cfx-stage svg').getScreenCTM().inverse(),rect=root.querySelector('.cfx-brush-box').getBoundingClientRect();
                const raw=[new DOMPoint(rect.left,rect.top),new DOMPoint(rect.right,rect.bottom)]
                    .map(point=>axis.minimum+(point.matrixTransform(inverse).x-p.x)/p.width*(axis.maximum-axis.minimum)).sort((a,b)=>a-b);
                const ticks=[...axis.displayTicks,...axis.displayBounds].filter(tick=>tick.value>=axis.minimum&&tick.value<=axis.maximum).sort((a,b)=>a.value-b.value);
                const enclosing=[ticks.filter(tick=>tick.value<=raw[0]).at(-1),ticks.find(tick=>tick.value>=raw[1])];
                return {range:enclosing.map(tick=>tick.instant),captions:enclosing.map(tick=>tick.text)};
            }
            """);
        await page.Mouse.UpAsync();
        var result = await page.EvaluateAsync<JsonElement>("()=>JSON.parse(JSON.stringify({brush:scheduleBrush[0],selection:chartController.getSelection()}))");
        var brush = result.GetProperty("brush");
        var range = brush.GetProperty("range");
        Assert.False(range.TryGetProperty("y", out _));
        Assert.Equal(expected.GetProperty("range").EnumerateArray().Select(endpoint => endpoint.GetString()),
            range.GetProperty("x").EnumerateArray().Select(endpoint => endpoint.GetString()));
        Assert.Equal(range.GetProperty("x").GetRawText(), brush.GetProperty("displayRange").GetProperty("x").GetRawText());
        Assert.Equal(string.Join(" – ", expected.GetProperty("captions").EnumerateArray().Select(caption => caption.GetString())), brush.GetProperty("rangeLabel").GetString());
        Assert.InRange(DateTimeOffset.Parse(range.GetProperty("x")[0].GetString()!).UtcDateTime, start, start.AddDays(2).AddMinutes(1));
        Assert.InRange(DateTimeOffset.Parse(range.GetProperty("x")[1].GetString()!).UtcDateTime, start.AddDays(8).AddMinutes(-1), end);
        var selected = Assert.Single(result.GetProperty("selection").EnumerateArray());
        Assert.Equal("point", selected.GetProperty("targetKind").GetString());
        Assert.Equal(start, DateTimeOffset.Parse(selected.GetProperty("range")[0].GetString()!).UtcDateTime);
        Assert.Equal(kind == ChartSeriesKind.StateTimeline ? end.AddTicks(-1) : end,
            DateTimeOffset.Parse(selected.GetProperty("range")[1].GetString()!).UtcDateTime);
        await CaptureAsync(page, "linked-schedule-brush-" + kind, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("2026-03-11T00:00:00.123Z")]
    [InlineData("1970-01-01T00:00:00.123Z")]
    [InlineData("1969-12-31T00:00:00.123Z")]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task StateIntervalClosedDateRangeExcludesItsExclusiveEndAcrossTheUnixEpoch(string instant) {
        if (!Enabled) return;
        var end = DateTimeOffset.Parse(instant).UtcDateTime;
        var chart = LinkedSchedule(ChartSeriesKind.StateTimeline, end.AddDays(-1), end);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Fit), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var result = await page.EvaluateAsync<JsonElement>("""
            instant=>{chartController.select({seriesKeys:[JSON.parse(chartController.root.dataset.cfxPreparedChart).series[0].key]});
                const target=chartController.getSelection()[0];chartController.highlight({ranges:[[instant,instant]]});
                return{target,highlighted:chartController.root.querySelectorAll('.cfx-highlighted').length}}
            """, instant);
        Assert.Equal(end.AddTicks(-1), DateTimeOffset.Parse(result.GetProperty("target").GetProperty("range")[1].GetString()!).UtcDateTime);
        Assert.Equal(0, result.GetProperty("highlighted").GetInt32());
        AssertNoConsoleErrors(session);
    }

    private static Chart LinkedSchedule(ChartSeriesKind kind, DateTime start, DateTime end) {
        var chart = Chart.Create().WithSize(640, 360).WithXAxisTimeScale().WithXAxisBounds(start.ToOADate(), end.ToOADate());
        if (kind == ChartSeriesKind.Timeline) chart.AddTimelineItem("Task", start, end);
        else if (kind == ChartSeriesKind.Gantt) chart.AddGanttTask("Task", start, end, .5);
        else {
            chart.WithStateCategories(new ChartStateCategory("up", "Up", ChartColor.FromRgb(13, 148, 136)));
            if (kind == ChartSeriesKind.StateTimeline) chart.AddStateTimelineLane("Task", new[] { new ChartStateTimelineSegment(start, end, "up") });
            else chart.AddGanttLane("Task", new[] { new ChartGanttLaneItem(start, end, "up") });
        }
        return chart;
    }
}
