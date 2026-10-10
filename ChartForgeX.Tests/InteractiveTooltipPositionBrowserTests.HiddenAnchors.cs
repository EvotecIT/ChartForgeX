using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, "target", false)]
    [InlineData(HtmlChartTooltipAnchor.Node, "target", true)]
    [InlineData(HtmlChartTooltipAnchor.Node, "ancestor", false)]
    [InlineData(HtmlChartTooltipAnchor.Node, "ancestor", true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "target", false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "target", true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "ancestor", false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "ancestor", true)]
    public async Task HiddenNativeAnchorDismissesReadoutAndReleasesPositionWatchers(HtmlChartTooltipAnchor anchor, string subject, bool pinned) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 760);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        // Hosts may hide SVG groups through their ordinary hidden-attribute convention.
        await page.SetContentAsync("<html><head><style>svg [hidden]{display:none}</style></head><body>" + Observations(false).ToInteractiveHtmlFragment(options => {
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
        }) + "</body></html>");
        const string literal = "<script>window.hiddenAnchorInjection=true</script> & \"quoted\" 'literal' — full host fact";
        await page.Locator(Reading).EvaluateAsync("(node,literal)=>node.setAttribute('data-cfx-meta-host-fact',literal)", literal);
        var baseline = await ListenerStateAsync(page);
        if (pinned) {
            await page.Locator(Reading).FocusAsync();
            await page.Keyboard.PressAsync("Space");
        } else await PointerAsync(page);
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        Assert.Contains(literal, shown.GetProperty("text").GetString()!, StringComparison.Ordinal);
        var active = await ListenerStateAsync(page);
        Assert.Equal(baseline.GetProperty("observed").GetInt32() + 1, active.GetProperty("observed").GetInt32());
        await page.EvaluateAsync("""
            ({selector,subject})=>{
                const node=document.querySelector(selector);
                window.hiddenPositionSubject=subject==='target'?node:node.parentElement;
                window.hiddenPositionSubject.setAttribute('hidden','');
            }
            """, new { selector = Reading, subject });
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var hidden = await GeometryAsync(page);
        var released = await ListenerStateAsync(page);
        await CaptureAsync(page, $"position-hidden-{anchor}-{subject}-{(pinned ? "pinned" : "shown")}", new { baseline, shown, active, hidden, released }, session);
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.False(hidden.GetProperty("pinned").GetBoolean());
        foreach (var name in new[] { "resize", "scroll", "transitionend", "observed" })
            Assert.Equal(baseline.GetProperty(name).GetInt32(), released.GetProperty(name).GetInt32());
        await page.EvaluateAsync("()=>window.hiddenPositionSubject.removeAttribute('hidden')");
        await page.Locator(Reading).EvaluateAsync("node=>{node.blur();node.focus();}");
        var recovered = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        Assert.Equal(shown.GetProperty("text").GetString(), recovered.GetProperty("text").GetString());
        Assert.False(await page.EvaluateAsync<bool>("()=>!!window.hiddenAnchorInjection"));
        await CaptureAsync(page, $"position-hidden-recovery-{anchor}-{subject}-{(pinned ? "pinned" : "shown")}", recovered, session);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, "outer", true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, "inner", false)]
    public async Task HiddenClosedShadowHostReleasesReadoutPositionWatchers(HtmlChartTooltipAnchor anchor, string subject, bool pinned) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 760);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync("<html><body>" + Observations(true).ToInteractiveHtmlFragment(options => options.Tooltip.Position.Anchor = anchor) + "</body></html>");
        await MountPositionFocusChartAsync(page, shadow: true);
        var baseline = await ListenerStateAsync(page);
        await page.EvaluateAsync("selector=>window.positionFocusChart.querySelector(selector).focus()", Reading);
        if (pinned) await page.Keyboard.PressAsync("Space");
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight }, hosted: true);
        await page.EvaluateAsync("subject=>{const inner=window.positionFocusChart.getRootNode().host; (subject==='inner'?inner:inner.getRootNode().host).hidden=true;}", subject);
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var hidden = await GeometryAsync(page, hosted: true);
        var released = await ListenerStateAsync(page);
        await CaptureAsync(page, $"position-hidden-shadow-{anchor}-{subject}-{(pinned ? "pinned" : "shown")}", new { baseline, shown, hidden, released }, session);
        Assert.True(hidden.GetProperty("hidden").GetBoolean());
        Assert.False(hidden.GetProperty("pinned").GetBoolean());
        foreach (var name in new[] { "resize", "scroll", "transitionend", "observed" })
            Assert.Equal(baseline.GetProperty(name).GetInt32(), released.GetProperty(name).GetInt32());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node, false)]
    [InlineData(HtmlChartTooltipAnchor.Node, true)]
    [InlineData(HtmlChartTooltipAnchor.Chart, false)]
    [InlineData(HtmlChartTooltipAnchor.Chart, true)]
    public async Task PinnedKeyboardOnlyFactKeepsReadoutAvailabilityWhilePointerMoves(HtmlChartTooltipAnchor anchor, bool precision) {
        if (!Enabled) return;
        var chart = NumericRadialSeriesTests.Add(Chart.Create().WithSize(600, 440).WithHeader(false)
            .WithLegend(false).WithAxes(false).WithGrid(false).WithDataLabels(false).WithTheme(ChartTheme.GraphiteLight()), false, "Observed",
            new ChartPoint(1, precision ? 1 : 0), new ChartPoint(2, 100000000));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.Position.Anchor = anchor;
        }), 950, 760);
        var page = session.Page;
        await page.Locator(Reading).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        var shown = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        Assert.Equal(precision ? "precision-collapse" : "zero", await page.Locator(Reading).GetAttributeAsync("data-cfx-geometry-status"));
        var mark = NumericRadialSeriesTests.Marks(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene).Last();
        var angle = mark.Start + mark.Sweep / 2; var radius = (mark.Inner + mark.Outer) / 2;
        var pointer = await page.EvaluateAsync<double[]>("p=>{const node=document.querySelector(p.selector),point=new DOMPoint(p.x,p.y).matrixTransform(node.ownerSVGElement.getScreenCTM());return[point.x,point.y];}",
            new { selector = Point(0, 1), x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius });
        await page.Mouse.MoveAsync((float)pointer[0], (float)pointer[1]);
        await page.Mouse.MoveAsync((float)pointer[0] + 1, (float)pointer[1]);
        await page.EvaluateAsync("()=>window.dispatchEvent(new Event('resize'))");
        await page.EvaluateAsync("()=>new Promise(resolve=>requestAnimationFrame(()=>requestAnimationFrame(resolve)))");
        var retained = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight });
        Assert.True(retained.GetProperty("pinned").GetBoolean());
        Assert.Equal(shown.GetProperty("text").GetString(), retained.GetProperty("text").GetString());
        await CaptureAsync(page, $"position-hidden-fact-retained-{anchor}-{(precision ? "precision" : "zero")}", new { shown, pointer, retained }, session);
        AssertNoConsoleErrors(session);
    }
}
