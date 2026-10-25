using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class StagePresentationBrowserTests {
    [BrowserTheory]
    [InlineData(false, false, 390, VisualThemeMode.Light)]
    [InlineData(false, false, 390, VisualThemeMode.Dark)]
    [InlineData(false, false, 800, VisualThemeMode.Light)]
    [InlineData(false, false, 800, VisualThemeMode.Dark)]
    [InlineData(true, false, 390, VisualThemeMode.Light)]
    [InlineData(true, false, 390, VisualThemeMode.Dark)]
    [InlineData(true, false, 800, VisualThemeMode.Light)]
    [InlineData(true, false, 800, VisualThemeMode.Dark)]
    [InlineData(false, true, 390, VisualThemeMode.Light)]
    [InlineData(false, true, 390, VisualThemeMode.Dark)]
    [InlineData(false, true, 800, VisualThemeMode.Light)]
    [InlineData(false, true, 800, VisualThemeMode.Dark)]
    public async Task NativeStageComponentsKeepResponsiveSelectionSourceIdentityAndCurrentExports(bool pyramid, bool cone, int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = Model(pyramid, cone, width, mode); var updated = Model(pyramid, cone, width, mode);
        updated.Series[0].Points[1] = new ChartForgeX.Primitives.ChartPoint(2, pyramid ? 35 : 85);
        var family = pyramid ? "pyramid" : "funnel";
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Options), width + 24, 850);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.stages = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        if (cone) await AssertConeInks(page, chart);
        var selector = "g[data-cfx-role='" + family + "-stage'][data-cfx-point='1']";
        var target = page.Locator(selector);
        Assert.Equal(pyramid ? "platform" : "reviewed", await target.GetAttributeAsync("data-cfx-observation-id"));
        await FocusWithKeyboardAsync(page, target); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await target.GetAttributeAsync("aria-selected"));
        var selected = await page.EvaluateAsync<JsonElement>("() => stages.getSelection()"); Assert.NotEqual(JsonValueKind.Null, selected.ValueKind);
        await page.EvaluateAsync("html => stages.update(html)", updated.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Equal("true", await page.Locator(selector).GetAttributeAsync("aria-selected"));
        if (cone) await AssertConeInks(page, updated);
        Assert.Equal(pyramid ? "35" : "85", await page.Locator(selector).GetAttributeAsync("data-cfx-value"));
        Assert.InRange(await page.Locator("[data-cfx-role='" + family + "-category-label']").CountAsync(), 1, 4);
        Assert.InRange(await page.Locator("[data-cfx-role='" + family + "-value-label']").CountAsync(), 1, 4);
        await page.SetViewportSizeAsync(width == 390 ? 824 : 414, 850);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 850);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No stage SVG download.")));
        var groups = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == family + "-stage").ToArray();
        Assert.Equal(4, groups.Length);
        Assert.Equal(pyramid ? "35" : "85", (string?)groups[1].Attribute("data-cfx-value"));
        Assert.Equal(pyramid ? "platform" : "reviewed", (string?)groups[1].Attribute("data-cfx-observation-id"));
        Assert.Contains(svg.Descendants(), node => node.Attribute("stroke-dasharray") != null);
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No stage PNG download.")));
        Assert.Equal(width, png.Width); Assert.Equal(pyramid ? width == 390 ? 500 : 560 : width == 390 ? 560 : 620, png.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "stage-components-" + family + (cone ? "-cone" : "-bars") + "-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".selection.json", selected.GetRawText());
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
    private static async Task AssertConeInks(IPage page, Chart chart) {
        var prepared = StagePresentationOptionsTests.Prepare(chart);
        var expected = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "funnel-value-label").ToDictionary(text => text.Id!);
        var actual = await page.Locator("[data-cfx-role=funnel-value-label] text").EvaluateAllAsync<JsonElement>(
            "nodes => nodes.map(node => ({ id:node.closest('[data-cfx-source-id]').getAttribute('data-cfx-source-id'), fill:getComputedStyle(node).fill }))");
        Assert.Equal(expected.Count, actual.GetArrayLength());
        foreach (var row in actual.EnumerateArray()) {
            var source = row.GetProperty("id").GetString()!; Assert.True(expected.ContainsKey(source));
            Assert.True(SvgRasterColor.TryParse(row.GetProperty("fill").GetString()!, out var painted)); Assert.Equal(expected[source].Color, painted);
        }
    }
    private static Chart Model(bool pyramid, bool cone, int width, VisualThemeMode mode) {
        var chart = pyramid ? PyramidPresentationRecipe.Create(width == 390 ? "compact" : "wide", mode)
            : FunnelPresentationRecipe.Create(cone ? width == 390 ? "compact-cone" : "cone" : width == 390 ? "compact" : "wide", mode);
        return chart.WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.Tooltip.PinOnSelect = false;
    }
}
