using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class InteractiveTooltipPaintBrowserTests {
    private const string Root = ".cfx-interactive-chart";
    private const string Term = "[data-cfx-role='word-cloud-term'][data-cfx-point='0']";

    [Theory]
    [InlineData(false, 950)]
    [InlineData(true, 340)]
    public async Task VisibleWordCloudTextRetainsNativePointerSelectionFocusAndPins(bool dark, int width) {
        if (!Enabled) return;
        var chart = Frame(dark).WithTitle("Visible text terms").AddWordCloud("Topics", new[] {
            new ChartWordCloudItem("Visible", 100), new ChartWordCloudItem("Companion", 50)
        });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ExactFeatures), width, 720);
        var page = session.Page;
        var target = page.Locator(Term);
        var id = await target.GetAttributeAsync("data-cfx-target-id");
        await MoveToNativeTextAsync(page, Term);
        Assert.Contains("Visible", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Mouse.DownAsync(); await page.Mouse.UpAsync();
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        await MoveAwayAsync(page);
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', e => window.textSelection = e.detail.target)");
        await target.FocusAsync();
        Assert.Contains("100", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal(id, await page.EvaluateAsync<string>("() => window.textSelection.targetId"));
        Assert.Equal("100", await page.EvaluateAsync<string>("() => window.textSelection.value"));
        await target.BlurAsync(); await MoveAwayAsync(page);
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await CaptureContractAsync(page, "paint-word-cloud-" + width + "-" + (dark ? "dark" : "light"), new { NativeTarget = id, KeyboardSelected = true });
        AssertNoConsoleErrors(session);
    }

    [Fact]
    public async Task WordCloudUsesActualTextPaintAndHonoursChildVisibilityOverrides() {
        if (!Enabled) return;
        var chart = Frame(false).AddWordCloud("Topics", new[] { new ChartWordCloudItem("Visible", 100) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ExactFeatures), 950, 720);
        var page = session.Page;
        var target = page.Locator(Term);
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = Term + " { visibility:hidden!important; } " + Term + " text { visibility:visible!important; fill:none!important; stroke:#ef4444!important; stroke-width:1px!important; }" });
        await MoveToNativeTextAsync(page, Term);
        Assert.Contains("Visible", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal("rgb(239, 68, 68)", await page.Locator(".cfx-tooltip__swatch").First.EvaluateAsync<string>("n => getComputedStyle(n).backgroundColor"));
        await CaptureContractAsync(page, "paint-word-cloud-stroke-child-override", new { Fill = "none", Stroke = "#ef4444", ParentVisibility = "hidden", ChildVisibility = "visible" });
        await target.EvaluateAsync("n => n.style.setProperty('visibility', 'visible', 'important')");
        await target.Locator("text").EvaluateAsync("n => n.style.setProperty('stroke', 'none', 'important')");
        await MoveAwayAsync(page);
        await target.FocusAsync();
        Assert.Equal(await target.GetAttributeAsync("data-cfx-target-id"), await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Keyboard.PressAsync("Space");
        Assert.Null(await target.GetAttributeAsync("aria-selected"));
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        await target.BlurAsync();
        await target.Locator("text").EvaluateAsync("n => n.style.setProperty('fill', '#176eaf', 'important')");
        await target.EvaluateAsync("n => n.style.opacity = '0'");
        await target.FocusAsync();
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await target.BlurAsync();
        await target.EvaluateAsync("n => n.style.removeProperty('opacity')");
        await target.FocusAsync();
        Assert.Contains("Visible", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(ChartAnnotationKind.HorizontalBand)]
    [InlineData(ChartAnnotationKind.VerticalBand)]
    public async Task UnpaintedBandsDoNotAcquireOrPinFromTheirRetainedBounds(ChartAnnotationKind kind) {
        if (!Enabled) return;
        var chart = AnnotationChart(false);
        chart.Annotations.Add(new ChartAnnotation(kind, 6, 8, "Unpainted interval", ChartColor.FromHex("#ef4444"), 0, showLabel: false));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(ExactFeatures), 950, 720);
        var page = session.Page;
        var selector = "[data-cfx-role='annotation'][data-cfx-kind='" + kind + "']";
        var annotationId = await page.Locator(selector).GetAttributeAsync("data-cfx-target-id");
        await page.EvaluateAsync("()=>{window.cfxBandEvents=[];const root=document.querySelector('.cfx-interactive-chart');for(const type of ['cfxhover','cfxselect','cfxtooltip'])root.addEventListener(type,event=>window.cfxBandEvents.push({type,detail:event.detail}));}");
        await MoveToAsync(page, selector);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Mouse.DownAsync(); await page.Mouse.UpAsync();
        Assert.Null(await page.Locator(selector).GetAttributeAsync("aria-selected"));
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        // Native SVG focus can retain a different keyboard observation after this background click.
        Assert.Equal(0, await page.EvaluateAsync<int>("id=>window.cfxBandEvents.filter(event=>event.detail.target?.targetKind==='annotation'&&event.detail.target?.targetId===id).length", annotationId));
        var focus = await page.EvaluateAsync<JsonElement>("()=>{const root=document.querySelector('.cfx-interactive-chart'),node=root.getRootNode().activeElement;return {kind:node?.dataset?.cfxTargetKind||null,id:node?.dataset?.cfxTargetId||null,events:window.cfxBandEvents};}");
        await CaptureContractAsync(page, "paint-transparent-" + kind, new { Opacity = 0, ShowLabel = false, AnnotationId = annotationId, Focus = focus });
        await page.EvaluateAsync("()=>document.activeElement?.blur()");
        // A host can restore the real shape, and later hide that child without altering semantic bounds.
        await page.Locator(selector + " rect").EvaluateAsync("n => n.style.fill = '#ef4444'");
        await MoveAwayAsync(page); await MoveToAsync(page, selector);
        Assert.Contains("Unpainted interval", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Locator(selector + " rect").EvaluateAsync("n => n.style.opacity = '0'");
        await MoveToAsync(page, selector, offsetX: 1);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Mouse.DownAsync(); await page.Mouse.UpAsync();
        Assert.Null(await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartResponsiveLayout.Fit)]
    [InlineData(HtmlChartResponsiveLayout.Readable)]
    public async Task AnnotationCaptionAndLegendCaptionRemainRealNativeTargetsWithoutShapePaint(HtmlChartResponsiveLayout layout) {
        if (!Enabled) return;
        var chart = AnnotationChart(true).WithLegend(true);
        chart.Annotations.Add(new ChartAnnotation(ChartAnnotationKind.HorizontalBand, 6, 8, "Caption", ChartColor.FromHex("#ef4444"), 0));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => { ExactFeatures(options); options.ResponsiveLayout = layout; options.Interaction.Enable(ChartInteractionFeatures.LegendToggles); }), 340, 720);
        var page = session.Page;
        const string annotation = "[data-cfx-role='annotation']";
        await page.Locator(annotation + " rect").EvaluateAllAsync<object?>("nodes => nodes.forEach(n => { n.style.fill = 'none'; n.style.stroke = 'none'; })");
        await MoveToNativeTextAsync(page, annotation);
        Assert.Contains("Caption", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Mouse.DownAsync(); await page.Mouse.UpAsync();
        Assert.Equal("true", await page.Locator(annotation).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal(0, await page.Locator(".cfx-stage").EvaluateAsync<double>("n => n.scrollLeft"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        await page.Locator(annotation).BlurAsync(); await page.Locator(annotation).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(annotation).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(Root).GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Equal(0, await page.Locator(".cfx-stage").EvaluateAsync<double>("n => n.scrollLeft"));
        await CaptureContractAsync(page, "paint-caption-only-annotation-" + layout + "-compact-dark", new { BandOpacity = 0, Caption = true, Layout = layout.ToString(), ScrollLeft = 0, PointerAndKeyboardPins = true });
        await page.Locator("[data-cfx-reset]").ClickAsync();
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = Legend(0) + " :is(rect,circle,ellipse,line,polyline,path,polygon) { opacity:0!important; }" });
        await MoveToNativeTextAsync(page, Legend(0));
        Assert.Contains("Measured", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Locator(Legend(0)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        // Let layout dispatch its real pointer exit while keyboard focus stays on the legend.
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        Assert.Contains("Measured", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal(await page.Locator(Legend(0)).GetAttributeAsync("data-cfx-target-id"),
            await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        await CaptureContractAsync(page, "paint-caption-only-legend-" + layout + "-compact-dark", new { Layout = layout.ToString(), Muted = true,
            ActiveTarget = await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId || ''") });
        await page.Keyboard.PressAsync("Space");
        await page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        Assert.Equal("false", await page.Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        Assert.Contains("Measured", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    private static void ExactFeatures(HtmlChartInteractionOptions options) {
        options.Interaction.Features = ChartInteractionFeatures.Tooltips | ChartInteractionFeatures.Selection | ChartInteractionFeatures.KeyboardNavigation;
        options.Tooltip.Range = HtmlChartTooltipRange.Exact;
    }

    private static Chart Frame(bool dark) => Chart.Create().WithSize(800, 540).WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
    private static Chart AnnotationChart(bool dark) => Frame(dark).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
        .AddLine("Measured", new[] { new ChartPoint(2, 2), new ChartPoint(8, 2) });

    private static async Task MoveToNativeTextAsync(IPage page, string selector) {
        var position = await page.Locator(selector).EvaluateAsync<double[]>("n => { const t = n.querySelector('text'), b = t.getExtentOfChar(Math.floor(t.textContent.length / 2)), p = new DOMPoint(b.x + b.width / 2, b.y + b.height / 2).matrixTransform(t.getScreenCTM()); return [p.x,p.y]; }");
        Assert.True(await page.Locator(selector).EvaluateAsync<bool>("(n,p) => n.contains(document.elementFromPoint(p[0],p[1]))", position));
        await page.Mouse.MoveAsync((float)position[0], (float)position[1]);
    }

    private static async Task CaptureContractAsync(IPage page, string name, object measurement) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var state = await page.EvaluateAsync<JsonElement>("() => { const r = document.querySelector('.cfx-interactive-chart'), t = r.querySelector('.cfx-tooltip'); return { root: Object.fromEntries(Array.from(r.attributes).filter(a => a.name.startsWith('data-cfx-')).map(a => [a.name,a.value])), tooltipVisible: !t.hidden, tooltipText: t.hidden ? '' : t.innerText, rows: Array.from(t.querySelectorAll('dt')).map(n => n.textContent), selected: Array.from(r.querySelectorAll('[aria-selected=true]')).map(n => n.dataset.cfxTargetId) }; }");
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(new { measurement, state }));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
