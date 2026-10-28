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
    [InlineData("numeric", ChartBrushAxis.XY)]
    [InlineData("numeric", ChartBrushAxis.X)]
    [InlineData("time", ChartBrushAxis.X)]
    [InlineData("category", ChartBrushAxis.X)]
    public async Task BrushPublishesFinalProjectedDataRangesAndSelectsOnlyMarks(string axis, ChartBrushAxis brushAxis) {
        if (!Enabled) return;
        var start = axis == "time" ? new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc).ToOADate() : 0;
        var span = axis == "category" ? 2 : 10;
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(start, start + span).WithYAxisBounds(0, 10)
            .AddLine("Readings", new[] { new ChartPoint(start, 0), new ChartPoint(start + span / 2, 5), new ChartPoint(start + span, 10) });
        if (axis == "time") chart.Options.XAxis.Scale = ChartScaleKind.Time;
        if (axis == "category") chart.WithXLabels(new[] { new ChartAxisLabel(0, "North"), new ChartAxisLabel(1, "South"), new ChartAxisLabel(2, "West") });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush); options.Interaction.BrushAxis = brushAxis;
            options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
        }), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var positions = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>{
                window.linkedBrush=[];window.linkedLasso=[];
                document.body.addEventListener('cfxbrush',event=>linkedBrush.push(event.detail));
                document.body.addEventListener('cfxlasso',event=>linkedLasso.push(event.detail));
                const svg=root.querySelector('.cfx-stage svg'),p=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                const screen=(x,y)=>{const point=new DOMPoint(p.x+x*p.width,p.y+y*p.height).matrixTransform(svg.getScreenCTM());return{x:point.x,y:point.y}};
                return {from:screen(.22,.27),to:screen(.78,.73)};
            }
            """);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("from").GetProperty("x").GetDouble(), (float)positions.GetProperty("from").GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions.GetProperty("to").GetProperty("x").GetDouble(), (float)positions.GetProperty("to").GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 5 });
        await page.Mouse.UpAsync();
        var result = await page.EvaluateAsync<JsonElement>("()=>JSON.parse(JSON.stringify({brush:linkedBrush[0],lasso:linkedLasso[0],selection:linkedEvents.at(-1).detail}))");
        var range = result.GetProperty("brush").GetProperty("range");
        var x = range.GetProperty("x").EnumerateArray().ToArray();
        if (axis == "time") {
            Assert.InRange(DateTimeOffset.Parse(x[0].GetString()!).UtcDateTime.ToOADate(), start, start + span * .22);
            Assert.InRange(DateTimeOffset.Parse(x[1].GetString()!).UtcDateTime.ToOADate(), start + span * .78, start + span);
            Assert.EndsWith("Z", x[0].GetString()); Assert.EndsWith("Z", x[1].GetString());
        } else {
            if (axis == "category") {
                Assert.InRange(x[0].GetDouble(), span * .22 - .02, span * .22 + .02);
                Assert.InRange(x[1].GetDouble(), span * .78 - .02, span * .78 + .02);
            } else {
                Assert.InRange(x[0].GetDouble(), 0, span * .22);
                Assert.InRange(x[1].GetDouble(), span * .78, span);
            }
        }
        if (axis != "category") foreach (var direction in new[] { "x", "y" }) {
            if (range.TryGetProperty(direction, out var coordinateRange)) Assert.Equal(coordinateRange.GetRawText(),
                result.GetProperty("brush").GetProperty("displayRange").GetProperty(direction).GetRawText());
        }
        Assert.Equal(brushAxis == ChartBrushAxis.XY, range.TryGetProperty("y", out var y));
        if (brushAxis == ChartBrushAxis.XY) {
            Assert.InRange(y[0].GetDouble(), 0, 2.7); Assert.InRange(y[1].GetDouble(), 7.3, 10);
        }
        if (axis == "category") {
            Assert.Equal("South", Assert.Single(result.GetProperty("brush").GetProperty("categories").EnumerateArray()).GetString());
            Assert.Equal("South", result.GetProperty("brush").GetProperty("rangeLabel").GetString());
            Assert.Equal(new[] { 1d, 1d }, result.GetProperty("brush").GetProperty("displayRange").GetProperty("x").EnumerateArray().Select(value => value.GetDouble()));
        }
        else Assert.False(result.GetProperty("brush").TryGetProperty("categories", out _));
        Assert.Equal(range.GetProperty("x").EnumerateArray().Select(value => value.GetRawText()),
            result.GetProperty("lasso").GetProperty("range").GetProperty("x").EnumerateArray().Select(value => value.GetRawText()));
        Assert.Equal(range.GetProperty("x").GetRawText(), result.GetProperty("brush").GetProperty("categoryRange").GetRawText());
        Assert.Equal(result.GetProperty("brush").GetProperty("categoryRange").GetRawText(), result.GetProperty("lasso").GetProperty("categoryRange").GetRawText());
        var replayed = await page.EvaluateAsync<int[]>("""
            rangeJson=>{chartController.select({ranges:[JSON.parse(rangeJson)]});return chartController.getSelection().map(target=>target.sourcePoint)}
            """, result.GetProperty("brush").GetProperty("categoryRange").GetRawText());
        Assert.Equal(new[] { 1 }, replayed);
        if (brushAxis == ChartBrushAxis.XY) Assert.Equal(range.GetProperty("y").EnumerateArray().Select(value => value.GetRawText()),
            result.GetProperty("lasso").GetProperty("range").GetProperty("y").EnumerateArray().Select(value => value.GetRawText()));
        Assert.All(result.GetProperty("lasso").GetProperty("targets").EnumerateArray(), target => Assert.Equal("point", target.GetProperty("targetKind").GetString()));
        Assert.Equal("lasso", result.GetProperty("selection").GetProperty("reason").GetString());
        Assert.Equal(1, await LinkedSelectionCountAsync(page));
        await CaptureAsync(page, "linked-brush-" + axis + "-" + brushAxis, result);
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Equal("reset", await page.EvaluateAsync<string>("()=>linkedEvents.at(-1).detail.reason"));
        Assert.Equal(0, await LinkedSelectionCountAsync(page));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task GroupedNativeBrushAndVisualViewportKeepPeerFactsWithoutEcho() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(560, 320).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("readings"), "Observed", new[] {
                new ChartPoint(0, 0), new ChartPoint(5, 5), new ChartPoint(10, 10) }));
        var html = new[] { chart, chart }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1; options.Interaction.GroupName = "native-brush-peer";
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Features = ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.Brush
                | ChartInteractionFeatures.Zoom | ChartInteractionFeatures.SynchronizedCharts;
        });
        await using var session = await OpenAsync(html, 820, 1100);
        var page = session.Page;
        var roots = page.Locator(Root); var source = roots.Nth(0); var peer = roots.Nth(1);
        await page.EvaluateAsync("""
            () => {
                const [source,peer]=document.querySelectorAll('.cfx-interactive-chart');
                window.groupedSource=[];window.groupedPeer=[];
                for(const [root,log] of [[source,groupedSource],[peer,groupedPeer]])
                    for(const name of ['cfxsync','cfxviewport','cfxbrush','cfxlasso','cfxcompare'])
                        root.addEventListener(name,event=>log.push({name,detail:event.detail}));
            }
            """);
        var positions = await source.EvaluateAsync<double[]>("""
            root => {
                const svg=root.querySelector('.cfx-stage svg'),p=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                return [[.22,.27],[.78,.73]].flatMap(([x,y])=>{const q=new DOMPoint(p.x+x*p.width,p.y+y*p.height).matrixTransform(svg.getScreenCTM());return[q.x,q.y];});
            }
            """);
        await source.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]); await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3], new MouseMoveOptions { Steps = 5 }); await page.Mouse.UpAsync();
        var brush = await source.GetAttributeAsync("data-cfx-brush");
        Assert.False(string.IsNullOrWhiteSpace(brush)); Assert.Equal(brush, await peer.GetAttributeAsync("data-cfx-brush"));
        var selections = await page.EvaluateAsync<JsonElement>("()=>[...document.querySelectorAll('.cfx-interactive-chart')].map(root=>ChartForgeXCharts.get(root).getSelection())");
        foreach (var selection in selections.EnumerateArray()) {
            var target = Assert.Single(selection.EnumerateArray());
            Assert.Equal("readings", target.GetProperty("seriesKey").GetString()); Assert.Equal("1", target.GetProperty("sourcePoint").GetString());
        }
        var brushEvents = await page.EvaluateAsync<JsonElement>("()=>({source:groupedSource,peer:groupedPeer})");
        Assert.Single(brushEvents.GetProperty("source").EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxbrush");
        Assert.Single(brushEvents.GetProperty("source").EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxlasso");
        Assert.Contains(brushEvents.GetProperty("source").EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxsync" && item.GetProperty("detail").GetProperty("action").GetString() == "brush");
        Assert.Empty(brushEvents.GetProperty("peer").EnumerateArray());
        await source.Locator("[data-cfx-zoom=in]").ClickAsync();
        var zoom = await source.GetAttributeAsync("data-cfx-zoom"); Assert.NotEqual("1.000", zoom);
        Assert.Equal(zoom, await peer.GetAttributeAsync("data-cfx-zoom"));
        var events = await page.EvaluateAsync<JsonElement>("()=>({source:groupedSource,peer:groupedPeer})");
        Assert.Single(events.GetProperty("source").EnumerateArray(), item => item.GetProperty("name").GetString() == "cfxviewport");
        Assert.Empty(events.GetProperty("peer").EnumerateArray());
        await CaptureAsync(page, "grouped-native-brush-viewport", new { brush, selections, brushEvents, zoom, events });
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Radar)]
    [InlineData(ChartSeriesKind.BoxPlot)]
    public async Task PublicTargetFactsKeepCategoryRawCoordinateAndNumericValueAcrossFamilies(ChartSeriesKind kind) {
        if (!Enabled) return;
        var series = kind == ChartSeriesKind.BoxPlot
            ? ChartSeries.CreateBoxPlot(new ChartSeriesId("readings"), "Readings", new[] { new ChartBoxPlot(0, 1, 2, 3, 4, 5), new ChartBoxPlot(1, 2, 3, 4, 5, 6), new ChartBoxPlot(2, 3, 4, 5, 6, 7) })
            : new ChartSeries(new ChartSeriesId("readings"), "Readings", kind, new[] { new ChartPoint(0, 3), new ChartPoint(1, 4), new ChartPoint(2, 5) });
        series.WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second"), new ChartObservationId("third") });
        var chart = Chart.Create().WithSize(640, 360).WithXLabels(new[] { new ChartAxisLabel(0, "North"), new ChartAxisLabel(1, "South"), new ChartAxisLabel(2, "West") }).AddSeries(series);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Fit), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var target = await page.EvaluateAsync<JsonElement>("()=>{chartController.select({observationIds:['first']});return chartController.getSelection()[0]}");
        Assert.Equal("North", target.GetProperty("category").GetString());
        Assert.Equal(0, target.GetProperty("x").GetDouble());
        Assert.True(target.GetProperty("numericValue").GetDouble() > 0);
        Assert.False(target.TryGetProperty("observationIds", out _));
        Assert.False(target.TryGetProperty("range", out _));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task PieAggregatesExposeContributorIdsAndCategoryTitlesWithoutTechnicalPointOrValueText() {
        if (!Enabled) return;
        var series = new ChartSeries(new ChartSeriesId("parts"), "Parts", ChartSeriesKind.Pie,
            new[] { new ChartPoint(1, 50), new ChartPoint(2, 25), new ChartPoint(3, 10), new ChartPoint(4, 5) })
            .WithObservationIds(new[] { new ChartObservationId("up"), new ChartObservationId("down"), new ChartObservationId("queued"), new ChartObservationId("unknown") });
        var chart = Chart.Create().WithSize(640, 360).WithXLabels("Up", "Down", "Queued", "Unknown").AddSeries(series);
        chart.Options.MaximumPieSlices = 3;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Fit), 820, 600);
        var page = session.Page;
        await BindLinkedEventsAsync(page);
        var result = await page.EvaluateAsync<JsonElement>("""
            ()=>{chartController.select({observationIds:['unknown']});const aggregate=chartController.getSelection()[0];
                chartController.select({observationIds:['down']});return{aggregate,individual:chartController.getSelection()[0]}}
            """);
        Assert.Equal(new[] { "queued", "unknown" }, result.GetProperty("aggregate").GetProperty("observationIds").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal("Down", result.GetProperty("individual").GetProperty("category").GetString());
        await page.Locator("[data-cfx-role=radial-point][data-cfx-observation-id=down]").FocusAsync();
        Assert.Equal("Down", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.Equal("Down", await page.Locator(".cfx-compare-chip").InnerTextAsync());
        await CaptureAsync(page, "linked-pie-category", result);
        AssertNoConsoleErrors(session);
    }
}
