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

/// <summary>Protects raw-value pointer and keyboard readouts when canonical numeric colors replace legacy intensity levels.</summary>
public sealed class InteractiveHeatmapColorScaleBrowserTests {
    [Theory]
    [InlineData(ChartSeriesKind.Heatmap, 360, false, true)]
    [InlineData(ChartSeriesKind.Heatmap, 800, true, true)]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 360, true, true)]
    [InlineData(ChartSeriesKind.HexbinHeatmap, 800, false, true)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, false, true)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 360, true, false)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 800, true, true)]
    [InlineData(ChartSeriesKind.CalendarHeatmap, 800, false, false)]
    public async Task PointerKeyboardAndSelectionKeepObservedValuesAndMissingIdentities(ChartSeriesKind kind, int width, bool dark, bool explicitScale) {
        if (!Enabled) return;
        var chart = Chart.Create().WithSize(width, 400).WithDataLabels(false)
            .WithTheme(dark ? ChartTheme.GraphiteDark() : ChartTheme.GraphiteLight())
            .WithHeatmapColorScale(explicitScale ? ChartColorScale.Discrete(new[] {
                new ChartColorBand(10, ChartColor.FromHex("#DAE8F8"), "Low"),
                new ChartColorBand(20, ChartColor.FromHex("#6F9ECE"), "Middle"),
                new ChartColorBand(null, ChartColor.FromHex("#1C5CAB"), "High")
            }) : null);
        if (kind == ChartSeriesKind.CalendarHeatmap) chart.AddCalendarHeatmap("Observed", new[] {
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 5), 0), new ChartCalendarHeatmapItem(new DateTime(2026, 1, 7), 10),
            new ChartCalendarHeatmapItem(new DateTime(2026, 1, 8), 20)
        });
        else if (kind == ChartSeriesKind.Heatmap) chart.AddHeatmapRow("Observed", new double?[] { 0, null, 10, 20 });
        else chart.AddHexbinHeatmapRow("Observed", new double?[] { 0, null, 10, 20 });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(), width + 24, 500);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var role = kind == ChartSeriesKind.Heatmap ? "heatmap-cell" : kind == ChartSeriesKind.HexbinHeatmap ? "hexbin-cell" : "calendar-cell";
        var cells = "[data-cfx-role='" + role + "'][data-cfx-value]";
        Assert.Equal(3, await page.Locator(cells).CountAsync());
        if (kind == ChartSeriesKind.CalendarHeatmap)
            Assert.Equal("true", await page.Locator("[data-cfx-date='2026-01-06']").GetAttributeAsync("data-cfx-empty"));
        else Assert.Equal(0, await page.Locator("[data-cfx-role='" + role + "'][data-cfx-column='1']").CountAsync());
        foreach (var value in new[] { "0", "10", "20" }) {
            var selector = cells + "[data-cfx-value='" + value + "']";
            if (value == "10") { await MoveAwayAsync(page); await page.Locator(selector).FocusAsync(); }
            else await MoveToAsync(page, selector);
            await page.WaitForFunctionAsync("value => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && Array.from(tip.querySelectorAll('dt')).some(term => term.textContent === 'Value' && term.nextElementSibling.textContent === value); }", value);
            var terms = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
            if (explicitScale) {
                Assert.DoesNotContain("Level", terms); Assert.DoesNotContain("Status", terms); Assert.DoesNotContain("Color", terms);
                Assert.Null(await page.Locator(selector).GetAttributeAsync("data-cfx-level"));
                Assert.Null(await page.Locator(selector).GetAttributeAsync("data-cfx-status"));
            }
        }
        await page.EvaluateAsync("() => document.querySelector('.cfx-interactive-chart').addEventListener('cfxselect', event => window.heatmapSelection = event.detail.target)");
        await MoveAwayAsync(page);
        await page.Locator(cells + "[data-cfx-value='20']").FocusAsync(); await page.Keyboard.PressAsync("Space");
        var selected = await page.EvaluateAsync<JsonElement>("() => window.heatmapSelection");
        Assert.Equal("20", selected.GetProperty("value").GetString());
        if (kind == ChartSeriesKind.CalendarHeatmap) {
            Assert.Equal("region", selected.GetProperty("targetKind").GetString());
            Assert.Equal("2026-01-08", selected.GetProperty("targetId").GetString());
            // Space pins the selected day's readout. Release that pin before inspecting a different day.
            var root = page.Locator(".cfx-interactive-chart");
            Assert.Equal("true", await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
            await page.Keyboard.PressAsync("Space");
            Assert.Null(await root.GetAttributeAsync("data-cfx-tooltip-pinned"));
            var missing = page.Locator("[data-cfx-role='calendar-cell'][data-cfx-date='2026-01-06']");
            var missingLabel = await missing.GetAttributeAsync("aria-label");
            Assert.Equal("Observed, 2026-01-06: No data", missingLabel);
            await MoveAwayAsync(page); await missing.FocusAsync();
            await CaptureMissingAsync("missing-focus");
            await page.WaitForFunctionAsync("label => { const tip = document.querySelector('.cfx-tooltip'); return tip && !tip.hidden && tip.querySelector('.cfx-tooltip__title').textContent === label && Array.from(tip.querySelectorAll('dt')).some(term => term.textContent === 'X' && term.nextElementSibling.textContent === '2026-01-06'); }", missingLabel);
            var missingTerms = await page.Locator(".cfx-tooltip dt").AllTextContentsAsync();
            Assert.DoesNotContain("Value", missingTerms); Assert.DoesNotContain("Y", missingTerms);
            Assert.DoesNotContain("Level", missingTerms); Assert.DoesNotContain("Status", missingTerms); Assert.DoesNotContain("Color", missingTerms);
            Assert.Null(await missing.GetAttributeAsync("data-cfx-value"));
            Assert.Null(await missing.GetAttributeAsync("data-cfx-point"));
            Assert.Null(await missing.GetAttributeAsync("data-cfx-source-points"));
            await page.Keyboard.PressAsync("Space");
            var empty = await page.EvaluateAsync<JsonElement>("() => window.heatmapSelection");
            Assert.Equal("region", empty.GetProperty("targetKind").GetString());
            Assert.Equal("2026-01-06", empty.GetProperty("targetId").GetString());
            Assert.Equal(missingLabel, empty.GetProperty("label").GetString());
            Assert.Equal("", empty.GetProperty("value").GetString());
            Assert.Equal(JsonValueKind.Null, empty.GetProperty("point").ValueKind);
            Assert.Equal(JsonValueKind.Null, empty.GetProperty("sourcePoint").ValueKind);
            Assert.Equal(JsonValueKind.Null, empty.GetProperty("sourcePoints").ValueKind);
            await CaptureMissingAsync("missing-selected");
        }
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var name = "heatmap-color-scale-" + kind + "-" + width + "-" + (dark ? "dark" : "light") + (explicitScale ? "-explicit" : "-default");
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(captures, name + ".png"), prepared.ToPng());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".browser.png") });
            await File.WriteAllTextAsync(Path.Combine(captures, name + ".runtime.json"), JsonSerializer.Serialize(new {
                Selected = selected, Readout = await TooltipTextAsync(page), Errors = errors,
                ConsoleErrors = session.ConsoleLog.Count(entry => entry.Type == HtmlTinkerX.HtmlConsoleMessageType.Error)
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        async Task CaptureMissingAsync(string stage) {
            var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
            if (string.IsNullOrWhiteSpace(directory)) return;
            Directory.CreateDirectory(directory);
            var name = "heatmap-color-scale-" + kind + "-" + width + "-" + (dark ? "dark" : "light") + (explicitScale ? "-explicit-" : "-default-") + stage;
            var state = await page.EvaluateAsync<JsonElement>("""
                () => {
                    const root = document.querySelector('.cfx-interactive-chart');
                    const missing = root.querySelector('[data-cfx-role="calendar-cell"][data-cfx-date="2026-01-06"]');
                    const tip = root.querySelector('.cfx-tooltip');
                    return { Missing: {...missing.dataset}, NativeLabel: missing.getAttribute('aria-label'),
                        FocusDate: document.activeElement.dataset.cfxDate, TooltipPinned: root.dataset.cfxTooltipPinned,
                        PinnedTarget: root.dataset.cfxPinnedTarget, Readout: tip.innerText, Hidden: tip.hidden,
                        Terms: Array.from(tip.querySelectorAll('dt'), term => term.textContent),
                        Selected: window.heatmapSelection };
                }
                """);
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".runtime.json"), JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png") });
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".svg"), prepared.ToSvg());
            await File.WriteAllBytesAsync(Path.Combine(directory, name + ".png"), prepared.ToPng());
        }
    }
}
