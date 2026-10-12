using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(false, "hidden")]
    [InlineData(true, "auto")]
    public async Task DisplayContentsOverflowDoesNotClipVisibleNativeReadout(bool shadow, string overflow) {
        if (!Enabled) return;
        await using var session = await OpenAsync(StatusLines(shadow).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }), 950, 760);
        var page = session.Page;
        await MountPositionFocusChartAsync(page, shadow);
        await page.EvaluateAsync("""
            overflow => {
                const root = window.positionFocusChart, wrapper = document.createElement('div');
                wrapper.id = 'contents-overflow'; wrapper.style.cssText = 'display:contents;overflow:' + overflow;
                root.before(wrapper); wrapper.append(root);
                window.contentsOverflowWrapper = wrapper;
            }
            """, overflow);
        await page.EvaluateAsync("selector => window.positionFocusChart.querySelector(selector).focus({preventScroll:true})", Point(0, 0));
        await page.Keyboard.PressAsync("Space"); await SettleObservedPositionAsync(page);
        var visible = await ScrollAncestorFactsAsync(page, Point(0, 0));
        await CaptureAsync(page, $"scroll-contents-{shadow}-{overflow}-visible", visible, session);
        Assert.Equal(0, await page.EvaluateAsync<int>("() => contentsOverflowWrapper.getClientRects().length"));
        Assert.True(visible.GetProperty("paintedHit").GetBoolean());
        Assert.False(visible.GetProperty("hidden").GetBoolean(), "Overflow on a display:contents ancestor has no clipping box.");
        Assert.True(visible.GetProperty("pinned").GetBoolean());

        // A real zero-width block must still clip: absence of a box differs from a zero-sized box.
        await page.EvaluateAsync("""
            () => {
                const root = window.positionFocusChart, clip = window.contentsOverflowWrapper;
                clip.style.cssText = 'display:block;width:0;height:400px;overflow:hidden';
                root.style.width = '650px'; root.style.maxWidth = 'none';
                window.scrollAncestorClips = [clip];
            }
            """);
        await SettleObservedPositionAsync(page);
        var clipped = await ScrollAncestorFactsAsync(page, Point(0, 0));
        await CaptureAsync(page, $"scroll-contents-{shadow}-{overflow}-real-zero-clip", clipped, session);
        Assert.True(clipped.GetProperty("hidden").GetBoolean());
        Assert.False(clipped.GetProperty("pinned").GetBoolean());
        Assert.Equal(1, await page.EvaluateAsync<int>("() => scrollAncestorClips[0].getClientRects().length"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("x", false, 1)]
    [InlineData("y", false, .8)]
    [InlineData("x", true, .8)]
    [InlineData("y", true, 1)]
    public async Task KeyboardNavigationRevealsNativeSourceThroughNestedOuterScrollAncestors(string axis, bool shadow, double scale) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(596, 338).WithTheme(shadow ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Keyboard access through host scrollers").WithLegend(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Reading", new[] { new ChartPoint(1, 9), new ChartPoint(5, 5), new ChartPoint(9, 1) });
        chart.Series[0].WithInteractionKey("scroll-reading");
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }), 950, 760);
        var page = session.Page;
        await MountPositionFocusChartAsync(page, shadow);
        await page.EvaluateAsync("""
            ({axis,scale,shadow}) => {
                const root = window.positionFocusChart, outer = document.createElement('div'), inner = document.createElement('div');
                const holder = shadow ? root.getRootNode().host.getRootNode().host : root;
                outer.id = 'outer-scroll-clip'; inner.id = 'inner-scroll-clip';
                outer.style.cssText = 'margin:20px;border:7px solid #777;transform-origin:top left;transform:scale(' + scale + ',' + (scale === 1 ? 1 : .9) + ');' +
                    (axis === 'x' ? 'width:280px;height:420px;overflow-x:auto;overflow-y:hidden' : 'width:720px;height:180px;overflow-y:auto;overflow-x:hidden');
                inner.style.cssText = 'border:5px solid #777;' +
                    (axis === 'x' ? 'width:360px;height:390px;overflow-x:auto;overflow-y:hidden' : 'width:680px;height:230px;overflow-y:auto;overflow-x:hidden');
                holder.before(outer); outer.append(holder); root.before(inner); inner.append(root);
                root.style.width = '650px'; root.style.maxWidth = 'none';
                window.scrollAncestorClips = [inner, outer]; window.scrollAncestorNavigation = [];
                root.addEventListener('cfxnavigate', event => scrollAncestorNavigation.push(event.detail));
            }
            """, new { axis, scale, shadow });
        await SettleObservedPositionAsync(page);
        await page.EvaluateAsync("selector => window.positionFocusChart.querySelector(selector).focus({preventScroll:true})", Point(0, 0));
        await page.Keyboard.PressAsync("Space"); await SettleObservedPositionAsync(page);
        var initial = await ScrollAncestorFactsAsync(page, Point(0, 0));
        Assert.False(initial.GetProperty("hidden").GetBoolean());
        Assert.True(initial.GetProperty("pinned").GetBoolean());
        await page.EvaluateAsync("axis => { for (const clip of scrollAncestorClips) { if (axis === 'x') clip.scrollLeft = clip.scrollWidth; else clip.scrollTop = clip.scrollHeight; } }", axis);
        await SettleObservedPositionAsync(page);
        var clipped = await ScrollAncestorFactsAsync(page, Point(0, 0));
        await CaptureAsync(page, $"scroll-keyboard-outer-{axis}-{shadow}-{scale}-clipped", new { initial, clipped }, session);
        Assert.True(clipped.GetProperty("hidden").GetBoolean());
        Assert.False(clipped.GetProperty("pinned").GetBoolean());
        Assert.Equal("scroll-reading:0", clipped.GetProperty("focusedId").GetString());

        // Home/End and arrows retain focus semantics while revealing the source through every native clip.
        await page.Keyboard.PressAsync("Home"); // Already at the first item: no navigation and no incidental browser scroll.
        await page.Keyboard.PressAsync("End"); await SettleObservedPositionAsync(page);
        var end = await ScrollAncestorFactsAsync(page, Point(0, 2));
        await CaptureAsync(page, $"scroll-keyboard-outer-{axis}-{shadow}-{scale}-end", end, session);
        AssertScrollAncestorSourceVisible(end, "scroll-reading:2");
        await page.Keyboard.PressAsync("Home"); await SettleObservedPositionAsync(page);
        var home = await ScrollAncestorFactsAsync(page, Point(0, 0));
        await CaptureAsync(page, $"scroll-keyboard-outer-{axis}-{shadow}-{scale}-home", home, session);
        AssertScrollAncestorSourceVisible(home, "scroll-reading:0");
        await page.Keyboard.PressAsync("ArrowRight"); await SettleObservedPositionAsync(page);
        var adjacent = await ScrollAncestorFactsAsync(page, Point(0, 1));
        await CaptureAsync(page, $"scroll-keyboard-outer-{axis}-{shadow}-{scale}-adjacent", adjacent, session);
        AssertScrollAncestorSourceVisible(adjacent, "scroll-reading:1");
        Assert.Equal(new[] { "End", "Home", "ArrowRight" }, (await page.EvaluateAsync<JsonElement>("() => scrollAncestorNavigation"))
            .EnumerateArray().Select(item => item.GetProperty("key").GetString()));
        Assert.Equal("true", await page.EvaluateAsync<string>("selector => window.positionFocusChart.querySelector(selector).getAttribute('aria-selected')", Point(0, 0)));
        AssertNoConsoleErrors(session);
    }

    private static void AssertScrollAncestorSourceVisible(JsonElement state, string targetId) {
        Assert.Equal(targetId, state.GetProperty("focusedId").GetString());
        Assert.False(state.GetProperty("hidden").GetBoolean(), "Keyboard navigation must reveal a target clipped by an outer overflow scroller.");
        Assert.True(state.GetProperty("paintedHit").GetBoolean(), state.ToString());
        var source = state.GetProperty("source");
        foreach (var clip in state.GetProperty("clips").EnumerateArray()) {
            Assert.True(source.GetProperty("left").GetDouble() >= clip.GetProperty("left").GetDouble() - 1, state.ToString());
            Assert.True(source.GetProperty("right").GetDouble() <= clip.GetProperty("right").GetDouble() + 1, state.ToString());
            Assert.True(source.GetProperty("top").GetDouble() >= clip.GetProperty("top").GetDouble() - 1, state.ToString());
            Assert.True(source.GetProperty("bottom").GetDouble() <= clip.GetProperty("bottom").GetDouble() + 1, state.ToString());
        }
    }

    private static Task<JsonElement> ScrollAncestorFactsAsync(IPage page, string selector) => page.EvaluateAsync<JsonElement>("""
        selector => {
            const root = window.positionFocusChart, node = root.querySelector(selector), tip = root.querySelector('.cfx-tooltip');
            const b = node.getBoundingClientRect(), hit = root.getRootNode().elementFromPoint((b.left+b.right)/2,(b.top+b.bottom)/2);
            const rect = n => { const r = n.getBoundingClientRect(); return {left:r.left,right:r.right,top:r.top,bottom:r.bottom}; };
            return {source:rect(node),data:{...node.dataset},focusedId:root.getRootNode().activeElement?.dataset.cfxTargetId,
                paintedHit:hit !== null && node.contains(hit),hidden:tip.hidden,pinned:root.dataset.cfxTooltipPinned==='true',text:tip.innerText,
                clips:(window.scrollAncestorClips || []).map(clip => {
                    const r = clip.getBoundingClientRect(), sx = clip.offsetWidth ? r.width/clip.offsetWidth : 1;
                    const sy = clip.offsetHeight ? r.height/clip.offsetHeight : 1;
                    return {id:clip.id,left:r.left+clip.clientLeft*sx,right:r.left+(clip.clientLeft+clip.clientWidth)*sx,
                        top:r.top+clip.clientTop*sy,bottom:r.top+(clip.clientTop+clip.clientHeight)*sy,
                        scrollLeft:clip.scrollLeft,scrollTop:clip.scrollTop,clientWidth:clip.clientWidth,clientHeight:clip.clientHeight,
                        scrollWidth:clip.scrollWidth,scrollHeight:clip.scrollHeight};
                })};
        }
        """, selector);
}
