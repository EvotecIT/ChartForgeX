using System.Text.Json;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

public sealed partial class SourceAnnotationBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    public async Task NativeSourceDrawingsKeepPaintedCaptionsAndAuthoredStateAcrossReplacement(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var initial = Model(width, mode, false);
        await using var session = await BrowserProofFonts.OpenAsync(initial.ToInteractiveHtmlFragment(Options), width + 24, 760);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.drawings = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        var references = await page.EvaluateAsync<JsonElement>("() => drawings.resolveTargets().filter(target => target.targetKind === 'annotation')");
        Assert.Equal(new[] { "channel", "note", "plan" }, references.EnumerateArray().Select(target => target.GetProperty("annotationId").GetString()).OrderBy(id => id, StringComparer.Ordinal));
        Assert.Equal(3, await page.Locator("[data-cfx-role='annotation-label']").CountAsync());
        Assert.Contains("Review", await page.Locator("[data-cfx-annotation-id='note']").GetAttributeAsync("aria-label"));
        await FocusWithKeyboardAsync(page, page.Locator("[data-cfx-annotation-id='note']"));
        await page.Keyboard.PressAsync("Space");
        Assert.Equal("note", (await page.EvaluateAsync<JsonElement>("() => drawings.getSelection()[0]")).GetProperty("annotationId").GetString());
        Assert.Equal("Review", await page.Locator(".cfx-compare-chip").InnerTextAsync());
        Assert.Equal("Review", await page.Locator(".cfx-compare-chip").GetAttributeAsync("title"));
        await Capture(page, width, mode, "selected");
        await page.EvaluateAsync("html => drawings.update(html)", Model(width, mode, true).ToInteractiveHtmlFragmentWithoutAssets(Options));
        var selected = await page.EvaluateAsync<JsonElement>("() => drawings.getSelection()[0]");
        Assert.Equal("note", selected.GetProperty("annotationId").GetString());
        Assert.Equal("Reviewed", selected.GetProperty("label").GetString());
        Assert.Equal("Reviewed", await page.Locator(".cfx-compare-chip").InnerTextAsync());
        Assert.Equal("Reviewed", await page.Locator(".cfx-compare-chip").GetAttributeAsync("title"));
        Assert.True(await page.Locator("[data-cfx-annotation-id='note']").EvaluateAsync<bool>("node => node === document.activeElement"));
        Assert.Equal("6", await page.Locator("[data-cfx-annotation-id='note']").GetAttributeAsync("data-cfx-source-x"));
        Assert.Equal(1, await page.Locator("[data-cfx-role='annotation-channel']").CountAsync());
        await Capture(page, width, mode, "updated");
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Chart Model(int width, VisualThemeMode mode, bool updated) {
        var chart = Chart.Create().WithSize(width, 520).WithTheme(VisualTheme.Graphite(), mode)
            .WithFontFamily(BrowserProofFonts.Family)
            .WithPngFont(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf"))
            .WithTitle("Source drawings").AddLine("Observed", new[] { new ChartPoint(0, 10), new ChartPoint(3, 40), new ChartPoint(10, 70) });
        chart.Options.XAxis.WithBounds(0, 10); chart.Options.YAxis.WithBounds(0, 100);
        chart.AddNote(new ChartPoint(updated ? 6 : 3, 40), updated ? "Reviewed" : "Review", id: new ChartAnnotationId("note"))
            .AddLineSegment(new ChartPoint(2, 20), new ChartPoint(8, updated ? 50 : 30), "Plan", id: new ChartAnnotationId("plan"))
            .AddParallelChannel(new ChartPoint(2, 65), new ChartPoint(8, 75), 10, "Target", id: new ChartAnnotationId("channel"));
        if (updated) { var note = chart.Annotations[0]; chart.Annotations.RemoveAt(0); chart.Annotations.Add(note); }
        return chart;
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.ChartId = "source-drawings"; options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Tooltip.PinOnSelect = false;
    }
    private static async Task Capture(IPage page, int width, VisualThemeMode mode, string state) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var stem = Path.Combine(directory, $"source-drawings-{width}-{mode}-{state}");
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".json", await page.EvaluateAsync<string>("""
            () => JSON.stringify({ selection: drawings.getSelection(), annotations: [...document.querySelectorAll('[data-cfx-role=annotation]')]
                .map(node => ({id:node.dataset.cfxAnnotationId,kind:node.dataset.cfxKind,caption:node.querySelector('[data-cfx-role=annotation-label]')?.textContent,
                    bounds:node.getBoundingClientRect().toJSON()})) }, null, 2)
            """));
    }
}
