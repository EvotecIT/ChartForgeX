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

/// <summary>Protects authored anchors, actual paint and raw interaction facts through three numeric-color consumers.</summary>
public sealed class InteractivePositionedColorScaleBrowserTests {
    [BrowserTheory]
    [InlineData(ChartSeriesKind.TileMap, 360, false)]
    [InlineData(ChartSeriesKind.TileMap, 800, true)]
    [InlineData(ChartSeriesKind.Heatmap, 360, false)]
    [InlineData(ChartSeriesKind.Heatmap, 800, true)]
    [InlineData(ChartSeriesKind.Scatter, 360, false)]
    [InlineData(ChartSeriesKind.Scatter, 800, true)]
    public async Task PositionedPaintAndReadoutsRetainAuthoredValuesAndSourceIdentity(ChartSeriesKind kind, int width, bool dark) {
        if (!Enabled) return;
        var mode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light;
        var chart = V2GalleryModels.Create(kind, width == 360 ? "compact-positioned-color-scale" : "positioned-color-scale", mode)
            .WithSize(width, width == 360 ? 360 : 440).WithTitle(V2GalleryModels.Title(kind))
            .WithSubtitle("Absolute color anchors use uneven numeric intervals")
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight());
        var scale = kind == ChartSeriesKind.TileMap ? chart.Options.MapColorScale!
            : kind == ChartSeriesKind.Heatmap ? chart.Options.HeatmapColorScale! : chart.Series[0].ColorScale!;
        var prefix = kind == ChartSeriesKind.TileMap ? "map-scale" : kind == ChartSeriesKind.Heatmap ? "heatmap-scale" : "color-scale-0";
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = HtmlChartTooltipMode.Single), width + 24, 600);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var root = page.Locator(".cfx-interactive-chart");
        var stops = page.Locator("[data-cfx-role='" + prefix + "-stops']");
        Assert.Equal(1, await stops.CountAsync());
        Assert.Equal("absolute", await stops.GetAttributeAsync("data-cfx-positioning"));
        Assert.Equal("5", await stops.GetAttributeAsync("data-cfx-stop-count"));
        Assert.Equal(5, await page.Locator("[data-cfx-role='" + prefix + "-stop-source']").CountAsync());
        for (var index = 0; index < scale.Stops.Count; index++) {
            var stop = page.Locator("[data-cfx-role='" + prefix + "-stop-source'][data-cfx-stop='" + index + "']");
            Assert.Equal(prefix + "-stop-" + index, await stop.GetAttributeAsync("data-cfx-source-id"));
            Assert.Equal(Number(scale.Stops[index].Value), await stop.GetAttributeAsync("data-cfx-value"));
            Assert.Equal(scale.Stops[index].Color.ToHexRgba(), await stop.GetAttributeAsync("data-cfx-color"));
            Assert.Equal(index == 2 ? "true" : null, await stop.GetAttributeAsync("data-cfx-midpoint"));
            Assert.Contains(scale.Stops[index].Color.ToHexRgba(), await stop.GetAttributeAsync("aria-label"));
        }
        var clamped = Target(55); var zero = Target(0); var interpolated = Target(2.5);
        foreach (var value in kind == ChartSeriesKind.TileMap ? new[] { 0d, 2.5, 55 } : new[] { -30d, 0, 2.5, 55 }) {
            var fill = await page.Locator(Target(value)).EvaluateAsync<string>("node => getComputedStyle(node.querySelector('[data-cfx-role=\"tile-map-region\"], [data-cfx-role=\"heatmap-cell-shape\"], ellipse')).fill");
            var color = scale.ColorFor(value);
            await AssertCssColorsEqualAsync(page, FormattableString.Invariant($"rgb({color.R}, {color.G}, {color.B})"), fill);
        }
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.positionedSelection = event.detail.target)");
        await MoveToAsync(page, clamped);
        await AssertRowAsync(kind == ChartSeriesKind.Scatter ? "Temperature (°C)" : "Value", "55");
        if (kind == ChartSeriesKind.Scatter) await AssertRowAsync("Y", "35");
        await CaptureAsync("clamped-pointer");
        if (kind != ChartSeriesKind.TileMap) {
            await MoveAwayAsync(page); await page.Locator(Target(-30)).FocusAsync();
            await AssertRowAsync(kind == ChartSeriesKind.Scatter ? "Temperature (°C)" : "Value", "-30");
        }
        await MoveAwayAsync(page); await page.Locator(zero).FocusAsync();
        await AssertRowAsync(kind == ChartSeriesKind.Scatter ? "Temperature (°C)" : "Value", "0");
        if (kind == ChartSeriesKind.Scatter) await AssertRowAsync("Y", "20");
        await MoveAwayAsync(page); await page.Locator(interpolated).FocusAsync();
        await AssertRowAsync(kind == ChartSeriesKind.Scatter ? "Temperature (°C)" : "Value", "2.5");
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.positionedSelection");
        Assert.Equal(kind == ChartSeriesKind.TileMap ? "region" : "point", selected.GetProperty("targetKind").GetString());
        Assert.Equal(kind == ChartSeriesKind.TileMap ? "TX" : kind == ChartSeriesKind.Heatmap ? "primary-measurements:4" : "recorded-batches:4",
            selected.GetProperty("targetId").GetString());
        Assert.Equal(kind == ChartSeriesKind.Scatter ? "32" : "2.5", selected.GetProperty("value").GetString());
        if (kind != ChartSeriesKind.TileMap) Assert.Equal("4", selected.GetProperty("sourcePoint").GetString());
        Assert.Equal("true", await page.Locator(interpolated).GetAttributeAsync("aria-selected"));
        Assert.Equal("true", await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        await AssertRowAsync(kind == ChartSeriesKind.Scatter ? "Temperature (°C)" : "Value", "2.5");
        await CaptureAsync("interpolated-selected");
        await page.Keyboard.PressAsync("Space");
        Assert.Null(await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        if (kind == ChartSeriesKind.Heatmap) {
            Assert.Equal(0, await page.Locator("[data-cfx-role='heatmap-cell'][data-cfx-row='1'][data-cfx-column='4']").CountAsync());
        } else {
            var missingSelector = kind == ChartSeriesKind.TileMap ? MapRegion("NC") : Point(0, 10);
            var missing = page.Locator(missingSelector);
            await MoveAwayAsync(page); await missing.FocusAsync();
            if (kind == ChartSeriesKind.Scatter) {
                Assert.Null(await missing.GetAttributeAsync("data-cfx-color-value"));
                Assert.Equal("true", await missing.GetAttributeAsync("data-cfx-color-missing"));
                await AssertRowAsync("Temperature (°C)", "Not sampled"); await AssertRowAsync("Y", "22");
            } else {
                Assert.Equal("true", await missing.GetAttributeAsync("data-cfx-empty"));
                Assert.Equal("", await missing.GetAttributeAsync("data-cfx-value"));
                await page.WaitForFunctionAsync("label => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.querySelector('.cfx-tooltip__title').textContent === label; }", await missing.GetAttributeAsync("aria-label"));
                Assert.DoesNotContain("Value", await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
            }
            await CaptureAsync("missing-focus");
        }
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);

        string Target(double value) => kind == ChartSeriesKind.TileMap ? MapRegion(value == 55 ? "CO" : value == 0 ? "CA" : "TX")
            : kind == ChartSeriesKind.Heatmap ? "[data-cfx-role='heatmap-cell'][data-cfx-value='" + Number(value) + "']"
            : Point(0, value == 55 ? 9 : value == 0 ? 3 : value == 2.5 ? 4 : 0);

        Task AssertRowAsync(string name, string value) => page.WaitForFunctionAsync("expected => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && Array.from(tip.querySelectorAll('dt')).some(term => term.textContent === expected.name && term.nextElementSibling.textContent === expected.value); }", new { name, value });

        async Task CaptureAsync(string stage) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            var name = "positioned-color-" + kind + "-" + width + "-" + (dark ? "dark" : "light");
            if (stage == "clamped-pointer") {
                await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
                await File.WriteAllBytesAsync(Path.Combine(directory, name + ".png"), prepared.ToPng());
            }
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + "-" + stage + ".browser.png") });
            var state = await page.EvaluateAsync<JsonElement>("""
                () => { const root = document.querySelector('.cfx-interactive-chart'); const tip = root.querySelector('.cfx-tooltip');
                    return { Focus: document.activeElement.dataset.cfxTargetId, Pinned: root.dataset.cfxTooltipPinned,
                        Readout: tip && !tip.hidden ? tip.innerText : '', Selected: window.positionedSelection,
                        Stops: Array.from(root.querySelectorAll('[data-cfx-stop]'), node => ({...node.dataset})) }; }
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, name + "-" + stage + ".runtime.json"), JsonSerializer.Serialize(new {
                State = state, Errors = errors, ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private static string MapRegion(string code) => "[data-cfx-role='tile-map-region-source'][data-cfx-region='" + code + "']";
    private static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);
}
