using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class PictorialArtworkBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    [InlineData(900, VisualThemeMode.Dark)]
    public async Task CapturedArtworkAndPartialDirectionSurviveNativeReplacementSelectionAndExports(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = PictorialArtworkRecipe.Create("wide", mode).WithSize(width, 460).WithFontFamily(BrowserProofFonts.Family);
        var renderer = new HtmlInteractiveChartRenderer(); var first = renderer.PrepareSection(chart, configure: Options);
        chart.WithPictorialFillDirection(ChartPictorialFillDirection.BottomToTop); chart.Series[0].Points[0] = new(1, 1.25);
        var next = renderer.PrepareSection(chart, configure: Options);
        await using var session = await BrowserProofFonts.OpenAsync("<style>" + HtmlInteractiveChartRenderer.BuildFragmentStyle() + "</style>"
            + first.Html + "<script>" + HtmlInteractiveChartRenderer.BuildInteractionScript() + "</script>", width + 24, 700);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => window.pictorialArtwork = ChartForgeXCharts.mount(document.querySelector('.cfx-interactive-chart'))");
        const string target = "[data-cfx-role=pictorial-item][data-cfx-point='0']";
        await FocusWithKeyboardAsync(page, page.Locator(target)); await page.Keyboard.PressAsync("Space");
        await Capture(page, first, width, mode, "selected");
        Assert.Equal("true", await page.Locator(target).GetAttributeAsync("aria-selected"));
        var selection = Assert.Single((await page.EvaluateAsync<JsonElement>("() => pictorialArtwork.getSelection()")).EnumerateArray());
        Assert.Equal("assessments", selection.GetProperty("seriesKey").GetString()); Assert.Equal("team-a", selection.GetProperty("observationId").GetString());
        await page.EvaluateAsync("html => pictorialArtwork.update(html)", next.Html);
        Assert.Equal("1.25", await page.Locator(target).GetAttributeAsync("data-cfx-value"));
        Assert.Equal("true", await page.Locator(target).GetAttributeAsync("aria-selected"));
        Assert.True(await page.Locator(target).EvaluateAsync<bool>("node => document.activeElement === node"));
        Assert.Equal("BottomToTop", await page.Locator("[data-cfx-role=pictorial-chart]").GetAttributeAsync("data-cfx-fill-direction"));
        await Capture(page, next, width, mode, "updated-before-readout");
        await page.Keyboard.PressAsync("Home"); Assert.Contains("1.25", await TooltipTextAsync(page));
        await page.SetViewportSizeAsync(width == 390 ? 924 : 414, 700);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 700);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svgText = await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("Missing pictorial SVG export."));
        var svg = XDocument.Parse(svgText); Assert.Equal(3, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "pictorial-item"));
        var images = svg.Descendants().Where(node => node.Name.LocalName == "image").ToArray(); Assert.NotEmpty(images);
        Assert.All(images, image => Assert.StartsWith("data:image/png;base64,", image.Attribute("href")!.Value));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName is "script" or "foreignObject");
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var pngBytes = await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("Missing pictorial PNG export."));
        var png = RasterImageDecoder.Decode(pngBytes); Assert.Equal(width, png.Width); Assert.Equal(460, png.Height);
        await Capture(page, next, width, mode, "updated");
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            var stem = Path.Combine(directory, "pictorial-artwork-" + width + "-" + mode);
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
        }
        await page.EvaluateAsync("() => pictorialArtwork.dispose()"); Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Interaction.Enable(ChartInteractionFeatures.Export); options.Tooltip.PinOnSelect = false;
    }
    private static async Task Capture(IPage page, HtmlChartSection section, int width, VisualThemeMode mode, string phase) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
        Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "pictorial-artwork-" + width + "-" + mode + "-" + phase);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
        await File.WriteAllTextAsync(stem + ".native.svg", section.PreparedVisual.ToSvg()); await File.WriteAllBytesAsync(stem + ".native.png", section.PreparedVisual.ToPng());
        await File.WriteAllTextAsync(stem + ".runtime.json", await page.EvaluateAsync<string>("() => JSON.stringify({selection:pictorialArtwork.getSelection(),viewport:{width:innerWidth,scrollWidth:document.documentElement.scrollWidth}},null,2)"));
    }
}
