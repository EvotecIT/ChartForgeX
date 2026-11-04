using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    // One observation per family, measured where its own ink sits: data coordinates of the mark centre
    // (or of a band vertex) and the window that keeps it on screen.
    private sealed record FamilyCase(string Name, Chart Chart, string Mark, double X, double Y, string Windows, string CategoryAxis,
        string ValueAxis = "y", bool Logarithmic = false, bool Vertex = false, string? SecondaryMark = null, double SecondaryY = 0);

    private static FamilyCase Family(string name) {
        var points = Enumerable.Range(0, 13).Select(index => new ChartPoint(index, 10 + index * 6)).ToArray();
        var id = new ChartSeriesId("samples");
        Chart Base() => Chart.Create().WithSize(800, 440).WithTitle("Saved " + name).WithLegend().WithDataLabels(false);
        ChartSeries Marked(ChartSeries series) => series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; });
        const string Observation = "[data-cfx-series-key=samples][data-cfx-point='4'] ";
        return name switch {
            "horizontal-bar" => new(name, Base().AddSeries(new ChartSeries(id, "Samples", ChartSeriesKind.HorizontalBar, points.Take(6))),
                "[data-cfx-series-key=samples][data-cfx-point='2'] path,[data-cfx-series-key=samples][data-cfx-point='2'] rect", 11, 2,
                "{x:{minimum:0,maximum:30},y:{minimum:0.5,maximum:3.5}}", "y", "x"),
            "logarithmic" => new(name, Base().WithYAxisScale(ChartScaleKind.Logarithmic).AddSeries(Marked(ChartSeries.CreateLine(id, "Samples",
                Enumerable.Range(0, 13).Select(index => new ChartPoint(index, Math.Pow(10, index / 3.0)))))),
                "[data-cfx-series-key=samples][data-cfx-point='6'] :is(circle,ellipse,path)",
                6, 100, "{x:{minimum:3,maximum:9},y:{minimum:10,maximum:1000}}", "x", Logarithmic: true),
            "secondary-y" => new(name, Base().WithSecondaryYAxis("Throughput")
                    .AddSeries(Marked(ChartSeries.CreateLine(id, "Samples", points)))
                    .AddSeries(Marked(ChartSeries.CreateLine(new ChartSeriesId("throughput"), "Throughput", points.Select(point => new ChartPoint(point.X, point.Y * 1000)))).UseSecondaryValueAxis()),
                Observation + ":is(circle,ellipse,path)", 4, 34, "{x:{minimum:2,maximum:10},y:{minimum:20,maximum:80},secondaryY:{minimum:30000,maximum:90000}}", "x",
                SecondaryMark: "[data-cfx-series-key=throughput][data-cfx-point='4'] :is(circle,ellipse,path)", SecondaryY: 34000),
            "bubble" => new(name, Base().AddSeries(ChartSeries.CreateBubble(id, "Samples", points.Select(point => new ChartBubble(point.X, point.Y, point.X + 1)))),
                Observation + "[data-cfx-role=bubble]", 4, 34, "{x:{minimum:2,maximum:8},y:{minimum:20,maximum:80}}", "x"),
            "range-band" => new(name, Base().AddSeries(ChartSeries.CreateRangeBand(id, "Samples", points.Select(point => new ChartRangeBand(point.X, point.Y - 5, point.Y + 5)))),
                "[data-cfx-series-key=samples] [data-cfx-role=range-upper]", 4, 39, "{x:{minimum:2,maximum:8},y:{minimum:20,maximum:80}}", "x", Vertex: true),
            "box-plot" => new(name, Base().AddSeries(ChartSeries.CreateBoxPlot(id, "Samples", points.Take(5).Select(point => new ChartBoxPlot(point.X, point.Y - 8, point.Y - 3, point.Y, point.Y + 3, point.Y + 8)))),
                "[data-cfx-series-key=samples][data-cfx-point='2'] [data-cfx-role=boxplot-body]", 2, 22, "{x:{minimum:0.5,maximum:3.5},y:{minimum:0,maximum:40}}", "x"),
            "error-bar" => new(name, Base().AddSeries(ChartSeries.CreateErrorBar(id, "Samples", points.Select(point => new ChartErrorBar(point.X, point.Y, null, new ChartErrorBounds(point.Y - 4, point.Y + 4))))),
                Observation + "[data-cfx-role=error-marker]", 4, 34, "{x:{minimum:2,maximum:8},y:{minimum:20,maximum:80}}", "x"),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }

    private static string FamilyPage(Chart chart) => chart.ToInteractiveHtmlPage(options => {
        options.Interaction.ChartId = "standalone-family";
        options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.LegendToggles | ChartInteractionFeatures.StateBookmarks);
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
    });

    [BrowserTheory]
    [InlineData("horizontal-bar")]
    [InlineData("logarithmic")]
    [InlineData("secondary-y")]
    [InlineData("bubble")]
    [InlineData("range-band")]
    [InlineData("box-plot")]
    [InlineData("error-bar")]
    public async Task SavedFamilyWindowRepaintsReadableTicksAlignsMarksAndResetRestoresTheDrawing(string name) {
        if (!Enabled) return;
        var family = Family(name);
        await using var session = await OpenAsync(FamilyPage(family.Chart), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-standalone-projection"));
        var fullX = Expected(family, await PlotAsync(page), await ViewportAsync(page)).X;
        var initial = await FamilySceneAsync(page, family.Mark, family.Vertex ? fullX : null);

        await page.EvaluateAsync("()=>controller.setViewport(" + family.Windows + ")");
        var viewport = await ViewportAsync(page);
        var plot = await PlotAsync(page);
        var expected = Expected(family, plot, viewport);
        var scene = await FamilySceneAsync(page, family.Mark, family.Vertex ? expected.X : null);
        Assert.InRange(scene.GetProperty("x").GetDouble(), expected.X - .05, expected.X + .05);
        Assert.InRange(scene.GetProperty("y").GetDouble(), expected.Y - .05, expected.Y + .05);
        // Only data ink moves: there is no whole-drawing scale and marker radii keep their drawn size.
        Assert.Equal("", scene.GetProperty("zoom").GetString());
        if (!family.Vertex && name != "horizontal-bar" && name != "box-plot")
            Assert.Equal(initial.GetProperty("width").GetDouble(), scene.GetProperty("width").GetDouble(), 1);
        foreach (var axis in new[] { "x", "y", "secondaryY" }.Where(axis => viewport.TryGetProperty(axis, out _) && family.Windows.Contains(axis + ":", StringComparison.Ordinal)))
            AssertAlignedTicks(scene, viewport, plot, axis, family);
        if (family.SecondaryMark != null) {
            var secondary = await FamilySceneAsync(page, family.SecondaryMark, null);
            var window = viewport.GetProperty("secondaryY");
            Assert.InRange(secondary.GetProperty("y").GetDouble(), Coordinate(plot, window, family.SecondaryY, true, false, false) - .05,
                Coordinate(plot, window, family.SecondaryY, true, false, false) + .05);
        }
        await CaptureAsync(page, "family-" + name);

        // Toolbar zoom narrows the category direction and still re-projects instead of scaling the drawing.
        await page.Locator("[data-cfx-reset]").ClickAsync();
        await SettleMotionAsync(page);
        await page.Locator(".cfx-stage").HoverAsync();
        await page.Locator("[data-cfx-zoom=in]").ClickAsync();
        var zoomed = (await ViewportAsync(page)).GetProperty(family.CategoryAxis);
        Assert.True(zoomed.GetProperty("maximum").GetDouble() - zoomed.GetProperty("minimum").GetDouble() < (await FullSpanAsync(page, family.CategoryAxis)) - 1e-9);
        Assert.Equal("", (await FamilySceneAsync(page, family.Mark, family.Vertex ? expected.X : null)).GetProperty("zoom").GetString());

        await page.Locator("[data-cfx-reset]").ClickAsync();
        var reset = await FamilySceneAsync(page, family.Mark, family.Vertex ? fullX : null);
        Assert.Equal(initial.GetProperty("x").GetDouble(), reset.GetProperty("x").GetDouble(), 2);
        Assert.Equal(initial.GetProperty("y").GetDouble(), reset.GetProperty("y").GetDouble(), 2);
        Assert.Equal(0, await page.Locator("[data-cfx-browser-axis]").CountAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Heatmap)]
    public async Task SavedFamilyWithoutDataWindowDeclinesZoomInsteadOfScalingTheDrawing(ChartSeriesKind kind) {
        if (!Enabled) return;
        var points = kind == ChartSeriesKind.Heatmap ? Enumerable.Range(0, 16).Select(index => new ChartPoint(index % 4, index / 4)).ToArray()
            : new[] { new ChartPoint(0, 4), new ChartPoint(1, 3), new ChartPoint(2, 2) };
        var chart = Chart.Create().WithSize(800, 440).WithTitle("Declined " + kind).AddSeries(new ChartSeries(new ChartSeriesId("cells"), "Cells", kind, points));
        await using var session = await OpenAsync(FamilyPage(chart), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-zoom-declined"));
        foreach (var selector in new[] { "[data-cfx-zoom=in]", "[data-cfx-zoom=out]", "[data-cfx-mode-button=pan]" }) {
            var button = page.Locator(selector);
            Assert.True(await button.IsDisabledAsync(), selector + " must be disabled.");
            Assert.Equal("Zoom is not available for this chart type", await button.GetAttributeAsync("aria-description"));
        }
        // Wheel input keeps scrolling the page, and bookmarks or peers cannot apply the old CSS scale.
        var result = await page.EvaluateAsync<JsonElement>("""
            () => {
                const root=controller.root,stage=root.querySelector('.cfx-stage'),box=stage.getBoundingClientRect();
                const wheel=new WheelEvent('wheel',{deltaY:-120,clientX:box.x+box.width/2,clientY:box.y+box.height/2,bubbles:true,cancelable:true});
                stage.dispatchEvent(wheel);
                root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:{version:2,viewport:{zoom:3,panX:40,panY:10}}}}));
                return {prevented:wheel.defaultPrevented,zoom:stage.style.getPropertyValue('--cfx-zoom'),transform:getComputedStyle(stage.querySelector('svg')).transform,
                    stateZoom:Number(root.dataset.cfxZoom||'1')};
            }
            """);
        Assert.False(result.GetProperty("prevented").GetBoolean());
        Assert.True(result.GetProperty("zoom").GetString() is "" or "1", "A declined family must not scale its drawing.");
        Assert.Equal(1, result.GetProperty("stateZoom").GetDouble());
        AssertNoConsoleErrors(session);
    }

    private static (double X, double Y) Expected(FamilyCase family, JsonElement plot, JsonElement viewport) {
        JsonElement Window(string axis) => viewport.GetProperty(axis);
        // A horizontal chart's category runs down the plot (reversed) and its value across it.
        return family.CategoryAxis == "y"
            ? (Coordinate(plot, Window("x"), family.X, false, false, false), Coordinate(plot, Window("y"), family.Y, true, true, false))
            : (Coordinate(plot, Window("x"), family.X, false, false, false), Coordinate(plot, Window("y"), family.Y, true, false, family.Logarithmic));
    }

    private static double Coordinate(JsonElement plot, JsonElement window, double value, bool vertical, bool reversed, bool logarithmic) {
        double Transform(double number) => logarithmic ? Math.Log10(number) : number;
        var ratio = (Transform(value) - Transform(window.GetProperty("minimum").GetDouble()))
            / (Transform(window.GetProperty("maximum").GetDouble()) - Transform(window.GetProperty("minimum").GetDouble()));
        if (reversed) ratio = 1 - ratio;
        return vertical ? plot.GetProperty("y").GetDouble() + (1 - ratio) * plot.GetProperty("height").GetDouble()
            : plot.GetProperty("x").GetDouble() + ratio * plot.GetProperty("width").GetDouble();
    }

    private static void AssertAlignedTicks(JsonElement scene, JsonElement viewport, JsonElement plot, string axis, FamilyCase family) {
        var ticks = scene.GetProperty("ticks").GetProperty(axis).EnumerateArray().ToArray();
        Assert.True(ticks.Length >= 2, "The " + axis + " window must paint at least two readable ticks.");
        var window = viewport.GetProperty(axis);
        var vertical = axis != "x";
        var logarithmic = family.Logarithmic && axis == "y";
        foreach (var tick in ticks) {
            var value = tick.GetProperty("value").GetDouble();
            Assert.InRange(value, window.GetProperty("minimum").GetDouble() - 1e-6, window.GetProperty("maximum").GetDouble() + 1e-6);
            Assert.False(string.IsNullOrWhiteSpace(tick.GetProperty("text").GetString()));
            Assert.True(tick.GetProperty("width").GetDouble() > 0);
            var expected = Coordinate(plot, window, value, vertical, family.CategoryAxis == "y" && axis == "y", logarithmic);
            Assert.InRange(tick.GetProperty("position").GetDouble(), expected - .05, expected + .05);
            // Logarithmic ticks sit on whole powers or their 1-2-5 multiples, as the native policy does.
            if (logarithmic) {
                var mantissa = value / Math.Pow(10, Math.Floor(Math.Log10(value) + 1e-9));
                Assert.Contains(Math.Round(mantissa, 6), new[] { 1d, 2d, 5d });
            }
        }
        if (axis == family.CategoryAxis && family.CategoryAxis == "y")
            Assert.All(ticks, tick => Assert.Equal(tick.GetProperty("value").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture), tick.GetProperty("text").GetString()));
    }

    private static Task<JsonElement> PlotAsync(IPage page) => page.EvaluateAsync<JsonElement>("()=>JSON.parse(controller.root.dataset.cfxPreparedChart).dataProjection.plotBounds");

    private static Task<double> FullSpanAsync(IPage page, string axis) => page.EvaluateAsync<double>(
        "axis=>{const meta=JSON.parse(controller.root.dataset.cfxPreparedChart),p=meta[axis+'Projection'];return p.fullMaximum-p.fullMinimum;}", axis);

    private static async Task<JsonElement> FamilySceneAsync(IPage page, string selector, double? vertexX) {
        await SettleMotionAsync(page);
        return await page.EvaluateAsync<JsonElement>("""
            ([selector,vertexX])=>{
                const root=controller.root,svg=root.querySelector('.cfx-stage svg'),paint=root.querySelector(selector);
                if(!paint)throw Error('Fixture needs painted geometry for '+selector);
                const canvas=svg.getBoundingClientRect(),view=svg.viewBox.baseVal,scaleX=canvas.width/view.width,scaleY=canvas.height/view.height;
                let x,y;
                if(vertexX!==null){
                    const numbers=(paint.getAttribute('d').match(/[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?/g)||[]).map(Number),pairs=[];
                    for(let index=0;index+1<numbers.length;index+=2)pairs.push([numbers[index],numbers[index+1]]);
                    [x,y]=pairs.sort((a,b)=>Math.abs(a[0]-vertexX)-Math.abs(b[0]-vertexX))[0];
                } else {
                    const box=paint.getBoundingClientRect();
                    x=view.x+(box.x+box.width/2-canvas.x)/scaleX;y=view.y+(box.y+box.height/2-canvas.y)/scaleY;
                }
                const box=paint.getBoundingClientRect(),ticks={};
                for(const layer of root.querySelectorAll('[data-cfx-browser-axis]')){
                    const vertical=layer.dataset.cfxBrowserAxis!=='x';
                    ticks[layer.dataset.cfxBrowserAxis]=[...layer.querySelectorAll('text[data-cfx-axis-value]')].map(node=>({value:Number(node.dataset.cfxAxisValue),
                        text:node.textContent,width:node.getBoundingClientRect().width,position:Number(node.getAttribute(vertical?'y':'x'))}));
                }
                return {x,y,width:box.width/scaleX,ticks,zoom:root.querySelector('.cfx-stage').style.getPropertyValue('--cfx-zoom')};
            }
            """, new object?[] { selector, vertexX });
    }
}
