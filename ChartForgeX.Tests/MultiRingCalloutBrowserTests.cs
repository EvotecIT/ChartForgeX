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
public sealed class MultiRingCalloutBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(900, VisualThemeMode.Light)]
    [InlineData(900, VisualThemeMode.Dark)]
    public async Task InnerCalloutsAndExplosionKeepRingSelectionThroughResponsiveUpdateAndNativeExports(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = MultiRingDepthTests.Fixture(mode == VisualThemeMode.Dark, width, false);
        chart.Series[0].WithPointSliceOffset(0, .16); chart.Series[1].WithPointSliceOffset(1, .1);
        var prepared = MultiRingDepthTests.Prepare(chart); MultiRingDepthTests.AssertCalloutAssociations(prepared);
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Options), width + 24, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.ringDepth = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        Assert.Equal(4, await page.Locator("[data-cfx-role=radial-point]").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
                const rows=[...document.querySelectorAll('[data-cfx-radial-callout=overlay][data-cfx-label-dropped=false]')];
                if(!rows.some(row=>row.dataset.cfxSeries==='0'))return false;
                return rows.every(row=>{
                    const source=document.querySelector('[data-cfx-source-id="'+row.dataset.cfxLabelFor+'"]');
                    const label=row.querySelector('text');return source&&label&&row.dataset.cfxFullLabel&&getComputedStyle(row).pointerEvents==='none';
                });
            }
            """));
        const string target = "[data-cfx-role=radial-point][data-cfx-series='0'][data-cfx-point='0']";
        var hit = await NativeInteriorPointAsync(page, target, "[data-cfx-role=donut-slice]", clearance: 2);
        await page.Mouse.MoveAsync((float)hit[0], (float)hit[1], new MouseMoveOptions { Steps = 4 });
        Assert.Contains("Alpha", await TooltipTextAsync(page)); await MoveAwayAsync(page);
        await FocusWithKeyboardAsync(page, page.Locator(target)); await page.Keyboard.PressAsync("Space");
        var selected = Assert.Single((await page.EvaluateAsync<JsonElement>("() => ringDepth.getSelection()")).EnumerateArray());
        Assert.Equal("ring-0", selected.GetProperty("seriesKey").GetString()); Assert.Equal("alpha", selected.GetProperty("observationId").GetString());
        chart.Series[0].WithPointSliceOffset(0, .25);
        await page.EvaluateAsync("html => ringDepth.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Contains((await page.EvaluateAsync<JsonElement>("() => ringDepth.getSelection()")).EnumerateArray(), item => item.GetProperty("observationId").GetString() == "alpha"
            && item.GetProperty("seriesKey").GetString() == "ring-0");
        Assert.Equal("0.25", await page.Locator(target).GetAttributeAsync("data-cfx-slice-offset-ratio"));
        await page.SetViewportSizeAsync(width == 390 ? 924 : 414, 800);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 800);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("Missing ring SVG download.")));
        Assert.Equal(4, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "radial-point"));
        Assert.Contains(svg.Descendants(), node => (string?)node.Attribute("data-cfx-radial-callout") == "overlay" && (string?)node.Attribute("data-cfx-series") == "0");
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("Missing ring PNG download.")));
        Assert.Equal(width, png.Width); Assert.Equal(540, png.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "multiring-depth-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".selection.json", await page.EvaluateAsync<string>("() => JSON.stringify(ringDepth.getSelection())"));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Interaction.Enable(ChartInteractionFeatures.Export); options.Tooltip.PinOnSelect = false;
    }
}
