using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveChartLifecycleBrowserTests {
    [BrowserTheory]
    [InlineData(false, 1440)]
    [InlineData(true, 1440)]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    public async Task ReadableToolbarFollowsTheVisibleNativeCardWithoutAGutterOffset(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Readings(dark).ToInteractiveHtmlPage(options => {
            options.Interaction.Enable(ChartInteractionFeatures.Brush | ChartInteractionFeatures.Zoom);
        }), width, 800);
        var page = session.Page;
        await page.AddStyleTagAsync(new() { Content = ".cfx-stage svg { transition-duration: 1s !important; }" });
        Assert.Equal("readable", await page.Locator(Root).GetAttributeAsync("data-cfx-responsive-layout"));
        await AssertReadableToolbarEdgesAsync(page);
        await CaptureAsync(page, "readable-toolbar-card-" + dark + "-" + width);
        await page.Locator("[data-cfx-zoom='in']").ClickAsync();
        // The final scroll extent is defined after the zoom transition completes.
        await page.Locator(".cfx-stage svg").EvaluateAsync("async svg => { await Promise.all(svg.getAnimations().map(animation => animation.finished)); }");
        await AssertReadableToolbarEdgesAsync(page);
        await page.Locator(".cfx-stage").EvaluateAsync("stage=>stage.scrollLeft=stage.scrollWidth");
        await AssertReadableToolbarEdgesAsync(page);
        await CaptureAsync(page, "readable-toolbar-card-zoom-scroll-" + dark + "-" + width);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Area, false, 1440, true)]
    [InlineData(ChartSeriesKind.StepArea, true, 390, true)]
    [InlineData(ChartSeriesKind.Area, true, 390, false)]
    [InlineData(ChartSeriesKind.StepArea, false, 1440, false)]
    public async Task NativeFillCoveredPointerActivatesTheObservationWithoutSelectingItsSeriesWrapper(
        ChartSeriesKind kind, bool dark, int width, bool tooltips) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(720, 360)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithTitle("Covered area observations").WithXAxisBounds(1, 5).WithYAxisBounds(0, 60)
            .AddSeries(new ChartSeries(new ChartSeriesId("api"), "API", kind,
                new[] { new ChartPoint(1, 20), new ChartPoint(2, 20), new ChartPoint(3, 20), new ChartPoint(4, 20), new ChartPoint(5, 20) })
                .WithObservationId(2, new ChartObservationId("api-covered")))
            .AddSeries(new ChartSeries(new ChartSeriesId("worker"), "Worker", kind,
                new[] { new ChartPoint(1, 50), new ChartPoint(2, 50), new ChartPoint(3, 50), new ChartPoint(4, 50), new ChartPoint(5, 50) })
                .WithObservationId(2, new ChartObservationId("worker-covered")));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            Fit(options);
            options.Tooltip.Range = HtmlChartTooltipRange.Exact;
            if (!tooltips) options.Interaction.Disable(ChartInteractionFeatures.Tooltips);
        }), width, 700);
        var page = session.Page;
        await BindControllerAsync(page);
        const string point = "[data-cfx-observation-id='api-covered']";
        var box = await BoxAsync(page, point);
        var x = (float)(box.X + box.Width / 2);
        var y = (float)(box.Y + box.Height / 2);
        var nativeHit = await page.EvaluateAsync<string>("p=>document.elementFromPoint(p[0],p[1]).closest('[data-cfx-series-key]').dataset.cfxSeriesKey", new double[] { x, y });
        Assert.Equal("worker", nativeHit);
        await page.Mouse.ClickAsync(x, y);
        Assert.Equal("worker-covered", await page.EvaluateAsync<string>("()=>chartController.getSelection()[0]?.observationId"));
        Assert.Equal("point", await page.EvaluateAsync<string>("()=>lifecycleSelections[0].target.targetKind"));
        Assert.Equal("worker", await page.EvaluateAsync<string>("()=>lifecycleSelections[0].target.seriesKey"));
        Assert.Equal(50, await page.EvaluateAsync<double>("()=>lifecycleSelections[0].target.numericValue"));
        Assert.Equal(1, await page.EvaluateAsync<int>("()=>lifecycleSelections.length"));
        if (tooltips) Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await CaptureAsync(page, "covered-fill-selection-" + kind + "-" + dark + "-" + width + "-" + tooltips);

        await page.Locator("[data-cfx-observation-id='worker-covered']").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>chartController.getSelection().length"));
        Assert.Equal(2, await page.EvaluateAsync<int>("()=>lifecycleSelections.length"));
        await page.Mouse.ClickAsync(x, (float)(y + Math.Max(12, box.Height * 3)));
        Assert.Equal("worker-covered", await page.EvaluateAsync<string>("()=>chartController.getSelection()[0]?.observationId"));
        Assert.Equal(3, await page.EvaluateAsync<int>("()=>lifecycleSelections.length"));
        await page.EvaluateAsync("()=>chartController.clearSelection()");
        var coveringPoint = await BoxAsync(page, "[data-cfx-observation-id='worker-covered']");
        await page.Mouse.ClickAsync(x, (float)(coveringPoint.Y - Math.Max(12, coveringPoint.Height * 3)));
        Assert.Equal(0, await page.EvaluateAsync<int>("()=>chartController.getSelection().length"));
        Assert.Equal(3, await page.EvaluateAsync<int>("()=>lifecycleSelections.length"));
        // Public mute makes the painted fact unavailable even while its dimmed fill remains the native hit.
        await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=worker]").ClickAsync();
        Assert.Equal("true", await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=worker]").GetAttributeAsync("data-cfx-muted"));
        await page.Mouse.ClickAsync(x, y);
        var mutedState = await page.EvaluateAsync<JsonElement>("""
            p=>{
                const native=document.elementFromPoint(p[0],p[1]),owner=native.closest('[data-cfx-role=series]'),style=getComputedStyle(native);
                return {nativeRole:native.dataset.cfxRole,nativeSeries:native.closest('[data-cfx-series-key]').dataset.cfxSeriesKey,
                  pointerEvents:style.pointerEvents,visibility:style.visibility,opacity:style.opacity,ownerClass:owner.getAttribute('class'),
                  selection:chartController.getSelection(),events:lifecycleSelections};
            }
            """, new double[] { x, y });
        await CaptureAsync(page, "covered-fill-muted-series-" + kind + "-" + dark + "-" + width + "-" + tooltips, mutedState);
        Assert.Equal("worker", mutedState.GetProperty("nativeSeries").GetString());
        Assert.Contains("cfx-series-muted", mutedState.GetProperty("ownerClass").GetString());
        Assert.Equal(0, mutedState.GetProperty("selection").GetArrayLength());
        Assert.Equal(3, mutedState.GetProperty("events").GetArrayLength());
        AssertNoConsoleErrors(session);
    }

    private static async Task AssertReadableToolbarEdgesAsync(Microsoft.Playwright.IPage page) {
        var bounds = await page.Locator(Root).EvaluateAsync<JsonElement>("""
            root=>{
                const stage=root.querySelector('.cfx-stage'),toolbar=root.querySelector('.cfx-toolbar'),
                  card=root.querySelector('.cfx-stage [data-cfx-role=frame-card]'),button=toolbar.querySelector('button:last-child'),
                  sr=stage.getBoundingClientRect(),cr=card.getBoundingClientRect(),tr=toolbar.getBoundingClientRect(),br=button.getBoundingClientRect(),
                  style=getComputedStyle(card),matrix=card.getScreenCTM(),
                  stroke=style.stroke==='none'?0:(parseFloat(style.strokeWidth)||0)*Math.hypot(matrix.a,matrix.c)/2,
                  left=Math.max(sr.left+stage.clientLeft,cr.left-stroke),right=Math.min(sr.left+stage.clientLeft+stage.clientWidth,cr.right+stroke);
                return {left:Math.abs(tr.left-left),right:Math.abs(tr.right-right),buttonRight:Math.abs(br.right-tr.right),verticalScroll:stage.scrollHeight>stage.clientHeight};
            }
            """);
        Assert.InRange(bounds.GetProperty("left").GetDouble(), 0, .6);
        Assert.InRange(bounds.GetProperty("right").GetDouble(), 0, .6);
        Assert.InRange(bounds.GetProperty("buttonRight").GetDouble(), 0, .6);
    }
}
