using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed class NumericRadialBrowserTests {
    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, true, true)]
    public async Task NumericMarksRetainPointerTooltipKeyboardSelectionAndSeriesLegend(bool bars, bool dark, bool compact, bool nonzero) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(compact ? 360 : 800, compact ? 360 : 440)
            .WithTitle(bars ? "Regional requests" : "Regional workload")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithXLabels("North", "South", "East", "West").WithLegend().WithDataLabels(false)
            .WithRadialGeometry(new ChartRadialGeometryOptions(-90, 180));
        var first = new[] { new ChartPoint(1, 1200), new ChartPoint(2, 800), new ChartPoint(3, 500), new ChartPoint(4, 900) };
        var second = new[] { new ChartPoint(1, 300), new ChartPoint(2, 200), new ChartPoint(3, 100), new ChartPoint(4, 250) };
        NumericRadialSeriesTests.Add(chart, bars, "Requests", first); NumericRadialSeriesTests.Add(chart, bars, "Follow-ups", second);
        chart.Series[0].WithInteractionKey("requests"); chart.Series[1].WithInteractionKey("follow-ups");
        if (dark) foreach (var series in chart.Series) series.WithStackGroup("work");
        chart.WithYAxisBounds(nonzero ? 100 : 0, 1600);
        var mark = NumericRadialSeriesTests.Marks(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene)[0];
        var angle = mark.Start + mark.Sweep * .45; var radius = (mark.Inner + mark.Outer) / 2;
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), compact ? 390 : 880, compact ? 460 : 560);
        var page = session.Page;
        await page.EvaluateAsync("() => { window.cfxSelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.cfxSelections.push(event.detail)); }");
        var screen = await page.EvaluateAsync<double[]>("point => { const svg = document.querySelector('.cfx-interactive-chart svg'); const p = svg.createSVGPoint(); p.x=point.x; p.y=point.y; const screen=p.matrixTransform(svg.getScreenCTM()); return [screen.x,screen.y]; }",
            new { x = mark.Cx + Math.Cos(angle) * radius, y = mark.Cy + Math.Sin(angle) * radius });
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1], new MouseMoveOptions { Steps = 4 });
        Assert.Matches("1,?200", await TooltipTextAsync(page));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        var empty = await page.EvaluateAsync<double[]>("point => { const svg=document.querySelector('.cfx-stage svg'); const p=svg.createSVGPoint(); p.x=point.x; p.y=point.y; const screen=p.matrixTransform(svg.getScreenCTM()); return [screen.x,screen.y]; }", new { x = mark.Cx, y = mark.Cy });
        await page.Mouse.MoveAsync((float)empty[0], (float)empty[1]);
        Assert.Equal(string.Empty, await TooltipTextAsync(page));
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-hover-key"));
        Assert.True(await page.Locator(".cfx-crosshair").EvaluateAsync<bool>("node => node.hidden"));
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1]);
        var target = page.Locator(Point(0, 0));
        Assert.Equal("requests:0", await target.GetAttributeAsync("data-cfx-target-id"));
        await page.Mouse.ClickAsync((float)screen[0], (float)screen[1]);
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal("0", await page.EvaluateAsync<string>("() => window.cfxSelections[0].target.sourcePoint"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        await target.FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await target.GetAttributeAsync("aria-selected"));
        Assert.Equal(2, await page.EvaluateAsync<int>("() => window.cfxSelections.length"));
        await MoveAwayAsync(page); await page.Locator(Legend(1)).ClickAsync();
        Assert.Equal(1, await page.Locator("[data-cfx-role='series'][data-cfx-series='1'].cfx-series-muted").CountAsync());
        await page.Locator("[data-cfx-reset]").ClickAsync(); await MoveAwayAsync(page);
        Assert.Equal(0, await page.Locator(".cfx-series-muted").CountAsync());
        var capture = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(capture)) {
            Directory.CreateDirectory(capture);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(capture, $"numeric-radial-{(bars ? "bar" : "column")}-{(dark ? "dark" : "light")}-{(compact ? "compact" : "wide")}.png") });
        }
        AssertNoConsoleErrors(session);
    }
}
