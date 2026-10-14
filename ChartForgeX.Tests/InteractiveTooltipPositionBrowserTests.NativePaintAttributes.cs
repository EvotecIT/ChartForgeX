using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [BrowserTheory]
    [InlineData(HtmlChartTooltipAnchor.Pointer, false)]
    [InlineData(HtmlChartTooltipAnchor.Pointer, true)]
    [InlineData(HtmlChartTooltipAnchor.Node, false)]
    [InlineData(HtmlChartTooltipAnchor.Node, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, true)]
    public Task NativePaintAttributeOpacityDismissesReadoutAcrossAnchorsAndPinStates(HtmlChartTooltipAnchor anchor, bool pinned) =>
        AssertNativePaintAttributeLifetimeAsync(anchor, pinned, "opacity");

    [BrowserTheory]
    [InlineData(HtmlChartTooltipAnchor.Node, false, "fill-opacity")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "fill")]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "stroke")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "stroke-opacity")]
    [InlineData(HtmlChartTooltipAnchor.Node, false, "visibility")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "display")]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "clip-path")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "rx")]
    public Task NativePaintAttributeLastInkAndGeometryChangesDismissAndRecover(HtmlChartTooltipAnchor anchor, bool pinned, string attribute) =>
        AssertNativePaintAttributeLifetimeAsync(anchor, pinned, attribute);

    private static async Task AssertNativePaintAttributeLifetimeAsync(HtmlChartTooltipAnchor anchor, bool pinned, string attribute) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 760);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(Observations(false).ToInteractiveHtmlPage(options => {
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            // The position lifetime must work without the keyboard owner's separate paint observer.
            options.Interaction.Disable(ChartInteractionFeatures.KeyboardNavigation);
        }));
        const string detail = "Native paint fact: <b>retained</b> & \"quoted\" observation";
        await page.Locator(Reading).EvaluateAsync("(node,detail)=>node.setAttribute('data-cfx-meta-host-fact',detail)", detail);
        Assert.Equal(1, await page.Locator(Reading + " [data-cfx-role=marker]").CountAsync());
        await page.Locator(Reading).EvaluateAsync("""
            (node,attribute)=>{
                const shape=node.querySelector('[data-cfx-role=marker]');
                // Scatter markers have one primary fill and no series path or hatch fallback.
                shape.setAttribute('stroke','none');
                if(attribute==='stroke'||attribute==='stroke-opacity') {
                    shape.setAttribute('stroke','#176eaf'); shape.setAttribute('stroke-width','2');
                }
                if(attribute==='clip-path') {
                    const ns='http://www.w3.org/2000/svg',clip=document.createElementNS(ns,'clipPath'),rect=document.createElementNS(ns,'rect');
                    clip.id='position-native-empty-clip'; clip.setAttribute('clipPathUnits','userSpaceOnUse');
                    rect.setAttribute('width','0'); rect.setAttribute('height','0'); clip.append(rect);
                    shape.ownerSVGElement.querySelector('defs').append(clip);
                }
                const names=['opacity','fill','fill-opacity','stroke','stroke-opacity','stroke-width','visibility','display','clip-path','rx'];
                window.positionNativePaint={shape,attributes:Object.fromEntries(names.map(name=>[name,shape.getAttribute(name)]))};
            }
            """, attribute);
        await SettleObservedPositionAsync(page);
        var baseline = await ListenerStateAsync(page);
        var pointer = await AcquireNativePaintAttributeReadoutAsync(page, pinned);
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight }, pointer: pointer);
        var active = await ListenerStateAsync(page);
        Assert.Contains(detail, shown.GetProperty("text").GetString()!, StringComparison.Ordinal);
        Assert.Equal(pinned, shown.GetProperty("pinned").GetBoolean());
        Assert.Equal(baseline.GetProperty("observed").GetInt32() + 1, active.GetProperty("observed").GetInt32());
        JsonElement? strokeOnly = null;
        if (attribute is "stroke" or "stroke-opacity") {
            // Removing fill must retain a readout while its native outline is still painted.
            await page.EvaluateAsync("()=>window.positionNativePaint.shape.setAttribute('fill','none')");
            await SettleObservedPositionAsync(page);
            strokeOnly = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight }, pointer: pointer);
            Assert.Equal(shown.GetProperty("text").GetString(), strokeOnly.Value.GetProperty("text").GetString());
            Assert.Equal(pinned, strokeOnly.Value.GetProperty("pinned").GetBoolean());
        }
        // Only the native presentation/geometry attribute changes after the observer has settled.
        await page.EvaluateAsync("""
            attribute=>{
                const value=attribute==='fill'||attribute==='stroke'?'none':attribute==='visibility'?'hidden':
                    attribute==='display'?'none':attribute==='clip-path'?'url(#position-native-empty-clip)':'0';
                window.positionNativePaint.shape.setAttribute(attribute,value);
            }
            """, attribute);
        await SettleObservedPositionAsync(page);
        var hidden = await GeometryAsync(page);
        var released = await ListenerStateAsync(page);
        var paint = await NativePaintAttributeStateAsync(page);
        var name = $"position-native-paint-{anchor}-{(pinned ? "pinned" : "shown")}-{attribute}";
        await CaptureAsync(page, name, new { baseline, shown, active, strokeOnly, paint, hidden, released }, session);
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.False(hidden.GetProperty("pinned").GetBoolean());
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-pinned-target"));
        AssertPositionWatchersReleased(baseline, released);
        await page.EvaluateAsync("""
            ()=>{
                const {shape,attributes}=window.positionNativePaint;
                for(const [name,value] of Object.entries(attributes)) {
                    if(value===null) shape.removeAttribute(name); else shape.setAttribute(name,value);
                }
                shape.ownerSVGElement.getElementById('position-native-empty-clip')?.remove();
            }
            """);
        await SettleObservedPositionAsync(page);
        await MoveAwayAsync(page);
        pointer = await AcquireNativePaintAttributeReadoutAsync(page, pinned);
        var recovered = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight }, pointer: pointer);
        var registered = await ListenerStateAsync(page);
        await CaptureAsync(page, name + "-recovered", new { recovered, registered, paint = await NativePaintAttributeStateAsync(page) }, session);
        Assert.Equal(shown.GetProperty("text").GetString(), recovered.GetProperty("text").GetString());
        Assert.Equal(pinned, recovered.GetProperty("pinned").GetBoolean());
        Assert.Equal(active.GetProperty("observed").GetInt32(), registered.GetProperty("observed").GetInt32());
        AssertNoConsoleErrors(session);
    }

    private static async Task<double[]> AcquireNativePaintAttributeReadoutAsync(IPage page, bool pinned) {
        var pointer = await PointerAsync(page);
        if (pinned) {
            // MouseEvent click coordinates can be integral even when PointerEvent movement was fractional.
            await page.Locator(Reading).EvaluateAsync("node=>node.addEventListener('click',event=>window.positionNativeClick=[event.clientX,event.clientY],{capture:true,once:true})");
            await page.Mouse.ClickAsync((float)pointer[0], (float)pointer[1]);
            pointer = await page.EvaluateAsync<double[]>("()=>window.positionNativeClick");
        }
        await SettleObservedPositionAsync(page);
        return pointer;
    }

    private static Task<JsonElement> NativePaintAttributeStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{
            const shape=window.positionNativePaint.shape,style=getComputedStyle(shape),box=shape.getBBox();
            return {tag:shape.tagName,opacity:style.opacity,fill:style.fill,fillOpacity:style.fillOpacity,stroke:style.stroke,
                strokeOpacity:style.strokeOpacity,strokeWidth:style.strokeWidth,visibility:style.visibility,display:style.display,
                clip:shape.getAttribute('clip-path'),rx:shape.getAttribute('rx'),bounds:{x:box.x,y:box.y,width:box.width,height:box.height}};
        }
        """);
}
