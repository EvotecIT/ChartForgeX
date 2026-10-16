using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RuntimeReadableValuesBrowserTests {
    private static readonly DateTime First = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private const double ScatterX = 4.242394913100822;
    private const double ScatterY = 8.88888888888889;

    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 1440)]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    public async Task PublicReadoutsUsePreparedTextAndKeepExactLinkedFilterValues(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(CreatePage(dark), width, 900);
        var page = session.Page;
        var readings = new Dictionary<string, JsonElement>();
        var time = await ReadAsync(page, 1, "time-middle", "[data-cfx-observation-id=time-middle]");
        Assert.Equal("2026-09-06", time.GetProperty("target").GetProperty("label").GetString());
        Assert.Equal("2026-09-06", time.GetProperty("title").GetString());
        Assert.Equal("2026-09-06", time.GetProperty("chip").GetString());
        Assert.Equal(First.AddDays(5), DateTimeOffset.Parse(time.GetProperty("target").GetProperty("x").GetString()!, CultureInfo.InvariantCulture).UtcDateTime);
        readings.Add("time", time);
        await CaptureAsync(page, "time", dark, width, time);

        var scatter = await ReadAsync(page, 2, "scatter-first", "[data-cfx-observation-id=scatter-first]");
        Assert.Equal("4,2", scatter.GetProperty("title").GetString());
        Assert.Equal("Samples", scatter.GetProperty("chip").GetString());
        Assert.Equal(ScatterX, scatter.GetProperty("target").GetProperty("x").GetDouble());
        Assert.Equal(ScatterY, scatter.GetProperty("target").GetProperty("numericValue").GetDouble());
        Assert.Contains("8,9", scatter.GetProperty("tooltip").GetString());
        readings.Add("scatter", scatter);
        await CaptureAsync(page, "scatter", dark, width, scatter);

        var histogram = await ReadAsync(page, 3, "histogram-second", "[data-cfx-bin-index='1']");
        Assert.Equal("40 – 60", histogram.GetProperty("title").GetString());
        Assert.Equal("40 – 60", histogram.GetProperty("chip").GetString());
        Assert.Equal("40 – 60", histogram.GetProperty("target").GetProperty("rangeLabel").GetString());
        // This final bin is inclusive. The first bin below proves a half-open endpoint remains exact too.
        Assert.Equal(60, histogram.GetProperty("target").GetProperty("range")[1].GetDouble());
        await CaptureAsync(page, "histogram", dark, width, histogram);
        var firstBin = await SelectAsync(page, 3, "histogram-first");
        Assert.Equal(Math.BitDecrement(40), firstBin.GetProperty("range")[1].GetDouble());
        Assert.Equal("20 – 40", firstBin.GetProperty("rangeLabel").GetString());
        readings.Add("histogram", histogram);
        readings.Add("firstBin", firstBin);

        var state = await ReadAsync(page, 4, "state-run", "[data-cfx-observation-id=state-run]");
        Assert.Equal("Service · Available", state.GetProperty("title").GetString());
        Assert.Equal("2026-09-01 00:00 – 2026-09-01 06:00", state.GetProperty("chip").GetString());
        Assert.Equal("Available", state.GetProperty("target").GetProperty("value").GetString());
        Assert.False(state.GetProperty("target").TryGetProperty("numericValue", out _));
        Assert.Equal("2026-09-01 00:00 – 2026-09-01 06:00", state.GetProperty("target").GetProperty("rangeLabel").GetString());
        Assert.Equal(First.AddHours(6).AddTicks(-1), DateTimeOffset.Parse(state.GetProperty("target").GetProperty("range")[1].GetString()!, CultureInfo.InvariantCulture).UtcDateTime);
        Assert.DoesNotContain(First.AddHours(6).ToOADate().ToString("R", CultureInfo.InvariantCulture), state.GetProperty("tooltip").GetString());
        readings.Add("state", state);
        await CaptureAsync(page, "state", dark, width, state);

        var box = await ReadAsync(page, 5, "box-first", "[data-cfx-observation-id=box-first]");
        Assert.Equal("Latency: Service A", box.GetProperty("title").GetString());
        Assert.Equal("Service A", box.GetProperty("chip").GetString());
        Assert.Equal(3, box.GetProperty("target").GetProperty("numericValue").GetDouble());
        Assert.Equal("3", box.GetProperty("target").GetProperty("value").GetString());
        readings.Add("box", box);
        await CaptureAsync(page, "box", dark, width, box);

        var brush = await BrushTimeAsync(page);
        var detail = brush.GetProperty("detail");
        var raw = detail.GetProperty("range").GetProperty("x");
        var display = detail.GetProperty("displayRange").GetProperty("x");
        Assert.Equal(raw.GetRawText(), detail.GetProperty("categoryRange").GetRawText());
        Assert.NotEqual(raw.GetRawText(), display.GetRawText());
        var ticks = brush.GetProperty("ticks").EnumerateArray().ToArray();
        var expected = raw.EnumerateArray().Select(bound => {
            var instant = DateTimeOffset.Parse(bound.GetString()!, CultureInfo.InvariantCulture).UtcDateTime.ToOADate();
            return ticks.OrderBy(tick => Math.Abs(tick.GetProperty("value").GetDouble() - instant)).First();
        }).ToArray();
        Assert.Equal(string.Join(" – ", expected.Select(tick => tick.GetProperty("text").GetString())), detail.GetProperty("rangeLabel").GetString());
        Assert.Equal(detail.GetProperty("rangeLabel").GetString(), detail.GetProperty("label").GetString());
        for (var index = 0; index < expected.Length; index++)
            Assert.Equal(DateTime.FromOADate(expected[index].GetProperty("value").GetDouble()), DateTimeOffset.Parse(display[index].GetString()!, CultureInfo.InvariantCulture).UtcDateTime);
        Assert.Equal(detail.GetProperty("rangeLabel").GetString(), brush.GetProperty("lasso").GetProperty("rangeLabel").GetString());
        Assert.True(brush.GetProperty("shown").GetBoolean());
        readings.Add("brush", brush);
        await CaptureAsync(page, "brush", dark, width, JsonSerializer.SerializeToElement(readings));
        AssertNoConsoleErrors(session);
    }

    private static string Root(int index) => "[data-cfx-chart-id=readable-values-" + index + "]";

    private static async Task<JsonElement> ReadAsync(IPage page, int index, string observation, string mark) {
        await SelectAsync(page, index, observation);
        var target = page.Locator(Root(index) + " " + mark);
        await target.ScrollIntoViewIfNeededAsync();
        await target.FocusAsync();
        await page.Locator(Root(index) + " .cfx-tooltip").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        return await page.Locator(Root(index)).EvaluateAsync<JsonElement>("""
            root => ({target:ChartForgeXCharts.get(root).getSelection()[0],
              title:root.querySelector('.cfx-tooltip__title').textContent,
              tooltip:root.querySelector('.cfx-tooltip').innerText,
              chip:root.querySelector('.cfx-compare-chip').textContent})
            """);
    }

    private static Task<JsonElement> SelectAsync(IPage page, int index, string observation) => page.Locator(Root(index)).EvaluateAsync<JsonElement>("""
        (root,id) => {const controller=ChartForgeXCharts.get(root);controller.select({observationIds:[id]});return controller.getSelection()[0];}
        """, observation);

    private static async Task<JsonElement> BrushTimeAsync(IPage page) {
        var root = page.Locator(Root(1));
        await root.ScrollIntoViewIfNeededAsync();
        await root.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        var points = await root.EvaluateAsync<JsonElement>("""
            root => {
              window.readableBrush=null;window.readableLasso=null;
              root.addEventListener('cfxbrush',event=>window.readableBrush=event.detail,{once:true});
              root.addEventListener('cfxlasso',event=>window.readableLasso=event.detail,{once:true});
              const svg=root.querySelector('.cfx-stage svg'),plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
              const screen=(x,y)=>{const point=new DOMPoint(plot.x+x*plot.width,plot.y+y*plot.height).matrixTransform(svg.getScreenCTM());return{x:point.x,y:point.y}};
              return{from:screen(.14,.25),to:screen(.76,.75)};
            }
            """);
        await page.Mouse.MoveAsync((float)points.GetProperty("from").GetProperty("x").GetDouble(), (float)points.GetProperty("from").GetProperty("y").GetDouble());
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)points.GetProperty("to").GetProperty("x").GetDouble(), (float)points.GetProperty("to").GetProperty("y").GetDouble(), new MouseMoveOptions { Steps = 4 });
        await page.Mouse.UpAsync();
        return await root.EvaluateAsync<JsonElement>("""
            root => JSON.parse(JSON.stringify({detail:readableBrush,lasso:readableLasso,
              ticks:JSON.parse(root.dataset.cfxPreparedChart).dataProjection.x.displayTicks,
              shown:!root.querySelector('.cfx-brush-box').hidden}))
            """);
    }

    private static string CreatePage(bool dark) {
        Chart Frame(string title) => Chart.Create().WithSize(800, 360).WithDataLabels(false).WithTitle(title)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var time = Frame("Requests over time").WithXAxisTimeScale().WithXAxisBounds(First.ToOADate(), First.AddDays(10).ToOADate())
            .AddSeries(ChartSeries.CreateLine(new ChartSeriesId("time"), "Requests", new[] {
                new ChartPoint(First, 17),new ChartPoint(First.AddDays(5), 27),new ChartPoint(First.AddDays(10), 21) })
                .WithObservationIds(new[] {new ChartObservationId("time-first"),new ChartObservationId("time-middle"),new ChartObservationId("time-last")}));
        time.Options.XAxis.LabelFormatter = value => DateTime.FromOADate(value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var scatter = Frame("Measured samples").AddSeries(new ChartSeries(new ChartSeriesId("scatter"), "Samples", ChartSeriesKind.Scatter,
            new[] { new ChartPoint(ScatterX, ScatterY),new ChartPoint(5.5, 11.25) })
            .WithObservationIds(new[] { new ChartObservationId("scatter-first"),new ChartObservationId("scatter-second") }));
        scatter.Options.XAxis.ValueFormat = ChartValueFormat.Number("0.0", CultureInfo.GetCultureInfo("de-DE"));
        scatter.Options.YAxis.ValueFormat = ChartValueFormat.Number("0.0", CultureInfo.GetCultureInfo("de-DE"));
        var histogram = Frame("Duration distribution").AddSeries(ChartSeries.CreateHistogram(new ChartSeriesId("histogram"), "Duration",
            new[] {30d,45}, ChartHistogramBinLayout.FromBoundaries(new[] {20d,40,60}))
            .WithObservationIds(new[] {new ChartObservationId("histogram-first"),new ChartObservationId("histogram-second")}));
        var state = Frame("Service state").WithXAxisTimeScale().WithStateCategories(new ChartStateCategory("up", "Available", ChartColor.FromRgb(13, 148, 136)))
            .AddStateTimelineLane("Service", new[] {new ChartStateTimelineSegment(First,First.AddHours(6),"up")});
        state.Series[0].WithObservationId(0,new ChartObservationId("state-run"));
        state.Options.XAxis.LabelFormatter = value => DateTime.FromOADate(value).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        var box = Frame("Service latency").WithXLabels(new[] {new ChartAxisLabel(0,"Service A")})
            .AddSeries(ChartSeries.CreateBoxPlot(new ChartSeriesId("box"),"Latency",new[] {new ChartBoxPlot(0,1,2,3,4,5)})
                .WithObservationId(0,new ChartObservationId("box-first")));
        return new HtmlInteractiveDashboardRenderer().RenderPage(new[] {time,scatter,histogram,state,box}, options => {
            options.IdScope = "readable-values";
            options.Toolbar = HtmlChartToolbarLayout.Compact;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Interaction.Enable(ChartInteractionFeatures.Brush);
            options.Interaction.BrushAxis = ChartBrushAxis.X;
            options.Tooltip.PinOnSelect = false;
        });
    }

    private static async Task CaptureAsync(IPage page, string scene, bool dark, int width, JsonElement payload) {
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        var name = "readable-values-" + scene + "-" + (dark ? "dark" : "light") + "-" + width;
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".png") });
        await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), payload.GetRawText());
    }
}
