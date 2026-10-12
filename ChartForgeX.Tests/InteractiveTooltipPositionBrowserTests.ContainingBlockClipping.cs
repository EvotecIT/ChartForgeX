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
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TooltipCardFitsApplicableFixedContainingBlockClips(bool shadow, bool clipAboveContainingBlock) {
        if (!Enabled) return;
        await using var session = await OpenAsync(PlacementClipChart(shadow), 950, 760);
        var page = session.Page;
        await MountPlacementClipsAsync(page, shadow, clipAboveContainingBlock, false, true);
        await page.EvaluateAsync("selector => positionFocusChart.querySelector(selector).focus({preventScroll:true})", Point(0, 0));
        await page.Keyboard.PressAsync("Space"); await SettleObservedPositionAsync(page);
        var shown = await PlacementClipFactsAsync(page);
        await CaptureAsync(page, $"card-clipping-{shadow}-{clipAboveContainingBlock}-shown", shown, session);
        AssertPlacementCardInsideApplicableClip(shown);

        // Resizing the host must recompute the available area while the readout remains pinned.
        await page.EvaluateAsync("() => placementOuter.style.height = '135px'");
        await SettleObservedPositionAsync(page);
        var resized = await PlacementClipFactsAsync(page);
        await CaptureAsync(page, $"card-clipping-{shadow}-{clipAboveContainingBlock}-resized", resized, session);
        AssertPlacementCardInsideApplicableClip(resized);
        Assert.True(resized.GetProperty("card").GetProperty("top").GetDouble() < shown.GetProperty("card").GetProperty("top").GetDouble() - 5);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FixedTooltipCardEscapesOverflowBelowItsContainingBlock(bool transformed) {
        if (!Enabled) return;
        await using var session = await OpenAsync(PlacementClipChart(false), 950, 760);
        var page = session.Page;
        await MountPlacementClipsAsync(page, false, false, true, transformed);
        await page.EvaluateAsync("selector => positionFocusChart.querySelector(selector).focus({preventScroll:true})", Point(0, 0));
        await page.Keyboard.PressAsync("Space"); await SettleObservedPositionAsync(page);
        var shown = await PlacementClipFactsAsync(page);
        await CaptureAsync(page, $"card-escape-{transformed}-shown", shown, session);
        Assert.False(shown.GetProperty("hidden").GetBoolean());
        Assert.True(shown.GetProperty("pinned").GetBoolean());
        Assert.True(shown.GetProperty("sourcePaintedHit").GetBoolean(), shown.ToString());
        Assert.True(shown.GetProperty("cardPaintedHits").EnumerateArray().All(hit => hit.GetBoolean()), shown.ToString());
        Assert.True(shown.GetProperty("card").GetProperty("bottom").GetDouble() > shown.GetProperty("innerClip").GetProperty("bottom").GetDouble() + 5,
            "A fixed tooltip escapes an overflow wrapper below its actual containing block.");
        if (transformed) AssertPlacementCardInsideApplicableClip(shown);
        AssertNoConsoleErrors(session);
    }

    private static string PlacementClipChart(bool dark) {
        var chart = Chart.Create().WithSize(596, 338).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Readout inside a clipped host").WithLegend(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Reading", new[] { new ChartPoint(1, 9), new ChartPoint(5, 5), new ChartPoint(9, 1) });
        chart.Series[0].WithInteractionKey("placement-reading");
        return chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        });
    }

    private static async Task MountPlacementClipsAsync(IPage page, bool shadow, bool clipAbove, bool escape, bool transformed) {
        await MountPositionFocusChartAsync(page, shadow, "placement-host");
        await page.EvaluateAsync("""
            ({shadow,clipAbove,escape,transformed}) => {
                const root = positionFocusChart, outer = window.placementOuter = document.createElement('div');
                const inner = window.placementInner = document.createElement('div');
                const holder = shadow ? root.getRootNode().host.getRootNode().host : root;
                outer.id = 'placement-outer'; inner.id = 'placement-inner';
                outer.style.cssText = 'margin:20px;border:7px solid #777;width:720px;height:' + (escape ? 300 : 180) + 'px;overflow:hidden;transform-origin:top left';
                inner.style.cssText = 'border:5px solid #777;width:680px;height:' + (escape ? 150 : 230) + 'px;overflow:' + (clipAbove ? 'visible' : 'hidden') + ';transform-origin:top left';
                if (transformed) (clipAbove ? inner : outer).style.transform = 'scale(.8,.9)';
                holder.before(outer); outer.append(holder); root.before(inner); inner.append(root);
                root.style.width = '650px'; root.style.maxWidth = 'none';
            }
            """, new { shadow, clipAbove, escape, transformed });
        await SettleObservedPositionAsync(page);
    }

    private static void AssertPlacementCardInsideApplicableClip(JsonElement state) {
        Assert.False(state.GetProperty("hidden").GetBoolean());
        Assert.True(state.GetProperty("pinned").GetBoolean());
        Assert.True(state.GetProperty("sourcePaintedHit").GetBoolean(), state.ToString());
        var card = state.GetProperty("card"); var clip = state.GetProperty("outerClip");
        Assert.True(card.GetProperty("width").GetDouble() < clip.GetProperty("right").GetDouble() - clip.GetProperty("left").GetDouble() - 16);
        Assert.True(card.GetProperty("height").GetDouble() < clip.GetProperty("bottom").GetDouble() - clip.GetProperty("top").GetDouble() - 16);
        Assert.True(card.GetProperty("left").GetDouble() >= clip.GetProperty("left").GetDouble() - .2, state.ToString());
        Assert.True(card.GetProperty("right").GetDouble() <= clip.GetProperty("right").GetDouble() + .2, state.ToString());
        Assert.True(card.GetProperty("top").GetDouble() >= clip.GetProperty("top").GetDouble() - .2, state.ToString());
        Assert.True(card.GetProperty("bottom").GetDouble() <= clip.GetProperty("bottom").GetDouble() + .2,
            "The complete tooltip card must fit its real clipping area when there is enough room. " + state);
        Assert.True(state.GetProperty("cardPaintedHits").EnumerateArray().All(hit => hit.GetBoolean()), state.ToString());
    }

    private static Task<JsonElement> PlacementClipFactsAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root = positionFocusChart, node = root.querySelector('g[data-cfx-role="point"][data-cfx-point="0"]');
            const tip = root.querySelector('.cfx-tooltip'), tree = root.getRootNode();
            const rect = node => { const r = node.getBoundingClientRect(); return {left:r.left,right:r.right,top:r.top,bottom:r.bottom,width:r.width,height:r.height}; };
            const clip = node => { const r = rect(node), sx = r.width/node.offsetWidth, sy = r.height/node.offsetHeight;
                return {left:r.left+node.clientLeft*sx,right:r.left+(node.clientLeft+node.clientWidth)*sx,
                    top:r.top+node.clientTop*sy,bottom:r.top+(node.clientTop+node.clientHeight)*sy}; };
            const source = rect(node), card = rect(tip), sourceHit = tree.elementFromPoint((source.left+source.right)/2,(source.top+source.bottom)/2);
            const originalPointerEvents = tip.style.pointerEvents;
            tip.style.pointerEvents = 'auto';
            const cardPaintedHits = [[card.left+8,card.top+8],[card.right-8,card.top+8],[card.left+8,card.bottom-8],[card.right-8,card.bottom-8]]
                .map(([x,y]) => tip.contains(tree.elementFromPoint(x,y)));
            tip.style.pointerEvents = originalPointerEvents;
            const ancestors = [];
            for (let ancestor = tip.parentElement; ancestor; ancestor = ancestor.parentElement || ancestor.getRootNode().host) {
                const style = getComputedStyle(ancestor);
                ancestors.push({id:ancestor.id,tag:ancestor.tagName,transform:style.transform,perspective:style.perspective,
                    filter:style.filter,contain:style.contain,willChange:style.willChange,contentVisibility:style.contentVisibility,
                    overflow:style.overflow,offsetParent:ancestor.offsetParent?.id});
            }
            return {source,card,outerClip:clip(placementOuter),innerClip:clip(placementInner),cardPaintedHits,
                hidden:tip.hidden,pinned:root.dataset.cfxTooltipPinned==='true',text:tip.innerText,
                sourcePaintedHit:node.contains(sourceHit),offsetParent:tip.offsetParent?.id,ancestors};
        }
        """);
}
