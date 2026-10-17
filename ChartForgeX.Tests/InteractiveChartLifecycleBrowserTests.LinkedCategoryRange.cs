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
    [InlineData(false)]
    [InlineData(true)]
    public async Task HorizontalCategoryRangeReplaysTheAuthoredNumericOrTimeDomain(bool time) {
        if (!Enabled) return;
        var start = time ? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate() : 0;
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(0, 100).WithYAxisBounds(start, start + 10)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("horizontal-readings"), "Readings",
                new[] { new ChartPoint(start, 0), new ChartPoint(start + 5, 50), new ChartPoint(start + 10, 100) })
                .WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("middle"), new ChartObservationId("last") }));
        chart.Options.LineOrientation = ChartOrientation.Horizontal;
        if (time) chart.Options.YAxis.Scale = ChartScaleKind.Time;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ConfigureProjectionBrush), 820, 650);
        await BindLinkedEventsAsync(session.Page);
        await session.Page.Locator(Root).EvaluateAsync("root=>{window.categoryLasso=null;root.addEventListener('cfxlasso',event=>categoryLasso=event.detail)}");
        var brush = await NativeProjectionBrushAsync(session.Page);
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            rangeJson=>{chartController.select({ranges:[JSON.parse(rangeJson)]});
                const range=categoryLasso.categoryRange,selection=chartController.getSelection();
                return{categoryRangeIsArray:Array.isArray(range),lassoCategoryStart:range[0],lassoCategoryEnd:range[1],
                    matchesPhysicalY:range.length===2&&range.every((value,index)=>value===categoryLasso.range.y[index]),
                    selectionCount:selection.length,selectionId:selection[0]?.observationId}}
            """, brush.GetProperty("categoryRange").GetRawText());
        Assert.True(result.GetProperty("categoryRangeIsArray").GetBoolean());
        Assert.True(result.GetProperty("matchesPhysicalY").GetBoolean());
        Assert.Equal(brush.GetProperty("range").GetProperty("y").GetRawText(), brush.GetProperty("categoryRange").GetRawText());
        Assert.Equal(brush.GetProperty("categoryRange")[0].GetRawText(), result.GetProperty("lassoCategoryStart").GetRawText());
        Assert.Equal(brush.GetProperty("categoryRange")[1].GetRawText(), result.GetProperty("lassoCategoryEnd").GetRawText());
        Assert.All(brush.GetProperty("range").GetProperty("x").EnumerateArray(), value => Assert.Equal(JsonValueKind.Number, value.ValueKind));
        Assert.All(brush.GetProperty("categoryRange").EnumerateArray(), value => Assert.Equal(time ? JsonValueKind.String : JsonValueKind.Number, value.ValueKind));
        Assert.Equal(1, result.GetProperty("selectionCount").GetInt32());
        Assert.Equal("middle", result.GetProperty("selectionId").GetString());
        // A physical-X-only brush cannot publish an interval on this chart's physical Y category axis.
        await UpdateAsync(session.Page, chart.ToInteractiveHtmlFragmentWithoutAssets(options => {
            ConfigureProjectionBrush(options); options.Interaction.BrushAxis = ChartBrushAxis.X;
        }));
        var xOnly = await NativeProjectionBrushAsync(session.Page);
        Assert.False(xOnly.TryGetProperty("categoryRange", out _));
        Assert.False(xOnly.GetProperty("range").TryGetProperty("y", out _));
        await CaptureAsync(session.Page, "hosted-horizontal-category-range-" + (time ? "time" : "numeric"), new { brush, result, xOnly });
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ABrushWithoutADataProjectionOmitsCategoryRangeOnBothPublicEvents() {
        if (!Enabled) return;
        var chart = ChartLinkedProjectionContractsTests.OverflowChart(false);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ConfigureProjectionBrush), 820, 650);
        var positions = await session.Page.Locator(Root).EvaluateAsync<double[]>("""
            root=>{window.unprojectedBrush=null;window.unprojectedLasso=null;
                root.addEventListener('cfxbrush',event=>unprojectedBrush=event.detail);root.addEventListener('cfxlasso',event=>unprojectedLasso=event.detail);
                const svg=root.querySelector('.cfx-stage svg'),p=svg.viewBox.baseVal,m=svg.getScreenCTM();
                const a=new DOMPoint(p.x+p.width*.25,p.y+p.height*.25).matrixTransform(m),b=new DOMPoint(p.x+p.width*.75,p.y+p.height*.75).matrixTransform(m);
                return[a.x,a.y,b.x,b.y]}
            """);
        await session.Page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await session.Page.Mouse.MoveAsync((float)positions[0], (float)positions[1]); await session.Page.Mouse.DownAsync();
        await session.Page.Mouse.MoveAsync((float)positions[2], (float)positions[3], new MouseMoveOptions { Steps = 4 }); await session.Page.Mouse.UpAsync();
        var result = await session.Page.EvaluateAsync<JsonElement>("()=>({brush:unprojectedBrush,lasso:unprojectedLasso})");
        foreach (var name in new[] { "brush", "lasso" }) {
            Assert.Equal(JsonValueKind.Object, result.GetProperty(name).ValueKind);
            Assert.True(result.GetProperty(name).TryGetProperty("bounds", out _));
            Assert.False(result.GetProperty(name).TryGetProperty("range", out _));
            Assert.False(result.GetProperty(name).TryGetProperty("categoryRange", out _));
        }
        await CaptureAsync(session.Page, "hosted-missing-category-projection", result);
        AssertNoConsoleErrors(session);
    }
}
