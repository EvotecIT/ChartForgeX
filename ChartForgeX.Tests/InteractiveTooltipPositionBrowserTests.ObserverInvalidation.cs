using System.Text.Json;
using ChartForgeX.Interactivity.Html;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, false)]
    [InlineData(HtmlChartTooltipAnchor.Node, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, true)]
    public async Task UnrelatedDashboardMutationsSkipPaintScansAndUnchangedPlacement(HtmlChartTooltipAnchor anchor, bool pinned) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 980);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(PositionObserverHtml(anchor));
        var baseline = await ListenerStateAsync(page);
        await AcquireObservedPositionAsync(page, pinned);
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        await InstallPositionWorkProbeAsync(page);
        var work = await page.EvaluateAsync<JsonElement>("""
            async()=>{
                const dashboard=document.getElementById('unrelated-dashboard'), badge=dashboard.querySelector('span');
                for(let index=0;index<20;index++) {
                    if(index%2===0) { const row=document.createElement('div');row.textContent='Dashboard update '+index;dashboard.append(row); }
                    else badge.hidden=!badge.hidden;
                    await new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)));
                }
                return window.positionWork;
            }
            """);
        var retained = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        await CaptureAsync(page, $"position-observer-unrelated-{anchor}-{pinned}", new { shown, work, retained }, session);
        Assert.Equal(0, work.GetProperty("paintStyleReads").GetInt32());
        Assert.Equal(0, work.GetProperty("placementStyleReads").GetInt32());
        Assert.Equal(0, work.GetProperty("tipStyleWrites").GetInt32());
        Assert.Equal(shown.GetProperty("text").GetString(), retained.GetProperty("text").GetString());
        Assert.Equal(pinned, retained.GetProperty("pinned").GetBoolean());
        await page.EvaluateAsync("selector=>document.querySelector(selector).setAttribute('hidden','')", Reading);
        await SettleObservedPositionAsync(page);
        var hidden = await GeometryAsync(page);
        var released = await ListenerStateAsync(page);
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.False(hidden.GetProperty("pinned").GetBoolean());
        AssertPositionWatchersReleased(baseline, released);
        await CaptureAsync(page, $"position-observer-unrelated-hidden-{anchor}-{pinned}", new { hidden, baseline, released }, session);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, false)]
    [InlineData(HtmlChartTooltipAnchor.Node, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, true)]
    public async Task OutsideContentMovesReadoutWithoutChangingStageSize(HtmlChartTooltipAnchor anchor, bool pinned) {
        if (!Enabled) return;
        await using var session = await OpenAsync(PositionObserverHtml(anchor), 950, 980);
        var page = session.Page;
        // Keyboard focus remains acquired when outside content moves the target away from a pointer.
        await page.Locator(Reading).FocusAsync();
        if (pinned) await page.Keyboard.PressAsync("Space");
        await SettleObservedPositionAsync(page);
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        await InstallPositionWorkProbeAsync(page);
        await page.EvaluateAsync("()=>{const spacer=document.createElement('div');spacer.style.height='120px';spacer.textContent='New dashboard content';document.getElementById('outside-content').append(spacer);}");
        await SettleObservedPositionAsync(page);
        var moved = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        var work = await page.EvaluateAsync<JsonElement>("()=>window.positionWork");
        await CaptureAsync(page, $"position-observer-outside-movement-{anchor}-{pinned}", new { shown, moved, work }, session);
        Assert.Equal(shown.GetProperty("stage").GetProperty("width").GetDouble(), moved.GetProperty("stage").GetProperty("width").GetDouble());
        Assert.Equal(shown.GetProperty("stage").GetProperty("height").GetDouble(), moved.GetProperty("stage").GetProperty("height").GetDouble());
        Assert.InRange(moved.GetProperty("stage").GetProperty("y").GetDouble() - shown.GetProperty("stage").GetProperty("y").GetDouble(), 119.85, 120.15);
        Assert.InRange(moved.GetProperty("tip").GetProperty("y").GetDouble() - shown.GetProperty("tip").GetProperty("y").GetDouble(), 119.85, 120.15);
        Assert.Equal(0, work.GetProperty("paintStyleReads").GetInt32());
        Assert.True(work.GetProperty("placementStyleReads").GetInt32() > 0);
        Assert.Equal(shown.GetProperty("text").GetString(), moved.GetProperty("text").GetString());
        Assert.Equal(pinned, moved.GetProperty("pinned").GetBoolean());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, false, "style-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, false, "style-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, false, "class-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, false, "class-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "style-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "style-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "class-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "class-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, "style-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, "style-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, "class-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, "class-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "style-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "style-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "class-opacity", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "class-visibility", "ancestor")]
    [InlineData(HtmlChartTooltipAnchor.Node, false, "style-visibility", "target")]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "class-visibility", "target")]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, "style-opacity", "target")]
    [InlineData(HtmlChartTooltipAnchor.Chart, true, "class-opacity", "target")]
    public async Task StablePaintChangesDismissAndReleaseReadout(HtmlChartTooltipAnchor anchor, bool pinned, string change, string subject) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 980);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(PositionObserverHtml(anchor));
        var baseline = await ListenerStateAsync(page);
        await AcquireObservedPositionAsync(page, pinned);
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        // Hosts can hide the target itself or an ancestor while retaining the same screen bounds.
        await page.EvaluateAsync("""
            ({selector,change,subject})=>{
                const node=document.querySelector(selector),element=window.positionPaintSubject=subject==='target'?node:node.parentElement;
                // Active hover restores target opacity; host hiding must override that existing rule.
                if(change==='style-opacity') element.style.setProperty('opacity','0',subject==='target'?'important':'');
                else if(change==='style-visibility') element.style.visibility='hidden';
                else element.classList.add('position-hidden-'+(change.endsWith('opacity')?(subject==='target'?'target-opacity':'opacity'):'visibility'));
            }
            """, new { selector = Reading, change, subject });
        await SettleObservedPositionAsync(page);
        var hidden = await GeometryAsync(page);
        var released = await ListenerStateAsync(page);
        var paint = await page.EvaluateAsync<JsonElement>("()=>{const s=getComputedStyle(window.positionPaintSubject);return{opacity:s.opacity,visibility:s.visibility};}");
        await CaptureAsync(page, $"position-observer-paint-{anchor}-{pinned}-{subject}-{change}", new { shown, hidden, paint, baseline, released }, session);
        Assert.Equal(shown.GetProperty("node").GetRawText(), hidden.GetProperty("node").GetRawText());
        Assert.Equal(change.EndsWith("opacity", StringComparison.Ordinal) ? "0" : "hidden", paint.GetProperty(change.EndsWith("opacity", StringComparison.Ordinal) ? "opacity" : "visibility").GetString());
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.False(hidden.GetProperty("pinned").GetBoolean());
        AssertPositionWatchersReleased(baseline, released);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, true, "outer")]
    [InlineData(HtmlChartTooltipAnchor.Chart, false, "inner")]
    public async Task ClosedShadowHostPaintChangeReleasesReadout(HtmlChartTooltipAnchor anchor, bool pinned, string subject) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 980);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(PositionObserverHtml(anchor));
        await MountPositionFocusChartAsync(page, true);
        var baseline = await ListenerStateAsync(page);
        if (pinned) {
            await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).focus()", Reading);
            await page.Keyboard.PressAsync("Space");
        } else {
            var pointer = await page.EvaluateAsync<double[]>("selector=>{const b=window.positionFocusChart.querySelector(selector).getBoundingClientRect();return[b.x+b.width/2,b.y+b.height/2];}", Reading);
            await page.Mouse.MoveAsync((float)pointer[0], (float)pointer[1]);
        }
        await SettleObservedPositionAsync(page);
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight }, hosted: true);
        await page.EvaluateAsync("subject=>{const inner=window.positionFocusChart.getRootNode().host;if(subject==='inner')inner.style.visibility='hidden';else inner.getRootNode().host.classList.add('position-hidden-opacity');}", subject);
        await SettleObservedPositionAsync(page);
        var hidden = await GeometryAsync(page, hosted: true);
        var released = await ListenerStateAsync(page);
        await CaptureAsync(page, $"position-observer-shadow-paint-{anchor}-{pinned}-{subject}", new { shown, hidden, baseline, released }, session);
        Assert.Equal(shown.GetProperty("node").GetRawText(), hidden.GetProperty("node").GetRawText());
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.False(hidden.GetProperty("pinned").GetBoolean());
        AssertPositionWatchersReleased(baseline, released);
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task OneChartsPaintMutationKeepsSiblingReadoutAndSharedWatcherUntilItsAncestorHides() {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 1300);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(new[] { Observations(false), Observations(true) }.ToInteractiveHtmlDashboardPage(options => {
            options.Columns = 1;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }));
        var baseline = await ListenerStateAsync(page);
        await page.EvaluateAsync("""
            selector=>{window.positionPaintRoots=Array.from(document.querySelectorAll('.cfx-interactive-chart'));
                positionPaintRoots[1].dataset.cfxTooltipAnchor='chart';
                for(const root of positionPaintRoots){const node=root.querySelector(selector),b=node.getBoundingClientRect();node.dispatchEvent(new MouseEvent('click',{bubbles:true,clientX:b.x+b.width/2,clientY:b.y+b.height/2}));}}
            """, Reading);
        await SettleObservedPositionAsync(page);
        var both = await PaintDashboardStateAsync(page);
        Assert.All(both.GetProperty("readouts").EnumerateArray(), readout => Assert.False(readout.GetProperty("hidden").GetBoolean()));
        await page.EvaluateAsync("selector=>positionPaintRoots[0].querySelector(selector).parentElement.style.visibility='hidden'", Reading);
        await SettleObservedPositionAsync(page);
        var one = await PaintDashboardStateAsync(page);
        Assert.True(one.GetProperty("readouts")[0].GetProperty("hidden").GetBoolean());
        Assert.False(one.GetProperty("readouts")[1].GetProperty("hidden").GetBoolean());
        Assert.Equal(baseline.GetProperty("observed").GetInt32() + 1, one.GetProperty("watchers").GetProperty("observed").GetInt32());
        await page.EvaluateAsync("()=>document.body.style.opacity='0'");
        await SettleObservedPositionAsync(page);
        var closed = await PaintDashboardStateAsync(page);
        await CaptureAsync(page, "position-observer-dashboard-paint", new { baseline, both, one, closed }, session);
        Assert.All(closed.GetProperty("readouts").EnumerateArray(), readout => Assert.True(readout.GetProperty("hidden").GetBoolean()));
        AssertPositionWatchersReleased(baseline, closed.GetProperty("watchers"));
        AssertNoConsoleErrors(session);
    }
}
