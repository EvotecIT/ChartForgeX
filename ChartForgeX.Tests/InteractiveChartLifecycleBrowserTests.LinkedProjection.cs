using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData("primary", false, 820)]
    [InlineData("secondary", true, 380)]
    [InlineData("mixed", false, 820)]
    [InlineData("empty-primary", false, 820)]
    [InlineData("empty-secondary", true, 380)]
    [InlineData("secondary-primary-y-line", false, 820)]
    [InlineData("secondary-primary-y-region", true, 380)]
    [InlineData("secondary-x-only", false, 820)]
    public async Task NativeBrushReportsTheSoleObservedValueDomainAndOmitsMixedY(string domains, bool dark, int width) {
        if (!Enabled) return;
        var chart = ChartLinkedProjectionContractsTests.ValueDomainsChart(domains)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ConfigureProjectionBrush), width, 650);
        var result = await NativeProjectionBrushAsync(session.Page);
        var range = result.GetProperty("range");
        Assert.InRange(range.GetProperty("x")[0].GetDouble(), 0, .77);
        Assert.InRange(range.GetProperty("x")[1].GetDouble(), 2.23, 3);
        foreach (var direction in new[] { "x", "y" }) {
            if (range.TryGetProperty(direction, out var coordinateRange)) Assert.Equal(coordinateRange.GetRawText(),
                result.GetProperty("displayRange").GetProperty(direction).GetRawText());
        }
        if (domains == "mixed" || domains.StartsWith("secondary-primary-y", StringComparison.Ordinal)) Assert.False(range.TryGetProperty("y", out _));
        else {
            var y = range.GetProperty("y");
            var secondary = domains.StartsWith("secondary", StringComparison.Ordinal) || domains == "empty-primary";
            Assert.InRange(y[0].GetDouble(), secondary ? 1000 : 0, secondary ? 1260 : 2.6);
            Assert.InRange(y[1].GetDouble(), secondary ? 1740 : 7.4, secondary ? 2000 : 10);
        }
        await CaptureAsync(session.Page, "hosted-value-domain-" + domains, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.HorizontalBar)]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.RangeBar)]
    [InlineData(ChartSeriesKind.BoxPlot)]
    [InlineData(ChartSeriesKind.Waterfall)]
    public async Task HorizontalNativeBrushKeepsScreenValueAndCategoryDomains(ChartSeriesKind kind) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(0, 100).WithYAxisBounds(.5, 3.5);
        chart.Options.YAxis.Labels.AddRange(new[] { new ChartAxisLabel(1, "A"), new ChartAxisLabel(2, "B"), new ChartAxisLabel(3, "C") });
        var points = new[] { new ChartPoint(1, 20), new ChartPoint(2, 50), new ChartPoint(3, 80) };
        if (kind == ChartSeriesKind.HorizontalBar) chart.AddHorizontalBar("Values", points);
        else if (kind == ChartSeriesKind.Line) { chart.AddLine("Values", points); chart.Options.LineOrientation = ChartOrientation.Horizontal; }
        else if (kind == ChartSeriesKind.RangeBar) {
            chart.AddRangeBar("Values", new[] { new ChartInterval(1, 10, 20), new ChartInterval(2, 30, 50), new ChartInterval(3, 60, 80) });
            chart.Options.RangeBar.Orientation = ChartOrientation.Horizontal;
        } else if (kind == ChartSeriesKind.BoxPlot) {
            chart.AddBoxPlot("Values", new[] { new ChartBoxPlot(1, 10, 12, 15, 18, 20), new ChartBoxPlot(2, 30, 35, 40, 45, 50), new ChartBoxPlot(3, 60, 65, 70, 75, 80) });
            chart.Options.BoxPlot.Orientation = ChartOrientation.Horizontal;
        } else {
            chart.AddWaterfall("Values", new[] { ChartWaterfallItem.OpeningBalance(1, 20), ChartWaterfallItem.Delta(2, 30), ChartWaterfallItem.Delta(3, 30) });
            chart.Options.Waterfall.Orientation = ChartOrientation.Horizontal;
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ConfigureProjectionBrush), 820, 650);
        var result = await NativeProjectionBrushAsync(session.Page);
        var range = result.GetProperty("range");
        Assert.InRange(range.GetProperty("x")[0].GetDouble(), 0, 25.2);
        Assert.InRange(range.GetProperty("x")[1].GetDouble(), 74.8, 100);
        Assert.Equal(range.GetProperty("x").GetRawText(), result.GetProperty("displayRange").GetProperty("x").GetRawText());
        Assert.InRange(range.GetProperty("y")[0].GetDouble(), 1.23, 1.27);
        Assert.InRange(range.GetProperty("y")[1].GetDouble(), 2.73, 2.77);
        Assert.Equal("B", Assert.Single(result.GetProperty("categories").EnumerateArray()).GetString());
        Assert.Equal("B", result.GetProperty("rangeLabel").GetString());
        Assert.Equal(new[] { 2d, 2d }, result.GetProperty("displayRange").GetProperty("y").EnumerateArray().Select(value => value.GetDouble()));
        await CaptureAsync(session.Page, "hosted-horizontal-projection-" + kind, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartTimeTickUnit.Day)]
    [InlineData(ChartTimeTickUnit.Week)]
    [InlineData(ChartTimeTickUnit.Month)]
    public async Task AdjacentCadenceBucketsRoundTripWithoutSharingBoundaryObservations(ChartTimeTickUnit unit) {
        if (!Enabled) return;
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var boundary = unit == ChartTimeTickUnit.Month ? start.AddMonths(1) : start.AddDays(unit == ChartTimeTickUnit.Week ? 7 : 1);
        var series = ChartSeries.CreateBar(new ChartSeriesId("buckets"), "Buckets", new[] { new ChartPoint(start, 5), new ChartPoint(boundary, 8) })
            .WithTimeBuckets(new ChartTimeTickInterval(unit)).WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second") });
        await using var session = await OpenAsync(Chart.Create().WithSize(640, 360).WithXAxisTimeScale().AddSeries(series).ToInteractiveHtmlPage(Fit), 820, 650);
        await BindLinkedEventsAsync(session.Page);
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            boundary=>{
                chartController.select({observationIds:['first']});const first=chartController.getSelection()[0];
                chartController.select({ranges:[first.range]});const roundTrip=chartController.getSelection().map(target=>target.observationId);
                chartController.select({ranges:[[boundary,boundary]]});const exact=chartController.getSelection().map(target=>target.observationId);
                const before=new Date(Date.parse(boundary)-1).toISOString();chartController.select({ranges:[[before,before]]});
                return{first,roundTrip,exact,before:chartController.getSelection().map(target=>target.observationId),events:linkedEvents.length};
            }
            """, boundary.ToString("O"));
        Assert.Equal(boundary.AddTicks(-1), DateTimeOffset.Parse(result.GetProperty("first").GetProperty("range")[1].GetString()!).UtcDateTime);
        Assert.Equal("first", Assert.Single(result.GetProperty("roundTrip").EnumerateArray()).GetString());
        Assert.Equal("second", Assert.Single(result.GetProperty("exact").EnumerateArray()).GetString());
        Assert.Equal("first", Assert.Single(result.GetProperty("before").EnumerateArray()).GetString());
        Assert.Equal(0, result.GetProperty("events").GetInt32());
        await CaptureAsync(session.Page, "hosted-cadence-boundary-" + unit, result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnusableProjectionStillMountsAWorkingPublicController(bool schedule) {
        if (!Enabled) return;
        await using var session = await OpenAsync(ChartLinkedProjectionContractsTests.OverflowChart(schedule).ToInteractiveHtmlPage(Fit), 820, 650);
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            ()=>{const root=document.querySelector('.cfx-interactive-chart'),metadata=JSON.parse(root.dataset.cfxPreparedChart),controller=ChartForgeXCharts.get(root);
                controller.select({seriesKeys:[metadata.series[0].key]});return{mounted:controller.root===root,projection:metadata.dataProjection??null,count:controller.getSelection().length}}
            """);
        Assert.True(result.GetProperty("mounted").GetBoolean());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("projection").ValueKind);
        Assert.Equal(schedule ? 1 : 2, result.GetProperty("count").GetInt32());
        await CaptureAsync(session.Page, "hosted-unusable-projection-" + (schedule ? "schedule-symlog" : "bar-padding"), result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(-2, -1)]
    [InlineData(-1, 1)]
    [InlineData(46000, 46002)]
    public async Task TimeRangesAndNativeBrushPairsKeepOneUnitAcrossTheOleEpoch(double start, double end) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithXAxisTimeScale().WithXAxisBounds(start, end).AddTimelineRange("Interval", start, end);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ConfigureProjectionBrush), 820, 650);
        await BindLinkedEventsAsync(session.Page);
        var target = await session.Page.EvaluateAsync<JsonElement>("()=>{chartController.select({seriesKeys:[JSON.parse(chartController.root.dataset.cfxPreparedChart).series[0].key]});return chartController.getSelection()[0]}");
        var brush = await NativeProjectionBrushAsync(session.Page);
        var targetRange = target.GetProperty("range"); var brushRange = brush.GetProperty("range").GetProperty("x");
        if (start < 0) {
            Assert.Equal(new[] { start, end }, targetRange.EnumerateArray().Select(value => value.GetDouble()));
            Assert.InRange(brushRange[0].GetDouble(), start, start + (end - start) * .25 + .01);
            Assert.InRange(brushRange[1].GetDouble(), start + (end - start) * .75 - .01, end);
        } else {
            Assert.All(targetRange.EnumerateArray(), value => Assert.EndsWith("Z", value.GetString()));
            Assert.All(brushRange.EnumerateArray(), value => Assert.EndsWith("Z", value.GetString()));
        }
        Assert.Equal(brushRange.GetRawText(), brush.GetProperty("displayRange").GetProperty("x").GetRawText());
        await UpdateAsync(session.Page, Chart.Create().WithSize(640, 360).WithXAxisTimeScale()
            .AddLine("Point", new[] { new ChartPoint(start < 0 ? -1.5 : start, 5) }).ToInteractiveHtmlFragmentWithoutAssets(Fit));
        var point = await session.Page.EvaluateAsync<JsonElement>("()=>{chartController.select({seriesKeys:[JSON.parse(chartController.root.dataset.cfxPreparedChart).series[0].key]});return chartController.getSelection()[0]}");
        if (start < 0) Assert.Equal(-1.5, point.GetProperty("x").GetDouble());
        else Assert.EndsWith("Z", point.GetProperty("x").GetString());
        await CaptureAsync(session.Page, "hosted-ole-units-" + (start < 0 ? end < 0 ? "negative" : "crossing" : "positive"), new { target, brush, point });
        AssertNoConsoleErrors(session);
    }

    private static void ConfigureProjectionBrush(HtmlChartInteractionOptions options) {
        Fit(options); options.Interaction.Enable(ChartInteractionFeatures.Brush); options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
    }

    [BrowserTheory]
    [InlineData("cadence", -1.25, -1.2916666666666667)]
    [InlineData("state", -3, -2)]
    [InlineData("state", -1, 1)]
    public async Task HalfOpenLegacyTimeRangesExcludeAdjacentNumericBoundaries(string family, double start, double boundary) {
        if (!Enabled) return;
        await using var session = await OpenAsync(ChartLinkedProjectionContractsTests.HalfOpenLegacyTimeChart(family, start, boundary).ToInteractiveHtmlPage(Fit), 820, 650);
        await BindLinkedEventsAsync(session.Page);
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            bounds=>{chartController.select({observationIds:['first']});const first=chartController.getSelection()[0];
                chartController.select({ranges:[first.range]});const roundTrip=chartController.getSelection().map(target=>target.observationId);
                chartController.select({ranges:[[bounds.boundary,bounds.boundary]]});const exact=chartController.getSelection().map(target=>target.observationId);
                chartController.select({ranges:[[bounds.before,bounds.before]]});return{first,roundTrip,exact,
                    before:chartController.getSelection().map(target=>target.observationId),events:linkedEvents.length}}
            """, new { boundary, before = boundary < start ? Math.BitIncrement(boundary) : Math.BitDecrement(boundary) });
        var inclusiveEnd = boundary < start ? Math.BitIncrement(boundary) : Math.BitDecrement(boundary);
        var firstRange = result.GetProperty("first").GetProperty("range");
        Assert.Equal(start, firstRange[0].GetDouble());
        Assert.Equal(inclusiveEnd, firstRange[1].GetDouble());
        Assert.Equal("first", Assert.Single(result.GetProperty("roundTrip").EnumerateArray()).GetString());
        Assert.Equal("second", Assert.Single(result.GetProperty("exact").EnumerateArray()).GetString());
        Assert.Equal("first", Assert.Single(result.GetProperty("before").EnumerateArray()).GetString());
        Assert.Equal(0, result.GetProperty("events").GetInt32());
        await CaptureAsync(session.Page, "hosted-legacy-halfopen-" + family + "-" + (boundary < 0 ? "negative" : "crossing"), result);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NativeTimeBrushRoundTripsBothSidesInThePreparedDomainUnit(bool legacyDomain) {
        if (!Enabled) return;
        var minimum = legacyDomain ? -3 : 10; var maximum = legacyDomain ? 3 : 16;
        var first = legacyDomain ? -1 : 12; var second = legacyDomain ? 1 : 14;
        var chart = Chart.Create().WithSize(640, 360).WithXAxisTimeScale().WithXAxisBounds(minimum, maximum)
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("time-readings"), "Readings", new[] { new ChartPoint(first, 5), new ChartPoint(second, 8) })
                .WithObservationIds(new[] { new ChartObservationId("first"), new ChartObservationId("second") }));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => { ConfigureProjectionBrush(options); options.Interaction.BrushAxis = ChartBrushAxis.X; }), 820, 650);
        await BindLinkedEventsAsync(session.Page);
        var brush = await NativeProjectionBrushAsync(session.Page);
        var result = await session.Page.EvaluateAsync<JsonElement>("""
            rangeJson=>{const range=JSON.parse(rangeJson);chartController.select({seriesKeys:['time-readings']});const all=chartController.getSelection();
                chartController.select({ranges:[range]});return{all,roundTrip:chartController.getSelection().map(target=>target.observationId)}}
            """, brush.GetProperty("range").GetProperty("x").GetRawText());
        Assert.Equal(new[] { "first", "second" }, result.GetProperty("roundTrip").EnumerateArray().Select(value => value.GetString()));
        Assert.All(result.GetProperty("all").EnumerateArray(), target => Assert.Equal(legacyDomain ? JsonValueKind.Number : JsonValueKind.String, target.GetProperty("x").ValueKind));
        Assert.All(brush.GetProperty("range").GetProperty("x").EnumerateArray(), endpoint => Assert.Equal(legacyDomain ? JsonValueKind.Number : JsonValueKind.String, endpoint.ValueKind));
        await CaptureAsync(session.Page, "hosted-domain-roundtrip-" + (legacyDomain ? "legacy" : "ordered"), new { brush, result });
        AssertNoConsoleErrors(session);
    }

    private static async Task<JsonElement> NativeProjectionBrushAsync(IPage page) {
        var positions = await page.Locator(Root).EvaluateAsync<double[]>("""
            root=>{window.projectionBrush=null;root.addEventListener('cfxbrush',event=>window.projectionBrush=event.detail,{once:true});
                const p=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds,m=root.querySelector('.cfx-stage svg').getScreenCTM();
                const a=new DOMPoint(p.x+p.width*.25,p.y+p.height*.25).matrixTransform(m),b=new DOMPoint(p.x+p.width*.75,p.y+p.height*.75).matrixTransform(m);
                return[a.x,a.y,b.x,b.y]}
            """);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]); await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3], new MouseMoveOptions { Steps = 4 }); await page.Mouse.UpAsync();
        return await page.EvaluateAsync<JsonElement>("()=>JSON.parse(JSON.stringify(projectionBrush))");
    }
}
