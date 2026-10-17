using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RuntimeBrushPrecisionBrowserTests {
    private const string Root = ".cfx-interactive-chart";

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FullPlotTimeBrushPreservesPrecisePointAndAxisEndpoints(bool horizontal) {
        if (!Enabled) return;
        var origin = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var instants = new[] { origin.AddTicks(1_000), origin.AddSeconds(5).AddTicks(3_000), origin.AddSeconds(10).AddTicks(5_000) };
        var native = instants.Select(instant => new ChartPoint(instant, 0).X).ToArray();
        var points = instants.Select((instant, index) => new ChartPoint(instant, horizontal ? 2 + index * 3 : native[index])).ToArray();
        var chart = Frame(horizontal).AddSeries(ChartSeries.CreateLine(new ChartSeriesId("precise"), "Precise readings", points)
            .WithObservationIds(new[] { new ChartObservationId("start"), new ChartObservationId("middle"), new ChartObservationId("last") }));
        if (horizontal) {
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            chart.WithXAxisBounds(0, 10).WithYAxisBounds(native[0], native[^1]);
        } else {
            chart.WithXAxisTimeScale().WithXAxisBounds(native[0], native[^1]).WithYAxisBounds(native[0], native[^1]);
        }
        chart.Options.YAxis.Scale = ChartScaleKind.Time;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ConfigureBrush), horizontal ? 390 : 940, 620);
        var errors = new List<string>();
        session.Page.PageError += (_, error) => errors.Add(error);
        // Cross the plot edges by one percent; the public gesture clamps to the native plot bounds.
        await BrushAsync(session.Page, new[] { -.01, -.01, 1.01, 1.01 });
        var facts = await SnapshotAsync(session.Page);
        await CaptureAsync(session.Page, "brush-precise-time-" + (horizontal ? "horizontal-category" : "vertical-xy"), facts);
        var detail = facts.GetProperty("detail");
        foreach (var direction in horizontal ? new[] { "y" } : new[] { "x", "y" }) {
            var axis = facts.GetProperty("projection").GetProperty(direction);
            Assert.Equal(native[0], axis.GetProperty("minimum").GetDouble());
            Assert.Equal(native[^1], axis.GetProperty("maximum").GetDouble());
            var range = detail.GetProperty("range").GetProperty(direction);
            Assert.Equal(instants[0].ToString("O"), range[0].GetString());
            Assert.Equal(instants[^1].ToString("O"), range[1].GetString());
            Assert.Equal(range.GetRawText(), detail.GetProperty("displayRange").GetProperty(direction).GetRawText());
            var bounds = axis.GetProperty("displayBounds").EnumerateArray().ToArray();
            Assert.Equal(instants[^1].ToString("O"), bounds.Single(bound => bound.GetProperty("value").GetDouble() == native[^1]).GetProperty("instant").GetString());
        }
        var selected = facts.GetProperty("selection").EnumerateArray().ToArray();
        Assert.Equal(new[] { "last", "middle", "start" }, selected.Select(target => target.GetProperty("observationId").GetString()).OrderBy(id => id));
        Assert.Equal(instants[^1].ToString("O"), selected.Single(target => target.GetProperty("observationId").GetString() == "last").GetProperty("x").GetString());
        Assert.Equal(selected.Select(target => target.GetProperty("observationId").GetString()).OrderBy(id => id),
            facts.GetProperty("lasso").GetProperty("targets").EnumerateArray().Select(target => target.GetProperty("observationId").GetString()).OrderBy(id => id));
        var replay = await session.Page.EvaluateAsync<string[]>("""
            range=>{const root=document.querySelector('.cfx-interactive-chart'),controller=ChartForgeXCharts.get(root);
              controller.clearSelection();controller.select({ranges:[JSON.parse(range)]});
              return controller.getSelection().map(target=>target.observationId).sort();}
            """, detail.GetProperty("categoryRange").GetRawText());
        Assert.Equal(new[] { "last", "middle", "start" }, replay);
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialNumericAxisAliasesKeepContinuousBrushCaptionsAndMatchingSelection(bool horizontal) {
        if (!Enabled) return;
        var values = new[] { 20d, 40, 60, 80 };
        var labels = new[] { new ChartAxisLabel(0, "Low"), new ChartAxisLabel(100, "High") };
        var chart = Frame(horizontal).AddSeries(ChartSeries.CreateLine(new ChartSeriesId("numeric"), "Measurements", values.Select(value => new ChartPoint(value, 5)))
            .WithObservationIds(values.Select(value => new ChartObservationId("reading-" + value)).ToArray()));
        if (horizontal) {
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            chart.WithXAxisBounds(0, 10).WithYAxisBounds(0, 100);
            chart.Options.YAxis.Labels.AddRange(labels);
        } else {
            chart.WithXAxisBounds(0, 100).WithYAxisBounds(0, 10).WithXLabels((IEnumerable<ChartAxisLabel>)labels);
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ConfigureBrush), horizontal ? 390 : 940, 620);
        var errors = new List<string>();
        session.Page.PageError += (_, error) => errors.Add(error);
        await BrushAsync(session.Page, horizontal ? new[] { .05, .32, .95, .78 } : new[] { .22, .05, .68, .95 });
        var facts = await SnapshotAsync(session.Page);
        await CaptureAsync(session.Page, "brush-numeric-aliases-" + (horizontal ? "horizontal" : "vertical"), facts);
        var detail = facts.GetProperty("detail");
        Assert.False(detail.TryGetProperty("categories", out _));
        var direction = horizontal ? "y" : "x";
        var range = detail.GetProperty("range").GetProperty(direction).EnumerateArray().Select(endpoint => endpoint.GetDouble()).ToArray();
        Assert.InRange(range[0], 0, 22);
        Assert.InRange(range[1], 68, 100);
        Assert.Equal(detail.GetProperty("range").GetProperty(direction).GetRawText(), detail.GetProperty("displayRange").GetProperty(direction).GetRawText());
        var axis = facts.GetProperty("projection").GetProperty(direction);
        var ticks = axis.GetProperty("displayTicks").EnumerateArray().Concat(axis.GetProperty("displayBounds").EnumerateArray()).ToArray();
        var captions = range.Select(endpoint => ticks.First(tick => tick.GetProperty("value").GetDouble() == endpoint).GetProperty("text").GetString()).ToArray();
        Assert.All(captions, caption => Assert.False(string.IsNullOrWhiteSpace(caption)));
        Assert.Equal(string.Join(" – ", captions), detail.GetProperty("rangeLabel").GetString());
        Assert.Equal(detail.GetProperty("rangeLabel").GetString(), detail.GetProperty("label").GetString());
        var expectedIds = values.Where(value => value >= range[0] && value <= range[1]).Select(value => "reading-" + value).OrderBy(id => id).ToArray();
        Assert.NotEmpty(expectedIds);
        Assert.Equal(expectedIds, facts.GetProperty("selection").EnumerateArray().Select(target => target.GetProperty("observationId").GetString()).OrderBy(id => id));
        Assert.Equal(expectedIds, facts.GetProperty("lasso").GetProperty("targets").EnumerateArray().Select(target => target.GetProperty("observationId").GetString()).OrderBy(id => id));
        Assert.Empty(errors);
        AssertNoConsoleErrors(session);
    }

    private static Chart Frame(bool horizontal) => Chart.Create().WithSize(horizontal ? 340 : 760, 390)
        .WithHeader(false).WithLegend(false).WithDataLabels(false)
        .WithTheme(VisualTheme.Graphite(), horizontal ? VisualThemeMode.Dark : VisualThemeMode.Light);

    private static void ConfigureBrush(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Enable(ChartInteractionFeatures.Brush);
        options.Interaction.BrushAxis = ChartBrushAxis.XY;
        options.Interaction.Disable(ChartInteractionFeatures.CompareMarkers);
    }

    private static async Task BrushAsync(IPage page, double[] ratios) {
        var positions = await page.Locator(Root).EvaluateAsync<double[]>("""
            (root,ratios)=>{
              window.precisionBrush=null;window.precisionLasso=null;
              root.addEventListener('cfxbrush',event=>precisionBrush=event.detail,{once:true});
              root.addEventListener('cfxlasso',event=>precisionLasso=event.detail,{once:true});
              const p=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds,m=root.querySelector('.cfx-stage svg').getScreenCTM();
              const a=new DOMPoint(p.x+p.width*ratios[0],p.y+p.height*ratios[1]).matrixTransform(m),
                b=new DOMPoint(p.x+p.width*ratios[2],p.y+p.height*ratios[3]).matrixTransform(m);
              return[a.x,a.y,b.x,b.y];
            }
            """, ratios);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await page.Mouse.MoveAsync((float)positions[0], (float)positions[1]);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)positions[2], (float)positions[3], new MouseMoveOptions { Steps = 4 });
        await page.Mouse.UpAsync();
    }

    private static Task<JsonElement> SnapshotAsync(IPage page) => page.Locator(Root).EvaluateAsync<JsonElement>("""
        root=>JSON.parse(JSON.stringify({detail:precisionBrush,lasso:precisionLasso,
          projection:JSON.parse(root.dataset.cfxPreparedChart).dataProjection,selection:ChartForgeXCharts.get(root).getSelection()}))
        """);

    private static async Task CaptureAsync(IPage page, string name, JsonElement facts) {
        var output = Environment.GetEnvironmentVariable("CFX_V2_CAPTURE_ROOT");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".png") });
        await File.WriteAllTextAsync(Path.Combine(output, name + ".json"), facts.GetRawText());
    }
}
