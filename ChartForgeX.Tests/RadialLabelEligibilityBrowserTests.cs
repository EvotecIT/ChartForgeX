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
        await page.Mouse.MoveAsync(pointer.X, pointer.Y);
        var hover = await StateAsync(page, pointer);
        await page.Mouse.ClickAsync(pointer.X, pointer.Y);
        var click = await StateAsync(page, pointer);
        await CaptureAsync(page, kind + (crosshair ? "-mute-default" : "-mute-no-crosshair"), new { source, hover, click });
        Assert.Equal("text", hover.GetProperty("hit").GetProperty("tag").GetString());
        Assert.Equal("series-0-point-" + source, hover.GetProperty("hit").GetProperty("alias").GetString());
        Assert.Equal(JsonValueKind.Null, hover.GetProperty("hit").GetProperty("pointAncestor").ValueKind);
        await AssertRejectedAsync(page);
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
    [InlineData("zero", "0")]
    [InlineData("precision-collapse", "1e-20")]
    public async Task RetainedNativeFactsKeepTheirKeyboardReadoutAndSelectionWithoutPointerPaint(string status, string value) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithLegend(false)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddHorizontalLine(6, "Reference guide");
        var html = chart.ToInteractiveHtmlPage();
        // This producer generation has no zero-geometry facts. Exercise the existing exported native
        // metadata boundary in the real adapter, without inventing a mouse hit area or a production hook.
        var fact = "<g data-cfx-role='link' data-cfx-target-kind='link' data-cfx-target-id='native-fact' data-cfx-geometry-status='"
            + status + "' data-cfx-label='Native retained fact' data-cfx-value='" + value + "' aria-label='Native retained fact'></g>";
        html = html.Insert(html.IndexOf('>', html.IndexOf("<svg", StringComparison.Ordinal)) + 1, fact);
        await using var session = await OpenAsync(html);
        AssertNoConsoleErrors(session);
        var page = session.Page;
        await RecordEventsAsync(page);
        var native = page.Locator("[data-cfx-target-id='native-fact']");
        Assert.True(await native.EvaluateAsync<bool>("node=>{const box=node.getBoundingClientRect();return node.childElementCount===0&&box.width===0&&box.height===0;}"));
        await native.FocusAsync();
        Assert.Contains("Native retained fact", await TooltipTextAsync(page));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await native.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal("native-fact", await page.EvaluateAsync<string>("()=>window.cfxEligibilityEvents.find(event=>event.type==='cfxselect').detail.target.targetId"));
        await CaptureAsync(page, "native-keyboard-" + status, new { status, value });
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

    private static async Task AssertRejectedAsync(IPage page) {
        var root = page.Locator(".cfx-interactive-chart");
        Assert.Null(await root.GetAttributeAsync("data-cfx-hover-key"));
        Assert.Null(await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>window.cfxEligibilityEvents.filter(event=>['cfxhover','cfxselect','cfxtooltip'].includes(event.type)).length"));
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
