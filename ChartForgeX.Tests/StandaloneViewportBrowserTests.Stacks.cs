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
    private static readonly double[][] StackValues = {
        new double[] { 12, 18, 9, 22, 15, 11 },
        new double[] { 8, -6, 14, 10, 7, 16 },
        new double[] { 5, 9, 11, 6, 13, 8 }
    };

    // Stacked columns (one negative contribution), stacked bars, 100% columns and stacked areas,
    // optionally without one series and inside fixed native windows.
    private static Chart StackedChart(string family, string? omit = null, (double, double)? category = null, (double, double)? value = null) {
        var kind = family switch { "horizontal" => ChartSeriesKind.HorizontalBar, "area" => ChartSeriesKind.StackedArea, _ => ChartSeriesKind.Bar };
        var chart = Chart.Create().WithSize(800, 440).WithTitle("Saved " + family + " stack").WithLegend().WithDataLabels(family == "column");
        if (kind != ChartSeriesKind.StackedArea) chart.WithStackedBars();
        // Columns also caption their segments and stack totals.
        if (family == "column") chart.WithStackTotals();
        for (var index = 0; index < StackValues.Length; index++) {
            if ("s" + index == omit) continue;
            var values = family == "column" ? StackValues[index] : StackValues[index].Select(Math.Abs).ToArray();
            var series = new ChartSeries(new ChartSeriesId("s" + index), "S" + index, kind, values.Select((y, x) => new ChartPoint(x, y)));
            if (family == "percent") series.WithNormalization();
            chart.AddSeries(series);
        }
        // Axis bounds name the physical axes, so a horizontal chart's categories run on Y.
        var horizontal = kind == ChartSeriesKind.HorizontalBar;
        if (category is var (cMin, cMax)) { if (horizontal) chart.WithYAxisBounds(cMin, cMax); else chart.WithXAxisBounds(cMin, cMax); }
        if (value is var (vMin, vMax)) { if (horizontal) chart.WithXAxisBounds(vMin, vMax); else chart.WithYAxisBounds(vMin, vMax); }
        return chart;
    }

    [BrowserTheory]
    [InlineData("column")]
    [InlineData("horizontal")]
    [InlineData("percent")]
    [InlineData("area")]
    public async Task SavedStackZoomsWithNativeTicksAndLegendTogglesRestackToNativePositions(string family) {
        if (!Enabled) return;
        var horizontal = family == "horizontal";
        string categoryAxis = horizontal ? "y" : "x", valueAxis = horizontal ? "x" : "y";
        await using var session = await OpenAsync(FamilyPage(StackedChart(family)), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-standalone-projection"));
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-zoom-declined"));
        var initial = await StackSceneAsync(page);

        // A window repaints the category and value captions native preparation paints for that window.
        await page.EvaluateAsync("([category,value,maximum])=>controller.setViewport({[category]:{minimum:0.5,maximum:3.5},[value]:{minimum:0,maximum}})",
            new object[] { categoryAxis, valueAxis, family == "percent" ? 60 : 30 });
        var zoomed = await ViewportAsync(page);
        var window = (Bounds(zoomed.GetProperty(categoryAxis)), Bounds(zoomed.GetProperty(valueAxis)));
        var native = NativeStack(StackedChart(family, category: window.Item1, value: window.Item2));
        Assert.Equal(window.Item1, native.Windows[categoryAxis]);
        Assert.Equal(window.Item2, native.Windows[valueAxis]);
        Assert.Equal(native.Captions[categoryAxis], await PaintedCaptionsAsync(page, categoryAxis));
        Assert.Equal(native.Captions[valueAxis], await PaintedCaptionsAsync(page, valueAxis));
        // Captions follow their marks; one whose mark left the window hides rather than floating outside the plot.
        Assert.All((await StackSceneAsync(page)).GetProperty("captions").EnumerateArray().Where(caption => caption.GetProperty("visible").GetBoolean()),
            caption => Assert.True(caption.GetProperty("inside").GetBoolean(), "A visible caption must sit inside the plot."));
        await CaptureAsync(page, "stack-" + family + "-zoomed");

        // Hiding the middle series restacks the others from the captured observations and re-fits the value axis.
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=s1]").ClickAsync();
        await SettleMotionAsync(page);
        var hidden = await ViewportAsync(page);
        Assert.Equal(window.Item1, Bounds(hidden.GetProperty(categoryAxis)));
        var refit = Bounds(hidden.GetProperty(valueAxis));
        if (family != "percent") Assert.NotEqual(window.Item2, refit);
        native = NativeStack(StackedChart(family, omit: "s1", category: window.Item1, value: refit));
        var scene = await StackSceneAsync(page);
        AssertRestacked(native, scene, family, hidden);
        // Segment captions ride on their restacked segments; every total changed, and its native caption hides.
        foreach (var caption in scene.GetProperty("captions").EnumerateArray().Where(caption => caption.GetProperty("visible").GetBoolean())) {
            Assert.False(caption.GetProperty("total").GetBoolean(), "A total whose stack lost a member must hide.");
            Assert.True(caption.GetProperty("onSegment").GetBoolean(), "A segment caption must sit on its restacked segment.");
        }
        await CaptureAsync(page, "stack-" + family + "-restacked");

        await page.Locator("[data-cfx-reset]").ClickAsync();
        await SettleMotionAsync(page);
        var reset = await StackSceneAsync(page);
        Assert.Equal(initial.GetProperty("geometry").GetString(), reset.GetProperty("geometry").GetString());
        Assert.Equal(initial.GetProperty("facts").GetRawText(), reset.GetProperty("facts").GetRawText());
        Assert.Equal(0, await page.Locator("[data-cfx-browser-axis]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    private static (double, double) Bounds(JsonElement window) => (window.GetProperty("minimum").GetDouble(), window.GetProperty("maximum").GetDouble());

    private sealed record NativeStackScene(Dictionary<string, string[]> Captions, JsonElement Plot, Dictionary<string, (double, double)> Windows, XElement[] Points, XElement[] Lines);

    // The SVG and captured projection a native preparation paints for the same chart and windows.
    private static NativeStackScene NativeStack(Chart chart) {
        var html = chart.ToInteractiveHtmlFragmentWithoutAssets();
        var metadata = JsonDocument.Parse(WebUtility.HtmlDecode(Regex.Match(html, "data-cfx-prepared-chart=\"([^\"]+)\"").Groups[1].Value)).RootElement;
        var svg = XDocument.Parse(html.Substring(html.IndexOf("<svg", StringComparison.Ordinal), html.LastIndexOf("</svg>", StringComparison.Ordinal) + 6 - html.IndexOf("<svg", StringComparison.Ordinal)));
        string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");
        var captions = new[] { "x", "y" }.ToDictionary(axis => axis, axis => svg.Descendants().Where(element => Role(element) == "axis-" + axis + "-label").Select(element => element.Value).ToArray());
        var windows = new[] { "x", "y" }.ToDictionary(axis => axis, axis => Bounds(metadata.GetProperty("dataProjection").GetProperty(axis)));
        return new(captions, metadata.GetProperty("dataProjection").GetProperty("plotBounds"), windows,
            svg.Descendants().Where(element => Role(element) == "point").ToArray(),
            svg.Descendants().Where(element => Role(element) is "line" or "bar" or "horizontal-bar").ToArray());
    }

    private static async Task<string[]> PaintedCaptionsAsync(IPage page, string axis) {
        await SettleMotionAsync(page);
        return (await page.Locator("[data-cfx-browser-axis=" + axis + "] text[data-cfx-axis-value]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.textContent)"));
    }

    private static void AssertRestacked(NativeStackScene native, JsonElement scene, string family, JsonElement viewport) {
        var horizontal = family == "horizontal";
        // Tooltip and readout facts carry the restacked baseline, end and total the native preparation computes.
        foreach (var point in scene.GetProperty("points").EnumerateArray()) {
            var key = point.GetProperty("key").GetString(); var index = point.GetProperty("index").GetString();
            Assert.NotEqual("s1", key);
            var expected = native.Points.SingleOrDefault(element => (string?)element.Attribute("data-cfx-series-key") == key && (string?)element.Attribute("data-cfx-point") == index);
            if (expected == null) continue;
            foreach (var fact in new[] { "base", "stack-end", "stack-total" })
                Assert.Equal(double.Parse((string)expected.Attribute("data-cfx-" + fact)!, System.Globalization.CultureInfo.InvariantCulture),
                    point.GetProperty(fact).GetDouble(), 9);
        }
        // Painted ink sits where native ink sits, compared in data units since each drawing has its own plot.
        var plot = scene.GetProperty("plot");
        (double Category, double Value) Data(JsonElement bounds, Dictionary<string, (double, double)> windows, double x, double y) {
            double Ratio(double coordinate, double start, double size) => (coordinate - start) / size;
            var across = Ratio(x, bounds.GetProperty("x").GetDouble(), bounds.GetProperty("width").GetDouble());
            var down = 1 - Ratio(y, bounds.GetProperty("y").GetDouble(), bounds.GetProperty("height").GetDouble());
            double At((double Minimum, double Maximum) window, double ratio) => window.Minimum + ratio * (window.Maximum - window.Minimum);
            return horizontal ? (At(windows["y"], 1 - down), At(windows["x"], across)) : (At(windows["x"], across), At(windows["y"], down));
        }
        var browserWindows = new[] { "x", "y" }.ToDictionary(axis => axis, axis => Bounds(viewport.GetProperty(axis)));
        var valueSpan = horizontal ? browserWindows["x"].Item2 - browserWindows["x"].Item1 : browserWindows["y"].Item2 - browserWindows["y"].Item1;
        var tolerance = 1.5 * valueSpan / (horizontal ? plot.GetProperty("width").GetDouble() : plot.GetProperty("height").GetDouble());
        var compared = 0;
        foreach (var ink in scene.GetProperty("ink").EnumerateArray()) {
            var key = ink.GetProperty("key").GetString();
            var index = ink.GetProperty("index").GetString();
            var expected = native.Lines.Where(element => element.Ancestors().Any(owner => (string?)owner.Attribute("data-cfx-series-key") == key)
                && (family == "area" || (string?)element.Parent?.Parent?.Attribute("data-cfx-point") == index)).ToArray();
            if (family == "area") {
                var nativeVertices = Vertices((string)expected.Single().Attribute("d")!).Select(vertex => Data(native.Plot, native.Windows, vertex.X, vertex.Y)).ToArray();
                foreach (var vertex in Vertices(ink.GetProperty("d").GetString()!).Select(vertex => Data(plot, browserWindows, vertex.X, vertex.Y))) {
                    var match = nativeVertices.FirstOrDefault(candidate => Math.Abs(candidate.Category - vertex.Category) < .01);
                    if (match == default) continue;
                    Assert.InRange(vertex.Value, match.Value - tolerance, match.Value + tolerance); compared++;
                }
                continue;
            }
            if (expected.Length == 0) continue;
            var rect = Assert.Single(expected);
            double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, System.Globalization.CultureInfo.InvariantCulture);
            var first = Data(native.Plot, native.Windows, Number(rect, "x"), Number(rect, "y"));
            var last = Data(native.Plot, native.Windows, Number(rect, "x") + Number(rect, "width"), Number(rect, "y") + Number(rect, "height"));
            var box = ink.GetProperty("box");
            var paintedFirst = Data(plot, browserWindows, box.GetProperty("x").GetDouble(), box.GetProperty("y").GetDouble());
            var paintedLast = Data(plot, browserWindows, box.GetProperty("x").GetDouble() + box.GetProperty("width").GetDouble(), box.GetProperty("y").GetDouble() + box.GetProperty("height").GetDouble());
            Assert.Equal((first.Category + last.Category) / 2, (paintedFirst.Category + paintedLast.Category) / 2, 2);
            Assert.InRange(Math.Min(paintedFirst.Value, paintedLast.Value), Math.Min(first.Value, last.Value) - tolerance, Math.Min(first.Value, last.Value) + tolerance);
            Assert.InRange(Math.Max(paintedFirst.Value, paintedLast.Value), Math.Max(first.Value, last.Value) - tolerance, Math.Max(first.Value, last.Value) + tolerance);
            // The stack's rounded silhouette follows its restacked ends, so no segment is cut short.
            Assert.True(ink.GetProperty("clipped").GetDouble() < .5, key + "/" + index + " is clipped by a stale stack silhouette.");
            compared++;
        }
        Assert.True(compared >= 6, "The restack must compare painted ink against native ink.");
    }

    private static IEnumerable<(double X, double Y)> Vertices(string path) {
        var numbers = Regex.Matches(path, @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        for (var index = 0; index + 1 < numbers.Length; index += 2) yield return (numbers[index], numbers[index + 1]);
    }

    private static async Task<JsonElement> StackSceneAsync(IPage page) {
        await SettleMotionAsync(page);
        return await page.EvaluateAsync<JsonElement>("""
            ()=>{
                const root=controller.root,svg=root.querySelector('.cfx-stage svg');
                const visible=node=>!node.closest('.cfx-series-muted');
                const ink=[...svg.querySelectorAll('[data-cfx-role=series] :is([data-cfx-role=bar],[data-cfx-role=horizontal-bar],[data-cfx-role=line])')].filter(node=>visible(node)&&node.closest('[data-cfx-point]')?.dataset.cfxVisible!=='false').map(node=>{
                    const owner=node.closest('[data-cfx-point]'),box=node.getBBox();
                    // How much of the segment its stack silhouette cuts away, in drawing units.
                    const clipNode=node.closest('[clip-path]'),id=clipNode?.getAttribute('clip-path').match(/#([^)]+)/)?.[1];
                    // Firefox measures an unrendered clip definition as empty, so measure a rendered copy of it.
                    const shape=owner&&id&&svg.getElementById(id)?.querySelector('path,rect'),copy=shape?.cloneNode(true);if(copy){copy.removeAttribute('id');svg.appendChild(copy);}
                    const clip=copy?.getBBox();copy?.remove();
                    const clipped=clip?Math.max(0,clip.x-box.x,clip.y-box.y,box.x+box.width-clip.x-clip.width,box.y+box.height-clip.y-clip.height):0;
                    return {key:node.closest('[data-cfx-series-key]').dataset.cfxSeriesKey,index:owner?.dataset.cfxPoint??null,d:node.getAttribute('d'),
                        box:{x:box.x,y:box.y,width:box.width,height:box.height},clipped};
                });
                const points=[...svg.querySelectorAll('[data-cfx-role=series] [data-cfx-point][data-cfx-stack-end]')].filter(visible).map(node=>({key:node.dataset.cfxSeriesKey,
                    index:node.dataset.cfxPoint,base:Number(node.dataset.cfxBase),'stack-end':Number(node.dataset.cfxStackEnd),'stack-total':Number(node.dataset.cfxStackTotal)}));
                const facts=[...svg.querySelectorAll('[data-cfx-point][data-cfx-stack-end]')].map(node=>[node.dataset.cfxBase,node.dataset.cfxStackEnd,node.dataset.cfxRenderedY,node.dataset.cfxStackTotal].join());
                const geometry=[...svg.querySelectorAll('[data-cfx-role=series] :is(path,rect),clipPath :is(path,rect)')].map(node=>['d','x','y','width','height'].map(key=>node.getAttribute(key)).join()).join('|');
                const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
                const captions=[...svg.querySelectorAll('[data-cfx-label-for],[data-cfx-role=stack-total-label-source]')].map(node=>{
                    const box=node.getBoundingClientRect(),canvas=svg.getBoundingClientRect(),scale=canvas.width/svg.viewBox.baseVal.width;
                    const x=svg.viewBox.baseVal.x+(box.x+box.width/2-canvas.x)/scale,y=svg.viewBox.baseVal.y+(box.y+box.height/2-canvas.y)/scale;
                    const ink=node.dataset.cfxLabelFor&&svg.querySelector('[data-cfx-source-id=\x22'+node.dataset.cfxLabelFor+'\x22] :is(rect,path)')?.getBoundingClientRect();
                    return {total:!node.dataset.cfxLabelFor,visible:getComputedStyle(node).visibility!=='hidden'&&!node.closest('.cfx-series-muted'),
                        inside:x>=plot.x&&x<=plot.x+plot.width&&y>=plot.y-24&&y<=plot.y+plot.height+24,
                        onSegment:!!ink&&box.y+box.height/2>=ink.y-1&&box.y+box.height/2<=ink.y+ink.height+1};
                });
                return {plot,ink,points,facts,geometry,captions};
            }
            """);
    }
}
