using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class StandaloneViewportBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.Area)]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Candlestick)]
    public async Task ReadoutUsesAxisEdgeLabelsAndKeyboardHideRestoresVisibleSeries(ChartSeriesKind kind) {
        if (!Enabled) return;
        await using var session = await OpenAsync(Page(kind, reference: true), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        await MoveToAsync(page, Sample);
        await page.WaitForFunctionAsync("()=>document.querySelector('.cfx-crosshair__axis-value[data-cfx-axis-value=x]')?.textContent==='4'");
        Assert.True(await page.Locator(".cfx-tooltip").IsVisibleAsync());
        Assert.Equal("4", await page.Locator("[data-cfx-axis-value=x].cfx-crosshair__axis-value").InnerTextAsync());
        Assert.False(string.IsNullOrWhiteSpace(await page.Locator("[data-cfx-axis-value=y].cfx-crosshair__axis-value").InnerTextAsync()));
        Assert.DoesNotContain("Series\n", await TooltipTextAsync(page));
        var legend = page.Locator("[data-cfx-role=legend-item][data-cfx-series-key=reference]");
        Assert.Equal("button", await legend.GetAttributeAsync("role"));
        Assert.Equal("true", await legend.GetAttributeAsync("aria-pressed"));
        await legend.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("false", await legend.GetAttributeAsync("aria-pressed"));
        await page.WaitForFunctionAsync("()=>getComputedStyle(document.querySelector('[data-cfx-role=series][data-cfx-series-key=samples]')).opacity==='1'");
        await CaptureAsync(page, "ux-keyboard-hide-" + kind);
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Area)]
    public async Task CategoryAcquisitionUsesHorizontalDistanceAcrossTheWholePlot(ChartSeriesKind kind) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite(), VisualThemeMode.Light)
            .WithXLabels("Monday", "Tuesday", "Wednesday").WithDataLabels(false)
            .AddSeries(new ChartSeries(new ChartSeriesId("week"), "Requests", kind, ChartPoints.FromValues(0, 100, 0)));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single), 980, 800);
        var page = session.Page;
        var first = await BoxAsync(page, "[data-cfx-point='0'][data-cfx-series='0']");
        var second = await BoxAsync(page, "[data-cfx-point='1'][data-cfx-series='0']");
        await page.Mouse.MoveAsync(second.X + second.Width / 2, first.Y + first.Height / 2 - 15);
        Assert.Contains("Tuesday", await TooltipTextAsync(page));
        Assert.Contains("100", await TooltipTextAsync(page));
        var colour = VisualTheme.Graphite().Resolve(VisualThemeMode.Light).Palette[0];
        Assert.Equal($"rgb({colour.R}, {colour.G}, {colour.B})",
            await page.Locator(".cfx-tooltip__swatch").First.EvaluateAsync<string>("node=>getComputedStyle(node).backgroundColor"));
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PaintedBarInteriorWinsOverAnOverlaidLine(bool tooltips) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite(), VisualThemeMode.Light)
            .WithXLabels("Monday", "Tuesday", "Wednesday").WithDataLabels(false)
            .AddBar("Capacity", ChartPoints.FromValues(90, 90, 90))
            .AddLine("Requests", ChartPoints.FromValues(88, 88, 88));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => {
            if (!tooltips) options.Interaction.Disable(ChartInteractionFeatures.Tooltips);
        }), 980, 800);
        var page = session.Page;
        await MountAsync(page);
        var line = await BoxAsync(page, "[data-cfx-point='1'][data-cfx-series='1']");
        await page.Mouse.MoveAsync(line.X + line.Width / 2, line.Y + line.Height / 2 + 3);
        Assert.Equal("1", await page.Locator("[data-cfx-point='1'][data-cfx-series='0']").EvaluateAsync<string>("node=>node.classList.contains('cfx-hovered')?'1':'0'"));
        await page.Mouse.ClickAsync(line.X + line.Width / 2, line.Y + line.Height / 2 + 3);
        Assert.Equal("Capacity", await page.EvaluateAsync<string>("()=>controller.getSelection()[0].label==='Tuesday'?document.querySelector('.cfx-selected').dataset.cfxSeriesName:''"));
        Assert.Equal("0", await page.EvaluateAsync<string>("()=>controller.getSelection()[0].series"));
        AssertNoConsoleErrors(session);
    }

    [BrowserFact]
    public async Task HorizontalCrosshairPutsTheValueOnXAndCategoryOnY() {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(640, 360).WithXAxisBounds(0, 10).WithYAxisBounds(.5, 3.5)
            .AddSeries(new ChartSeries(new ChartSeriesId("requests"), "Requests", ChartSeriesKind.HorizontalBar, ChartPoints.FromValues(2, 7, 4)));
        chart.Options.YAxis.Labels.AddRange(new[] { new ChartAxisLabel(1, "Monday"), new ChartAxisLabel(2, "Tuesday"), new ChartAxisLabel(3, "Wednesday") });
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), 980, 800);
        var page = session.Page;
        await MoveToAsync(page, "[data-cfx-point='1'][data-cfx-series='0']");
        Assert.Equal("7", await page.Locator(".cfx-crosshair__axis-value[data-cfx-axis-value=x]").InnerTextAsync());
        Assert.Equal("Tuesday", await page.Locator(".cfx-crosshair__axis-value[data-cfx-axis-value=y]").InnerTextAsync());
        AssertNoConsoleErrors(session);
    }

    [BrowserTheory]
    [InlineData(false, 980)]
    [InlineData(true, 375)]
    public async Task RadarFillResolvesASpokeAndShowsEveryVisibleSeriesValue(bool dark, int width) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(800, 440).WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithXLabels("Speed", "Coverage", "Capacity", "Quality", "Recovery", "Support").WithDataLabels(false)
            .AddSeries(new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.Radar, ChartPoints.FromValues(27, 34, 41, 48, 26, 33)))
            .AddSeries(new ChartSeries(new ChartSeriesId("expected"), "Expected", ChartSeriesKind.Radar, ChartPoints.FromValues(39, 48, 55, 61, 40, 46)));
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width, 812);
        var page = session.Page;
        var screen = await page.Locator("[data-cfx-role=radar-spoke]").Nth(1).EvaluateAsync<double[]>("node=>{const svg=node.ownerSVGElement,box=svg.getBoundingClientRect(),v=svg.viewBox.baseVal,x=(+node.getAttribute('x1') + +node.getAttribute('x2'))/2,y=(+node.getAttribute('y1') + +node.getAttribute('y2'))/2;return[box.x+(x-v.x)*box.width/v.width,box.y+(y-v.y)*box.height/v.height]}");
        await page.Mouse.MoveAsync((float)screen[0], (float)screen[1]);
        var tip = await TooltipTextAsync(page);
        Assert.Contains("Coverage", tip);
        Assert.Contains("Observed", tip); Assert.Contains("34", tip);
        Assert.Contains("Expected", tip); Assert.Contains("48", tip);
        Assert.DoesNotContain("Series", tip);
        for (var index = 0; index < 2; index++) {
            var colour = await page.Locator("[data-cfx-series='" + index + "'] [data-cfx-role=radar-outline]").First
                .EvaluateAsync<string>("node=>getComputedStyle(node).stroke");
            Assert.Equal(colour, await page.Locator("[data-cfx-tooltip-series='" + index + "'] .cfx-tooltip__swatch")
                .EvaluateAsync<string>("node=>getComputedStyle(node).backgroundColor"));
        }
        await CaptureAsync(page, "ux-radar-" + width + "-" + dark);
        AssertNoConsoleErrors(session);
    }
}
