using System.Globalization;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class MixedNumericRadialBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ColumnsAndRadarRetainDistinctPointerKeyboardAndLegendTargets(bool compact, bool dark) {
        if (!Enabled) return;
        var chart = V2GalleryModels.CreateMixedRadial()
            .WithSize(compact ? 360 : 800, compact ? 360 : 440)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithTitle("Regional counts and targets").WithLegend();
        chart.Series[0].WithInteractionKey("reference");
        chart.Series[1].WithInteractionKey("observed");
        chart.Series[2].WithInteractionKey("target");
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var owner = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == "series-1-point-0");
        double Fact(string key) => double.Parse(owner.Metadata[key], CultureInfo.InvariantCulture);
        var start = Fact("data-cfx-start-angle"); var sweep = Fact("data-cfx-sweep-angle");
        var inner = Fact("data-cfx-inner-radius"); var outer = Fact("data-cfx-outer-radius");
        var bounds = prepared.Regions.Single(region => region.Id == owner.Id).Bounds;
        var relative = ChartSlicePathGeometry.Bounds(0, 0, outer, inner, start, sweep);
        var cx = bounds.Left - relative.Left; var cy = bounds.Top - relative.Top;
        var angle = start + sweep * .45;
        var radius = (inner + outer) / 2;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), compact ? 390 : 880, compact ? 520 : 560);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.mixedSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.mixedSelections.push(event.detail)); }");
        var columnPosition = await ScreenAsync(page, cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius);
        await page.Mouse.MoveAsync((float)columnPosition[0], (float)columnPosition[1], new MouseMoveOptions { Steps = 4 });
        Assert.Matches("1,?200", await TooltipTextAsync(page));
        var column = page.Locator(Point(1, 0));
        Assert.Equal("observed:0", await column.GetAttributeAsync("data-cfx-target-id"));
        await page.Mouse.ClickAsync((float)columnPosition[0], (float)columnPosition[1]);
        Assert.Equal("true", await column.GetAttributeAsync("aria-selected"));
        Assert.Equal("observed", await page.EvaluateAsync<string>("() => window.mixedSelections.at(-1).target.seriesKey"));
        Assert.Equal("0", await page.EvaluateAsync<string>("() => window.mixedSelections.at(-1).target.sourcePoint"));
        // Activating the same observation releases its pinned readout before the next pointer acquisition.
        await column.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await column.GetAttributeAsync("aria-selected"));

        const string targetSelector = "[data-cfx-role='radar-point-source'][data-cfx-source-id='series-2-point-0']";
        var target = page.Locator(targetSelector);
        await target.ScrollIntoViewIfNeededAsync();
        await MoveToAsync(page, targetSelector);
        Assert.Contains("Target", await TooltipTextAsync(page));
        Assert.Matches("1,?400", await TooltipTextAsync(page));
        Assert.Matches("^Target, North: 1,?400$", await page.Locator(".cfx-tooltip__title").InnerTextAsync());
        await CaptureAsync(page, compact, dark, "target-hover");
        Assert.Equal("target:0", await target.GetAttributeAsync("data-cfx-target-id"));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        await target.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("target", await page.EvaluateAsync<string>("() => window.mixedSelections.at(-1).target.seriesKey"));
        Assert.Equal("0", await page.EvaluateAsync<string>("() => window.mixedSelections.at(-1).target.sourcePoint"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("point", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetKind"));
        Assert.NotEqual("target:0", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));

        await MoveAwayAsync(page);
        await page.Locator(Legend(0)).ClickAsync();
        Assert.Equal("true", await page.Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        Assert.NotEqual("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        Assert.NotEqual("true", await page.Locator(Legend(2)).GetAttributeAsync("data-cfx-muted"));
        Assert.False(await target.EvaluateAsync<bool>("node => !!node.closest('.cfx-series-muted')"));
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await CaptureAsync(page, compact, dark, "reference-muted");
        await page.Locator(Legend(1)).ClickAsync();
        Assert.Equal("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        Assert.NotEqual("true", await page.Locator(Legend(2)).GetAttributeAsync("data-cfx-muted"));
        Assert.False(await target.EvaluateAsync<bool>("node => !!node.closest('.cfx-series-muted')"));
        await page.Locator("[data-cfx-reset]").ClickAsync();
        await page.EvaluateAsync("() => document.activeElement.blur()");
        await MoveAwayAsync(page);
        var hole = await ScreenAsync(page, cx, cy);
        await page.Mouse.MoveAsync((float)hole[0], (float)hole[1], new MouseMoveOptions { Steps = 4 });
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        Assert.Equal(0, await page.Locator(".cfx-series-muted").CountAsync());
        await CaptureAsync(page, compact, dark, "hole-clear");
        AssertNoConsoleErrors(session);
    }

    private static Task<double[]> ScreenAsync(IPage page, double x, double y) =>
        page.EvaluateAsync<double[]>("point => { const svg=document.querySelector('.cfx-stage svg'); const p=svg.createSVGPoint(); p.x=point.x; p.y=point.y; const screen=p.matrixTransform(svg.getScreenCTM()); return [screen.x,screen.y]; }", new { x, y });

    private static async Task CaptureAsync(IPage page, bool compact, bool dark, string state) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await page.ScreenshotAsync(new PageScreenshotOptions {
            Path = Path.Combine(capture, $"mixed-radial-{(compact ? "compact" : "wide")}-{(dark ? "dark" : "light")}-{state}.png")
        });
    }
}
