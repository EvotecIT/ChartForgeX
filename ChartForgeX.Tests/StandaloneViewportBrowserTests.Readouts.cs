using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    [BrowserTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultPresentationAndUpdatesRespectReducedMotion(bool reduced) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).AddLine("Requests", ChartPoints.FromValues(2, 7, 4));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 980, 800,
            configurePageAsync: page => page.EmulateMediaAsync(new PageEmulateMediaOptions {
                ReducedMotion = reduced ? ReducedMotion.Reduce : ReducedMotion.NoPreference
            }));
        var page = session.Page;
        await MountAsync(page);
        // The motion bundle replaces the stylesheet fade with per-series entry effects.
        Assert.Equal("none", await page.Locator(".cfx-stage").EvaluateAsync<string>("node=>getComputedStyle(node).animationName"));
        Assert.Equal(reduced ? null : "enter", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-motion"));
        var moving = await page.EvaluateAsync<bool>("html=>{controller.update(html);return !!document.querySelector('[data-cfx-update-motion]');}",
            chart.ToInteractiveHtmlFragmentWithoutAssets());
        Assert.Equal(!reduced, moving);
        await page.WaitForFunctionAsync("()=>!document.querySelector('[data-cfx-update-motion]')");
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task DefaultMotionKeepsAuthoredNativeOpacityHiddenDuringLoadAndUpdate() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).AddLine("Hidden observations", ChartPoints.FromValues(2, 7, 4));
        var html = chart.ToInteractiveHtmlPage()
            .Replace("</head>", "<style>.cfx-stage [data-cfx-role=series]{opacity:0}</style></head>")
            .Replace("</body>", "<script>window.loadNativeOpacity=getComputedStyle(document.querySelector('.cfx-stage [data-cfx-role=series]')).opacity;</script></body>");
        await using var session = await OpenAsync(html, 980, 800,
            configurePageAsync: page => page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference }));
        var page = session.Page;
        Assert.Equal("0", await page.EvaluateAsync<string>("()=>window.loadNativeOpacity"));
        await MountAsync(page);
        var updateOpacity = await page.EvaluateAsync<string>("""
            html => {
                controller.update(html, {motion:{duration:1000}});
                const svg=controller.root.querySelector('.cfx-stage svg');
                svg.style.opacity='0';
                const animation=controller.root.querySelector('.cfx-stage').getAnimations()[0];
                animation.pause(); animation.currentTime=500;
                return getComputedStyle(svg).opacity;
            }
            """, chart.ToInteractiveHtmlFragmentWithoutAssets());
        Assert.Equal("0", updateOpacity);
        await MoveToAsync(page, Point(0, 1));
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-hover-key"));
        await page.EvaluateAsync("()=>controller.dispose()");
        Assert.Equal(0, await page.Locator(".cfx-stage").EvaluateAsync<int>("node=>node.getAnimations().length"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task SavedTemplateAndBrowserRendererShareFormattedValuesAndTreatStringsAsText() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithXLabels("Monday", "Tuesday", "Wednesday")
            .AddLine("Response time", ChartPoints.FromValues(18, 41.2, 43));
        chart.Options.ValueUnit = "ms";
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Template = "{series} · {category}: {value} <literal>";
        }), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        await MoveToAsync(page, Point(0, 0));
        Assert.Equal("Response time · Monday: 18.0 ms <literal>", await TooltipTextAsync(page));
        Assert.Equal(0, await page.Locator(".cfx-tooltip literal").CountAsync());
        await page.EvaluateAsync("()=>controller.setTooltipRenderer(context=>{const label=document.createElement('strong');label.textContent=context.category+' = '+context.value;return label;})");
        await MoveToAsync(page, Point(0, 1));
        Assert.Equal("Tuesday = 41.2 ms", await page.Locator(".cfx-tooltip strong").InnerTextAsync());
        await page.EvaluateAsync("()=>controller.setTooltipRenderer(()=>'<img src=invalid onerror=alert(1)>')");
        await MoveToAsync(page, Point(0, 2));
        Assert.Equal("<img src=invalid onerror=alert(1)>", await TooltipTextAsync(page));
        Assert.Equal(0, await page.Locator(".cfx-tooltip img").CountAsync());
        await page.EvaluateAsync("()=>controller.setTooltipRenderer(null)");
        await MoveToAsync(page, Point(0, 0));
        Assert.Contains("18.0 ms", await TooltipTextAsync(page));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 980)]
    [InlineData(true, 375)]
    public async Task DoubleClickIsolationRestoresThePreviousHiddenSeries(bool dark, int width) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(ChartSeriesKind.Line, reference: true, dark: dark), width, 1000);
        var page = session.Page;
        await MountAsync(page);
        var legend = page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]");
        await legend.ClickAsync();
        Assert.Equal("false", await legend.GetAttributeAsync("aria-pressed"));
        await legend.DblClickAsync();
        Assert.NotNull(await page.Locator(Root).GetAttributeAsync("data-cfx-isolated-series"));
        Assert.Equal("true", await legend.GetAttributeAsync("aria-pressed"));
        Assert.Equal("false", await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=samples]").GetAttributeAsync("aria-pressed"));
        await legend.DblClickAsync();
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-isolated-series"));
        Assert.Equal("false", await legend.GetAttributeAsync("aria-pressed"));
        Assert.Equal("true", await page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=samples]").GetAttributeAsync("aria-pressed"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task CompactPlotSelectionUsesReadableTargetsAndKeepsPinnedTooltipInsideTheViewport() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(ChartSeriesKind.Line, dark: true), 375, 812,
            touch: Engine == HtmlTinkerX.HtmlBrowserEngine.Chromium);
        var page = session.Page;
        await MountAsync(page);
        var bounds = await page.Locator(Sample + " [data-cfx-browser-hit-area]").BoundingBoxAsync();
        Assert.NotNull(bounds);
        Assert.True(bounds!.Width >= 24 - .1 && bounds.Height >= 24 - .1);
        var point = await PlotPointerAsync(page, 4d / 12, .82);
        if (Engine == HtmlTinkerX.HtmlBrowserEngine.Chromium) await page.Touchscreen.TapAsync(point.X, point.Y);
        else await page.Mouse.ClickAsync(point.X, point.Y);
        Assert.True(await page.Locator(Sample).EvaluateAsync<bool>("node=>node.classList.contains('cfx-selected')"));
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.True(await page.Locator(".cfx-brush-box").IsHiddenAsync());
        var tip = await page.Locator(".cfx-tooltip").BoundingBoxAsync();
        Assert.NotNull(tip); Assert.InRange(tip!.X, 8 - .1, 375 - tip.Width - 8 + .1);
        Assert.True((await page.Locator(Root).BoundingBoxAsync())!.Y <= 12);
        Assert.InRange(await page.Locator("[data-cfx-browser-axis=x] text").First.EvaluateAsync<double>("node=>node.getBoundingClientRect().height"), 10, 20);
        await CaptureAsync(page, "ux-compact-pinned");
        await page.Locator(".cfx-tooltip__close").ClickAsync();
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        AssertNoConsoleErrors(session);
    }
}
