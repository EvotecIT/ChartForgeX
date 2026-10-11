using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Exercises native angular targets through pointer readouts, keyboard selection and reset.</summary>
public sealed class RadialAngularReversalBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Radar, false, false)]
    [InlineData(ChartSeriesKind.Radar, false, true)]
    [InlineData(ChartSeriesKind.Radar, true, false)]
    [InlineData(ChartSeriesKind.Radar, true, true)]
    [InlineData(ChartSeriesKind.Polar, false, false)]
    [InlineData(ChartSeriesKind.Polar, false, true)]
    [InlineData(ChartSeriesKind.Polar, true, false)]
    [InlineData(ChartSeriesKind.Polar, true, true)]
    [InlineData(ChartSeriesKind.PolarArea, false, false)]
    [InlineData(ChartSeriesKind.PolarArea, false, true)]
    [InlineData(ChartSeriesKind.PolarArea, true, false)]
    [InlineData(ChartSeriesKind.PolarArea, true, true)]
    [InlineData(ChartSeriesKind.RadialColumn, false, false)]
    [InlineData(ChartSeriesKind.RadialColumn, false, true)]
    [InlineData(ChartSeriesKind.RadialColumn, true, false)]
    [InlineData(ChartSeriesKind.RadialColumn, true, true)]
    public async Task ReversedNativeTargetsKeepSourceReadoutsAndKeyboardSelection(ChartSeriesKind kind, bool compact, bool dark) {
        if (!Enabled) return;
        var chart = kind == ChartSeriesKind.RadialColumn ? V2GalleryModels.CreateMixedRadial().ConfigureXAxis(axis => axis.WithReversal())
            : V2GalleryModels.Create(kind, "angular-reversal");
        chart.WithSize(compact ? 360 : 800, compact ? 360 : 440).WithLegend()
            .WithTitle("Reversed angular observations").WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        for (var index = 0; index < chart.Series.Count; index++) chart.Series[index].WithInteractionKey("angular-" + index);
        var series = kind == ChartSeriesKind.PolarArea ? 0 : kind == ChartSeriesKind.RadialColumn ? 2 : 1;
        var point = kind is ChartSeriesKind.PolarArea or ChartSeriesKind.RadialColumn ? 0 : 1;
        var expectedValue = chart.Series[series].Points[point].Y.ToString("R", CultureInfo.InvariantCulture);
        var sourceId = $"series-{series}-point-{point}";
        var role = kind == ChartSeriesKind.PolarArea ? "polar-area-point-source" : kind == ChartSeriesKind.Polar ? "polar-point-source" : "radar-point-source";
        var selector = $"[data-cfx-role='{role}'][data-cfx-source-id='{sourceId}']";
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), compact ? 390 : 880, compact ? 560 : 600);
        var page = session.Page; var target = page.Locator(selector);
        await page.EvaluateAsync("() => { window.angularSelections=[]; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.angularSelections.push(event.detail)); }");
        await target.ScrollIntoViewIfNeededAsync();
        // Use painted native geometry rather than a broad observation box or an adapter hit overlay.
        var painted = await target.EvaluateAsync<double[]>("node => { const box=node.getBoundingClientRect(); for(let y=1;y<20;y++) for(let x=1;x<20;x++) { const px=box.left+box.width*x/20, py=box.top+box.height*y/20; const hit=document.elementFromPoint(px,py); if(hit && hit.closest('[data-cfx-point]')===node && !hit.hasAttribute('data-cfx-browser-hit-area')) return [px,py]; } throw new Error('No native painted angular observation'); }");
        await page.Mouse.MoveAsync((float)painted[0], (float)painted[1]);
        Assert.Contains(chart.Series[series].Name, await TooltipTextAsync(page));
        Assert.Equal(expectedValue, await target.GetAttributeAsync("data-cfx-value"));
        if (kind == ChartSeriesKind.Polar)
            Assert.Equal(chart.Series[series].Points[point].X.ToString("R", CultureInfo.InvariantCulture), await target.GetAttributeAsync("data-cfx-angle"));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        await page.Mouse.ClickAsync((float)painted[0], (float)painted[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("angular-" + series + ":" + point, await target.GetAttributeAsync("data-cfx-target-id"));
        Assert.Equal(point.ToString(CultureInfo.InvariantCulture), await page.EvaluateAsync<string>("() => window.angularSelections.at(-1).target.sourcePoint"));
        Assert.Equal(expectedValue, await page.EvaluateAsync<string>("() => window.angularSelections.at(-1).target.value"));
        await CaptureAsync(page, kind, compact, dark, "pointer-selected");
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await target.GetAttributeAsync("aria-selected"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        await page.Keyboard.PressAsync("ArrowRight");
        Assert.NotEqual("angular-" + series + ":" + point, await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        await page.Locator("[data-cfx-reset]").ClickAsync(); await MoveAwayAsync(page);
        Assert.Equal(0, await page.Locator("[data-cfx-point][aria-selected='true']").CountAsync());
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        AssertNoConsoleErrors(session);
    }

    private static async Task CaptureAsync(IPage page, ChartSeriesKind kind, bool compact, bool dark, string state) {
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(capture)) return;
        Directory.CreateDirectory(capture);
        await page.ScreenshotAsync(new PageScreenshotOptions {
            Path = Path.Combine(capture, $"angular-reversal-{kind}-{(compact ? "compact" : "wide")}-{(dark ? "dark" : "light")}-{state}.png")
        });
    }
}
