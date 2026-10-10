using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class InteractiveTooltipAcquisitionBrowserTests {
    [Fact]
    public async Task DefaultRadiusRetains120CssPixelsWithTooltipsOnly() {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observation(false).ToInteractiveHtmlPage(options => options.Interaction.Features = ChartInteractionFeatures.Tooltips), 950, 720);
        var page = session.Page;
        await MoveToAsync(page, Point(0, 0), offsetY: 119);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await CaptureAsync(page, "acquisition-default120-tooltips-only", new { Offset = 119, Guide = false });
        await MoveToAsync(page, Point(0, 0), offsetY: 121);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(HtmlChartResponsiveLayout.Fit, 340, true, true, true)]
    [InlineData(HtmlChartResponsiveLayout.Fit, 950, false, false, true)]
    [InlineData(HtmlChartResponsiveLayout.Readable, 340, false, false, false)]
    [InlineData(HtmlChartResponsiveLayout.Readable, 950, true, true, false)]
    public async Task BoundedAcquisitionUsesCssPixelsAndDoesNotDependOnGuideOrPalette(HtmlChartResponsiveLayout layout, int width, bool dark, bool guide, bool label) {
        if (!Enabled) return;
        var chart = Observation(dark, graphite: width == 340);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.ResponsiveLayout = layout;
            options.Tooltip.Range = HtmlChartTooltipRange.WithinDistance(60);
            options.Crosshair.ShowLabel = label;
            if (!guide) options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), width, 720);
        var page = session.Page;
        await page.Locator(Point(0, 0)).EvaluateAsync("node => { const stage = node.closest('.cfx-stage'), box = node.getBoundingClientRect(), viewport = stage.getBoundingClientRect(); stage.scrollLeft += box.x + box.width / 2 - viewport.x - viewport.width / 2; }");
        await MoveToAsync(page, Point(0, 0), offsetY: 59);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        Assert.Equal(!guide, await page.Locator(".cfx-crosshair").IsHiddenAsync());
        var labelDisplay = await page.Locator(".cfx-crosshair__label").EvaluateAsync<string>("node => getComputedStyle(node).display");
        Assert.Equal(!label, labelDisplay == "none");
        var screenGeometry = await page.Locator(Point(0, 0)).EvaluateAsync<double[]>("node => { const svg = node.closest('svg'), stage = node.closest('.cfx-stage'), box = node.getBoundingClientRect(); return [svg.getBoundingClientRect().width, svg.viewBox.baseVal.width, stage.scrollLeft, box.x + box.width / 2, box.y + box.height / 2]; }");
        await CaptureAsync(page, "acquisition-radius-" + layout + "-" + width + "-" + (dark ? "dark" : "light"), new { Offset = 59, Guide = guide, Label = label, LabelDisplay = labelDisplay, ScreenGeometry = screenGeometry });
        await MoveToAsync(page, Point(0, 0), offsetY: 61);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.Equal(!guide, await page.Locator(".cfx-crosshair").IsHiddenAsync());
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ExactAndNearestRangesKeepNativeHitsKeyboardAndPinsWithIndependentGuides(bool nearest, bool guide) {
        if (!Enabled) return;
        var chart = Observation(nearest, graphite: true);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Range = nearest ? HtmlChartTooltipRange.Nearest : HtmlChartTooltipRange.Exact;
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            if (!guide) options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), 950, 720);
        var page = session.Page;
        await MoveToAsync(page, Point(0, 0));
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        await MoveToAsync(page, Point(0, 0), offsetY: 150);
        Assert.Equal(!nearest, await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        await CaptureAsync(page, "acquisition-" + (nearest ? "nearest" : "exact") + "-guide-" + guide, new { Offset = 150, Nearest = nearest, Guide = guide });
        await MoveAwayAsync(page);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Locator(Point(0, 0)).FocusAsync();
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await page.Locator(Point(0, 0)).BlurAsync();
        await MoveAwayAsync(page);
        Assert.False(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Locator("[data-cfx-reset]").ClickAsync();
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData("opacity", "0", HtmlChartTooltipMode.Single, 1)]
    [InlineData("visibility", "hidden", HtmlChartTooltipMode.SharedX, 1)]
    [InlineData("display", "none", HtmlChartTooltipMode.Single, 2)]
    [InlineData("opacity", "0", HtmlChartTooltipMode.SharedX, 0)]
    public async Task HiddenPrimaryDataCannotBecomeDirectInferredOrPinnedReadouts(string property, string value, HtmlChartTooltipMode mode, int range) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Observation(false).ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = mode;
            options.Tooltip.Range = range == 0 ? HtmlChartTooltipRange.Exact : range == 2 ? HtmlChartTooltipRange.Nearest : HtmlChartTooltipRange.WithinDistance(120);
        }), 950, 720);
        var page = session.Page;
        var target = page.Locator(Point(0, 0));
        var box = await BoxAsync(page, Point(0, 0));
        await target.EvaluateAsync("(node, style) => node.style.setProperty(style[0], style[1], 'important')", new[] { property, value });
        await page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2 + 60));
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        Assert.True(await page.Locator(".cfx-crosshair").IsHiddenAsync());
        // SVG can hit opacity-zero paint; test real pointer entry as well as inference from the stage.
        await page.Mouse.MoveAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Mouse.ClickAsync((float)(box.X + box.Width / 2), (float)(box.Y + box.Height / 2));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));
        await CaptureAsync(page, "acquisition-hidden-" + property + "-" + range, new { Property = property, Value = value, Mode = mode.ToString(), Range = range });
        await target.EvaluateAsync("(node, property) => node.style.removeProperty(property)", property);
        await MoveAwayAsync(page);
        await MoveToAsync(page, Point(0, 0), offsetY: range == 0 ? 0 : 60);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RestoredChildAndMarkerFreePaintRemainEligibleUntilTheirActualSurfaceIsHidden(bool markerFree) {
        if (!Enabled) return;
        var chart = Observation(markerFree, graphite: true);
        if (markerFree) chart = Chart.Create().WithSize(800, 540).WithTheme(ChartTheme.GraphiteDark()).WithLegend(true)
            .WithLineMarkers(ChartLineMarkerMode.None).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddLine("Reading", new[] { new ChartPoint(3, 5), new ChartPoint(5, 5), new ChartPoint(7, 5) });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Range = HtmlChartTooltipRange.Nearest;
            options.Interaction.Disable(ChartInteractionFeatures.Crosshair);
        }), 950, 720);
        var page = session.Page;
        var point = markerFree ? Point(0, 1) : Point(0, 0);
        var parent = markerFree ? "[data-cfx-role='series'][data-cfx-series='0']" : point;
        var surface = markerFree ? parent + " [data-cfx-role='line']" : point + " [data-cfx-role='marker']";
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = parent + " { visibility:hidden !important; } " + surface + " { visibility:visible !important; }" });
        await MoveToAsync(page, point, offsetY: 70);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        await CaptureAsync(page, "acquisition-restored-" + (markerFree ? "line" : "marker"), new { MarkerFree = markerFree });
        await page.Locator(surface).EvaluateAsync("node => node.style.setProperty('visibility', 'hidden', 'important')");
        await MoveToAsync(page, point, offsetY: 71);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Locator(surface).EvaluateAsync("node => node.style.setProperty('visibility', 'visible', 'important')");
        await page.Locator(Legend(0)).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await page.Locator(Legend(0)).BlurAsync();
        await MoveToAsync(page, point, offsetY: 70);
        Assert.True(await page.Locator(".cfx-tooltip").IsHiddenAsync());
        await page.Locator(Legend(0)).FocusAsync();
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        await page.Keyboard.PressAsync("Space");
        await page.Locator(Legend(0)).BlurAsync();
        await MoveToAsync(page, point, offsetY: 70);
        Assert.Contains("Reading", await TooltipTextAsync(page), StringComparison.Ordinal);
        AssertNoConsoleErrors(session);
    }

    private static Chart Observation(bool dark, bool graphite = false) => Chart.Create().WithSize(800, 540).WithLegend(true)
        .WithTheme(graphite ? dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight() : dark ? ChartTheme.Dark() : ChartTheme.Light())
        .WithXAxisBounds(0, 10).WithYAxisBounds(0, 10).AddScatter("Reading", new[] { new ChartPoint(5, 5) });

    private static async Task CaptureAsync(IPage page, string name, object observations) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(observations));
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".png") });
    }
}
