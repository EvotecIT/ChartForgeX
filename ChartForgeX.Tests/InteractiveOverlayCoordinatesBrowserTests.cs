using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveOverlayCoordinatesBrowserTests {
    private const string Root = ".cfx-interactive-chart";

    [Theory]
    [InlineData(false, HtmlChartResponsiveLayout.Fit, 650, 0.8, false)]
    [InlineData(true, HtmlChartResponsiveLayout.Fit, 650, 0.8, true)]
    [InlineData(false, HtmlChartResponsiveLayout.Readable, 340, 1, false)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 340, 0.8, true)]
    [InlineData(false, HtmlChartResponsiveLayout.Fit, 340, 1, false)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 650, 1, false)]
    public async Task NativeAndInferredGuidesAlignWithMarksInsideScaledAndScrolledHosts(bool graphite, HtmlChartResponsiveLayout layout, int width, double scale, bool pageScroll) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Host(graphite, layout, width, scale, pageScroll, ChartInteractionFeatures.Crosshair), 950, 760);
        var page = session.Page;
        await PrepareHostAsync(page, layout, width, pageScroll);
        await RecordEventsAsync(page);
        var extent = await ScrollExtentAsync(page);
        var mark = await BoxAsync(page, Point(0, 1));
        var native = new double[] { mark.X + mark.Width / 2, mark.Y + mark.Height / 2 };
        Assert.True(await page.Locator(Point(0, 1)).EvaluateAsync<bool>("(n,p) => n.contains(document.elementFromPoint(p[0],p[1]))", native));
        await page.Mouse.MoveAsync((float)native[0], (float)native[1]);
        var nativeGuide = await GuideGeometryAsync(page);
        var inferred = new[] { native[0], native[1] + 24 };
        Assert.True(await page.Locator(Root).EvaluateAsync<bool>("(r,p) => { const h=document.elementFromPoint(p[0],p[1]); return !!h && r.contains(h) && !h.closest('[data-cfx-target-kind]'); }", inferred));
        await page.Mouse.MoveAsync((float)inferred[0], (float)inferred[1]);
        var inferredGuide = await GuideGeometryAsync(page);
        var after = await ScrollExtentAsync(page);
        var events = await EventsAsync(page);
        await CaptureAsync(session, "overlay-guide-" + Variant(graphite, layout, width, scale, pageScroll), new { native, inferred, nativeGuide, inferredGuide, extent, after, events });
        AssertGuideGeometry(nativeGuide, native);
        AssertGuideGeometry(inferredGuide, native);
        Assert.Equal(extent, after);
        Assert.Equal("reading-source:1", nativeGuide.GetProperty("targetId").GetString());
        Assert.Equal("reading-source:1", inferredGuide.GetProperty("targetId").GetString());
        Assert.Equal(new[] { "reading-source:1", "reading-source:1" }, events.EnumerateArray().Where(e => e.GetProperty("name").GetString() == "cfxcrosshair")
            .Select(e => e.GetProperty("detail").GetProperty("target").GetProperty("targetId").GetString()));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, HtmlChartResponsiveLayout.Fit, 650, 0.8, false)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 340, 0.8, true)]
    [InlineData(false, HtmlChartResponsiveLayout.Readable, 340, 1, false)]
    public async Task BrushBoundsFollowThePointerAndSelectTheSameNativeObservation(bool graphite, HtmlChartResponsiveLayout layout, int width, double scale, bool pageScroll) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Host(graphite, layout, width, scale, pageScroll, ChartInteractionFeatures.Brush | ChartInteractionFeatures.Selection), 950, 760);
        var page = session.Page;
        await PrepareHostAsync(page, layout, width, pageScroll);
        await page.Locator("[data-cfx-mode-button=brush]").ClickAsync();
        await RecordEventsAsync(page);
        var extent = await ScrollExtentAsync(page);
        var mark = await BoxAsync(page, Point(0, 1));
        var start = new double[] { mark.X + mark.Width / 2 - 15, mark.Y + mark.Height / 2 - 15 };
        var end = new[] { start[0] + 30, start[1] + 30 };
        var textSelection = await DragAsync(page, start, end);
        var brush = await BoxAsync(page, ".cfx-brush-box");
        var events = await EventsAsync(page);
        var selected = await page.Locator("g[data-cfx-role=point][aria-selected=true]").EvaluateAllAsync<string[]>("nodes => nodes.map(n => n.dataset.cfxTargetId)");
        var after = await ScrollExtentAsync(page);
        await CaptureAsync(session, "overlay-brush-" + Variant(graphite, layout, width, scale, pageScroll), new {
            start, end, brush, selected, extent, after, events, TextSelectionDuring = textSelection.During, TextSelectionAfter = textSelection.After
        });
        Assert.InRange(brush.X - start[0], -1, 1);
        Assert.InRange(brush.Y - start[1], -1, 1);
        Assert.InRange(brush.Width - 30, -1, 1);
        Assert.InRange(brush.Height - 30, -1, 1);
        Assert.Equal(new[] { "reading-source:1" }, selected);
        var lasso = Assert.Single(events.EnumerateArray(), e => e.GetProperty("name").GetString() == "cfxlasso");
        Assert.Contains(lasso.GetProperty("detail").GetProperty("targets").EnumerateArray(), t => t.GetProperty("targetId").GetString() == "reading-source:1");
        var notification = Assert.Single(events.EnumerateArray(), e => e.GetProperty("name").GetString() == "cfxbrush");
        Assert.Equal(await page.Locator(Root).GetAttributeAsync("data-cfx-brush"), notification.GetProperty("detail").GetProperty("bounds").GetString());
        Assert.Equal(extent, after);
        Assert.Empty(textSelection.During);
        Assert.Empty(textSelection.After);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, HtmlChartResponsiveLayout.Fit, 650, 0.8, false)]
    [InlineData(true, HtmlChartResponsiveLayout.Readable, 340, 0.8, true)]
    public async Task PanMovesNativeGeometryByTheScreenPointerDelta(bool graphite, HtmlChartResponsiveLayout layout, int width, double scale, bool pageScroll) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Host(graphite, layout, width, scale, pageScroll, ChartInteractionFeatures.Pan), 950, 760);
        var page = session.Page;
        await PrepareHostAsync(page, layout, width, pageScroll);
        await page.Locator("[data-cfx-mode-button=pan]").ClickAsync();
        await RecordEventsAsync(page);
        var before = await BoxAsync(page, Point(0, 1));
        var start = new double[] { before.X + before.Width / 2, before.Y + before.Height / 2 + 24 };
        var end = new[] { start[0] + 40, start[1] + 30 };
        await DragAsync(page, start, end);
        // Observe the final native geometry after the browser finishes its CSS transform transition.
        await page.Locator(".cfx-stage svg").EvaluateAsync("svg => new Promise(resolve => requestAnimationFrame(resolve)).then(() => Promise.all(svg.getAnimations().map(a => a.finished)))");
        var after = await BoxAsync(page, Point(0, 1));
        var events = await EventsAsync(page);
        await CaptureAsync(session, "overlay-pan-" + Variant(graphite, layout, width, scale, pageScroll), new { start, end, before, after, events });
        Assert.InRange(after.X - before.X - 40, -1, 1);
        Assert.InRange(after.Y - before.Y - 30, -1, 1);
        var viewport = Assert.Single(events.EnumerateArray(), e => e.GetProperty("name").GetString() == "cfxviewport");
        Assert.InRange(viewport.GetProperty("detail").GetProperty("state").GetProperty("panX").GetDouble() - 40 / scale, -0.1, 0.1);
        Assert.InRange(viewport.GetProperty("detail").GetProperty("state").GetProperty("panY").GetDouble() - 30 / scale, -0.1, 0.1);
        AssertNoConsoleErrors(session);
    }

    private static string Host(bool graphite, HtmlChartResponsiveLayout layout, int width, double scale, bool pageScroll, ChartInteractionFeatures features) {
        var fragment = CoordinateChart(graphite).ToInteractiveHtmlFragment(options => {
            options.ResponsiveLayout = layout;
            options.Interaction.Features = features;
            options.IncludeResetButton = false;
        });
        var transform = scale == 1 ? "" : "transform:translate(80px,70px) scale(" + scale.ToString(CultureInfo.InvariantCulture) + ");transform-origin:0 0;";
        var customStage = pageScroll ? "<style>#host .cfx-stage{border-width:5px;padding:18px}</style>" : "";
        return "<!doctype html><html><head><title>Native overlay coordinates</title></head><body>"
            + (pageScroll ? "<div style='height:200px'></div>" : "")
            + "<main id='host' style='width:" + width + "px;" + transform + "'>" + fragment + "</main>" + customStage
            + (pageScroll ? "<div style='height:1200px'></div>" : "") + "</body></html>";
    }

    private static Chart CoordinateChart(bool graphite) {
        var chart = Chart.Create().WithSize(596, 338).WithTitle("Native overlay coordinates")
            .WithTheme(graphite ? ChartTheme.GraphiteLight() : ChartTheme.Light())
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Reading", new[] { new ChartPoint(3, 5), new ChartPoint(5, 5), new ChartPoint(7, 5) });
        chart.Series[0].WithInteractionKey("reading-source");
        return chart;
    }

    private static async Task PrepareHostAsync(IPage page, HtmlChartResponsiveLayout layout, int width, bool pageScroll) {
        if (pageScroll) await page.EvaluateAsync("() => window.scrollTo(0,150)");
        if (layout == HtmlChartResponsiveLayout.Readable && width == 340) {
            await page.Locator(".cfx-stage").EvaluateAsync("stage => stage.scrollLeft = 120");
            Assert.InRange(await page.Locator(".cfx-stage").EvaluateAsync<double>("stage => stage.scrollLeft"), 119, 121);
        }
    }

    private static Task<double[]> ScrollExtentAsync(IPage page) => page.Locator(".cfx-stage").EvaluateAsync<double[]>("s => [s.scrollWidth,s.scrollHeight,s.scrollLeft,s.scrollTop]");
    private static Task<JsonElement> GuideGeometryAsync(IPage page) => page.Locator(Root).EvaluateAsync<JsonElement>("""
        r => {
            const s=r.querySelector('.cfx-stage'), c=r.querySelector('.cfx-crosshair'), b=s.getBoundingClientRect();
            const sx=b.width/s.offsetWidth, sy=b.height/s.offsetHeight;
            const x=c.querySelector('.cfx-crosshair__line--y').getBoundingClientRect(), y=c.querySelector('.cfx-crosshair__line--x').getBoundingClientRect();
            return {visible:!c.hidden,x:x.left,y:y.top,left:y.left,right:y.right,top:x.top,bottom:x.bottom,
                viewport:{left:b.left+s.clientLeft*sx,top:b.top+s.clientTop*sy,width:s.clientWidth*sx,height:s.clientHeight*sy},
                targetId:(r.dataset.cfxCrosshair||'').split('|')[1],label:c.querySelector('[data-cfx-crosshair-label]').textContent};
        }
        """);

    private static void AssertGuideGeometry(JsonElement guide, double[] native) {
        Assert.True(guide.GetProperty("visible").GetBoolean());
        Assert.InRange(guide.GetProperty("x").GetDouble() - native[0], -1, 1);
        Assert.InRange(guide.GetProperty("y").GetDouble() - native[1], -1, 1);
        var viewport = guide.GetProperty("viewport");
        Assert.InRange(guide.GetProperty("left").GetDouble() - viewport.GetProperty("left").GetDouble(), -1, 1);
        Assert.InRange(guide.GetProperty("right").GetDouble() - viewport.GetProperty("left").GetDouble() - viewport.GetProperty("width").GetDouble(), -1, 1);
        Assert.InRange(guide.GetProperty("top").GetDouble() - viewport.GetProperty("top").GetDouble(), -1, 1);
        Assert.InRange(guide.GetProperty("bottom").GetDouble() - viewport.GetProperty("top").GetDouble() - viewport.GetProperty("height").GetDouble(), -1, 1);
        Assert.False(string.IsNullOrWhiteSpace(guide.GetProperty("label").GetString()));
    }

    private static Task RecordEventsAsync(IPage page) => page.Locator(Root).EvaluateAsync("""
        r => {
            window.overlayCoordinateEvents=[];
            for(const name of ['cfxcrosshair','cfxbrush','cfxlasso','cfxviewport','cfxselect'])
                r.addEventListener(name,e=>window.overlayCoordinateEvents.push({name,detail:e.detail}));
        }
        """);
    private static Task<JsonElement> EventsAsync(IPage page) => page.EvaluateAsync<JsonElement>("() => window.overlayCoordinateEvents");
    private static string Variant(bool graphite, HtmlChartResponsiveLayout layout, int width, double scale, bool pageScroll) =>
        (graphite ? "graphite" : "generic") + "-" + layout + "-" + width + "-scale" + scale.ToString(CultureInfo.InvariantCulture) + (pageScroll ? "-scrolled" : "");

    private static async Task<(string During, string After)> DragAsync(IPage page, double[] start, double[] end) {
        await page.Mouse.MoveAsync((float)start[0], (float)start[1]);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)end[0], (float)end[1], new MouseMoveOptions { Steps = 3 });
        var during = await page.EvaluateAsync<string>("() => window.getSelection()?.toString() || ''");
        await page.Mouse.UpAsync();
        return (during, await page.EvaluateAsync<string>("() => window.getSelection()?.toString() || ''"));
    }

    private static async Task CaptureAsync(HtmlBrowserSession session, string name, object measurement) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { measurement, Console = session.ConsoleLog.Select(e => e.Type.ToString()) }));
        await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
