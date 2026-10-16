using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Numeric size keys remain descriptive while native Bubble readouts and stable observation commands stay usable.</summary>
public sealed class BubbleSizeKeyBrowserTests {
    [BrowserTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task IndependentSizeReadoutsAndDescriptiveKeysSurviveAdaptiveHosting(bool dark, bool compact) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = Chart.Create().WithTitle("Processing time and request volume")
            .WithSize(compact ? 340 : 900, 560).WithTheme(VisualTheme.Graphite(), mode)
            .WithXAxis("Workload batches").WithYAxis("Processing time")
            .WithXAxisBounds(0, 5).WithYAxisBounds(0, 50).WithLegend().WithDataLabels(false)
            .WithValueFormat(ChartValueFormat.Number("0.0 'ms'"));
        var series = ChartSeries.CreateBubble(new ChartSeriesId("requests"), "Recorded requests", new[] {
            new ChartBubble(1, 18, 10).WithColorValue(-5), new ChartBubble(2.5, 30, 50).WithColorValue(5),
            new ChartBubble(4, 24, 200).WithColorValue(15)
        }).WithObservationId(1, new ChartObservationId("batch-two"));
        series.WithColorScale(ChartColorScale.Sequential(ChartColor.FromHex("#3388cc"), ChartColor.FromHex("#d65e2c")).WithValueRange(-5, 15))
            .ConfigureColorDimension(options => { options.Name = "Temperature"; options.ValueFormat = ChartValueFormat.Number("0 '°C'"); });
        chart.AddSeries(series).ConfigureBubble(options => {
            options.WithSizeDomain(0, 100); options.MinimumRadius = 3; options.MaximumRadius = 18;
            options.SizeDimension.Name = "Requests"; options.SizeDimension.ValueFormat = ChartValueFormat.Number("0 'req'");
            options.SizeDimension.ShowLegend = true;
        });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code is "cartesian.size-scale-overflow" or "cartesian.color-scale-overflow");
        var html = chart.ToInteractiveHtmlPage(options => {
            options.Tooltip.Mode = HtmlChartTooltipMode.Single;
            options.Tooltip.PinOnSelect = false;
            options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive;
            options.Adaptive.NarrowWidth = 340; options.Adaptive.NarrowHeight = 560;
        });
        await using var session = await OpenAsync(html, compact ? 390 : 1440, 900);
        var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        Assert.Equal(1, await page.Locator("[data-cfx-role='bubble-size-key']").CountAsync());
        Assert.Equal(0, await page.Locator("[data-cfx-role='bubble-size-key'] [data-cfx-target-kind], [data-cfx-role='bubble-size-key'][data-cfx-target-kind]").CountAsync());
        Assert.Equal("Requests", await page.Locator("[data-cfx-role='bubble-size-key-title']").InnerTextAsync());
        var target = page.Locator(Point(0, 1));
        await MoveToAsync(page, Point(0, 1));
        Assert.Equal("50 req", await Row(page, "Requests"));
        Assert.Equal("5 °C", await Row(page, "Temperature"));
        Assert.Equal("30", await target.GetAttributeAsync("data-cfx-y"));
        Assert.Equal("50", await target.GetAttributeAsync("data-cfx-size"));
        await MoveAwayAsync(page); await FocusWithKeyboardAsync(page, target);
        Assert.Equal("50 req", await Row(page, "Requests"));
        Assert.Equal("5 °C", await Row(page, "Temperature"));
        await page.EvaluateAsync("() => { window.bubbleKeySelections = []; document.querySelector('.cfx-interactive-chart').addEventListener('cfxselectionchange', event => window.bubbleKeySelections.push(event.detail)); }");
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.bubbleKeySelections.at(-1).targets");
        var identity = Assert.Single(selected.EnumerateArray());
        Assert.Equal("requests", identity.GetProperty("seriesKey").GetString());
        Assert.Contains("batch-two", identity.GetProperty("observationIds").EnumerateArray().Select(value => value.GetString()));
        var keyRadii = await page.Locator("[data-cfx-role='bubble-size-key-sample']").EvaluateAllAsync<double[]>("nodes => nodes.map(node => Number(node.dataset.cfxRadius))");
        Assert.Contains(18, keyRadii); Assert.Equal("18", await page.Locator(Point(0, 2) + " [data-cfx-role='bubble']").GetAttributeAsync("rx"));
        var keyBounds = await page.Locator("[data-cfx-role='bubble-size-key']").EvaluateAsync<double[]>("node => { const r=node.getBoundingClientRect(); return [r.left,r.right,r.top,r.bottom]; }");
        Assert.True(keyBounds[0] >= 0 && keyBounds[1] <= (compact ? 390 : 1440));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await Capture(page, prepared, dark, compact, "initial", selected);
        await page.SetViewportSizeAsync(compact ? 1440 : 390, 900);
        await page.WaitForFunctionAsync("() => document.querySelector('.cfx-interactive-chart .cfx-stage svg').getAttribute('viewBox').split(' ')[2] === '340' || innerWidth > 640");
        Assert.Equal(1, await page.EvaluateAsync<int>("() => ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).getSelection().length"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.bubbleKeySelections.length"));
        Assert.Equal(1, await page.Locator("[data-cfx-role='bubble-size-key']").CountAsync());
        await page.EvaluateAsync("() => ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).clearSelection()");
        Assert.Equal(0, await page.EvaluateAsync<int>("() => ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')).getSelection().length"));
        Assert.Equal(1, await page.EvaluateAsync<int>("() => window.bubbleKeySelections.length"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Task<string> Row(IPage page, string name) => page.Locator(".cfx-tooltip dt")
        .Filter(new LocatorFilterOptions { HasText = name }).Locator("+ dd").InnerTextAsync();

    private static async Task Capture(IPage page, PreparedVisual prepared, bool dark, bool compact, string state, JsonElement selection) {
        var directory = Environment.GetEnvironmentVariable("CFX_BUBBLE_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var name = "size-key-" + (dark ? "dark" : "light") + "-" + (compact ? "390" : "1440") + "-" + state;
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
        await File.WriteAllBytesAsync(Path.Combine(directory, name + ".png"), prepared.ToPng());
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png") });
        await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(new {
            selection, readout = await TooltipTextAsync(page), sizeKey = await page.Locator("[data-cfx-role='bubble-size-key']").GetAttributeAsync("aria-label")
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
