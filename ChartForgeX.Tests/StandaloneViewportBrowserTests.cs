using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Accepts detached report navigation against painted SVG, without a native preparation callback.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed partial class StandaloneViewportBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private const string Sample = "g[data-cfx-role=point][data-cfx-series-key=samples][data-cfx-observation-id=sample-4]";

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Area)]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Candlestick)]
    public async Task DetachedWindowApisRetickAxesReprojectAndClipDataWithoutScalingTheFrame(ChartSeriesKind kind) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(kind), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        var initial = await SceneAsync(page);
        await page.EvaluateAsync("()=>controller.setXWindow(2,10)");
        await AssertSceneAsync(page, kind, 2, 10, 0, 100, initial);
        await page.EvaluateAsync("()=>controller.setYWindow(20,80)");
        await AssertSceneAsync(page, kind, 2, 10, 20, 80, initial);
        await page.EvaluateAsync("()=>controller.setViewport({x:{minimum:3,maximum:9},y:{minimum:25,maximum:75}})");
        await AssertSceneAsync(page, kind, 3, 9, 25, 75, initial);
        await CaptureAsync(page, "api-" + kind);
        Assert.Equal("false", await page.Locator("g[data-cfx-role=point][data-cfx-series-key=samples][data-cfx-observation-id=sample-0]").GetAttributeAsync("data-cfx-visible"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        AssertWindow((await ViewportAsync(page)).GetProperty("x"), 0, 12);
        AssertWindow((await ViewportAsync(page)).GetProperty("y"), 0, 100);
        var reset = await SceneAsync(page);
        Assert.Equal(initial.GetProperty("markX").GetDouble(), reset.GetProperty("markX").GetDouble(), 2);
        Assert.Equal(initial.GetProperty("markY").GetDouble(), reset.GetProperty("markY").GetDouble(), 2);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 980)]
    [InlineData(false, 390)]
    [InlineData(true, 980)]
    [InlineData(true, 390)]
    public async Task GeneratedExampleCombinesStandaloneNavigatorContextMenuAndNativeOnlyAnnotationEditor(bool dark, int width) {
        if (!Enabled) return;
        var errors = new List<string>();
        var html = HtmlCrosshairExamples.CreatePage(HtmlChartCrosshairLineStyle.Solid, dark);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "standalone-example-" + (dark ? "dark" : "light") + ".html"), html);
        }
        await using var session = await OpenAsync(html, width, 1000,
            configurePage: page => page.PageError += (_, error) => errors.Add(error));
        var page = session.Page;
        await MountAsync(page);
        Assert.Empty(errors);
        Assert.True(await page.Locator("[data-cfx-x-navigation]").IsVisibleAsync());
        await CaptureAsync(page, "example-" + width + "-" + (dark ? "dark" : "light"));
        Assert.True(await page.Locator(".cfx-annotation-editor").IsVisibleAsync());
        Assert.Contains("Connect native preparation", await page.Locator(".cfx-annotation-editor").InnerTextAsync());
        Assert.True(await page.Locator("[data-cfx-annotation-control=commit]").IsDisabledAsync());
        await page.EvaluateAsync("()=>controller.setViewport({x:{minimum:2,maximum:8},y:{minimum:2,maximum:8}})");
        await AssertTicksAsync(page, "x", 2, 8);
        await AssertTicksAsync(page, "y", 2, 8);
        await CaptureAsync(page, "example-" + width + "-" + (dark ? "dark" : "light"));
        await page.Locator(".cfx-stage").ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        Assert.True(await page.Locator(".cfx-chart-context-menu").IsVisibleAsync());
        await CaptureAsync(page, "example-menu-" + width + "-" + (dark ? "dark" : "light"));
        await page.Locator("[data-cfx-context-action=reset]").ClickAsync();
        AssertWindow((await ViewportAsync(page)).GetProperty("x"), 0, 10);
        AssertWindow((await ViewportAsync(page)).GetProperty("y"), 0, 10);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Area)]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Candlestick)]
    public async Task DetachedLegendToggleRescalesVisibleSeriesAndRestoresTheHiddenSeries(ChartSeriesKind kind) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(kind, reference: true), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        var before = await SceneAsync(page);
        var legend = page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]");
        await legend.ClickAsync();
        var muted = await ViewportAsync(page);
        Assert.True(muted.GetProperty("y").GetProperty("maximum").GetDouble() < 95);
        Assert.Equal("true", await legend.GetAttributeAsync("data-cfx-muted"));
        Assert.True(await page.Locator("[data-cfx-role=series][data-cfx-series-key=reference]").EvaluateAsync<bool>("owner=>[...owner.querySelectorAll('path,line,rect,circle')].every(node=>getComputedStyle(node).visibility==='hidden'||getComputedStyle(node).display==='none')"));
        AssertWindow(muted.GetProperty("x"), 0, 12);
        await AssertTicksAsync(page, "y", muted.GetProperty("y").GetProperty("minimum").GetDouble(), muted.GetProperty("y").GetProperty("maximum").GetDouble());
        Assert.NotEqual(before.GetProperty("geometry").GetString(), (await SceneAsync(page)).GetProperty("geometry").GetString());
        await legend.ClickAsync();
        Assert.True((await ViewportAsync(page)).GetProperty("y").GetProperty("maximum").GetDouble() >= 95);
        Assert.Equal("false", await legend.GetAttributeAsync("data-cfx-muted"));
        Assert.True(await page.Locator("[data-cfx-role=series][data-cfx-series-key=reference] path").First.EvaluateAsync<bool>(
            "node=>getComputedStyle(node).visibility==='visible'&&getComputedStyle(node).display!=='none'&&node.getBBox().width>0"));
        AssertNoConsoleErrors(session);
    }

    private static string Page(ChartSeriesKind kind, bool reference = false, bool dark = false, HtmlChartRuntime runtime = HtmlChartRuntime.Full) {
        var points = Enumerable.Range(0, 13).Select(index => new ChartPoint(index, 10 + index * 6)).ToArray();
        var id = new ChartSeriesId("samples");
        var series = kind == ChartSeriesKind.Candlestick
            ? ChartSeries.CreateCandlestick(id, "Measurements", points.Select(point => new ChartCandlestick(point.X, point.Y, point.Y + 4, point.Y - 4, point.Y + 2)))
            : new ChartSeries(id, "Measurements", kind, points);
        for (var index = 0; index < points.Length; index++) series.WithObservationId(index, new ChartObservationId("sample-" + index));
        if (kind is ChartSeriesKind.Line or ChartSeriesKind.Area or ChartSeriesKind.Scatter)
            series.ConfigureMarkers(markers => { markers.Enabled = true; markers.Radius = 4; });
        var chart = Chart.Create().WithSize(800, 440).WithTitle("Inspect saved measurements")
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(0, 12).WithYAxisBounds(0, 100).WithLegend().WithDataLabels(false).AddSeries(series);
        if (reference) chart.AddSeries(new ChartSeries(new ChartSeriesId("reference"), "Reference", ChartSeriesKind.Line,
            new[] { new ChartPoint(0, 95), new ChartPoint(12, 95) }));
        return chart.ToInteractiveHtmlPage(options => {
            options.Runtime = runtime;
            options.Interaction.ChartId = "standalone-readings";
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.Pan | ChartInteractionFeatures.Brush | ChartInteractionFeatures.LegendToggles);
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.XNavigation.Enabled = true;
        });
    }

    private static Task MountAsync(IPage page) => page.EvaluateAsync("()=>{window.controller=ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'));}");
    private static Task<JsonElement> ViewportAsync(IPage page) => page.EvaluateAsync<JsonElement>("()=>controller.getViewport()");

    private static async Task CaptureAsync(IPage page, string name) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("CFX_BROWSER_CAPTURE_DIRECTORY must be absolute.");
        Directory.CreateDirectory(directory);
        await page.ScreenshotAsync(new PageScreenshotOptions {
            Path = Path.Combine(directory, "standalone-" + Engine.ToString().ToLowerInvariant() + "-" + name + ".png")
        });
    }

    private static void AssertWindow(JsonElement window, double minimum, double maximum) {
        Assert.Equal(minimum, window.GetProperty("minimum").GetDouble(), 7);
        Assert.Equal(maximum, window.GetProperty("maximum").GetDouble(), 7);
    }

    private static async Task AssertTicksAsync(IPage page, string axis, double minimum, double maximum) {
        var values = await page.Locator("[data-cfx-browser-axis=" + axis + "] text[data-cfx-axis-value]").EvaluateAllAsync<JsonElement>(
            "nodes=>nodes.map(node=>({value:Number(node.dataset.cfxAxisValue),text:node.textContent,width:node.getBoundingClientRect().width}))");
        var ticks = values.EnumerateArray().ToArray();
        Assert.True(ticks.Length >= 2, "The standalone window must paint at least two readable " + axis + " ticks.");
        Assert.All(ticks, tick => {
            var value = tick.GetProperty("value").GetDouble();
            Assert.InRange(value, minimum - .000001, maximum + .000001);
            Assert.Equal(value, double.Parse(tick.GetProperty("text").GetString()!, System.Globalization.CultureInfo.CurrentCulture), 5);
            Assert.True(tick.GetProperty("width").GetDouble() > 0);
        });
    }

    private static async Task AssertSceneAsync(IPage page, ChartSeriesKind kind, double xMin, double xMax, double yMin, double yMax, JsonElement initial) {
        var viewport = await ViewportAsync(page);
        AssertWindow(viewport.GetProperty("x"), xMin, xMax);
        AssertWindow(viewport.GetProperty("y"), yMin, yMax);
        await AssertTicksAsync(page, "x", xMin, xMax);
        if (yMin != 0 || yMax != 100) await AssertTicksAsync(page, "y", yMin, yMax);
        var scene = await SceneAsync(page);
        Assert.NotEqual(initial.GetProperty("geometry").GetString(), scene.GetProperty("geometry").GetString());
        Assert.Equal(initial.GetProperty("frame").GetRawText(), scene.GetProperty("frame").GetRawText());
        Assert.Equal(initial.GetProperty("stroke").GetDouble(), scene.GetProperty("stroke").GetDouble(), 5);
        var plot = scene.GetProperty("plot");
        var expectedX = plot.GetProperty("x").GetDouble() + (4 - xMin) / (xMax - xMin) * plot.GetProperty("width").GetDouble();
        // DOM layout bounds are quantized in CSS pixels. Bound the error instead
        // of comparing rounded buckets on opposite sides of a decimal boundary.
        Assert.InRange(scene.GetProperty("markX").GetDouble(), expectedX - .02, expectedX + .02);
        if (kind is ChartSeriesKind.Line or ChartSeriesKind.Area or ChartSeriesKind.Scatter) {
            var expectedY = plot.GetProperty("y").GetDouble() + (1 - (34 - yMin) / (yMax - yMin)) * plot.GetProperty("height").GetDouble();
            Assert.InRange(scene.GetProperty("markY").GetDouble(), expectedY - .02, expectedY + .02);
            Assert.Equal(initial.GetProperty("markerWidth").GetDouble(), scene.GetProperty("markerWidth").GetDouble(), 2);
            Assert.Equal(initial.GetProperty("markerHeight").GetDouble(), scene.GetProperty("markerHeight").GetDouble(), 2);
        }
        Assert.True(scene.GetProperty("clipMatchesPlot").GetBoolean(), "Data ink needs a fixed clip at the plot bounds.");
        AssertViewportScale(initial, scene);
    }

    private static void AssertViewportScale(JsonElement initial, JsonElement scene) {
        Assert.Equal(initial.GetProperty("viewportScaleX").GetDouble(), scene.GetProperty("viewportScaleX").GetDouble(), 6);
        Assert.Equal(initial.GetProperty("viewportScaleY").GetDouble(), scene.GetProperty("viewportScaleY").GetDouble(), 6);
    }

    private static Task<JsonElement> SceneAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        selector=>{
            const root=controller.root,svg=root.querySelector('.cfx-stage svg'), point=root.querySelector(selector);
            const series=point.closest('[data-cfx-role=series]');
            const paint=point.querySelector('.cfx-prepared-point-marker')||point.querySelector('circle,ellipse,rect,path,line,polygon');
            if(!paint)throw Error('Fixture needs painted observation geometry');
            // Measure painted browser pixels independently of engine-specific CTM
            // treatment of the existing responsive-fit CSS zoom.
            const box=paint.getBoundingClientRect(),canvas=svg.getBoundingClientRect(),view=svg.viewBox.baseVal;
            const scaleX=canvas.width/view.width,scaleY=canvas.height/view.height;
            const center={x:view.x+(box.x+box.width/2-canvas.x)/scaleX,y:view.y+(box.y+box.height/2-canvas.y)/scaleY};
            const plot=JSON.parse(root.dataset.cfxPreparedChart).dataProjection.plotBounds;
            const clipOwner=series.closest('[clip-path]'),clipRef=clipOwner?.getAttribute('clip-path'),id=clipRef?.match(/#([^)]*)/)?.[1];
            const clip=id&&svg.querySelector('[id="'+id+'"] rect');
            return {plot,geometry:[...series.querySelectorAll('path,line,rect,circle,ellipse,polygon')].map(node=>['d','x','y','cx','cy','x1','x2','y1','y2','width','height','points'].map(key=>node.getAttribute(key)).join(',')).join('|'),
                frame:{viewBox:svg.getAttribute('viewBox'),title:svg.querySelector('[data-cfx-role=frame-heading]')?.getBoundingClientRect().toJSON()||null},
                stroke:parseFloat(getComputedStyle(paint).strokeWidth),markX:center.x,markY:center.y,
                markerWidth:box.width/scaleX,markerHeight:box.height/scaleY,
                viewportScaleX:scaleX,viewportScaleY:scaleY,
                clipMatchesPlot:!!clip&&['x','y','width','height'].every(key=>Math.abs(Number(clip.getAttribute(key))-plot[key])<.001)};
        }
        """, Sample);
}
