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

public sealed partial class InteractiveTooltipPositionBrowserTests {
    private static string CssClipChart(bool keyboard = true, bool dark = false, bool readable = false) {
        var chart = Chart.Create().WithSize(650, 338).WithHeader(false).WithLegend(false)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("css-reading"), "Reading", new[] { new ChartPoint(1, 5), new ChartPoint(5, 5), new ChartPoint(9, 5) }));
        return chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = readable ? HtmlChartResponsiveLayout.Readable : HtmlChartResponsiveLayout.Fit;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection
                | (keyboard ? ChartInteractionFeatures.KeyboardNavigation : ChartInteractionFeatures.None);
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        });
    }

    [BrowserTheory]
    [InlineData("inline", true)]
    [InlineData("contents", true)]
    [InlineData("block", false)]
    public async Task OverflowClipsOnlyBoxesThatActuallyClipNativePaint(string display, bool painted) {
        if (!Enabled) return;
        await using var session = await OpenAsync(CssClipChart(), 950, 760);
        var page = session.Page;
        await page.EvaluateAsync("""
            display => {
                const root=document.querySelector('.cfx-interactive-chart'), outer=document.createElement('div'),clip=document.createElement('span');
                outer.style.width='650px';root.before(outer);outer.append(clip);
                clip.id='css-clip'; clip.style.cssText=`display:${display};overflow:hidden;width:0;height:0`;
                clip.append(root);root.style.cssText+=';width:650px;max-width:none';
            }
            """, display);
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})");
        await SettleObservedPositionAsync(page);
        var state = await CssClipFactsAsync(page);
        await CaptureAsync(page, "css-clip-box-" + display, state, session);
        Assert.Equal(painted, state.GetProperty("painted").GetBoolean());
        Assert.Equal(!painted, state.GetProperty("hidden").GetBoolean());
        Assert.Equal("css-reading:0", state.GetProperty("targetId").GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("hidden", true)]
    [InlineData("clip", false)]
    [InlineData("auto", true)]
    public async Task KeyboardRevealsProgrammaticallyScrollableClipsWithoutSmoothDelay(string overflow, bool reveal) {
        if (!Enabled) return;
        await using var session = await OpenAsync(CssClipChart(true, true), 950, 760);
        var page = session.Page;
        await page.EvaluateAsync("""
            overflow=>{
                const root=document.querySelector('.cfx-interactive-chart'),clip=document.createElement('div');
                clip.id='css-clip';clip.style.cssText=`width:340px;height:420px;overflow:${overflow};scroll-behavior:smooth;border:3px solid #777`;
                root.before(clip);clip.append(root);root.style.cssText+=';width:650px;max-width:none';
            }
            """, overflow);
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})");
        await page.Keyboard.PressAsync("End");
        var immediate = await CssClipFactsAsync(page, Point(0, 2));
        await SettleObservedPositionAsync(page);
        var state = await CssClipFactsAsync(page, Point(0, 2));
        await CaptureAsync(page, "css-keyboard-" + overflow, new { immediate, state }, session);
        Assert.Equal("css-reading:2", state.GetProperty("focused").GetString());
        Assert.Equal("9", state.GetProperty("x").GetString());
        Assert.Equal("5", state.GetProperty("y").GetString());
        Assert.Equal(reveal, immediate.GetProperty("painted").GetBoolean());
        Assert.Equal(reveal, state.GetProperty("painted").GetBoolean());
        Assert.Equal(!reveal, state.GetProperty("hidden").GetBoolean());
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component=data][tabindex='0']").CountAsync());
        if (reveal) Assert.True(state.GetProperty("scrollLeft").GetDouble() > 150);
        else Assert.Equal(0, state.GetProperty("scrollLeft").GetDouble());
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task KeyboardRevealsSmoothNativeStageBeforeAcquiringReadout() {
        if (!Enabled) return;
        await using var session = await OpenAsync(CssClipChart(true, false, true), 390, 844);
        var page = session.Page;
        await page.Locator(".cfx-stage").EvaluateAsync("stage=>{stage.id='css-clip';stage.style.scrollBehavior='smooth'}");
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})");
        await page.Keyboard.PressAsync("End");
        var state = await CssClipFactsAsync(page, Point(0, 2));
        await CaptureAsync(page, "css-keyboard-smooth-stage", state, session);
        Assert.True(state.GetProperty("painted").GetBoolean());
        Assert.False(state.GetProperty("hidden").GetBoolean());
        Assert.Equal("css-reading:2", state.GetProperty("focused").GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("paint")]
    [InlineData("content")]
    [InlineData("strict")]
    public async Task PaintContainmentFitsCardAndDismissesClippedSource(string contain) {
        if (!Enabled) return;
        await using var session = await OpenAsync(CssClipChart(true, true), 950, 760);
        var page = session.Page;
        await page.EvaluateAsync("""
            contain=>{
                const root=document.querySelector('.cfx-interactive-chart'),clip=document.createElement('div');
                clip.id='css-clip';clip.style.cssText=`margin:20px;width:700px;height:190px;contain:${contain};border:5px solid #777;overflow:visible`;
                root.before(clip);clip.append(root);root.style.cssText+=';width:650px;max-width:none';
            }
            """, contain);
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})"); await page.Keyboard.PressAsync("Space");
        await SettleObservedPositionAsync(page);
        var shown = await CssClipFactsAsync(page);
        await CaptureAsync(page, "css-contain-" + contain + "-shown", shown, session);
        Assert.True(shown.GetProperty("painted").GetBoolean());
        Assert.False(shown.GetProperty("hidden").GetBoolean());
        Assert.True(shown.GetProperty("cardPainted").EnumerateArray().All(value => value.GetBoolean()), shown.ToString());
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("root=>root.style.transform='translateX(-350px)'");
        await SettleObservedPositionAsync(page);
        var clipped = await CssClipFactsAsync(page);
        await CaptureAsync(page, "css-contain-" + contain + "-clipped", clipped, session);
        Assert.False(clipped.GetProperty("painted").GetBoolean());
        Assert.True(clipped.GetProperty("hidden").GetBoolean());
        Assert.Equal("css-reading:0", clipped.GetProperty("selected").GetString());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("absolute", false)]
    [InlineData("fixed", false)]
    [InlineData("absolute", true)]
    public async Task PositionedChartUsesItsActualContainingBlockClip(string position, bool contains) {
        if (!Enabled) return;
        await using var session = await OpenAsync(CssClipChart(false), 950, 760);
        var page = session.Page;
        await page.EvaluateAsync("""
            ({position,contains})=>{
                const root=document.querySelector('.cfx-interactive-chart'),outer=document.createElement('div'),clip=document.createElement('div');
                outer.style.cssText='position:relative;width:850px;height:600px';
                clip.id='css-clip';clip.style.cssText='width:80px;height:80px;overflow:hidden;border:3px solid #777';
                if(contains)clip.style.transform='translate(0)';
                root.before(outer);outer.append(clip);clip.append(root);
                root.style.cssText+=`;position:${position};left:150px;top:20px;width:650px;max-width:none`;
            }
            """, new { position, contains });
        await MoveToAsync(page, Point(0, 0));
        await SettleObservedPositionAsync(page);
        var state = await CssClipFactsAsync(page);
        await CaptureAsync(page, "css-positioned-" + position + "-" + contains, state, session);
        Assert.Equal(!contains, state.GetProperty("painted").GetBoolean());
        Assert.Equal(contains, state.GetProperty("hidden").GetBoolean());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SlottedChartScrollUsesNativeComposedClipAndReleasesWatchers(bool closed) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 760);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(CssClipChart(true, true));
        await page.EvaluateAsync("""
            closed=>{
                const root=document.querySelector('.cfx-interactive-chart'),host=document.createElement('div');
                root.before(host);host.id='slot-host';host.style.width='360px';
                const tree=window.clipShadow=host.attachShadow({mode:closed?'closed':'open'});
                tree.innerHTML='<div id="css-clip" style="width:340px;height:420px;overflow:auto;border:3px solid #777"><div style="width:650px"><slot></slot></div></div>';
                host.append(root);root.style.cssText+=';width:650px;max-width:none';
                window.slotScrolls={native:0,document:0};tree.addEventListener('scroll',()=>slotScrolls.native++,true);
                document.addEventListener('scroll',()=>slotScrolls.document++,true);
                const nativeAdd=tree.addEventListener.bind(tree),nativeRemove=tree.removeEventListener.bind(tree);
                window.slotListeners={scroll:new Set(),transitionend:new Set()};
                tree.addEventListener=(name,listener,options)=>{slotListeners[name]?.add(listener);nativeAdd(name,listener,options)};
                tree.removeEventListener=(name,listener,options)=>{slotListeners[name]?.delete(listener);nativeRemove(name,listener,options)};
                const NativeIntersection=window.IntersectionObserver;window.slotObserved=new Map();
                window.IntersectionObserver=class extends NativeIntersection {
                    constructor(callback,options){super(callback,options);slotObserved.set(this,new Set())}
                    observe(node){slotObserved.get(this).add(node);super.observe(node)}
                    disconnect(){slotObserved.get(this).clear();super.disconnect()}
                };
                window.slotIntersections=[];window.slotNativeObserver=new IntersectionObserver(entries=>slotIntersections.push(...entries.map(e=>({ratio:e.intersectionRatio,intersection:{x:e.intersectionRect.x,y:e.intersectionRect.y,width:e.intersectionRect.width,height:e.intersectionRect.height}}))));
                slotNativeObserver.observe(root.querySelector('g[data-cfx-role="point"][data-cfx-point="0"]'));
            }
            """, closed);
        var baseline = await ListenerStateAsync(page);
        var intersectionBaseline = await page.EvaluateAsync<int>("()=>Array.from(slotObserved.values()).reduce((n,s)=>n+s.size,0)");
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})"); await page.Keyboard.PressAsync("Space");
        await SettleObservedPositionAsync(page);
        var shown = await CssClipFactsAsync(page);
        await CaptureAsync(page, "css-slot-" + closed + "-shown", shown, session);
        Assert.False(shown.GetProperty("hidden").GetBoolean());
        await page.EvaluateAsync("()=>clipShadow.getElementById('css-clip').scrollLeft=650");
        await SettleObservedPositionAsync(page);
        var clipped = await CssClipFactsAsync(page);
        var native = await page.EvaluateAsync<JsonElement>("()=>({assignedSlot:!!document.querySelector('.cfx-interactive-chart').assignedSlot,events:slotScrolls,intersections:slotIntersections,listeners:{scroll:slotListeners.scroll.size,transitionend:slotListeners.transitionend.size}})");
        var released = await ListenerStateAsync(page);
        await CaptureAsync(page, "css-slot-" + closed + "-clipped", new { clipped, native, baseline, released }, session);
        Assert.False(clipped.GetProperty("painted").GetBoolean());
        Assert.True(native.GetProperty("events").GetProperty("native").GetInt32() > 0);
        Assert.Equal(0, native.GetProperty("events").GetProperty("document").GetInt32());
        if (closed) {
            // Native scrolling can traverse an opaque slot without exposing its internal DOM.
            await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.scrollIntoView({behavior:'instant',block:'nearest',inline:'nearest'})");
            await SettleObservedPositionAsync(page);
            var nativeRecovery = await CssClipFactsAsync(page);
            await CaptureAsync(page, "css-slot-native-opaque-reveal", nativeRecovery, session);
            Assert.True(nativeRecovery.GetProperty("painted").GetBoolean());
        }
        Assert.True(clipped.GetProperty("hidden").GetBoolean());
        AssertPositionWatchersReleased(baseline, released);
        Assert.Equal(0, native.GetProperty("listeners").GetProperty("scroll").GetInt32());
        Assert.Equal(0, native.GetProperty("listeners").GetProperty("transitionend").GetInt32());
        // Only the independent native capability probe remains after the product readout is dismissed.
        Assert.Equal(intersectionBaseline, await page.EvaluateAsync<int>("()=>Array.from(slotObserved.values()).reduce((n,s)=>n+s.size,0)"));
        await page.Keyboard.PressAsync("End");
        var end = await CssClipFactsAsync(page, Point(0, 2));
        Assert.True(end.GetProperty("painted").GetBoolean());
        Assert.False(end.GetProperty("hidden").GetBoolean());
        await page.Keyboard.PressAsync("Home");
        await SettleObservedPositionAsync(page);
        var recovered = await CssClipFactsAsync(page);
        await CaptureAsync(page, "css-slot-" + closed + "-recovered", new { end, recovered }, session);
        Assert.True(recovered.GetProperty("painted").GetBoolean());
        Assert.False(recovered.GetProperty("hidden").GetBoolean());
        await page.EvaluateAsync("()=>{slotNativeObserver.disconnect();document.getElementById('slot-host').remove()}");
        await SettleObservedPositionAsync(page);
        var removed = await ListenerStateAsync(page);
        foreach (var name in new[] { "resize", "scroll", "transitionend", "observed" })
            Assert.True(removed.GetProperty(name).GetInt32() <= baseline.GetProperty(name).GetInt32());
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>Array.from(slotObserved.values()).reduce((n,s)=>n+s.size,0)"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task NativeMobileVisualViewportDismissesPinAndReleasesListeners() {
        if (!Enabled) return;
        await using var session = await HtmlBrowser.OpenSessionAsync("about:blank", new HtmlBrowserLaunchOptions {
            Browser = HtmlBrowserEngine.Chromium, Headless = true, Timeout = 15000,
            ViewportWidth = 390, ViewportHeight = 844, IsMobile = true, HasTouch = true, DeviceScaleFactor = 1
        });
        var page = session.Page;
        var cdp = await page.Context.NewCDPSessionAsync(page);
        
        await page.SetContentAsync(CssClipChart(true, true));
        await page.EvaluateAsync("""
            ()=>{
                document.body.style.minHeight='1800px';window.visualEvents={scroll:0,resize:0};
                visualViewport.addEventListener('scroll',()=>visualEvents.scroll++);
                visualViewport.addEventListener('resize',()=>visualEvents.resize++);
                const add=visualViewport.addEventListener.bind(visualViewport),remove=visualViewport.removeEventListener.bind(visualViewport);
                window.visualListeners={scroll:new Set(),resize:new Set()};
                visualViewport.addEventListener=(name,listener,options)=>{visualListeners[name]?.add(listener);add(name,listener,options)};
                visualViewport.removeEventListener=(name,listener,options)=>{visualListeners[name]?.delete(listener);remove(name,listener,options)};
            }
            """);
        // Exercise native visual viewport movement independently of physical pinch recognition.
        await cdp.SendAsync("Emulation.setPageScaleFactor", new Dictionary<string, object> { ["pageScaleFactor"] = 2 });
        await SettleObservedPositionAsync(page);
        await page.Locator(Point(0, 0)).EvaluateAsync("node=>node.focus({preventScroll:true})"); await page.Keyboard.PressAsync("Space");
        await SettleObservedPositionAsync(page);
        await CssClipFactsAsync(page);
        var before = await VisualViewportFactsAsync(page);
        await CaptureAsync(page, "css-mobile-visual-before", before, session);
        Assert.True(before.GetProperty("viewport").GetProperty("scale").GetDouble() > 1.5);
        Assert.False(before.GetProperty("source").GetProperty("hidden").GetBoolean());
        Assert.True(before.GetProperty("source").GetProperty("pinned").GetBoolean());
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchStart", ["touchPoints"] = new[] { new { x = 150, y = 20 } } });
        for (var step = 1; step <= 8; step++) {
            await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchMove", ["touchPoints"] = new[] { new { x = 150 - step * 17, y = 20 } } });
            await ScrollSettledAsync(page);
        }
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object> { ["type"] = "touchEnd", ["touchPoints"] = Array.Empty<object>() });
        await SettleObservedPositionAsync(page);
        var after = await VisualViewportFactsAsync(page);
        await CaptureAsync(page, "css-mobile-visual-clipped", after, session);
        Assert.True(after.GetProperty("viewport").GetProperty("left").GetDouble() > after.GetProperty("source").GetProperty("node").GetProperty("right").GetDouble(), after.ToString());
        Assert.True(after.GetProperty("events").GetProperty("scroll").GetInt32() > 0);
        Assert.True(after.GetProperty("source").GetProperty("hidden").GetBoolean());
        Assert.False(after.GetProperty("source").GetProperty("pinned").GetBoolean());
        Assert.Equal("css-reading:0", after.GetProperty("source").GetProperty("selected").GetString());
        var card = before.GetProperty("source").GetProperty("card"); var visual = before.GetProperty("viewport");
        Assert.True(card.GetProperty("left").GetDouble() >= visual.GetProperty("left").GetDouble());
        Assert.True(card.GetProperty("right").GetDouble() <= visual.GetProperty("left").GetDouble() + visual.GetProperty("width").GetDouble());
        Assert.True(card.GetProperty("top").GetDouble() >= visual.GetProperty("top").GetDouble());
        Assert.True(card.GetProperty("bottom").GetDouble() <= visual.GetProperty("top").GetDouble() + visual.GetProperty("height").GetDouble());
        Assert.Equal(0, after.GetProperty("listeners").GetProperty("scroll").GetInt32());
        Assert.Equal(0, after.GetProperty("listeners").GetProperty("resize").GetInt32());
        await cdp.DetachAsync();
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> VisualViewportFactsAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>({viewport:{left:visualViewport.offsetLeft,top:visualViewport.offsetTop,width:visualViewport.width,height:visualViewport.height,scale:visualViewport.scale},
            events:visualEvents,listeners:{scroll:visualListeners.scroll.size,resize:visualListeners.resize.size},source:window.cssClipFacts()})
        """);

    private static async Task<JsonElement> CssClipFactsAsync(IPage page, string? selector = null) {
        await page.EvaluateAsync("""
            ()=>window.cssClipFacts=(selector='g[data-cfx-role="point"][data-cfx-series="0"][data-cfx-point="0"]')=>{
                const root=document.querySelector('.cfx-interactive-chart'),node=root.querySelector(selector),tip=root.querySelector('.cfx-tooltip');
                const clip=document.getElementById('css-clip')||window.clipShadow?.getElementById('css-clip');
                const rect=n=>{const r=n.getBoundingClientRect();return{left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width,height:r.height}};
                const b=rect(node),card=rect(tip),hit=document.elementFromPoint((b.left+b.right)/2,(b.top+b.bottom)/2);
                const old=tip.style.pointerEvents;tip.style.pointerEvents='auto';
                const cardPainted=[[card.left+8,card.top+8],[card.right-8,card.top+8],[card.left+8,card.bottom-8],[card.right-8,card.bottom-8]].map(([x,y])=>tip.contains(document.elementFromPoint(x,y)));
                tip.style.pointerEvents=old;
                return{node:b,card,clip:clip&&rect(clip),scrollLeft:clip?.scrollLeft,clientWidth:clip?.clientWidth,hidden:tip.hidden,pinned:root.dataset.cfxTooltipPinned==='true',
                    painted:node.contains(hit),cardPainted,targetId:node.dataset.cfxTargetId,x:node.dataset.cfxX,y:node.dataset.cfxY,
                    focused:document.activeElement?.dataset.cfxTargetId,selected:root.querySelector('.cfx-selected')?.dataset.cfxTargetId,text:tip.innerText};
            }
            """);
        return await page.EvaluateAsync<JsonElement>("selector=>window.cssClipFacts(selector||undefined)", selector);
    }
}
