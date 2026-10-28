using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class AlignedDifferenceBrowserTests {
    [BrowserTheory]
    [InlineData("wide", 390, VisualThemeMode.Light)]
    [InlineData("wide", 390, VisualThemeMode.Dark)]
    [InlineData("wide", 800, VisualThemeMode.Light)]
    [InlineData("wide", 800, VisualThemeMode.Dark)]
    [InlineData("negative-log", 390, VisualThemeMode.Light)]
    [InlineData("negative-log", 390, VisualThemeMode.Dark)]
    [InlineData("negative-log", 800, VisualThemeMode.Light)]
    [InlineData("negative-log", 800, VisualThemeMode.Dark)]
    [InlineData("sym-log", 390, VisualThemeMode.Light)]
    [InlineData("sym-log", 390, VisualThemeMode.Dark)]
    [InlineData("sym-log", 800, VisualThemeMode.Light)]
    [InlineData("sym-log", 800, VisualThemeMode.Dark)]
    public async Task RealignedSourcesRetainBothProvenancesSelectionAndAcceptedNativeExports(string variant, int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = Model(variant, width, mode); var updated = Model(variant, width, mode);
        var alignment = updated.Series[0].DifferenceAlignment!;
        var first = ChartSeries.CreateLine(alignment.FirstSeriesId, alignment.FirstName, alignment.FirstPoints)
            .WithObservationIds(Enumerable.Range(0, alignment.FirstPoints.Count).Select(index => alignment.FirstObservationIds[index]));
        var second = ChartSeries.CreateLine(alignment.SecondSeriesId, alignment.SecondName, alignment.SecondPoints)
            .WithObservationIds(Enumerable.Range(0, alignment.SecondPoints.Count).Select(index => alignment.SecondObservationIds[index]));
        var changed = first.Points[2].Y * 1.25; first.Points[2] = new ChartPoint(first.Points[2].X, changed);
        var current = ChartDifferencePoints.Align(first, second, alignment.MissingDataPolicy);
        updated.Series[0].ReplaceDifferenceAlignment(current).WithObservationIds(current.FirstSourceIndices.Select(index => current.FirstObservationIds[index]));
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Options), width + 24, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.aligned = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        const string selector = "[data-cfx-role=point][data-cfx-series='0'][data-cfx-point='1']";
        var point = page.Locator(selector); Assert.Equal("2", await point.GetAttributeAsync("data-cfx-first-source-index"));
        Assert.Equal("actual-2", await point.GetAttributeAsync("data-cfx-first-source-observation-id"));
        Assert.Equal("forecast-2", await point.GetAttributeAsync("data-cfx-second-source-observation-id"));
        await FocusWithKeyboardAsync(page, point); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await point.GetAttributeAsync("aria-selected"));
        await page.EvaluateAsync("html => aligned.update(html)", updated.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Equal("true", await page.Locator(selector).GetAttributeAsync("aria-selected"));
        Assert.Equal(changed.ToString("R", System.Globalization.CultureInfo.InvariantCulture), await page.Locator(selector).GetAttributeAsync("data-cfx-first"));
        Assert.Equal("forecast-2", await page.Locator(selector).GetAttributeAsync("data-cfx-second-source-observation-id"));
        await page.SetViewportSizeAsync(width == 390 ? 824 : 414, 800);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 800);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No aligned SVG download.")));
        var sources = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(4, sources.Length); Assert.Equal("actual-2", (string?)sources[1].Attribute("data-cfx-first-source-observation-id"));
        Assert.Equal(changed.ToString("R", System.Globalization.CultureInfo.InvariantCulture), (string?)sources[1].Attribute("data-cfx-first"));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No aligned PNG download.")));
        Assert.Equal(width, png.Width); Assert.Equal(width == 390 ? 500 : 520, png.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "aligned-difference-" + variant + "-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
    private static Chart Model(string variant, int width, VisualThemeMode mode) {
        var selected = width == 390 ? variant == "wide" ? "compact" : "compact-" + variant : variant;
        return AlignedDifferenceRecipe.Create(selected, mode).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.Tooltip.PinOnSelect = false;
    }
}
