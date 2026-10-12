using System.Text.Json;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Fact]
    public async Task ClosedShadowNativeViewportTransitionDismissesFullyClippedPin() {
        if (!Enabled) return;
        var html = StatusLines(true).ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.StateBookmarks;
        });
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
            await File.WriteAllTextAsync(Path.Combine(directory, "transition-fixture.html"), html);
        }
        await using var session = await OpenAsync("<html><body></body></html>", 390, 844);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(html);
        await MountPositionFocusChartAsync(page, true);
        await page.EvaluateAsync("""
            selector => {
                const root = window.positionFocusChart, node = root.querySelector(selector);
                const svg = root.querySelector('.cfx-stage svg'), stage = root.querySelector('.cfx-stage');
                svg.style.transitionDuration = '4s';
                window.transitionSource = node;
                window.transitionEvents = {native:0, document:0, run:0};
                window.transitionTrees = [];
                for (let tree = root.getRootNode(); tree.host; tree = tree.host.getRootNode()) {
                    const names = {scroll:new Set(),transitionend:new Set()}, add=tree.addEventListener.bind(tree), remove=tree.removeEventListener.bind(tree);
                    tree.addEventListener = (name, listener, options) => { names[name]?.add(listener); add(name, listener, options); };
                    tree.removeEventListener = (name, listener, options) => { names[name]?.delete(listener); remove(name, listener, options); };
                    window.transitionTrees.push(names);
                }
                root.getRootNode().addEventListener('transitionrun', event => {
                    if (event.target === svg && event.propertyName === 'transform') window.transitionEvents.run++;
                }, true);
                root.getRootNode().addEventListener('transitionend', event => {
                    if (event.target === svg && event.propertyName === 'transform') window.transitionEvents.native++;
                }, true);
                document.addEventListener('transitionend', event => {
                    if (event.target === svg && event.propertyName === 'transform') window.transitionEvents.document++;
                }, true);
                window.transitionFacts = () => {
                    const b = node.getBoundingClientRect(), clip = stage.getBoundingClientRect(), tip = root.querySelector('.cfx-tooltip');
                    return {source:{left:b.left,right:b.right,top:b.top,bottom:b.bottom,width:b.width,height:b.height},
                        clip:{left:clip.left+stage.clientLeft,right:clip.left+stage.clientLeft+stage.clientWidth,
                            top:clip.top+stage.clientTop,bottom:clip.top+stage.clientTop+stage.clientHeight},
                        hidden:tip.hidden,pinned:root.dataset.cfxTooltipPinned==='true',text:tip.innerText,
                        data:{...node.dataset},features:root.dataset.cfxInteractionFeatures,
                        transform:getComputedStyle(svg).transform,transition:getComputedStyle(svg).transition,
                        pan:root.dataset.cfxPanX,events:{...window.transitionEvents},time:performance.now()};
                };
                window.transitionTreeCounts = () => window.transitionTrees.map(names => ({scroll:names.scroll.size,transitionend:names.transitionend.size}));
            }
            """, Point(0, 0));
        await ScrollSettledAsync(page);
        var baseline = await ListenerStateAsync(page);
        var treeBaseline = await page.EvaluateAsync<JsonElement>("() => transitionTreeCounts()");
        var point = await page.EvaluateAsync<JsonElement>("() => transitionFacts().source");
        await page.Mouse.MoveAsync((float)((point.GetProperty("left").GetDouble() + point.GetProperty("right").GetDouble()) / 2),
            (float)((point.GetProperty("top").GetDouble() + point.GetProperty("bottom").GetDouble()) / 2));
        await page.Mouse.ClickAsync((float)((point.GetProperty("left").GetDouble() + point.GetProperty("right").GetDouble()) / 2),
            (float)((point.GetProperty("top").GetDouble() + point.GetProperty("bottom").GetDouble()) / 2));
        await SettleObservedPositionAsync(page);
        var initial = await page.EvaluateAsync<JsonElement>("() => transitionFacts()");
        var treeActive = await page.EvaluateAsync<JsonElement>("() => transitionTreeCounts()");
        Assert.Equal(2, treeActive.GetArrayLength());
        for (var index = 0; index < treeActive.GetArrayLength(); index++) {
            Assert.Equal(treeBaseline[index].GetProperty("scroll").GetInt32() + 1, treeActive[index].GetProperty("scroll").GetInt32());
            Assert.Equal(treeBaseline[index].GetProperty("transitionend").GetInt32() + 1, treeActive[index].GetProperty("transitionend").GetInt32());
        }
        Assert.False(initial.GetProperty("hidden").GetBoolean());
        Assert.True(initial.GetProperty("pinned").GetBoolean());
        Assert.Contains("Tooltips", initial.GetProperty("features").GetString());
        Assert.Contains("StateBookmarks", initial.GetProperty("features").GetString());
        Assert.DoesNotContain("KeyboardNavigation", initial.GetProperty("features").GetString());
        Assert.DoesNotContain("Crosshair", initial.GetProperty("features").GetString());
        Assert.True(initial.GetProperty("source").GetProperty("left").GetDouble() > initial.GetProperty("clip").GetProperty("left").GetDouble());
        await CaptureAsync(page, "transition-closed-shadow-initial", new { initial, baseline, treeBaseline, treeActive }, session);
        await page.EvaluateAsync("""
            () => {
                const root = window.positionFocusChart, b = transitionSource.getBoundingClientRect(), clip = root.querySelector('.cfx-stage').getBoundingClientRect();
                // A real built-in transform transition ends just beyond the clip; extending only its duration makes the partial interval observable.
                root.dispatchEvent(new CustomEvent('cfx-apply-state', {detail:{snapshot:{viewport:{zoom:1,panX:clip.left-b.right-3,panY:0}}}}));
            }
            """);
        await ScrollSettledAsync(page);
        var early = await page.EvaluateAsync<JsonElement>("() => transitionFacts()");
        Assert.True(early.GetProperty("source").GetProperty("right").GetDouble() > early.GetProperty("clip").GetProperty("left").GetDouble());
        Assert.False(early.GetProperty("hidden").GetBoolean());
        await CaptureAsync(page, "transition-closed-shadow-early", early, session);
        await page.WaitForFunctionAsync("() => { const f=transitionFacts(); return f.source.left<f.clip.left && f.source.right>f.clip.left; }");
        var partial = await page.EvaluateAsync<JsonElement>("() => transitionFacts()");
        Assert.False(partial.GetProperty("hidden").GetBoolean());
        await CaptureAsync(page, "transition-closed-shadow-partial", partial, session);
        await page.WaitForFunctionAsync("() => transitionEvents.native > 0");
        await ScrollSettledAsync(page);
        var final = await page.EvaluateAsync<JsonElement>("() => transitionFacts()");
        var watchers = await ListenerStateAsync(page);
        var treeFinal = await page.EvaluateAsync<JsonElement>("() => transitionTreeCounts()");
        await CaptureAsync(page, "transition-closed-shadow-final", new { initial, early, partial, final, baseline, watchers, treeBaseline, treeActive, treeFinal }, session);
        Assert.True(final.GetProperty("source").GetProperty("right").GetDouble() < final.GetProperty("clip").GetProperty("left").GetDouble());
        Assert.Equal(1, final.GetProperty("events").GetProperty("native").GetInt32());
        Assert.Equal(0, final.GetProperty("events").GetProperty("document").GetInt32());
        Assert.Equal(initial.GetProperty("data").GetRawText(), final.GetProperty("data").GetRawText());
        AssertNoConsoleErrors(session);
        Assert.True(final.GetProperty("hidden").GetBoolean(), "A native transform transition inside a closed shadow tree must dismiss a fully clipped pinned source readout.");
        Assert.False(final.GetProperty("pinned").GetBoolean());
        AssertPositionWatchersReleased(baseline, watchers);
        Assert.Equal(treeBaseline.GetRawText(), treeFinal.GetRawText());
    }
}
