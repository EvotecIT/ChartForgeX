using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class PatternClipAreaBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.RangeBand, false)]
    [InlineData(ChartSeriesKind.RangeArea, false)]
    [InlineData(ChartSeriesKind.RangeArea, true)]
    [InlineData(ChartSeriesKind.Radar, false)]
    public async Task RetracedEnvelopeCannotMakeAnInvisibleObservationInteractive(ChartSeriesKind kind, bool smooth) {
        var chart = Create(kind, collapsed: true, smooth);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var name = "pattern-retraced-" + kind.ToString().ToLowerInvariant() + (smooth ? "-smooth" : "");
        NumericRadialEncodedGeometryTests.Capture(chart, name);
        var patterns = prepared.Scene.Nodes.OfType<VisualSceneLine>().Count(line => line.Role?.EndsWith("-pattern", StringComparison.Ordinal) == true);
        var png = prepared.ToPng();
        var pixels = RasterImageDecoder.Decode(png).Pixels;
        var nativeVisiblePixels = Enumerable.Range(0, pixels.Length / 4).Count(pixel => pixels[pixel * 4 + 3] != 0);
        chart.Series[0].WithFillPattern(ChartFillPattern.None);
        Assert.Equal(chart.ToPng(), png);
        if (Enabled) {
            chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
            await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
            var page = session.Page;
            var source = kind == ChartSeriesKind.Radar ? 1 : 0;
            var target = page.Locator("[data-cfx-target-kind='point'][data-cfx-point='" + source + "']");
            var nativeSvgVisiblePixels = await page.EvaluateAsync<int>("""
                async svg=>{const image=new Image();image.src='data:image/svg+xml;base64,'+btoa(unescape(encodeURIComponent(svg)));await image.decode();
                  const canvas=document.createElement('canvas');canvas.width=image.width;canvas.height=image.height;const context=canvas.getContext('2d');context.drawImage(image,0,0);
                  const pixels=context.getImageData(0,0,canvas.width,canvas.height).data;let count=0;for(let i=3;i<pixels.length;i+=4)if(pixels[i])count++;return count;}
                """, prepared.ToSvg());
            var nativeMarks = await target.EvaluateAsync<JsonElement>("""
                node=>Array.from(node.querySelectorAll('ellipse,path')).map(shape=>{const style=getComputedStyle(shape),box=shape.getBBox();return {role:shape.dataset.cfxRole,
                  rx:shape.getAttribute('rx'),ry:shape.getAttribute('ry'),fill:style.fill,stroke:style.stroke,strokeWidth:style.strokeWidth,box:{x:box.x,y:box.y,width:box.width,height:box.height}}})
                """);
            var id = await target.GetAttributeAsync("data-cfx-target-id");
            await page.EvaluateAsync("()=>{window.cfxPatternClipEvents=[];const root=document.querySelector('.cfx-interactive-chart');for(const type of ['cfxhover','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.cfxPatternClipEvents.push({type,detail:event.detail}));}");
            var screen = await target.EvaluateAsync<float[]>("""
                node=>{const root=node.closest('.cfx-interactive-chart'),region=JSON.parse(root.dataset.cfxPreparedChart).regions.find(region=>region.id===node.dataset.cfxSourceId);
                  const p=new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(node.ownerSVGElement.getScreenCTM());return [p.x,p.y];}
                """);
            await page.Mouse.MoveAsync(screen[0], screen[1], new MouseMoveOptions { Steps = 4 });
            var hover = await TooltipTextAsync(page);
            await page.Mouse.ClickAsync(screen[0], screen[1]);
            var pointerSelected = await target.GetAttributeAsync("aria-selected");
            var reset = page.Locator("[data-cfx-reset]");
            if (await reset.IsVisibleAsync()) await reset.ClickAsync();
            await MoveAwayAsync(page);
            await target.FocusAsync();
            var keyboard = await TooltipTextAsync(page);
            await page.Keyboard.PressAsync("Space");
            var selected = await target.GetAttributeAsync("aria-selected");
            var tabindex = await target.GetAttributeAsync("tabindex");
            var events = await page.EvaluateAsync<JsonElement>("()=>window.cfxPatternClipEvents");
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (!string.IsNullOrWhiteSpace(directory)) {
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png") });
                await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { kind = kind.ToString(), smooth, patterns, nativeVisiblePixels, nativeSvgVisiblePixels, nativeMarks, id, screen, hover, pointerSelected, keyboard, selected, tabindex, events }, new JsonSerializerOptions { WriteIndented = true }));
                await File.WriteAllTextAsync(Path.Combine(directory, name + ".console.json"), JsonSerializer.Serialize(session.ConsoleLog, new JsonSerializerOptions { WriteIndented = true }));
            }
            Assert.NotEqual("true", pointerSelected);
            Assert.NotEqual("true", selected);
            Assert.Equal("-1", tabindex);
            Assert.DoesNotContain(events.EnumerateArray(), item => item.GetProperty("detail").TryGetProperty("target", out var value)
                && value.ValueKind == JsonValueKind.Object && value.TryGetProperty("targetId", out var targetId) && targetId.GetString() == id);
            Assert.Equal(0, nativeSvgVisiblePixels);
            AssertNoConsoleErrors(session);
        }
        Assert.Equal(0, patterns);
        Assert.Equal(0, nativeVisiblePixels);
    }

    [Theory]
    [InlineData(ChartSeriesKind.RangeBand, false)]
    [InlineData(ChartSeriesKind.RangeArea, false)]
    [InlineData(ChartSeriesKind.RangeArea, true)]
    [InlineData(ChartSeriesKind.Radar, false)]
    public async Task PositiveEnvelopeRetainsItsNativePatternInk(ChartSeriesKind kind, bool smooth) {
        var chart = Create(kind, collapsed: false, smooth);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var name = "pattern-filled-" + kind.ToString().ToLowerInvariant() + (smooth ? "-smooth" : "");
        NumericRadialEncodedGeometryTests.Capture(chart, name);
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role?.EndsWith("-pattern", StringComparison.Ordinal) == true);
        var png = prepared.ToPng();
        chart.Series[0].WithFillPattern(ChartFillPattern.None);
        Assert.NotEqual(chart.ToPng(), png);
        if (!Enabled) return;
        chart.Series[0].WithFillPattern(ChartFillPattern.Crosshatch);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page;
        var target = page.Locator("[data-cfx-target-kind='point'][data-cfx-point='1']");
        var screen = await target.EvaluateAsync<float[]>("""
            node=>{const region=JSON.parse(node.closest('.cfx-interactive-chart').dataset.cfxPreparedChart).regions.find(region=>region.id===node.dataset.cfxSourceId);
              const p=new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(node.ownerSVGElement.getScreenCTM());return [p.x,p.y];}
            """);
        await page.Mouse.MoveAsync(screen[0], screen[1], new MouseMoveOptions { Steps = 4 });
        Assert.Contains("Observed", await TooltipTextAsync(page));
        await page.Mouse.ClickAsync(screen[0], screen[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        var reset = page.Locator("[data-cfx-reset]"); if (await reset.IsVisibleAsync()) await reset.ClickAsync();
        await MoveAwayAsync(page); await target.FocusAsync();
        Assert.Contains("Observed", await TooltipTextAsync(page)); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory))
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png") });
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task VisibleRadarMarkerStrokeRetainsAnOtherwiseInvisibleObservation() {
        var chart = Create(ChartSeriesKind.Radar, collapsed: true);
        chart.Series[0].WithFillPattern(ChartFillPattern.None).WithMarkerRadius(6);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), ellipse => ellipse.Rx == 6 && ellipse.Stroke?.A > 0);
        var rgba = prepared.ToRgba();
        Assert.Contains(Enumerable.Range(0, rgba.Width * rgba.Height), pixel => rgba.Pixels[pixel * 4 + 3] != 0);
        if (!Enabled) return;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        var page = session.Page; var target = page.Locator("[data-cfx-target-kind='point'][data-cfx-point='1']");
        await target.FocusAsync(); Assert.Contains("70", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space"); Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        var reset = page.Locator("[data-cfx-reset]"); if (await reset.IsVisibleAsync()) await reset.ClickAsync(); await MoveAwayAsync(page);
        var screen = await target.Locator("ellipse").EvaluateAsync<float[]>("node=>{const box=node.getBBox(),p=new DOMPoint(box.x+box.width,box.y+box.height/2).matrixTransform(node.getScreenCTM());return [p.x,p.y]}");
        await page.Mouse.MoveAsync(screen[0], screen[1], new MouseMoveOptions { Steps = 4 });
        Assert.Contains("70", await TooltipTextAsync(page)); await page.Mouse.ClickAsync(screen[0], screen[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected")); AssertNoConsoleErrors(session);
    }

    internal static Chart Create(ChartSeriesKind kind, bool collapsed, bool smooth = false) {
        var chart = Chart.Create().WithSize(600, 440).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels(false).WithTransparentBackground().WithCard(false).WithPlotBackground(false).WithYAxisBounds(0, 100);
        var ranges = new[] { new ChartRangeBand(1, collapsed ? 30 : 20, collapsed ? 30 : 40),
            new ChartRangeBand(2, collapsed ? 70 : 60, collapsed ? 70 : 80), new ChartRangeBand(3, collapsed ? 50 : 40, collapsed ? 50 : 60) };
        if (kind == ChartSeriesKind.RangeBand) chart.AddRangeBand("Observed", ranges);
        else if (kind == ChartSeriesKind.RangeArea) chart.AddRangeArea("Observed", ranges, smooth: smooth);
        else chart.AddRadar("Observed", collapsed ? new[] { new ChartPoint(1, 0), new ChartPoint(2, 70), new ChartPoint(3, 0) }
            : new[] { new ChartPoint(1, 30), new ChartPoint(2, 70), new ChartPoint(3, 50) });
        chart.Series[0].WithColor(ChartColor.White.WithAlpha(0)).WithMarkerRadius(0).WithFillPattern(ChartFillPattern.Crosshatch);
        return chart;
    }
}
