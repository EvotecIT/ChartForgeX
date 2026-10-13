using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Checks that key presentation leaves raw observations and browser target identities intact.</summary>
public sealed class InteractiveColorLegendPresentationBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.TileMap, 360, true, ChartColorLegendStyle.Gradient)]
    [InlineData(ChartSeriesKind.Scatter, 800, false, ChartColorLegendStyle.Gradient)]
    [InlineData(ChartSeriesKind.Heatmap, 360, false, ChartColorLegendStyle.Swatches)]
    public async Task NumericKeyStylePreservesPointerReadoutsKeyboardSelectionAndRawFacts(
        ChartSeriesKind kind, int width, bool dark, ChartColorLegendStyle style) {
        if (!Enabled) return;
        var chart = ColorDimensionPresentationFixtures.Create(kind, width: width, dark: dark);
        var dimension = ColorDimensionPresentationFixtures.Options(chart, kind);
        dimension.Name = "Temperature"; dimension.ValueFormat = ChartValueFormat.Number("0.000 '°C'");
        dimension.LegendStyle = style;
        if (kind == ChartSeriesKind.TileMap) chart.WithMapScaleLegendPosition(ChartMapScaleLegendPosition.Right);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single), width + 24, 600);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var prefix = kind == ChartSeriesKind.TileMap ? "map-scale" : kind == ChartSeriesKind.Heatmap ? "heatmap-scale" : "color-scale-0";
        Assert.Equal(style.ToString().ToLowerInvariant(), await page.Locator("[data-cfx-role='" + prefix + "']").GetAttributeAsync("data-cfx-legend-style"));
        if (style == ChartColorLegendStyle.Gradient) {
            var gradient = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGradient>());
            Assert.Equal(gradient.Stops.Count, await page.Locator("linearGradient stop").CountAsync());
        } else Assert.Equal(0, await page.Locator("linearGradient").CountAsync());
        var measured = Target(false); var zero = Target(true); var sameValue = kind != ChartSeriesKind.Scatter;
        Assert.Equal("7.125", await page.Locator(measured).GetAttributeAsync(sameValue ? "data-cfx-value" : "data-cfx-color-value"));
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.legendSelection = event.detail.target)");
        await MoveToAsync(page, measured); await AssertTemperatureAsync("7.125 °C");
        await CaptureAsync("pointer");
        await MoveAwayAsync(page); await page.Locator(zero).FocusAsync(); await AssertTemperatureAsync("0.000 °C");
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.legendSelection");
        Assert.Equal(kind == ChartSeriesKind.TileMap ? "region" : "point", selected.GetProperty("targetKind").GetString());
        Assert.Equal(kind == ChartSeriesKind.TileMap ? "TX" : "samples:1", selected.GetProperty("targetId").GetString());
        Assert.Equal(sameValue ? "0" : "30", selected.GetProperty("value").GetString());
        Assert.Equal("true", await page.Locator(zero).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        await CaptureAsync("zero-selected");
        await page.Keyboard.PressAsync("Space");
        Assert.Null(await page.Locator(".cfx-interactive-chart").GetAttributeAsync("data-cfx-tooltip-pinned"));
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);

        string Target(bool isZero) => kind == ChartSeriesKind.TileMap ? "[data-cfx-role='tile-map-region-source'][data-cfx-region='" + (isZero ? "TX" : "CA") + "']"
            : kind == ChartSeriesKind.Heatmap ? "[data-cfx-role='heatmap-cell'][data-cfx-column='" + (isZero ? "1" : "0") + "']" : Point(0, isZero ? 1 : 0);

        Task AssertTemperatureAsync(string value) => page.WaitForFunctionAsync("value => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && Array.from(tip.querySelectorAll('dt')).some(term => term.textContent === 'Temperature' && term.nextElementSibling.textContent === value); }", value);

        async Task CaptureAsync(string stage) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            var file = Path.Combine(directory, "legend-" + kind + "-" + width + "-" + (dark ? "dark" : "light") + "-" + style.ToString().ToLowerInvariant());
            if (stage == "pointer") { await File.WriteAllTextAsync(file + ".svg", prepared.ToSvg()); await File.WriteAllBytesAsync(file + ".png", prepared.ToPng()); }
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = file + "-" + stage + ".browser.png" });
            var state = await page.EvaluateAsync<JsonElement>("() => { const root = document.querySelector('.cfx-interactive-chart'); return { Focus: document.activeElement.dataset.cfxTargetId, Pinned: root.dataset.cfxTooltipPinned, Selected: window.legendSelection, Readout: root.querySelector('.cfx-tooltip').innerText }; }");
            await File.WriteAllTextAsync(file + "-" + stage + ".runtime.json", JsonSerializer.Serialize(new {
                State = state, Errors = errors, ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
