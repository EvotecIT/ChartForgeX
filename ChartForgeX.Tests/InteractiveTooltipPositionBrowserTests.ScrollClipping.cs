using System.Text.Json;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [BrowserFact]
    public async Task PinnedReadableScrollDismissesOutOfViewSourceAndKeyboardRecovers() {
        if (!Enabled) return;
        var chart = StatusLines(true);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 390, 844);
        var page = session.Page;
        var first = page.Locator(Point(0, 0));
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.scrollSelection = event.detail.target)");
        await first.FocusAsync(); await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.scrollSelection");
        Assert.Equal("passed:0", selected.GetProperty("targetId").GetString());
        Assert.Equal("0", selected.GetProperty("sourcePoint").GetString());
        Assert.Equal("820", selected.GetProperty("value").GetString());
        Assert.Equal("Mon", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await CaptureAsync(page, "scroll-readable-dark-pinned-before", await ScrollFactsAsync(page, Point(0, 0)), session);
        await page.Locator(".cfx-stage").EvaluateAsync("stage => stage.scrollLeft = stage.scrollWidth");
        await ScrollSettledAsync(page);
        var clipped = await ScrollFactsAsync(page, Point(0, 0));
        Assert.True(clipped.GetProperty("node").GetProperty("right").GetDouble() < clipped.GetProperty("viewport").GetProperty("left").GetDouble());
        await CaptureAsync(page, "scroll-readable-dark-pinned-clipped", clipped, session);
        Assert.True(await TipHiddenAsync(page), "A pinned source fully outside its native readable viewport must dismiss the tooltip.");
        Assert.NotEqual("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal("passed:0", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        Assert.Equal("820", await first.GetAttributeAsync("data-cfx-y"));
        Assert.Equal("passed:0", await page.Locator(".cfx-selected").GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(1, await page.Locator("[data-cfx-keyboard-component=data][tabindex='0']").CountAsync());

        await page.Keyboard.PressAsync("End");
        Assert.Equal("passed:6", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        Assert.Equal("Sun", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await page.Keyboard.PressAsync("Home");
        await ScrollSettledAsync(page);
        var recovered = await ScrollFactsAsync(page, Point(0, 0));
        Assert.Equal("passed:0", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        Assert.True(recovered.GetProperty("node").GetProperty("left").GetDouble() >= recovered.GetProperty("viewport").GetProperty("left").GetDouble());
        Assert.False(await TipHiddenAsync(page));
        Assert.Equal("Mon", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        await CaptureAsync(page, "scroll-readable-dark-keyboard-recovered", recovered, session);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    [Trait("BrowserSuite", "Compatibility")]
    public async Task PointerReadoutDismissesWhenReadableViewportClipsItsSource() {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(false).ToInteractiveHtmlPage(options => {
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }), 390, 844);
        var page = session.Page;
        await page.EvaluateAsync("""
            ()=>{
                const root=document.querySelector('.cfx-interactive-chart');
                window.readableScrollTarget=null;window.readableScrollEvents=[];
                root.addEventListener('cfxhover',event=>{
                    if(event.detail.target?.targetKind==='point')window.readableScrollTarget=event.detail.target;
                    readableScrollEvents.push({type:'cfxhover',target:event.detail.target});
                });
                root.addEventListener('pointermove',event=>readableScrollEvents.push({type:'pointermove',x:event.clientX,y:event.clientY}),{passive:true});
            }
            """);
        var pointer = await PointerAsync(page);
        await WaitForTipAsync(page);
        Assert.Equal("Mon", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await page.Locator(".cfx-stage").EvaluateAsync("stage => stage.scrollLeft = stage.scrollWidth");
        await ScrollSettledAsync(page);
        var clipped = await ReadablePointerFactAsync(page);
        Assert.True(clipped.GetProperty("original").GetProperty("right").GetDouble() < clipped.GetProperty("viewport").GetProperty("left").GetDouble());
        // A native pointer event after scrolling may acquire a different visible source. Mon must never remain shown.
        if (!clipped.GetProperty("hidden").GetBoolean()) AssertVisibleThursdayFact(clipped);
        await CaptureAsync(page, "scroll-readable-light-pointer-clipped", clipped, session);
        var priorPointerMoves = clipped.GetProperty("events").EnumerateArray().Count(evt => evt.GetProperty("type").GetString() == "pointermove");
        await page.Mouse.MoveAsync((float)pointer[0] + 2, (float)pointer[1]);
        await page.Mouse.MoveAsync((float)pointer[0], (float)pointer[1]);
        await ScrollSettledAsync(page);
        var reacquired = await ReadablePointerFactAsync(page);
        Assert.True(reacquired.GetProperty("events").EnumerateArray().Count(evt => evt.GetProperty("type").GetString() == "pointermove") > priorPointerMoves);
        Assert.False(reacquired.GetProperty("hidden").GetBoolean());
        AssertVisibleThursdayFact(reacquired);
        await CaptureAsync(page, "scroll-readable-light-pointer-reacquired", new { pointer, clipped, reacquired }, session);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task ClosedShadowScrollDismissesPinAndReleasesTreeListeners() {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 390, 844);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(StatusLines(true).ToInteractiveHtmlPage());
        await MountPositionFocusChartAsync(page, true);
        await page.EvaluateAsync("() => window.positionFocusChart.getRootNode().host.style.width = '362px'");
        Assert.True(await page.EvaluateAsync<bool>("() => { const stage = window.positionFocusChart.querySelector('.cfx-stage'); return stage.scrollWidth > stage.clientWidth; }"));
        await page.EvaluateAsync("""
            () => {
                const root = window.positionFocusChart;
                window.shadowClipTrees = [];
                for (let tree = root.getRootNode(); tree.host; tree = tree.host.getRootNode()) {
                    const listeners = new Set(), add = tree.addEventListener.bind(tree), remove = tree.removeEventListener.bind(tree);
                    tree.addEventListener = (name, listener, options) => { if (name === 'scroll') listeners.add(listener); add(name, listener, options); };
                    tree.removeEventListener = (name, listener, options) => { if (name === 'scroll') listeners.delete(listener); remove(name, listener, options); };
                    window.shadowClipTrees.push(listeners);
                }
                window.shadowClipEvents = {native:0,document:0};
                root.getRootNode().addEventListener('scroll', () => window.shadowClipEvents.native++, true);
                document.addEventListener('scroll', () => window.shadowClipEvents.document++, true);
            }
            """);
        var baseline = await ListenerStateAsync(page);
        var shadowBaseline = await page.EvaluateAsync<int[]>("() => shadowClipTrees.map(set => set.size)");
        await page.EvaluateAsync("selector => window.positionFocusChart.querySelector(selector).focus()", Point(0, 0));
        await page.Keyboard.PressAsync("Space"); await SettleObservedPositionAsync(page);
        Assert.False((await GeometryAsync(page, Point(0, 0), true)).GetProperty("hidden").GetBoolean());
        var shown = await GeometryAsync(page, Point(0, 0), true);
        // Keep the pointer outside the chart: scrolling may otherwise acquire a different visible source.
        await page.Mouse.MoveAsync(2, 2);
        Assert.True(await page.EvaluateAsync<bool>("() => !window.positionFocusChart.contains(document.elementFromPoint(2, 2))"));
        await CaptureAsync(page, "scroll-closed-shadow-pinned-before", shown, session);
        await page.EvaluateAsync("() => { const stage = window.positionFocusChart.querySelector('.cfx-stage'); stage.scrollLeft = stage.scrollWidth; }");
        await SettleObservedPositionAsync(page);
        var clipped = await GeometryAsync(page, Point(0, 0), true);
        var events = await page.EvaluateAsync<JsonElement>("() => shadowClipEvents");
        var released = await ListenerStateAsync(page);
        var shadowReleased = await page.EvaluateAsync<int[]>("() => shadowClipTrees.map(set => set.size)");
        await CaptureAsync(page, "scroll-closed-shadow-clipped", new { clipped, events, baseline, released, shadowBaseline, shadowReleased }, session);
        Assert.True(events.GetProperty("native").GetInt32() > 0);
        Assert.Equal(0, events.GetProperty("document").GetInt32());
        Assert.True(clipped.GetProperty("hidden").GetBoolean(), "A native scroll inside a closed shadow tree must dismiss its clipped source readout.");
        Assert.False(clipped.GetProperty("pinned").GetBoolean());
        AssertPositionWatchersReleased(baseline, released);
        Assert.Equal(shadowBaseline, shadowReleased);
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task PairedNativeExtentDismissesEvenWhileTransparentHitAreaStillOverlaps() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTheme(VisualTheme.Graphite()).WithHeader(false).WithLegend(false)
            .WithDataLabels(false).WithXAxisBounds(0, 2).WithYAxisBounds(0, 10).AddDifferenceArea("Envelope", new[] {
                new ChartDifferencePoint(0, 8, 8.01), new ChartDifferencePoint(1, 8, 8.01), new ChartDifferencePoint(2, 8, 8.01)
            });
        chart.Series[0].WithColor(ChartColor.Transparent).ConfigureMarkers(markers => markers.Enabled = false)
            .ConfigureDifferenceArea(options => { options.NegativeFill = ChartColor.FromHex("#3578D4"); options.NegativeFillOpacity = .8; });
        var html = "<html><body style='margin:0'><div id='scroll-host' style='width:650px;height:160px;overflow:auto;border:3px solid #777'><div style='padding-bottom:500px'>"
            + chart.ToInteractiveHtmlFragment(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit) + "</div></div></body></html>";
        await using var session = await OpenAsync(html, 950, 760);
        var page = session.Page;
        var firstSource = await page.Locator(Point(0, 1)).GetAttributeAsync("data-cfx-first");
        var secondSource = await page.Locator(Point(0, 1)).GetAttributeAsync("data-cfx-second");
        Assert.Equal(8, double.Parse(firstSource!, CultureInfo.InvariantCulture));
        Assert.Equal(8.01, double.Parse(secondSource!, CultureInfo.InvariantCulture));
        await page.Locator(Point(0, 1)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.False(await TipHiddenAsync(page));
        await page.Locator(Point(0, 1)).EvaluateAsync("""
            node => {
                const host = document.getElementById('scroll-host'), r = node._cfxPairedRegion;
                const bottom = new DOMPoint(r.x, r.y + r.height).matrixTransform(node.getScreenCTM()).y;
                host.scrollTop += bottom - host.getBoundingClientRect().top - host.clientTop + 1;
            }
            """);
        await ScrollSettledAsync(page);
        var clipped = await ScrollFactsAsync(page, Point(0, 1), "#scroll-host");
        Assert.True(clipped.GetProperty("node").GetProperty("bottom").GetDouble() > clipped.GetProperty("viewport").GetProperty("top").GetDouble());
        Assert.True(clipped.GetProperty("native").GetProperty("bottom").GetDouble() < clipped.GetProperty("viewport").GetProperty("top").GetDouble());
        await CaptureAsync(page, "scroll-paired-native-extent-clipped", clipped, session);
        Assert.True(await TipHiddenAsync(page), "An enlarged transparent hit rectangle cannot keep a fully clipped native source extent available.");
        Assert.Equal(firstSource, await page.Locator(Point(0, 1)).GetAttributeAsync("data-cfx-first"));
        Assert.Equal(secondSource, await page.Locator(Point(0, 1)).GetAttributeAsync("data-cfx-second"));
        Assert.Equal("true", await page.Locator(Point(0, 1)).GetAttributeAsync("aria-selected"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData("x", false)]
    [InlineData("y", true)]
    public async Task NestedHostClipKeepsPartialSourceThenDismissesFullyClippedPin(string axis, bool dark) {
        if (!Enabled) return;
        var fragment = StatusLines(dark).ToInteractiveHtmlFragment(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Chart;
        });
        var style = axis == "x" ? "width:360px;height:420px;overflow-x:auto;overflow-y:hidden" : "width:660px;height:200px;overflow-x:hidden;overflow-y:auto";
        var childStyle = axis == "x" ? "width:650px" : "width:650px;padding-bottom:500px";
        var html = "<html><body style='margin:0'><div id='scroll-host' style='border:3px solid #777;" + style + "'><div style='" + childStyle + "'>" + fragment + "</div></div></body></html>";
        await using var session = await OpenAsync(html, 950, 760);
        var page = session.Page;
        await page.Locator(Point(0, 0)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        await page.Locator(Point(0, 0)).EvaluateAsync("""
            (node, axis) => {
                const host = document.getElementById('scroll-host'), b = node.getBoundingClientRect(), h = host.getBoundingClientRect();
                if (axis === 'x') host.scrollLeft += b.left + b.width / 2 - (h.left + host.clientLeft);
                else host.scrollTop += b.top + b.height / 2 - (h.top + host.clientTop);
            }
            """, axis);
        await ScrollSettledAsync(page);
        var partial = await ScrollFactsAsync(page, Point(0, 0), "#scroll-host");
        Assert.False(await TipHiddenAsync(page), "A source that still overlaps the native clip retains its readout.");
        await CaptureAsync(page, "scroll-nested-" + axis + "-partial", partial, session);
        await page.Locator("#scroll-host").EvaluateAsync("(host, axis) => { if (axis === 'x') host.scrollLeft += 25; else host.scrollTop += 25; }", axis);
        await ScrollSettledAsync(page);
        var clipped = await ScrollFactsAsync(page, Point(0, 0), "#scroll-host");
        var dimension = axis == "x" ? "right" : "bottom"; var edge = axis == "x" ? "left" : "top";
        Assert.True(clipped.GetProperty("node").GetProperty(dimension).GetDouble() < clipped.GetProperty("viewport").GetProperty(edge).GetDouble());
        await CaptureAsync(page, "scroll-nested-" + axis + "-clipped", clipped, session);
        Assert.True(await TipHiddenAsync(page));
        Assert.NotEqual("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> ReadablePointerFactAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        ()=>{
            const root=document.querySelector('.cfx-interactive-chart'),tip=root.querySelector('.cfx-tooltip'),stage=root.querySelector('.cfx-stage');
            const target=window.readableScrollTarget;
            const node=target&&root.querySelector('[data-cfx-target-id="'+CSS.escape(target.targetId)+'"]');
            const rect=node=>node.getBoundingClientRect().toJSON(),clip=stage.getBoundingClientRect();
            const viewport={left:clip.left+stage.clientLeft,right:clip.left+stage.clientLeft+stage.clientWidth,
                top:clip.top+stage.clientTop,bottom:clip.top+stage.clientTop+stage.clientHeight};
            const box=node&&rect(node),style=node&&getComputedStyle(node);
            return{hidden:tip.hidden,title:tip.querySelector('.cfx-tooltip__title')?.textContent,text:tip.innerText,
                original:rect(root.querySelector('g[data-cfx-role=point][data-cfx-series="0"][data-cfx-point="0"]')),viewport,target,
                source:node&&{id:node.dataset.cfxTargetId,label:node.dataset.cfxXLabel,value:Number(node.dataset.cfxY),formatted:node.dataset.cfxFormattedValue,box,
                    painted:box.right>viewport.left&&box.left<viewport.right&&box.bottom>viewport.top&&box.top<viewport.bottom
                        &&style.display!=='none'&&style.visibility!=='hidden'&&Number(style.opacity)>0},events:readableScrollEvents};
        }
        """);

    private static void AssertVisibleThursdayFact(JsonElement state) {
        var source = state.GetProperty("source");
        Assert.Equal("passed:3", state.GetProperty("target").GetProperty("targetId").GetString());
        Assert.Equal("passed:3", source.GetProperty("id").GetString());
        Assert.True(source.GetProperty("painted").GetBoolean());
        Assert.Equal(1041d, state.GetProperty("target").GetProperty("numericValue").GetDouble());
        Assert.Equal(1041d, source.GetProperty("value").GetDouble());
        Assert.Equal("Thu", source.GetProperty("label").GetString());
        Assert.Equal("Thu", state.GetProperty("title").GetString());
        Assert.Contains(source.GetProperty("formatted").GetString()!, state.GetProperty("text").GetString()!, StringComparison.Ordinal);
    }

    private static Task ScrollSettledAsync(IPage page) => page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");

    private static Task<JsonElement> ScrollFactsAsync(IPage page, string selector, string viewport = ".cfx-stage") => page.EvaluateAsync<JsonElement>("""
        ({selector, viewport}) => {
            const node = document.querySelector(selector), root = node.closest('.cfx-interactive-chart'), tip = root.querySelector('.cfx-tooltip');
            const host = document.querySelector(viewport), b = host.getBoundingClientRect(), style = getComputedStyle(host);
            const rect = n => { const r = n.getBoundingClientRect(); return {left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width,height:r.height}; };
            const r = node._cfxNativeRegion, m = r && node.getScreenCTM(), corners = m && [[r.x,r.y],[r.x+r.width,r.y],[r.x,r.y+r.height],[r.x+r.width,r.y+r.height]].map(([x,y]) => new DOMPoint(x,y).matrixTransform(m));
            const native = corners && {left:Math.min(...corners.map(p=>p.x)),right:Math.max(...corners.map(p=>p.x)),top:Math.min(...corners.map(p=>p.y)),bottom:Math.max(...corners.map(p=>p.y))};
            return {node:rect(node), native, viewport:{left:b.left+host.clientLeft,top:b.top+host.clientTop,right:b.left+host.clientLeft+host.clientWidth,bottom:b.top+host.clientTop+host.clientHeight},
                scroll:{left:host.scrollLeft,top:host.scrollTop,width:host.scrollWidth,height:host.scrollHeight,overflowX:style.overflowX,overflowY:style.overflowY},
                data:{...node.dataset}, tooltip:{hidden:tip.hidden,pinned:root.dataset.cfxTooltipPinned==='true',text:tip.innerText}, focused:document.activeElement?.dataset.cfxTargetId};
        }
        """, new { selector, viewport });
}
