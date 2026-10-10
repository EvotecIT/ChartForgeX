using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NumericRadialEncodedGeometryBrowserTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task RoundedNativeSlicesRejectPhysicalPointerAcquisitionAndKeepKeyboardFacts(bool bars, bool caption) {
        if (!Enabled) return;
        var chart = NumericRadialEncodedGeometryTests.Create(bars);
        if (caption) chart.WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Right);
        var stem = (bars ? "bar" : "column") + "-rounded" + (caption ? "-caption" : string.Empty);
        NumericRadialEncodedGeometryTests.Capture(chart, stem);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page;
        await RecordEventsAsync(page);
        var small = page.Locator(Point(0, 0));
        var id = await small.GetAttributeAsync("data-cfx-target-id");
        var pointer = await small.EvaluateAsync<float[]>("""
            node=>{const root=node.closest('.cfx-interactive-chart'),prepared=JSON.parse(root.dataset.cfxPreparedChart),region=prepared.regions.find(region=>region.id===node.dataset.cfxSourceId);
              const point=new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(node.ownerSVGElement.getScreenCTM());return [point.x,point.y];}
            """);
        if (caption) {
            var box = await page.Locator("[data-cfx-label-for='series-0-point-0'] text").BoundingBoxAsync()
                ?? throw new InvalidOperationException("No visible source caption.");
            pointer = new[] { (float)(box.X + box.Width * .4), (float)(box.Y + box.Height * .4) };
        }
        await page.Mouse.MoveAsync(pointer[0], pointer[1], new MouseMoveOptions { Steps = 4 });
        await page.Mouse.ClickAsync(pointer[0], pointer[1]);
        await CaptureAsync(page, stem + "-pointer", pointer);
        Assert.Equal(0, await page.EvaluateAsync<int>("id=>window.cfxEncodedEvents.filter(event=>event.detail.target?.targetId===id).length", id));
        Assert.NotEqual("true", await small.GetAttributeAsync("aria-selected"));
        Assert.Equal(0, await small.Locator("[data-cfx-browser-hit-area]").CountAsync());
        Assert.Equal("precision-collapse", await small.GetAttributeAsync("data-cfx-geometry-status"));

        var reset = page.Locator("[data-cfx-reset]");
        if (await reset.IsVisibleAsync()) await reset.ClickAsync();
        await MoveAwayAsync(page);
        await small.FocusAsync(); Assert.Contains("Observed", await TooltipTextAsync(page));
        Assert.Equal("1", await small.GetAttributeAsync("data-cfx-y"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await small.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await CaptureAsync(page, stem + "-keyboard", pointer);

        await page.Locator("[data-cfx-reset]").ClickAsync(); await MoveAwayAsync(page);
        var large = page.Locator(Point(0, 1));
        var mark = NumericRadialSeriesTests.Marks(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene).Last();
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var painted = await large.EvaluateAsync<float[]>("(node,p)=>{const point=new DOMPoint(p.x,p.y).matrixTransform(node.ownerSVGElement.getScreenCTM());return [point.x,point.y];}",
            new { x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius });
        await page.Mouse.MoveAsync(painted[0], painted[1], new MouseMoveOptions { Steps = 4 });
        Assert.Contains("100,000,000", await TooltipTextAsync(page));
        await page.Mouse.ClickAsync(painted[0], painted[1]);
        Assert.Equal("true", await large.GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedCollapsedFillRequiresItsActualSeparatorStrokeForPointerAndKeyboardReadout(bool donut) {
        if (!Enabled) return;
        var chart = NumericRadialEncodedGeometryTests.SharedStrokedSlice(donut);
        var stem = donut ? "donut-rounded" : "pie-rounded";
        NumericRadialEncodedGeometryTests.Capture(chart, stem);
        var mark = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes.OfType<VisualSceneSlice>().Last();
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page;
        await RecordEventsAsync(page);
        var small = page.Locator("[data-cfx-point='1'][data-cfx-target-kind='point']");
        var id = await small.GetAttributeAsync("data-cfx-target-id");
        Assert.Equal("false", await small.Locator("path").GetAttributeAsync("data-cfx-fill-area"));
        var radius = (mark.Inner + mark.Outer) / 2;
        var source = await small.EvaluateAsync<float[]>("(node,p)=>{const point=new DOMPoint(p.x,p.y).matrixTransform(node.ownerSVGElement.getScreenCTM());return [point.x,point.y];}",
            new { x = mark.Cx + Math.Cos(mark.Start) * radius, y = mark.Cy + Math.Sin(mark.Start) * radius });
        await page.Mouse.MoveAsync(source[0], source[1], new MouseMoveOptions { Steps = 4 });
        Assert.Contains(id!, (await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"))!);
        await page.Mouse.ClickAsync(source[0], source[1]);
        Assert.Equal("true", await small.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, stem + "-stroke-pointer", source, 1);
        await page.Locator("[data-cfx-reset]").ClickAsync(); await MoveAwayAsync(page);
        await small.Locator("path").EvaluateAsync("node=>node.style.stroke='none'");
        Assert.Equal("-1", await small.GetAttributeAsync("tabindex"));
        await page.EvaluateAsync("()=>window.cfxEncodedEvents=[]");
        var label = await page.Locator("[data-cfx-label-for='series-0-point-1'] text").BoundingBoxAsync()
            ?? throw new InvalidOperationException("No visible source caption.");
        var caption = new[] { (float)(label.X + label.Width * .4), (float)(label.Y + label.Height * .4) };
        await page.Mouse.MoveAsync(caption[0], caption[1]); await page.Mouse.ClickAsync(caption[0], caption[1]);
        Assert.Equal(0, await page.EvaluateAsync<int>("id=>window.cfxEncodedEvents.filter(event=>event.detail.target?.targetId===id).length", id));
        await CaptureAsync(page, stem + "-no-stroke-caption", caption, 1);
        await small.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.NotEqual("true", await small.GetAttributeAsync("aria-selected"));
        await small.Locator("path").EvaluateAsync("node=>node.style.stroke=''");
        await small.FocusAsync(); Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        Assert.Equal("1", await small.GetAttributeAsync("data-cfx-value"));
        AssertNoConsoleErrors(session);
    }

    private static Task RecordEventsAsync(IPage page) => page.EvaluateAsync("()=>{window.cfxEncodedEvents=[];const root=document.querySelector('.cfx-interactive-chart');for(const type of ['cfxhover','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.cfxEncodedEvents.push({type,detail:event.detail}));}");

    private static async Task CaptureAsync(IPage page, string stem, float[] pointer, int point = 0) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, stem + ".png") });
        await File.WriteAllTextAsync(Path.Combine(directory, stem + ".json"), JsonSerializer.Serialize(await page.EvaluateAsync<JsonElement>("""
            p=>{const root=document.querySelector('.cfx-interactive-chart'),small=root.querySelector(`[data-cfx-point="${p.point}"][data-cfx-target-kind="point"]`),path=small.querySelector('path'),box=path?.getBBox(),hit=document.elementFromPoint(p.x,p.y);return {
              geometry:small.dataset.cfxGeometryStatus||null,raw:small.dataset.cfxY||small.dataset.cfxValue,selected:small.getAttribute('aria-selected'),pinned:root.dataset.cfxTooltipPinned||null,
              path:path?.getAttribute('d')||null,box:box?{x:box.x,y:box.y,width:box.width,height:box.height}:null,hit:hit?.outerHTML||null,tooltip:root.querySelector('.cfx-tooltip').innerText,events:window.cfxEncodedEvents};}
            """, new { x = (double)pointer[0], y = (double)pointer[1], point }), new JsonSerializerOptions { WriteIndented = true }));
    }
}
