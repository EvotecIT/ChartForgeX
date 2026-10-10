using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class RadialLabelEligibilityBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Pie, true)]
    [InlineData(ChartSeriesKind.Pie, false)]
    [InlineData(ChartSeriesKind.Donut, true)]
    [InlineData(ChartSeriesKind.Donut, false)]
    [InlineData(ChartSeriesKind.RadialBar, true)]
    [InlineData(ChartSeriesKind.RadialBar, false)]
    [InlineData(ChartSeriesKind.RadialColumn, true)]
    [InlineData(ChartSeriesKind.RadialColumn, false)]
    [InlineData(ChartSeriesKind.Polar, true)]
    [InlineData(ChartSeriesKind.Polar, false)]
    [InlineData(ChartSeriesKind.Radar, true)]
    [InlineData(ChartSeriesKind.Radar, false)]
    [InlineData(ChartSeriesKind.PolarArea, true)]
    [InlineData(ChartSeriesKind.PolarArea, false)]
    public async Task LegendMuteRejectsCaptionHoverSelectionAndPinUntilItsNativeObservationIsRestored(ChartSeriesKind kind, bool crosshair) {
        if (!Enabled) return;
        var chart = RadialLabelTargetBrowserTests.Create(kind, true).WithLegend();
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            if (!crosshair) options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), 800, 560);
        AssertNoConsoleErrors(session);
        var page = session.Page;
        await RecordEventsAsync(page);
        var (label, native, source) = await CaptionAsync(page);
        var id = await native.GetAttributeAsync("data-cfx-target-id");
        var pointer = await PointerAsync(label);
        var initialPointer = new { X = pointer.X, Y = pointer.Y };
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        Assert.Contains(id!, (await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"))!);
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        await MoveAwayAsync(page);

        var legend = page.Locator(kind is ChartSeriesKind.Pie or ChartSeriesKind.Donut
            ? $"[data-cfx-role='legend-item'][data-cfx-series='0'][data-cfx-point='{source}']"
            : "[data-cfx-role='legend-item'][data-cfx-series='0']").First;
        await legend.Locator("text").First.ClickAsync();
        await MoveAwayAsync(page);
        Assert.True(await native.EvaluateAsync<bool>("node=>!!node.closest('.cfx-series-muted')"));
        Assert.Equal("true", await legend.GetAttributeAsync("data-cfx-muted"));
        var selected = await native.GetAttributeAsync("aria-selected");
        await ResetEventsAsync(page);
        // Activating a legend can scroll its containing page or readable stage.
        // The caption contract belongs to its current painted location.
        pointer = await PointerAsync(label);
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        var hover = await StateAsync(page, pointer);
        await page.Mouse.ClickAsync(pointer.X, pointer.Y);
        var click = await StateAsync(page, pointer);
        await CaptureAsync(page, kind + (crosshair ? "-mute-default" : "-mute-no-crosshair"), new {
            source, initialPointer, captionPointer = new { X = pointer.X, Y = pointer.Y }, hover, click
        });
        Assert.Equal("text", hover.GetProperty("hit").GetProperty("tag").GetString());
        Assert.Equal("series-0-point-" + source, hover.GetProperty("hit").GetProperty("alias").GetString());
        Assert.Equal(JsonValueKind.Null, hover.GetProperty("hit").GetProperty("pointAncestor").ValueKind);
        await AssertRejectedAsync(page, id);
        Assert.Equal(selected, await native.GetAttributeAsync("aria-selected"));
        Assert.True(await native.EvaluateAsync<bool>("node=>!!node.closest('.cfx-series-muted')"));

        await legend.Locator("text").First.ClickAsync();
        await MoveAwayAsync(page);
        await ResetEventsAsync(page);
        await native.FocusAsync();
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await native.GetAttributeAsync("aria-selected"));
        Assert.Equal(id, await page.EvaluateAsync<string>("()=>window.cfxEligibilityEvents.find(event=>event.type==='cfxselect').detail.target.targetId"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartSeriesKind.RadialBar, "display")]
    [InlineData(ChartSeriesKind.RadialBar, "visibility")]
    [InlineData(ChartSeriesKind.RadialBar, "opacity")]
    [InlineData(ChartSeriesKind.RadialBar, "unpainted")]
    [InlineData(ChartSeriesKind.RadialBar, "clip")]
    [InlineData(ChartSeriesKind.Pie, "clip")]
    public async Task PaintedCaptionCannotActivateAHiddenUnpaintedOrClippedNativeObservation(ChartSeriesKind kind, string state) {
        if (!Enabled) return;
        await using var session = await OpenAsync(RadialLabelTargetBrowserTests.Create(kind, true).ToInteractiveHtmlPage(), 800, 560);
        AssertNoConsoleErrors(session);
        var page = session.Page;
        await RecordEventsAsync(page);
        var (label, native, source) = await CaptionAsync(page);
        var pointer = await PointerAsync(label);
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        Assert.NotEqual(string.Empty, await TooltipTextAsync(page));
        await MoveAwayAsync(page);
        await native.EvaluateAsync("""
            (node,state)=>{
              if(state==='display') node.style.display='none';
              else if(state==='visibility') node.style.visibility='hidden';
              else if(state==='opacity') node.style.opacity='0';
              else if(state==='unpainted') node.querySelectorAll('path,rect,circle,ellipse,line,polyline,polygon').forEach(shape=>{shape.style.fill='none';shape.style.stroke='none';});
              else if(state==='clip') {
                const svg=node.ownerSVGElement,ns='http://www.w3.org/2000/svg',clip=document.createElementNS(ns,'clipPath'),rect=document.createElementNS(ns,'rect');
                clip.id='eligibility-native-clip';clip.setAttribute('clipPathUnits','userSpaceOnUse');
                for(const [key,value] of Object.entries({x:'-20',y:'-20',width:'1',height:'1'}))rect.setAttribute(key,value);
                clip.appendChild(rect);svg.querySelector('defs').appendChild(clip);node.setAttribute('clip-path','url(#eligibility-native-clip)');
              }
            }
            """, state);
        var selected = await native.GetAttributeAsync("aria-selected");
        await ResetEventsAsync(page);
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        var hover = await StateAsync(page, pointer);
        await page.Mouse.ClickAsync(pointer.X, pointer.Y);
        var click = await StateAsync(page, pointer);
        await CaptureAsync(page, kind + "-native-" + state, new { source, hover, click });
        Assert.Equal("text", hover.GetProperty("hit").GetProperty("tag").GetString());
        Assert.Equal("series-0-point-" + source, hover.GetProperty("hit").GetProperty("alias").GetString());
        await AssertRejectedAsync(page);
        Assert.Equal(selected, await native.GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(true, "zero")]
    [InlineData(false, "zero")]
    [InlineData(true, "precision-collapse")]
    [InlineData(false, "precision-collapse")]
    public async Task RetainedNativeFactsKeepTheirKeyboardReadoutAndSelectionWithoutPointerPaint(bool bars, string status) {
        if (!Enabled) return;
        var (chart, series) = NumericRadialOwnerClosureTests.RetainedFact(bars, status);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 700, 520);
        AssertNoConsoleErrors(session);
        var page = session.Page;
        await RecordEventsAsync(page);
        var native = page.Locator(Point(series, 0));
        Assert.True(await native.EvaluateAsync<bool>("node=>node.childElementCount===0"));
        Assert.Equal(status, await native.GetAttributeAsync("data-cfx-geometry-status"));
        var id = await native.GetAttributeAsync("data-cfx-target-id");
        var source = await native.EvaluateAsync<float[]>("""
            node=>{
              const root=node.closest('.cfx-interactive-chart'),prepared=JSON.parse(root.dataset.cfxPreparedChart);
              const region=prepared.regions.find(region=>region.id===node.dataset.cfxSourceId);
              const location=new DOMPoint(region.x+region.width/2,region.y+region.height/2).matrixTransform(node.ownerSVGElement.getScreenCTM());
              return [location.x,location.y];
            }
            """);
        await page.Mouse.ClickAsync(source[0], source[1]);
        var pointer = await StateAsync(page, (source[0], source[1]));
        Assert.Equal(0, await page.EvaluateAsync<int>("id=>window.cfxEligibilityEvents.filter(event=>event.detail.target?.targetId===id&&['cfxhover','cfxselect','cfxtooltip'].includes(event.type)).length", id));
        Assert.NotEqual("true", await native.GetAttributeAsync("aria-selected"));
        await MoveAwayAsync(page);
        if (await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned") == "true")
            await page.Locator("[data-cfx-reset]").ClickAsync();
        await ResetEventsAsync(page);
        await native.FocusAsync();
        Assert.Contains("Retained", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await native.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal(id, await page.EvaluateAsync<string>("()=>window.cfxEligibilityEvents.find(event=>event.type==='cfxselect').detail.target.targetId"));
        await CaptureAsync(page, "native-keyboard-" + (bars ? "bar-" : "column-") + status, new { status, series, id, pointer });
        AssertNoConsoleErrors(session);
    }

    private static async Task<(ILocator Label, ILocator Native, string Source)> CaptionAsync(IPage page) {
        var label = page.Locator("[data-cfx-label-for^='series-0-point-'] text").First;
        var reference = await label.EvaluateAsync<string>("node=>node.closest('[data-cfx-label-for]').dataset.cfxLabelFor");
        var source = reference["series-0-point-".Length..];
        // Polar/Radar source points inherit their series identity from the containing native series.
        return (label, page.Locator($"[data-cfx-point='{source}'][data-cfx-target-kind='point']"), source);
    }

    private static async Task<(float X, float Y)> PointerAsync(ILocator label) {
        var box = await label.BoundingBoxAsync() ?? throw new InvalidOperationException("No painted caption.");
        return ((float)(box.X + box.Width * .35), (float)(box.Y + box.Height * .45));
    }

    private static Task RecordEventsAsync(IPage page) => page.EvaluateAsync("() => { window.cfxEligibilityEvents=[]; const root=document.querySelector('.cfx-interactive-chart'); for(const type of ['cfxhover','cfxhoverclear','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.cfxEligibilityEvents.push({type,detail:event.detail})); }");
    private static Task ResetEventsAsync(IPage page) => page.EvaluateAsync("()=>window.cfxEligibilityEvents=[]");

    private static async Task AssertRejectedAsync(IPage page, string? nativeId = null) {
        var root = page.Locator(".cfx-interactive-chart");
        Assert.Null(await root.GetAttributeAsync("data-cfx-hover-key"));
        Assert.Null(await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        // A focused legend can retain its readout while the muted observation's caption rejects pointer input.
        Assert.Equal(0, await page.EvaluateAsync<int>("id=>window.cfxEligibilityEvents.filter(event=>['cfxhover','cfxselect','cfxtooltip'].includes(event.type)&&(!id||event.detail.target?.targetKind==='point'&&event.detail.target?.targetId===id)).length", nativeId));
    }

    private static Task<JsonElement> StateAsync(IPage page, (float X, float Y) pointer) => page.EvaluateAsync<JsonElement>("""
        p=>{const root=document.querySelector('.cfx-interactive-chart'),hit=document.elementFromPoint(p.x,p.y),alias=hit?.closest('[data-cfx-label-for]'),native=alias?Array.from(root.querySelectorAll('[data-cfx-source-id]')).find(node=>node.dataset.cfxSourceId===alias.dataset.cfxLabelFor)?.closest('[data-cfx-point]'):null,tip=root.querySelector('.cfx-tooltip');return {
          hit:{tag:hit?.tagName,alias:alias?.dataset.cfxLabelFor||null,pointAncestor:hit?.closest('[data-cfx-point]')?.dataset.cfxPoint||null},
          native:native?{id:native.dataset.cfxTargetId,classes:native.getAttribute('class'),selected:native.getAttribute('aria-selected'),display:getComputedStyle(native).display,visibility:getComputedStyle(native).visibility,opacity:getComputedStyle(native).opacity,clip:native.getAttribute('clip-path')}:null,
          hover:root.dataset.cfxHoverKey||null,pinned:root.dataset.cfxTooltipPinned||null,tooltip:tip.hidden?'':tip.innerText,events:window.cfxEligibilityEvents};}
        """, new { x = (double)pointer.X, y = (double)pointer.Y });

    private static async Task CaptureAsync(IPage page, string stem, object state) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, stem + ".png") });
        await File.WriteAllTextAsync(Path.Combine(capture, stem + ".json"), JsonSerializer.Serialize(new { state,
            events = await page.EvaluateAsync<JsonElement>("()=>window.cfxEligibilityEvents") }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
