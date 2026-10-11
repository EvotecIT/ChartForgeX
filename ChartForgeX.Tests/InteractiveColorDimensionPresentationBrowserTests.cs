using System.Globalization;
using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

/// <summary>Protects captured cultural/custom text, empty output and raw identities across the six presentation owners.</summary>
public sealed class InteractiveColorDimensionPresentationBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.TileMap, 360, false, false)]
    [InlineData(ChartSeriesKind.TileMap, 800, true, false)]
    [InlineData(ChartSeriesKind.Heatmap, 360, false, false)]
    [InlineData(ChartSeriesKind.Heatmap, 800, true, false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, false, false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 800, true, false)]
    [InlineData(ChartSeriesKind.Treemap, 360, false, false)]
    [InlineData(ChartSeriesKind.Treemap, 800, true, false)]
    [InlineData(ChartSeriesKind.Scatter, 360, false, false)]
    [InlineData(ChartSeriesKind.Scatter, 800, true, false)]
    [InlineData(ChartSeriesKind.Bubble, 360, false, false)]
    [InlineData(ChartSeriesKind.Bubble, 800, true, false)]
    [InlineData(ChartSeriesKind.Scatter, 360, false, true)]
    [InlineData(ChartSeriesKind.Scatter, 800, true, true)]
    [InlineData(ChartSeriesKind.Bubble, 360, false, true)]
    [InlineData(ChartSeriesKind.Bubble, 800, true, true)]
    public async Task CapturedPresentationSurvivesPointerFocusPinAndLegendSummaryWithRawIdentities(ChartSeriesKind kind, int width, bool dark, bool shared) {
        if (!Enabled) return;
        var chart = ColorDimensionPresentationFixtures.Create(kind, width == 800 ? "discrete" : "positioned", width, dark);
        var sameValue = kind is ChartSeriesKind.TileMap or ChartSeriesKind.Heatmap or ChartSeriesKind.CalendarHeatmap;
        const string name = "Temperature (°C)";
        var format = width == 800 ? ChartValueFormat.Number("0.00 '°C'", CultureInfo.GetCultureInfo("pl-PL"))
            : ChartValueFormat.Custom(value => value == 0 ? "" : "t<" + value.ToString(CultureInfo.InvariantCulture) + "> °C");
        var options = ColorDimensionPresentationFixtures.Options(chart, kind); options.Name = name; options.ValueFormat = format;
        if (kind == ChartSeriesKind.Treemap) chart.WithPointLegend().ConfigureTreemap(treemap => treemap.ColorDimension.ShowLegend = false);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var html = chart.ToInteractiveHtmlPage(options => options.Tooltip.Mode = shared ? HtmlChartTooltipMode.SharedX : HtmlChartTooltipMode.Single);
        options.Name = "Changed"; options.ValueFormat = ChartValueFormat.Custom(_ => throw new InvalidOperationException("Detached HTML must not rerun source formatting."));
        await using var session = await OpenAsync(html, width + 24, 620);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var root = page.Locator(".cfx-interactive-chart"); var measured = Target("measured"); var zero = Target("zero");
        var rawAttribute = sameValue ? "data-cfx-value" : "data-cfx-color-value";
        var displayAttribute = sameValue ? "data-cfx-formatted-value" : "data-cfx-formatted-color-value";
        Assert.Equal("7.125", await page.Locator(measured).GetAttributeAsync(rawAttribute));
        Assert.Equal("0", await page.Locator(zero).GetAttributeAsync(rawAttribute));
        Assert.Equal(format.Format(7.125), await page.Locator(measured).GetAttributeAsync(displayAttribute));
        Assert.Equal(format.Format(0), await page.Locator(zero).GetAttributeAsync(displayAttribute));
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.dimensionSelection = event.detail.target)");
        await MoveToAsync(page, measured); await AssertRowAsync(name, format.Format(7.125));
        if (sameValue) {
            var terms = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
            Assert.DoesNotContain("Color", terms); Assert.DoesNotContain("Default color", terms); Assert.DoesNotContain("Value", terms);
        } else await AssertRowAsync(kind == ChartSeriesKind.Treemap ? "Value" : "Y", kind == ChartSeriesKind.Treemap ? "9.0 units" : "18");
        if (kind == ChartSeriesKind.Bubble) await AssertRowAsync("Size", "9");
        await CaptureAsync("pointer");
        await MoveAwayAsync(page); await page.Locator(zero).FocusAsync(); await AssertRowAsync(name, format.Format(0));
        if (kind == ChartSeriesKind.Bubble) await AssertRowAsync("Size", "36");
        await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.dimensionSelection");
        Assert.Equal(kind is ChartSeriesKind.TileMap or ChartSeriesKind.CalendarHeatmap ? "region" : kind == ChartSeriesKind.Treemap ? "node" : "point", selected.GetProperty("targetKind").GetString());
        Assert.Equal(kind == ChartSeriesKind.TileMap ? "TX" : kind == ChartSeriesKind.CalendarHeatmap ? "2026-01-07" : kind == ChartSeriesKind.Treemap ? "zero" : "samples:1", selected.GetProperty("targetId").GetString());
        Assert.Equal(sameValue ? "0" : kind == ChartSeriesKind.Treemap ? "4" : "30", selected.GetProperty("value").GetString());
        Assert.Equal("true", await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        await AssertRowAsync(name, format.Format(0)); await CaptureAsync("zero-selected");
        await page.Keyboard.PressAsync("Space"); Assert.Null(await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
        if (kind == ChartSeriesKind.Treemap) {
            await MoveAwayAsync(page); await page.Locator("[data-cfx-role='legend-item'][data-cfx-legend-target-id='zero']").FocusAsync();
            await AssertRowAsync(name, format.Format(0)); await AssertRowAsync("Value", "4.0 units");
            await CaptureAsync("zero-legend-summary");
        }
        if (kind == ChartSeriesKind.Heatmap) Assert.Equal(0, await page.Locator("[data-cfx-role='heatmap-cell'][data-cfx-column='2']").CountAsync());
        else {
            var missing = page.Locator(Target("missing"));
            await MoveAwayAsync(page); await missing.FocusAsync();
            if (sameValue) {
                await page.WaitForFunctionAsync("label => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.querySelector('.cfx-tooltip__title').textContent === label; }", await missing.GetAttributeAsync("aria-label"));
                Assert.DoesNotContain(name, await page.Locator(".cfx-tooltip dt").AllTextContentsAsync());
                Assert.Equal(kind == ChartSeriesKind.TileMap ? "Not sampled" : null, await missing.GetAttributeAsync(displayAttribute));
            } else { await AssertRowAsync(name, "Not sampled"); Assert.Null(await missing.GetAttributeAsync(rawAttribute)); }
            if (kind == ChartSeriesKind.Bubble) await AssertRowAsync("Size", "81");
            await CaptureAsync("missing-focus");
        }
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);

        string Target(string item) => kind switch {
            ChartSeriesKind.TileMap => "[data-cfx-role='tile-map-region-source'][data-cfx-region='" + (item == "measured" ? "CA" : item == "zero" ? "TX" : "NC") + "']",
            ChartSeriesKind.Heatmap => "[data-cfx-role='heatmap-cell'][data-cfx-column='" + (item == "measured" ? "0" : "1") + "']",
            ChartSeriesKind.CalendarHeatmap => "[data-cfx-role='calendar-cell'][data-cfx-date='2026-01-" + (item == "measured" ? "05" : item == "zero" ? "07" : "06") + "']",
            ChartSeriesKind.Treemap => "[data-cfx-role='treemap-tile'][data-cfx-node='" + item + "']",
            _ => Point(0, item == "measured" ? 0 : item == "zero" ? 1 : 2)
        };

        Task AssertRowAsync(string label, string value) => page.WaitForFunctionAsync("expected => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && Array.from(tip.querySelectorAll('dt')).some(term => term.textContent === expected.label && term.nextElementSibling.textContent === expected.value); }", new { label = shared ? label == "Y" ? "Samples" : "Samples · " + label : label, value });

        async Task CaptureAsync(string stage) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            var file = Path.Combine(directory, "color-dimension-" + kind + "-" + width + "-" + (dark ? "dark" : "light") + (shared ? "-shared" : "-single"));
            if (stage == "pointer") { await File.WriteAllTextAsync(file + ".svg", prepared.ToSvg()); await File.WriteAllBytesAsync(file + ".png", prepared.ToPng()); }
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = file + "-" + stage + ".browser.png" });
            var state = await page.EvaluateAsync<JsonElement>("() => { const root = document.querySelector('.cfx-interactive-chart'); return { Focus: document.activeElement.dataset.cfxTargetId, Readout: root.querySelector('.cfx-tooltip').innerText, Pinned: root.dataset.cfxTooltipPinned, Selected: window.dimensionSelection }; }");
            await File.WriteAllTextAsync(file + "-" + stage + ".runtime.json", JsonSerializer.Serialize(new { State = state, Errors = errors }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
