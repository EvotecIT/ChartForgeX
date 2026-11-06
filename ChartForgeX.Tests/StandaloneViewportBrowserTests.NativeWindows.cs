using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    // A family whose saved window must paint what a native preparation of the same bounds paints. Bounds name the
    // physical axes: on horizontal charts X is the value axis across the plot and secondaryX the value axis along the top.
    private sealed record NativeWindowCase(Func<IReadOnlyDictionary<string, (double Minimum, double Maximum)>?, Chart> Build,
        Dictionary<string, (double Minimum, double Maximum)> Window, string Mark, int CategoryDigits = 3);

    private static NativeWindowCase NativeWindow(string name) {
        var id = new ChartSeriesId("samples");
        var points = Enumerable.Range(0, 13).Select(index => new ChartPoint(index, 10 + index * 6)).ToArray();
        Chart Base(IReadOnlyDictionary<string, (double Minimum, double Maximum)>? bounds) {
            var chart = Chart.Create().WithSize(800, 440).WithTitle("Saved " + name).WithLegend().WithDataLabels(false);
            if (bounds == null) return chart;
            foreach (var (axis, (minimum, maximum)) in bounds) _ = axis switch {
                "x" => chart.WithXAxisBounds(minimum, maximum), "y" => chart.WithYAxisBounds(minimum, maximum),
                "secondaryX" => chart.WithSecondaryXAxisBounds(minimum, maximum), _ => chart.WithSecondaryYAxisBounds(minimum, maximum)
            };
            return chart;
        }
        Dictionary<string, (double, double)> Window(params (string Axis, double Minimum, double Maximum)[] axes) => axes.ToDictionary(axis => axis.Axis, axis => (axis.Minimum, axis.Maximum));
        const string Point = "[data-cfx-series-key=samples][data-cfx-point='4'] ";
        return name switch {
            "lollipop" => new(bounds => Base(bounds).AddSeries(new ChartSeries(id, "Samples", ChartSeriesKind.Lollipop, points)),
                Window(("x", 2, 10), ("y", 20, 80)), Point + "ellipse"),
            "dumbbell" => new(bounds => Base(bounds).AddSeries(ChartSeries.CreateDumbbell(id, "Samples", points.Take(7).Select(point => new ChartDumbbell(point.X, point.Y, point.Y + 20)))),
                Window(("x", 0.5, 5.5), ("y", 20, 70)), Point + "ellipse"),
            "range-bar" => new(bounds => Base(bounds).AddSeries(ChartSeries.CreateRangeBar(id, "Samples", points.Take(7).Select(point => new ChartInterval(point.X, point.Y, point.Y + 20)))),
                Window(("x", 1.5, 5.5), ("y", 20, 70)), Point + "rect"),
            "horizontal-lollipop" => new(bounds => {
                    var chart = Base(bounds).AddSeries(new ChartSeries(id, "Samples", ChartSeriesKind.Lollipop, points));
                    chart.Options.LollipopOrientation = ChartOrientation.Horizontal;
                    return chart;
                }, Window(("x", 20, 80), ("y", 1.5, 7.5)), Point + "ellipse"),
            "horizontal-secondary" => new(bounds => Base(bounds).WithSecondaryYAxis("Throughput")
                    .AddSeries(new ChartSeries(id, "Samples", ChartSeriesKind.HorizontalBar, points.Take(6)))
                    .AddSeries(new ChartSeries(new ChartSeriesId("throughput"), "Throughput", ChartSeriesKind.HorizontalBar,
                        points.Take(6).Select(point => new ChartPoint(point.X, point.Y * 1000))).UseSecondaryValueAxis()),
                // Grouped bars keep their drawn share of the band, while a native window re-fits the slots to within a quarter pixel.
                Window(("x", 0, 30), ("secondaryX", 0, 30000), ("y", 0.5, 4.5)), "[data-cfx-series-key=throughput][data-cfx-point='2'] path", CategoryDigits: 2),
            "symmetric-log" => new(bounds => Base(bounds).WithYAxisScale(ChartScaleKind.SymmetricLogarithmic)
                    .AddSeries(ChartSeries.CreateLine(id, "Samples", points.Select(point => new ChartPoint(point.X, (point.X - 6) * Math.Abs(point.X - 6) * 40)))
                        .ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; })),
                Window(("x", 2, 10), ("y", -100, 1000)), "[data-cfx-series-key=samples][data-cfx-point='7'] :is(circle,ellipse,path)"),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }

    [BrowserTheory]
    [InlineData("lollipop")]
    [InlineData("dumbbell")]
    [InlineData("range-bar")]
    [InlineData("horizontal-lollipop")]
    [InlineData("horizontal-secondary")]
    [InlineData("symmetric-log")]
    public async Task SavedFamilyWindowPaintsTheTicksAndMarkPositionOfANativePreparation(string name) {
        if (!Enabled) return;
        var family = NativeWindow(name);
        await using var session = await OpenAsync(FamilyPage(family.Build(null)), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-standalone-projection"));
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-zoom-declined"));
        var window = "{" + string.Join(",", family.Window.Select(axis => FormattableString.Invariant($"{axis.Key}:{{minimum:{axis.Value.Minimum},maximum:{axis.Value.Maximum}}}"))) + "}";
        await page.EvaluateAsync("()=>controller.setViewport(" + window + ")");
        await SettleMotionAsync(page);
        var saved = await MarkRatioAsync(page, family.Mark);
        await CaptureAsync(page, "native-window-" + name);

        var nativeChart = family.Build(family.Window);
        await using var nativeSession = await OpenAsync(FamilyPage(nativeChart), 980, 800);
        await MountAsync(nativeSession.Page);
        var native = await MarkRatioAsync(nativeSession.Page, family.Mark);
        // The plot keeps its drawn bounds, so marks compare by their place within it.
        var horizontal = name.StartsWith("horizontal", StringComparison.Ordinal);
        Assert.Equal(native.GetProperty("x").GetDouble(), saved.GetProperty("x").GetDouble(), horizontal ? 3 : family.CategoryDigits);
        Assert.Equal(native.GetProperty("y").GetDouble(), saved.GetProperty("y").GetDouble(), horizontal ? family.CategoryDigits : 3);

        var html = nativeChart.ToInteractiveHtmlFragmentWithoutAssets();
        using var metadata = JsonDocument.Parse(WebUtility.HtmlDecode(Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"").Groups[1].Value));
        var svg = XDocument.Parse(html.Substring(html.IndexOf("<svg", StringComparison.Ordinal), html.LastIndexOf("</svg>", StringComparison.Ordinal) + 6 - html.IndexOf("<svg", StringComparison.Ordinal)));
        foreach (var axis in family.Window.Keys) {
            var role = "axis-" + axis.Replace("secondary", "secondary-").ToLowerInvariant() + "-label";
            var captions = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == role).Select(element => element.Value).ToArray();
            var painted = (await page.Locator("[data-cfx-browser-axis=" + axis + "] text[data-cfx-axis-value]").EvaluateAllAsync<JsonElement>(
                "nodes=>nodes.map(node=>({value:Number(node.dataset.cfxAxisValue),text:node.textContent}))")).EnumerateArray().ToArray();
            Assert.True(captions.Length >= 2, "The native " + axis + " window must caption at least two ticks.");
            Assert.Equal(captions, painted.Select(tick => tick.GetProperty("text").GetString()));
            if (metadata.RootElement.GetProperty("dataProjection").TryGetProperty(axis, out var projection))
                Assert.Equal(projection.GetProperty("displayTicks").EnumerateArray().Select(tick => Math.Round(tick.GetProperty("value").GetDouble(), 9)),
                    painted.Select(tick => Math.Round(tick.GetProperty("value").GetDouble(), 9)));
        }
        AssertNoConsoleErrors(session);
    }

    // A mark's centre as a fraction of the plot it is drawn in.
    private static Task<JsonElement> MarkRatioAsync(IPage page, string selector) => page.EvaluateAsync<JsonElement>("""
        selector=>{
            const root=controller.root,svg=root.querySelector('.cfx-stage svg'),mark=root.querySelector(selector);
            if(!mark)throw Error('Fixture needs painted geometry for '+selector);
            const canvas=svg.getBoundingClientRect(),view=svg.viewBox.baseVal,box=mark.getBoundingClientRect();
            const x=view.x+(box.x+box.width/2-canvas.x)*view.width/canvas.width,y=view.y+(box.y+box.height/2-canvas.y)*view.height/canvas.height;
            const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
            return {x:(x-plot.x)/plot.width,y:(y-plot.y)/plot.height};
        }
        """, selector);
}
