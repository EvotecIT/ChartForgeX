using System.Text.Json;
using System.Text.RegularExpressions;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPositionBrowserTests {
    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node)]
    [InlineData(HtmlChartTooltipAnchor.Chart)]
    public async Task SwitchingFromShownToDelayedTargetKeepsTimingOwnership(HtmlChartTooltipAnchor anchor) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.DelayMilliseconds = 180;
            options.Tooltip.Position.Anchor = anchor;
        }), 950, 760);
        var page = session.Page;
        // Stay in plot background so acquisition switches without a native mark's pointerleave hide path.
        await PointerAsync(page, offsetY: 24);
        await WaitForTipAsync(page);
        await PointerAsync(page, Point(0, 1), offsetY: 24);
        Assert.True(await TipHiddenAsync(page));
        await WaitForTipAsync(page);
        Assert.Contains("6", await TooltipTextAsync(page), StringComparison.Ordinal);
        var geometry = await AssertPositionAsync(page, anchor, new[] { HtmlChartTooltipPlacement.BottomRight }, selector: Point(0, 1));
        await CaptureAsync(page, "position-delay-switch-" + anchor, geometry);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Pointer)]
    [InlineData(HtmlChartTooltipAnchor.Node)]
    [InlineData(HtmlChartTooltipAnchor.Chart)]
    public async Task DelayUsesLatestPointerAndLiveAnchorWhileFocusAndPinsAreImmediate(HtmlChartTooltipAnchor anchor) {
        if (!Enabled) return;
        var placements = new[] { HtmlChartTooltipPlacement.BottomRight, HtmlChartTooltipPlacement.TopLeft };
        await using var session = await OpenAsync(Observations(true).ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.DelayMilliseconds = 360;
            options.Tooltip.Range = HtmlChartTooltipRange.WithinDistance(60);
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Position.Placements = placements;
        }), 950, 760);
        var page = session.Page;
        await PointerAsync(page);
        Assert.True(await TipHiddenAsync(page));
        await Task.Delay(80);
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("root=>root.style.transform='translate(16px,7px)'");
        var pointer = await PointerAsync(page, offsetY: 24);
        await WaitForTipAsync(page);
        var shown = await AssertPositionAsync(page, anchor, placements, pointer: pointer);
        await PointerAsync(page, Point(0, 1));
        Assert.True(await TipHiddenAsync(page));
        await page.Locator(Point(0, 1)).FocusAsync();
        Assert.False(await TipHiddenAsync(page));
        var focus = await AssertPositionAsync(page, anchor, placements, selector: Point(0, 1));
        await page.Keyboard.PressAsync("Space");
        Assert.False(await TipHiddenAsync(page));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        pointer = await PointerAsync(page);
        var pin = await AssertPositionAsync(page, anchor, placements, pointer: pointer, selector: Point(0, 1));
        Assert.Contains("6", await TooltipTextAsync(page), StringComparison.Ordinal);
        await Task.Delay(420);
        Assert.False(await TipHiddenAsync(page));
        await CaptureAsync(page, "position-delay-focus-pin-" + anchor, new { shown, focus, pin });
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Node)]
    [InlineData(HtmlChartTooltipAnchor.Chart)]
    public async Task PinnedReadoutFollowsContainedAndPageScrollResizeAndZoom(HtmlChartTooltipAnchor anchor) {
        if (!Enabled) return;
        var placements = new[] { HtmlChartTooltipPlacement.BottomRight, HtmlChartTooltipPlacement.TopLeft };
        await using var session = await OpenAsync(Observations(false).ToInteractiveHtmlPage(options => {
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Position.Placements = placements;
            options.Interaction.Enable(ChartInteractionFeatures.Zoom | ChartInteractionFeatures.StateBookmarks);
        }), 400, 620);
        var page = session.Page;
        await CentreReadableTargetAsync(page);
        await page.Locator(Reading).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        var before = await AssertPositionAsync(page, anchor, placements);
        await page.EvaluateAsync("() => { document.body.style.paddingBottom='900px'; document.querySelector('.cfx-stage').scrollLeft+=35; window.scrollBy(0,30); }");
        await Task.Delay(100);
        var scrolled = await AssertPositionAsync(page, anchor, placements);
        if (anchor == HtmlChartTooltipAnchor.Node)
            Assert.NotEqual(before.GetProperty("node").GetProperty("x").GetDouble(), scrolled.GetProperty("node").GetProperty("x").GetDouble());
        await page.SetViewportSizeAsync(520, 650);
        await Task.Delay(100);
        var resized = await AssertPositionAsync(page, anchor, placements);
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("root=>root.dispatchEvent(new CustomEvent('cfx-apply-state',{detail:{snapshot:{viewport:{zoom:1.25,panX:10,panY:-8}}}}))");
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-interactive-chart').dataset.cfxZoom==='1.250'");
        await Task.Delay(220);
        var zoomed = await AssertPositionAsync(page, anchor, placements);
        Assert.True(zoomed.GetProperty("pinned").GetBoolean());
        await CaptureAsync(page, "position-scroll-resize-zoom-" + anchor, new { before, scrolled, resized, zoomed });
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartTooltipAnchor.Pointer)]
    [InlineData(HtmlChartTooltipAnchor.Node)]
    [InlineData(HtmlChartTooltipAnchor.Chart)]
    public async Task TranslatedScaledHostKeepsGapAndOffsetsInScreenCssPixels(HtmlChartTooltipAnchor anchor) {
        if (!Enabled) return;
        var placements = new[] { HtmlChartTooltipPlacement.BottomRight, HtmlChartTooltipPlacement.TopLeft };
        var html = Observations(true).ToInteractiveHtmlFragment(options => {
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
            options.Tooltip.Position.Anchor = anchor;
            options.Tooltip.Position.Placements = placements;
            options.Tooltip.Position.Gap = 9;
            options.Tooltip.Position.OffsetX = 5;
            options.Tooltip.Position.OffsetY = -3;
        });
        const string host = "width:650px;transform:translate(80px,70px) scale(.8);transform-origin:0 0";
        var pageHtml = "<html><body style='margin:0'><div style='" + host + "'>" + html + "</div></body></html>";
        await using var session = await OpenAsync(pageHtml, 950, 760);
        var pointer = await PointerAsync(session.Page);
        var geometry = await AssertPositionAsync(session.Page, anchor, placements, 9, 5, -3, pointer);
        await CaptureAsync(session.Page, "position-translated-scaled-" + anchor, geometry);
        await PointerAsync(session.Page, Point(0, 2));
        var edge = await GeometryAsync(session.Page, Point(0, 2));
        pointer = await PointerAsync(session.Page, Point(0, 2), offsetY: 1);
        var moved = await AssertPositionAsync(session.Page, anchor, placements, 9, 5, -3, pointer, Point(0, 2));
        Assert.InRange(moved.GetProperty("tip").GetProperty("width").GetDouble(), edge.GetProperty("tip").GetProperty("width").GetDouble() - .15, edge.GetProperty("tip").GetProperty("width").GetDouble() + .15);
        pointer = await PointerAsync(session.Page);
        geometry = await AssertPositionAsync(session.Page, anchor, placements, 9, 5, -3, pointer);
        var baseline = Environment.GetEnvironmentVariable("CFX_TOOLTIP_BASELINE_SCRIPT");
        if (anchor == HtmlChartTooltipAnchor.Node && !string.IsNullOrWhiteSpace(baseline)) {
            // Optional reviewer evidence keeps guide geometry separate from the position contract.
            var currentGuide = await GuideGeometryAsync(session.Page);
            await CaptureAsync(session.Page, "guide-host-current", new { host, pointer, geometry, guide = currentGuide, console = session.ConsoleLog });
            var oldScript = await File.ReadAllTextAsync(baseline);
            await session.Page.SetContentAsync(Regex.Replace(pageHtml, "<script[^>]*>.*?</script>", _ => "<script>" + oldScript + "</script>", RegexOptions.Singleline));
            await session.Page.Mouse.MoveAsync(0, 0);
            pointer = await PointerAsync(session.Page);
            var oldGeometry = await GeometryAsync(session.Page);
            var oldGuide = await GuideGeometryAsync(session.Page);
            await CaptureAsync(session.Page, "guide-host-baseline", new { host, pointer, geometry = oldGeometry, guide = oldGuide, console = session.ConsoleLog });
        }
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task ExactLineSummaryUsesItsCanonicalNativeGeometry() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 440).WithTheme(ChartTheme.GraphiteLight())
            .WithLineMarkers(ChartLineMarkerMode.None).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Reading", new[] { new ChartPoint(3, 3), new ChartPoint(5, 7), new ChartPoint(7, 3) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }), 950, 760);
        var page = session.Page;
        const string series = "[data-cfx-role=series][data-cfx-series=\"0\"]";
        var point = await page.Locator(series + " [data-cfx-role=line]").EvaluateAsync<double[]>("path=>{const p=path.getPointAtLength(path.getTotalLength()*.25).matrixTransform(path.getScreenCTM());return [p.x,p.y];}");
        await page.Mouse.MoveAsync((float)point[0], (float)point[1]);
        await WaitForTipAsync(page);
        Assert.StartsWith("series|", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"), StringComparison.Ordinal);
        var geometry = await AssertPositionAsync(page, HtmlChartTooltipAnchor.Node, new[] { HtmlChartTooltipPlacement.BottomRight }, selector: series);
        await CaptureAsync(page, "position-exact-line-summary", new { point, geometry });
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MovingNearAContainingHostEdgeKeepsTheReadoutSizeStable() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 440).WithTheme(ChartTheme.GraphiteLight())
            .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddScatter("Median processing duration for exported jobs", new[] { new ChartPoint(3, 5), new ChartPoint(9.5, 5) });
        var fragment = chart.ToInteractiveHtmlFragment(options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit);
        await using var session = await OpenAsync("<html><body style='margin:0'><div style='width:650px;transform:translate(80px,70px) scale(.8);transform-origin:0 0'>" + fragment + "</div></body></html>", 950, 760);
        var page = session.Page;
        await PointerAsync(page);
        await page.Locator(Reading).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        var first = await GeometryAsync(page);
        await PointerAsync(page, Point(0, 1));
        var pointer = await PointerAsync(page, Point(0, 1), offsetY: 1);
        var moved = await AssertPositionAsync(page, HtmlChartTooltipAnchor.Pointer, new[] { HtmlChartTooltipPlacement.BottomRight }, pointer: pointer);
        var width = first.GetProperty("tip").GetProperty("width").GetDouble();
        Assert.InRange(moved.GetProperty("tip").GetProperty("width").GetDouble(), width - .15, width + .15);
        await CaptureAsync(page, "position-host-edge-size", new { first, moved });
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task MultipleDashboardReadoutsShareWatchersUntilTheLastOneCloses() {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 980);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(new[] { Observations(false), Observations(true) }.ToInteractiveHtmlDashboardPage(options => {
            options.IncludeResetButton = false;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }));
        await page.EvaluateAsync("""
            () => document.querySelectorAll('.cfx-interactive-chart').forEach(root=>{
                const node=root.querySelector('[data-cfx-role=point][data-cfx-point="0"]'), b=node.getBoundingClientRect();
                node.dispatchEvent(new MouseEvent('click',{bubbles:true,clientX:b.x+b.width/2,clientY:b.y+b.height/2}));
            })
            """);
        await page.WaitForFunctionAsync("()=>Array.from(document.querySelectorAll('.cfx-tooltip')).every(tip=>!tip.hidden)");
        var both = await ListenerStateAsync(page);
        Assert.Equal(1, both.GetProperty("resize").GetInt32());
        Assert.Equal(1, both.GetProperty("scroll").GetInt32());
        Assert.Equal(2, both.GetProperty("observed").GetInt32());
        await page.Locator(".cfx-interactive-chart").First.EvaluateAsync("root=>root.remove()");
        await page.WaitForFunctionAsync("()=>Array.from(window.positionObserved.values()).reduce((n,s)=>n+s.size,0)===1");
        var one = await ListenerStateAsync(page);
        Assert.Equal(1, one.GetProperty("resize").GetInt32());
        Assert.False(await TipHiddenAsync(page));
        await page.Locator(".cfx-interactive-chart").EvaluateAsync("root=>root.remove()");
        await page.WaitForFunctionAsync("()=>window.positionListeners.resize.size===0");
        var closed = await ListenerStateAsync(page);
        Assert.Equal(0, closed.GetProperty("observed").GetInt32());
        await CaptureAsync(page, "position-dashboard-lifetime", new { both, one, closed });
        AssertNoConsoleErrors(session);
    }

    private static Task<JsonElement> GuideGeometryAsync(IPage page) => page.EvaluateAsync<JsonElement>("""
        () => {
            const root=document.querySelector('.cfx-interactive-chart'), guide=root.querySelector('.cfx-crosshair');
            const rect=element=>{const b=element.getBoundingClientRect();return {x:b.x,y:b.y,width:b.width,height:b.height};};
            return {hidden:guide.hidden,x:rect(guide.querySelector('.cfx-crosshair__line--y')),y:rect(guide.querySelector('.cfx-crosshair__line--x')),
                localX:guide.style.getPropertyValue('--cfx-crosshair-x'),localY:guide.style.getPropertyValue('--cfx-crosshair-y'),stage:rect(root.querySelector('.cfx-stage'))};
        }
        """);

    [Theory]
    [InlineData("leave")]
    [InlineData("root-removed")]
    [InlineData("target-removed")]
    [InlineData("root-hidden")]
    [InlineData("tip-hidden")]
    public async Task HiddenOrRemovedReadoutReleasesSharedPositionWatchers(string change) {
        if (!Enabled) return;
        await using var session = await OpenAsync("<html><body></body></html>", 950, 760);
        var page = session.Page;
        await InstallListenerProbeAsync(page);
        await page.SetContentAsync(Observations(false).ToInteractiveHtmlPage(options => {
            options.IncludeResetButton = false;
            options.Interaction.Features = ChartInteractionFeatures.Tooltips;
            options.Tooltip.Position.Anchor = HtmlChartTooltipAnchor.Node;
        }));
        await PointerAsync(page);
        await page.WaitForFunctionAsync("()=>window.positionListeners.resize.size===1");
        var active = await ListenerStateAsync(page);
        Assert.Equal(1, active.GetProperty("scroll").GetInt32());
        Assert.Equal(1, active.GetProperty("transitionend").GetInt32());
        if (change == "leave") await MoveAwayAsync(page);
        else await page.EvaluateAsync("""
            change => {
                const root=document.querySelector('.cfx-interactive-chart'), node=root.querySelector('[data-cfx-role=point][data-cfx-point="0"]');
                window.retainedPositionRoot=root;
                if(change==='root-removed') root.remove(); else if(change==='target-removed') node.remove();
                else if(change==='root-hidden') root.hidden=true; else root.querySelector('.cfx-tooltip').hidden=true;
            }
            """, change);
        await page.WaitForFunctionAsync("()=>window.positionListeners.resize.size===0&&window.positionListeners.scroll.size===0&&window.positionListeners.transitionend.size===0");
        var released = await ListenerStateAsync(page);
        Assert.Equal(0, released.GetProperty("observed").GetInt32());
        await CaptureAsync(page, "position-lifetime-" + change, new { active, released });
        AssertNoConsoleErrors(session);
    }

    private static Task InstallListenerProbeAsync(IPage page) => page.EvaluateAsync("""
        () => {
            window.positionListeners={resize:new Set(),scroll:new Set(),transitionend:new Set()};
            for(const target of [window,document]) {
                const add=target.addEventListener.bind(target), remove=target.removeEventListener.bind(target);
                target.addEventListener=(name,listener,options)=>{window.positionListeners[name]?.add(listener);add(name,listener,options);};
                target.removeEventListener=(name,listener,options)=>{window.positionListeners[name]?.delete(listener);remove(name,listener,options);};
            }
            window.positionObserved=new Map();const NativeResize=window.ResizeObserver;
            window.ResizeObserver=class extends NativeResize {
                constructor(callback){super(callback);window.positionObserved.set(this,new Set());}
                observe(node){window.positionObserved.get(this).add(node);super.observe(node);}
                unobserve(node){window.positionObserved.get(this).delete(node);super.unobserve(node);}
                disconnect(){window.positionObserved.get(this).clear();super.disconnect();}
            };
        }
        """);

    private static Task<JsonElement> ListenerStateAsync(IPage page) => page.EvaluateAsync<JsonElement>("() => ({resize:window.positionListeners.resize.size,scroll:window.positionListeners.scroll.size,transitionend:window.positionListeners.transitionend.size,observed:Array.from(window.positionObserved.values()).reduce((sum,targets)=>sum+targets.size,0)})");
}
