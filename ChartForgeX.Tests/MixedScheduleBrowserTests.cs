using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MixedScheduleBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)]
    [InlineData(390, true)]
    [InlineData(900, false)]
    [InlineData(900, true)]
    public async Task MixedReversalKeepsRealTypedTargetsReadoutsResponsiveSelectionAndDownloads(int width, bool dark) {
        if (!Enabled) return;
        var chart = MixedScheduleDepthTests.Fixture(width, dark, false);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Options), width + 24, 900);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.scheduleDepth = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        const string state = "[data-cfx-role=state-timeline-segment][data-cfx-series-key=states]";
        Assert.Equal("0,1", await page.Locator(state).GetAttributeAsync("data-cfx-source-points"));
        await FocusWithKeyboardAsync(page, page.Locator(state)); await page.Keyboard.PressAsync("Space");
        var selected = Assert.Single((await page.EvaluateAsync<JsonElement>("() => scheduleDepth.getSelection()")).EnumerateArray());
        Assert.Equal("states", selected.GetProperty("seriesKey").GetString()); Assert.Equal(new[] { 0, 1 }, selected.GetProperty("sourcePoints").EnumerateArray().Select(value => value.GetInt32()));
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const data=JSON.parse(document.querySelector('.cfx-interactive-chart').dataset.cfxPreparedChart);
                return data.dataProjection.x.reversed===false && data.dataProjection.y===undefined;
            }
            """));
        chart.Options.XAxis.Reversed = chart.Options.YAxis.Reversed = true;
        await page.EvaluateAsync("html => scheduleDepth.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets(Options));
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } captureDirectory) {
            Directory.CreateDirectory(captureDirectory);
            var prefix = Path.Combine(captureDirectory, "mixed-schedule-update-" + width + (dark ? "-dark" : "-light"));
            await File.WriteAllTextAsync(prefix + ".runtime.json", await page.EvaluateAsync<string>(
                "() => JSON.stringify({selection:scheduleDepth.getSelection(),targets:[...document.querySelectorAll('[data-cfx-role=state-timeline-segment]')].map(node=>({...node.dataset,selected:node.getAttribute('aria-selected')}))},null,2)"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".browser.png", FullPage = true });
        }
        // This run aggregates two source observations and has no singular durable observation ID.
        // Ordinary full replacement clears positional selection while preserving its complete source facts.
        Assert.Empty((await page.EvaluateAsync<JsonElement>("() => scheduleDepth.getSelection()")).EnumerateArray());
        Assert.Equal("0,1", await page.Locator(state).GetAttributeAsync("data-cfx-source-points"));
        const string task = "[data-cfx-role=gantt-task][data-cfx-series-key=task]";
        await FocusWithKeyboardAsync(page, page.Locator(task)); await page.Keyboard.PressAsync("Space");
        Assert.Equal("task-first", Assert.Single((await page.EvaluateAsync<JsonElement>("() => scheduleDepth.getSelection()")).EnumerateArray()).GetProperty("observationId").GetString());
        chart.Options.YAxis.Reversed = false;
        await page.EvaluateAsync("html => scheduleDepth.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets(Options));
        selected = Assert.Single((await page.EvaluateAsync<JsonElement>("() => scheduleDepth.getSelection()")).EnumerateArray());
        Assert.Equal("task", selected.GetProperty("seriesKey").GetString()); Assert.Equal("task-first", selected.GetProperty("observationId").GetString());
        Assert.Equal("true", await page.Locator(task).GetAttributeAsync("aria-selected"));
        Assert.True(await page.Locator(task).EvaluateAsync<bool>("node => document.activeElement === node"));
        Assert.True(await page.EvaluateAsync<bool>("() => JSON.parse(document.querySelector('.cfx-interactive-chart').dataset.cfxPreparedChart).dataProjection.x.reversed"));
        foreach (var role in new[] { "timeline-item", "gantt-task", "state-timeline-segment", "gantt-lane-item", "gantt-vertical-marker" })
            Assert.True(await page.Locator("[data-cfx-role=" + role + "]").CountAsync() > 0);
        var hit = await NativeInteriorPointAsync(page, "[data-cfx-role=gantt-task][data-cfx-series-key=task]", "[data-cfx-role=gantt-task-shape]", clearance: 2);
        await page.Mouse.MoveAsync((float)hit[0], (float)hit[1], new MouseMoveOptions { Steps = 4 }); Assert.Contains("Implement", await TooltipTextAsync(page));
        await MoveAwayAsync(page); await page.SetViewportSizeAsync(width == 390 ? 924 : 414, 900);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 900);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No mixed schedule SVG download.")));
        Assert.Single(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "schedule-composition");
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "gantt-lane-open-end");
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No mixed schedule PNG download.")));
        Assert.Equal(width, png.Width); Assert.Equal(640, png.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "mixed-schedule-" + width + (dark ? "-dark" : "-light"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".selection.json", await page.EvaluateAsync<string>("() => JSON.stringify(scheduleDepth.getSelection())"));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Interaction.Enable(ChartInteractionFeatures.Export); options.Tooltip.PinOnSelect = false;
    }
}
