using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using HtmlTinkerX;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    // The same rendered fixture can bind an exact retained runtime for causal validation.
    private static string ComposedHostRuntime(string html) {
        var baseline = Environment.GetEnvironmentVariable("CFX_CSS_HOST_BASELINE_SCRIPT");
        return string.IsNullOrWhiteSpace(baseline) ? html : Regex.Replace(html, "<script[^>]*>.*?</script>",
            _ => "<script>" + File.ReadAllText(baseline) + "</script>", RegexOptions.Singleline);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NestedAbsoluteWrapperPreservesFixedChartContainingBlock(bool contains) {
        if (!Enabled) return;
        await using var session = await OpenAsync(ComposedHostRuntime(CssClipChart(false)), 950, 760);
        var page = session.Page;
        await page.EvaluateAsync("""
            contains=>{
                const root=document.querySelector('.cfx-interactive-chart'),clip=document.createElement('div'),wrapper=document.createElement('div');
                clip.id='css-clip';clip.style.cssText='position:relative;width:80px;height:80px;overflow:hidden;border:3px solid #777';
                if(contains)clip.style.transform='translate(0)';
                wrapper.style.cssText='position:absolute;left:0;top:0';
                root.before(clip);clip.append(wrapper);wrapper.append(root);
                root.style.cssText+=';position:fixed;left:150px;top:20px;width:650px;max-width:none';
            }
            """, contains);
        await MoveToAsync(page, Point(0, 0));
        await SettleObservedPositionAsync(page);
        var state = await CssClipFactsAsync(page);
        var native = await page.EvaluateAsync<JsonElement>("()=>{const root=document.querySelector('.cfx-interactive-chart');return {rootOffsetParent:root.offsetParent?.id??null,wrapperOffsetParent:root.parentElement.offsetParent?.id??null}}");
        await CaptureAsync(page, "css-nested-fixed-" + contains, new { state, native }, session);
        Assert.Equal(!contains, state.GetProperty("painted").GetBoolean());
        Assert.Equal(contains, state.GetProperty("hidden").GetBoolean());
        Assert.Equal(contains ? "css-clip" : null, native.GetProperty("rootOffsetParent").GetString());
        Assert.Equal("css-reading:0", state.GetProperty("targetId").GetString());
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task NamedSlotReassignmentRebindsContainingBlockAndAncestorLifetime() {
        if (!Enabled) return;
        await using var session = await OpenAsync(ComposedHostRuntime(CssClipChart(true, true)), 950, 760);
        var page = session.Page;
        await InstallReassignableSlotAsync(page);
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})");
        await page.Keyboard.PressAsync("Space");
        await SettleObservedPositionAsync(page);
        var before = await CssClipFactsAsync(page);
        await page.EvaluateAsync("()=>document.querySelector('.cfx-interactive-chart').slot='contained'");
        await SettleObservedPositionAsync(page);
        var after = await CssClipFactsAsync(page);
        var slots = await SlotHostFactsAsync(page);
        await CaptureAsync(page, "css-slot-reassigned", new { before, after, slots }, session);
        Assert.False(after.GetProperty("hidden").GetBoolean());
        Assert.True(after.GetProperty("pinned").GetBoolean());
        Assert.Equal(before.GetProperty("node").ToString(), after.GetProperty("node").ToString());
        Assert.Equal("contained", slots.GetProperty("assigned").GetString());
        Assert.True(after.GetProperty("cardPainted").EnumerateArray().All(x => x.GetBoolean()), after.ToString());
        await page.EvaluateAsync("()=>clipShadow.getElementById('slot-contained').style.opacity='0'");
        await SettleObservedPositionAsync(page);
        var hidden = await CssClipFactsAsync(page);
        await CaptureAsync(page, "css-slot-reassigned-hidden", new { hidden, slots = await SlotHostFactsAsync(page) }, session);
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.Equal("css-reading:0", hidden.GetProperty("selected").GetString());
        await page.EvaluateAsync("()=>document.querySelector('.cfx-interactive-chart').remove()");
        await SettleObservedPositionAsync(page);
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>slotChangeListeners.size"));
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task SlottedAncestorOpacityControlsPinnedAndKeyboardAvailability() {
        if (!Enabled) return;
        await using var session = await OpenAsync(ComposedHostRuntime(CssClipChart(true, true)), 950, 760);
        var page = session.Page;
        await InstallReassignableSlotAsync(page);
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})");
        await page.Keyboard.PressAsync("Space");
        await SettleObservedPositionAsync(page);
        Assert.False(await TipHiddenAsync(page));
        await page.EvaluateAsync("()=>clipShadow.getElementById('slot-ordinary').style.opacity='0'");
        await SettleObservedPositionAsync(page);
        var hidden = await CssClipFactsAsync(page);
        var unavailable = await page.Locator("[data-cfx-keyboard-component='data'][tabindex='0']").CountAsync();
        await CaptureAsync(page, "css-slot-opacity-hidden", new { hidden, unavailable, slots = await SlotHostFactsAsync(page) }, session);
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.False(hidden.GetProperty("pinned").GetBoolean());
        Assert.Equal(0, unavailable);
        Assert.Equal("css-reading:0", hidden.GetProperty("selected").GetString());
        await page.EvaluateAsync("()=>clipShadow.getElementById('slot-ordinary').style.opacity='1'");
        await SettleObservedPositionAsync(page);
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component='data'][tabindex='0']").CountAsync());
        await page.Keyboard.PressAsync("ArrowRight");
        var recovered = await CssClipFactsAsync(page, Point(0, 1));
        await CaptureAsync(page, "css-slot-opacity-recovered", recovered, session);
        Assert.Equal("css-reading:1", recovered.GetProperty("focused").GetString());
        Assert.False(recovered.GetProperty("hidden").GetBoolean());
        AssertNoConsoleErrors(session);
    }

    private static Task InstallReassignableSlotAsync(IPage page) => page.EvaluateAsync("""
        ()=>{
            const root=document.querySelector('.cfx-interactive-chart'),host=document.createElement('div');
            host.style.cssText='position:relative;width:650px;height:420px';root.before(host);
            window.clipShadow=host.attachShadow({mode:'open'});
            clipShadow.innerHTML='<div id="slot-ordinary" style="width:650px;height:338px"><slot></slot></div><div id="slot-contained" style="position:absolute;left:0;top:0;width:650px;height:210px;transform:translate(0);contain:paint;pointer-events:none"><slot name="contained"></slot></div>';
            const add=clipShadow.addEventListener.bind(clipShadow),remove=clipShadow.removeEventListener.bind(clipShadow);
            window.slotChangeListeners=new Set();
            clipShadow.addEventListener=(name,listener,options)=>{if(name==='slotchange')slotChangeListeners.add(listener);add(name,listener,options)};
            clipShadow.removeEventListener=(name,listener,options)=>{if(name==='slotchange')slotChangeListeners.delete(listener);remove(name,listener,options)};
            host.append(root);root.style.cssText+=';width:650px;max-width:none;pointer-events:auto';
        }
        """);

    private static Task<JsonElement> SlotHostFactsAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{const root=document.querySelector('.cfx-interactive-chart'),tip=root.querySelector('.cfx-tooltip');return {
            containingBlock:tip.offsetParent?.id??null,assigned:root.assignedSlot?.name??null,
            ordinaryOpacity:getComputedStyle(clipShadow.getElementById('slot-ordinary')).opacity,
            containedOpacity:getComputedStyle(clipShadow.getElementById('slot-contained')).opacity,slotListeners:slotChangeListeners.size}}
        """);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NarrowOuterClipPreservesVisibleWideIntervalAndRevealsOffscreenInterval(bool partial) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(650, 338).WithHeader(false).WithLegend(false).WithTheme(ChartTheme.GraphiteDark())
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 3).ConfigureRangeBar(options => options.Orientation = ChartOrientation.Horizontal)
            .AddRangeBar("Window", new[] { new ChartInterval(1, 2, 5.5), new ChartInterval(2, 7, 9) });
        chart.Series[0].WithInteractionKey("css-interval");
        var html = chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Features = ChartInteractionFeatures.KeyboardNavigation | ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        });
        await using var session = await OpenAsync(ComposedHostRuntime(html), 950, 760);
        var page = session.Page;
        await page.EvaluateAsync("""
            partial=>{
                const root=document.querySelector('.cfx-interactive-chart'),clip=document.createElement('div');
                clip.id='css-clip';clip.style.cssText='width:120px;height:420px;overflow:auto;border:3px solid #777';root.before(clip);clip.append(root);
                root.style.cssText+=';width:650px;max-width:none';
                window.intervalSelections=[];root.addEventListener('cfxselect',e=>intervalSelections.push(e.detail.target));
                if(partial){const b=root.querySelector('g[data-cfx-role="point"]').getBoundingClientRect(),c=clip.getBoundingClientRect();clip.scrollLeft=b.right-c.left-44}
            }
            """, partial);
        await SettleObservedPositionAsync(page);
        var before = await WideIntervalFactsAsync(page);
        if (partial) {
            Assert.True(before.GetProperty("visibleHit").GetBoolean());
            await page.Mouse.ClickAsync((float)before.GetProperty("hitX").GetDouble(), (float)before.GetProperty("hitY").GetDouble());
        } else {
            Assert.False(before.GetProperty("visibleHit").GetBoolean());
            await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})");
            await page.Keyboard.PressAsync("Space");
        }
        await SettleObservedPositionAsync(page);
        var after = await WideIntervalFactsAsync(page);
        await CaptureAsync(page, "css-outer-wide-" + partial, new { before, after }, session);
        Assert.True(before.GetProperty("width").GetDouble() > 150);
        Assert.True(after.GetProperty("visibleHit").GetBoolean());
        if (partial) Assert.Equal(before.GetProperty("scrollLeft").GetDouble(), after.GetProperty("scrollLeft").GetDouble());
        else Assert.True(after.GetProperty("scrollLeft").GetDouble() > 0);
        Assert.Equal("css-interval:0", after.GetProperty("selected").GetString());
        Assert.True(after.GetProperty("pinned").GetBoolean());
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component='data'][tabindex='0']").CountAsync());
        Assert.Equal("0", after.GetProperty("sourcePoint").GetString());
        Assert.Equal("2", after.GetProperty("start").GetString());
        Assert.Equal("5.5", after.GetProperty("end").GetString());
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> WideIntervalFactsAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{
            const root=document.querySelector('.cfx-interactive-chart'),node=root.querySelector('g[data-cfx-role="point"]'),clip=document.getElementById('css-clip');
            const b=node.getBoundingClientRect(),c=clip.getBoundingClientRect(),left=Math.max(b.left,c.left+clip.clientLeft),right=Math.min(b.right,c.left+clip.clientLeft+clip.clientWidth);
            const hitX=(left+right)/2,hitY=(b.top+b.bottom)/2;
            return{width:b.width,left:b.left,right:b.right,hitX,hitY,scrollLeft:clip.scrollLeft,visibleHit:right>left&&node.contains(document.elementFromPoint(hitX,hitY)),
                selected:root.querySelector('.cfx-selected')?.dataset.cfxTargetId,pinned:root.dataset.cfxTooltipPinned==='true',sourcePoint:node.dataset.cfxSourcePoint,start:node.dataset.cfxStart,end:node.dataset.cfxEnd,selections:intervalSelections};
        }
        """);

    [Fact]
    public async Task NativeVisualViewportResizeRepositionsAlreadyPinnedReadout() {
        if (!Enabled) return;
        await using var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
            Browser = HtmlBrowserEngine.Chromium, Headless = true, Timeout = 15000,
            ViewportWidth = 390, ViewportHeight = 844, IsMobile = true, HasTouch = true, DeviceScaleFactor = 1
        });
        var page = session.Page;
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await page.SetContentAsync(ComposedHostRuntime(CssClipChart(true, true)));
        await page.EvaluateAsync("()=>{window.activeVisualResizes=0;visualViewport.addEventListener('resize',()=>activeVisualResizes++)}");
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})");
        await page.Keyboard.PressAsync("Space");
        await SettleObservedPositionAsync(page);
        var before = await CssClipFactsAsync(page);
        Assert.True(before.GetProperty("pinned").GetBoolean());
        await page.EvaluateAsync("()=>window.activeVisualResizes=0");
        await cdp.SendAsync("Input.synthesizePinchGesture", new Dictionary<string, object> { ["x"] = 100, ["y"] = 100, ["scaleFactor"] = 2, ["relativeSpeed"] = 800, ["gestureSourceType"] = "touch" });
        await SettleObservedPositionAsync(page);
        var after = await CssClipFactsAsync(page);
        var native = await page.EvaluateAsync<JsonElement>("()=>({left:visualViewport.offsetLeft,top:visualViewport.offsetTop,width:visualViewport.width,height:visualViewport.height,scale:visualViewport.scale,resizes:activeVisualResizes})");
        await CaptureAsync(page, "css-mobile-active-resize", new { before, after, native }, session);
        Assert.True(native.GetProperty("scale").GetDouble() > 1.5);
        Assert.True(native.GetProperty("resizes").GetInt32() > 0);
        Assert.True(after.GetProperty("pinned").GetBoolean());
        Assert.False(after.GetProperty("hidden").GetBoolean());
        var card = after.GetProperty("card");
        Assert.True(card.GetProperty("left").GetDouble() >= native.GetProperty("left").GetDouble());
        Assert.True(card.GetProperty("right").GetDouble() <= native.GetProperty("left").GetDouble() + native.GetProperty("width").GetDouble());
        Assert.True(card.GetProperty("bottom").GetDouble() <= native.GetProperty("top").GetDouble() + native.GetProperty("height").GetDouble());
        await cdp.DetachAsync();
        AssertNoConsoleErrors(session);
    }
}
