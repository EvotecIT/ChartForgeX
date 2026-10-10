using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipDelayBrowserTests {
    [Theory]
    [InlineData("rect", "nearest", 0)]
    [InlineData("rect", "distance", 0)]
    [InlineData("foreignObject", "nearest", 0)]
    [InlineData("foreignObject", "distance", 0)]
    [InlineData("rect", "nearest", Delay)]
    [InlineData("rect", "distance", Delay)]
    [InlineData("foreignObject", "nearest", Delay)]
    [InlineData("foreignObject", "distance", Delay)]
    public async Task HostSvgVeilCannotInferCoveredObservations(string element, string range, int delay) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => {
            ConfigureDelay(options, range); options.Tooltip.DelayMilliseconds = delay;
        }), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        await page.EvaluateAsync("""
            ({element,delay}) => {
                const root=document.querySelector('.cfx-interactive-chart'), svg=root.querySelector('.cfx-stage svg'), tip=root.querySelector('.cfx-tooltip');
                window.svgVeilExpiry=[];
                const cover=()=>{
                    const veil=document.createElementNS('http://www.w3.org/2000/svg',element);
                    veil.id='delay-svg-veil';
                    for(const [key,value] of Object.entries({x:0,y:0,width:800,height:540,fill:'#eee','pointer-events':'all'})) veil.setAttribute(key,String(value));
                    // A foreignObject's native rectangle owns the hit even when it has no HTML children.
                    svg.append(veil);
                };
                const record=()=>{
                    const hit=document.elementFromPoint(...window.delayTrace.pointer);
                    window.svgVeilExpiry.push({hit:hit?.id,tag:hit?.localName,owned:!!hit&&root.contains(hit),hidden:tip.hidden,readout:tip.innerText});
                };
                window.recordSvgVeil=record;
                if(!delay) { cover(); return; }
                const nativeTimer=window.setTimeout.bind(window);
                window.setTimeout=(callback,ms,...args)=>nativeTimer((...values)=>{
                    if(ms!==delay) { callback(...values); return; }
                    cover(); callback(...values); record();
                },ms,...args);
            }
            """, new { element, delay });
        await PointerAsync(page, Point(0, 0));
        if (delay == 0) await page.EvaluateAsync("() => window.recordSvgVeil()");
        else await page.WaitForFunctionAsync("() => window.svgVeilExpiry.length>0", null, new PageWaitForFunctionOptions { Timeout = 5000 });
        var expiry = await page.EvaluateAsync<JsonElement>("() => window.svgVeilExpiry");
        var trace = await TraceStateAsync(page);
        await CaptureDelayAsync(page, "delay-svg-veil-" + element + "-" + range + "-" + delay, new { expiry, trace }, session);
        var observed = Assert.Single(expiry.EnumerateArray());
        Assert.Equal("delay-svg-veil", observed.GetProperty("hit").GetString());
        Assert.Equal(element, observed.GetProperty("tag").GetString());
        Assert.True(observed.GetProperty("owned").GetBoolean());
        Assert.True(observed.GetProperty("hidden").GetBoolean());
        Assert.Empty(trace.GetProperty("shown").EnumerateArray());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, "nearest", 0)]
    [InlineData(false, "distance", 0)]
    [InlineData(true, "nearest", 0)]
    [InlineData(true, "distance", 0)]
    [InlineData(false, "nearest", Delay)]
    [InlineData(false, "distance", Delay)]
    [InlineData(true, "nearest", Delay)]
    [InlineData(true, "distance", Delay)]
    public async Task NativeSparsePlotBackgroundStillInfersAnObservation(bool transparent, string range, int delay) {
        if (!Enabled) return;
        var chart = Observations(false);
        chart.Options.TransparentBackground = transparent;
        chart.Options.HostOwnsFrame = false; chart.Options.ShowCard = false; chart.Options.ShowPlotBackground = false;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            ConfigureDelay(options, range); options.Tooltip.DelayMilliseconds = delay;
        }), 950, 760);
        var page = session.Page;
        await TraceAsync(page);
        var point = await BoxAsync(page, Point(0, 0));
        var x = point.X + point.Width / 2 + 25; var y = point.Y + point.Height / 2 + 25;
        await page.Mouse.MoveAsync((float)x, (float)y);
        var hit = await page.EvaluateAsync<JsonElement>("()=>{const node=document.elementFromPoint(...window.delayTrace.pointer);return {tag:node.localName,role:node.dataset.cfxRole||null};}");
        Assert.Equal(transparent ? "svg" : "rect", hit.GetProperty("tag").GetString());
        if (!transparent) Assert.Equal("background", hit.GetProperty("role").GetString());
        await WaitForTipAsync(page);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        var trace = await TraceStateAsync(page);
        if (delay > 0) AssertDelay(trace);
        await CaptureDelayAsync(page, "delay-native-background-" + transparent + "-" + range + "-" + delay, new { hit, trace }, session);
        AssertNoConsoleErrors(session);
    }
}
