using System.Globalization;
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

/// <summary>Protects independent raw-color readouts and normal point identity alongside Bubble size and series controls.</summary>
public sealed class InteractiveScatterBubbleColorScaleBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.Scatter, 360, false)]
    [InlineData(ChartSeriesKind.Scatter, 360, true)]
    [InlineData(ChartSeriesKind.Scatter, 800, false)]
    [InlineData(ChartSeriesKind.Scatter, 800, true)]
    [InlineData(ChartSeriesKind.Bubble, 360, false)]
    [InlineData(ChartSeriesKind.Bubble, 360, true)]
    [InlineData(ChartSeriesKind.Bubble, 800, false)]
    [InlineData(ChartSeriesKind.Bubble, 800, true)]
    public async Task PointerKeyboardAndSelectionRetainColorYSizeAndSourceIdentity(ChartSeriesKind kind, int width, bool dark) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, 400).WithLegend().WithDataLabels(false)
            .WithTitle("Independent measured color").WithXAxis("Batch").WithYAxis("Energy (kWh)")
            .WithXAxisBounds(0, 4).WithYAxisBounds(0, 50)
            .WithValueFormat(ChartValueFormat.Number("0.0 'kWh'", CultureInfo.InvariantCulture))
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .ConfigureLabels(labels => { labels.Color = "Temperature (°C)"; labels.Size = "Volume (m³)"; labels.NoData = "Not sampled"; });
        var x = new[] { 1d, 2, 3 }; var y = new[] { 18d, 30, 24 }; var sizes = new[] { 9d, 36, 81 };
        var colors = new double?[] { -12, 0, null };
        if (kind == ChartSeriesKind.Bubble) chart.AddSeries(ChartSeries.CreateBubble(new ChartSeriesId("thermal-source"), "Samples", Enumerable.Range(0, 3)
            .Select(index => new ChartBubble(x[index], y[index], sizes[index]).WithColorValue(colors[index]))))
            .ConfigureBubble(bubble => { bubble.WithSizeDomain(0, 100); bubble.MinimumRadius = 4; bubble.MaximumRadius = 20; });
        else chart.AddSeries(ChartSeries.CreateScatter(new ChartSeriesId("thermal-source"), "Samples", Enumerable.Range(0, 3)
            .Select(index => new ChartPoint(x[index], y[index]).WithColorValue(colors[index]))));
        chart.Series[0].WithColorScale(ChartColorScale.Sequential(
            ChartColor.FromHex("#2166AC"), ChartColor.FromHex("#B2182B"))
            .WithValueRange(-10, 10).WithNoDataColor(ChartColor.FromHex("#9CA3AF")));
        chart.AddSeries(ChartSeries.CreateLine(new ChartSeriesId("reference-source"), "Reference", new[] { new ChartPoint(.5, 42), new ChartPoint(3.5, 42) }));
        chart.Series[1].ConfigureMarkers(markers => markers.Enabled = false);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single);
        await using var session = await OpenAsync(html, width + 24, 560);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var root = page.Locator(".cfx-interactive-chart");
        Assert.Equal(3, await page.Locator("g[data-cfx-role='point'][data-cfx-series='0']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-cfx-role='color-scale-source'][data-cfx-series='0']").CountAsync());
        Assert.Equal(1, await page.Locator(Legend(0)).CountAsync()); Assert.Equal(1, await page.Locator(Legend(1)).CountAsync());
        for (var index = 0; index < 3; index++) {
            var point = page.Locator(Point(0, index));
            Assert.Equal(x[index].ToString(CultureInfo.InvariantCulture), await point.GetAttributeAsync("data-cfx-x"));
            Assert.Equal(y[index].ToString(CultureInfo.InvariantCulture), await point.GetAttributeAsync("data-cfx-y"));
            Assert.Equal(kind == ChartSeriesKind.Bubble ? sizes[index].ToString(CultureInfo.InvariantCulture) : null,
                await point.GetAttributeAsync("data-cfx-size"));
            Assert.Equal(kind == ChartSeriesKind.Bubble ? "Volume (m³)" : null, await point.GetAttributeAsync("data-cfx-label-size"));
            Assert.Equal(colors[index]?.ToString(CultureInfo.InvariantCulture), await point.GetAttributeAsync("data-cfx-color-value"));
            Assert.Equal(index == 2 ? "true" : null, await point.GetAttributeAsync("data-cfx-color-missing"));
            Assert.Equal(index.ToString(CultureInfo.InvariantCulture), await point.GetAttributeAsync("data-cfx-source-point"));
            Assert.Equal("thermal-source:" + index, await point.GetAttributeAsync("data-cfx-target-id"));
        }
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.colorSelection = event.detail.target)");
        await MoveToAsync(page, Point(0, 0));
        await AssertRowAsync("Temperature (°C)", "-12"); await AssertRowAsync("Y", "18");
        if (kind == ChartSeriesKind.Bubble) await AssertRowAsync("Volume (m³)", "9");
        await CaptureAsync("negative-pointer");
        await MoveAwayAsync(page); await page.Locator(Point(0, 1)).FocusAsync();
        await AssertRowAsync("Temperature (°C)", "0"); await AssertRowAsync("Y", "30");
        if (kind == ChartSeriesKind.Bubble) await AssertRowAsync("Volume (m³)", "36");
        await CaptureAsync("zero-focus");
        await page.Keyboard.PressAsync("ArrowRight");
        var missing = page.Locator(Point(0, 2));
        Assert.True(await missing.EvaluateAsync<bool>("node => node === document.activeElement"));
        await AssertRowAsync("Temperature (°C)", "Not sampled"); await AssertRowAsync("Y", "24");
        if (kind == ChartSeriesKind.Bubble) await AssertRowAsync("Volume (m³)", "81");
        Assert.Null(await missing.GetAttributeAsync("data-cfx-color-value"));
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.colorSelection");
        Assert.Equal("point", selected.GetProperty("targetKind").GetString());
        Assert.Equal("thermal-source:2", selected.GetProperty("targetId").GetString());
        Assert.Equal("thermal-source", selected.GetProperty("seriesKey").GetString());
        Assert.Equal("2", selected.GetProperty("point").GetString()); Assert.Equal("2", selected.GetProperty("sourcePoint").GetString());
        Assert.Equal("24", selected.GetProperty("value").GetString());
        Assert.Equal("true", await missing.GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        if (kind == ChartSeriesKind.Bubble) await AssertRowAsync("Volume (m³)", "81");
        await CaptureAsync("missing-selected");
        await page.Keyboard.PressAsync("Space");
        Assert.Null(await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        await MoveAwayAsync(page); await page.Locator(Legend(1)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        Assert.NotEqual("true", await page.Locator(Legend(0)).GetAttributeAsync("data-cfx-muted"));
        Assert.False(await missing.EvaluateAsync<bool>("node => !!node.closest('.cfx-series-muted')"));
        Assert.Equal(1, await page.Locator("[data-cfx-role='color-scale-source'][data-cfx-series='0']").CountAsync());
        await CaptureAsync("reference-muted");
        await page.Keyboard.PressAsync("Space"); Assert.Equal("false", await page.Locator(Legend(1)).GetAttributeAsync("data-cfx-muted"));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);

        Task AssertRowAsync(string name, string value) => page.WaitForFunctionAsync("expected => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && Array.from(tip.querySelectorAll('dt')).some(term => term.textContent === expected.name && term.nextElementSibling.textContent === expected.value); }", new { name, value });

        async Task CaptureAsync(string stage) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            var name = "scatter-bubble-color-" + kind + "-" + width + "-" + (dark ? "dark" : "light");
            if (stage == "negative-pointer") {
                await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
                await File.WriteAllBytesAsync(Path.Combine(directory, name + ".png"), prepared.ToPng());
            }
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + "-" + stage + ".browser.png") });
            var state = await page.EvaluateAsync<JsonElement>("""
                () => { const root = document.querySelector('.cfx-interactive-chart'); const tip = root.querySelector('.cfx-tooltip');
                    return { Focus: document.activeElement.dataset.cfxTargetId, Pinned: root.dataset.cfxTooltipPinned,
                        Readout: tip && !tip.hidden ? tip.innerText : '', Selected: window.colorSelection,
                        Points: Array.from(root.querySelectorAll('g[data-cfx-role="point"][data-cfx-series="0"]'), node => ({...node.dataset})),
                        Legends: Array.from(root.querySelectorAll('[data-cfx-role="legend-item"]'), node => ({...node.dataset})) }; }
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, name + "-" + stage + ".runtime.json"), JsonSerializer.Serialize(new {
                State = state, Errors = errors, ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
