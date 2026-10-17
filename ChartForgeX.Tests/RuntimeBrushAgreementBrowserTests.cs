using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RuntimeBrushAgreementBrowserTests {
    [BrowserTheory]
    [InlineData("numeric")]
    [InlineData("time")]
    [InlineData("category")]
    [InlineData("box")]
    public async Task BrushCaptionRangeVisibleBoxAndPublicSelectionDescribeTheSameFilter(string family) {
        if (!Enabled) return;
        var time = family == "time";
        var categorical = family is "category" or "box";
        var minimum = time ? new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate() : 0;
        var span = time ? 28 : family == "category" ? 4 : family == "box" ? 2 : 100;
        var values = family switch {
            "time" => new[] { 0, 5 + 8d / 24, 6, 13, 14 + 10d / 24, 20, 28 }.Select(day => minimum + day).ToArray(),
            "category" => new[] { 0d, 1, 2, 3, 4 },
            "box" => new[] { 0d, 1, 2 },
            _ => new[] { 10d, 20, 22, 40, 60, 68, 70, 80, 90 }
        };
        var chart = Chart.Create().WithSize(760, 390).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(minimum, minimum + span).WithYAxisBounds(0, 10);
        if (family == "box") chart.AddBoxPlot("Services", values.Select(value => new ChartBoxPlot(value, 2, 3, 4, 5, 6)).ToArray());
        else if (family == "category") chart.AddBar("Regions", values.Select(value => new ChartPoint(value, 5)).ToArray());
        else chart.AddLine("Readings", values.Select(value => new ChartPoint(value, 5)).ToArray());
        chart.Series[0].WithObservationIds(values.Select((_, index) => new ChartObservationId("row-" + index)).ToArray());
        if (time) { chart.Options.XAxis.Scale = ChartScaleKind.Time; chart.Options.XAxis.TickCount = 5; }
        if (categorical) chart.WithXLabels((family == "box" ? new[] { "API", "Worker", "Queue" } : new[] { "North", "South", "East", "West", "Central" })
            .Select((text, index) => new ChartAxisLabel(index, text)).ToArray());
        var from = time ? (5 + 18d / 1440) / span : family == "category" ? .2 : family == "box" ? .3 : .22;
        var to = time ? (14 + (8 * 60 + 57d) / 1440) / span : family == "category" ? .8 : family == "box" ? .7 : .68;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Brush);
            options.Interaction.BrushAxis = ChartBrushAxis.X;
        }), 940, 620);
        var page = session.Page;
        var positions = await page.Locator(".cfx-interactive-chart").EvaluateAsync<double[]>("""
            (root,ratios)=>{
              window.agreementBrush=null;root.addEventListener('cfxbrush',event=>window.agreementBrush=event.detail,{once:true});
              const p=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds,m=root.querySelector('.cfx-stage svg').getScreenCTM();
              const a=new DOMPoint(p.x+p.width*ratios[0],p.y+p.height*.3).matrixTransform(m);
              const b=new DOMPoint(p.x+p.width*ratios[1],p.y+p.height*.7).matrixTransform(m);return[a.x,a.y,b.x,b.y];
            }
            """, new[] { from, to });
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]); await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3], new MouseMoveOptions { Steps = 4 }); await page.Mouse.UpAsync();
        var facts = await page.Locator(".cfx-interactive-chart").EvaluateAsync<JsonElement>("""
            (root,sourceValues)=>{
              const detail=agreementBrush,projection=JSON.parse(root.dataset.cfxPreparedChart).dataProjection;
              const native=value=>typeof value==='number'?value:Date.parse(value)/86400000+25569;
              const range=detail.range.x.map(native),rect=root.querySelector('.cfx-brush-box').getBoundingClientRect();
              const inverse=root.querySelector('.cfx-stage svg').getScreenCTM().inverse(),p=projection.plotBounds;
              const box=[new DOMPoint(rect.left,rect.top),new DOMPoint(rect.right,rect.bottom)].map(point=>
                projection.x.minimum+(point.matrixTransform(inverse).x-p.x)/p.width*(projection.x.maximum-projection.x.minimum));
              const selection=ChartForgeXCharts.get(root).getSelection();
              const selected=selection.map(target=>native(target.x)).sort((a,b)=>a-b),selectedIds=selection.map(target=>target.observationId).sort();
              const filtered=sourceValues.filter(value=>value>=range[0]&&value<=range[1]);
              const filteredIds=sourceValues.map((value,index)=>value>=range[0]&&value<=range[1]?'row-'+index:null).filter(Boolean).sort();
              const ticks=[...projection.x.displayTicks,...projection.x.displayBounds];
              return JSON.parse(JSON.stringify({detail,selected,filtered,selectedIds,filteredIds,box,ticks}));
            }
            """, values);
        var detail = facts.GetProperty("detail");
        Assert.Equal(detail.GetProperty("label").GetString(), detail.GetProperty("rangeLabel").GetString());
        Assert.Equal(facts.GetProperty("filteredIds").GetRawText(), facts.GetProperty("selectedIds").GetRawText());
        Assert.NotEmpty(facts.GetProperty("selected").EnumerateArray());
        if (categorical) {
            var names = family == "box" ? new[] { "Worker" } : new[] { "South", "East", "West" };
            Assert.Equal(names, detail.GetProperty("categories").EnumerateArray().Select(value => value.GetString()));
            Assert.Equal(string.Join(", ", names), detail.GetProperty("rangeLabel").GetString());
            Assert.Equal(family == "box" ? new[] { 1d, 1d } : new[] { 1d, 3d },
                detail.GetProperty("displayRange").GetProperty("x").EnumerateArray().Select(value => value.GetDouble()));
        } else {
            Assert.Equal(detail.GetProperty("range").GetProperty("x").GetRawText(), detail.GetProperty("displayRange").GetProperty("x").GetRawText());
            var endpoints = detail.GetProperty("range").GetProperty("x").EnumerateArray().Select(value => time
                ? DateTimeOffset.Parse(value.GetString()!).UtcDateTime.ToOADate() : value.GetDouble()).ToArray();
            var captions = endpoints.Select(value => facts.GetProperty("ticks").EnumerateArray().First(tick => tick.GetProperty("value").GetDouble() == value).GetProperty("text").GetString());
            Assert.Equal(string.Join(" – ", captions), detail.GetProperty("rangeLabel").GetString());
            Assert.InRange(endpoints[0], minimum, minimum + from * span);
            Assert.InRange(endpoints[1], minimum + to * span, minimum + span);
            Assert.InRange(Math.Abs(facts.GetProperty("box")[0].GetDouble() - endpoints[0]), 0, span * .002);
            Assert.InRange(Math.Abs(facts.GetProperty("box")[1].GetDouble() - endpoints[1]), 0, span * .002);
        }
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, "brush-agreement-" + family + ".png") });
            await File.WriteAllTextAsync(Path.Combine(output, "brush-agreement-" + family + ".json"), facts.GetRawText());
        }
        AssertNoConsoleErrors(session);
    }
}
