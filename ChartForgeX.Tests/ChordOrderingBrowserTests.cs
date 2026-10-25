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
public sealed class ChordOrderingBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(390, VisualThemeMode.Dark)]
    [InlineData(800, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public async Task DisplayOrderAndTensionKeepSourceKeyboardSelectionAndCurrentNativeExports(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = Model(width, mode); var updated = Model(width, mode);
        updated.ConfigureChord(options => {
            options.Ordering = new ChartChordOrdering(new[] { "ops", "web", "data", "api", "idle" },
                updated.Series[0].FlowLinks.Select(flow => flow.Id), updated.Series[0].FlowLinks.Reverse().Select(flow => flow.Id));
            options.RibbonTension = 1;
        });
        await using var session = await BrowserProofFonts.OpenAsync(chart.ToInteractiveHtmlFragment(Options), width + 24, 800);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.orderChord = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        Assert.Equal(new[] { "api-data", "api-priority", "web-ops", "ops-web", "data-self", "zero" },
            await page.Locator("[data-cfx-target-kind=link]").EvaluateAllAsync<string[]>("nodes => nodes.map(node => node.dataset.cfxTargetId)"));
        var hit = await NativeInteriorPointAsync(page, Link("api-data"), "[data-cfx-role=chord-ribbon]", clearance: 2);
        await MoveAwayAsync(page); await page.Mouse.ClickAsync((float)hit[0], (float)hit[1]);
        Assert.Contains((await page.EvaluateAsync<JsonElement>("() => orderChord.getSelection()")).EnumerateArray(),
            item => item.GetProperty("targetId").GetString() == "api-data");
        var priority = page.Locator(Link("api-priority"));
        await FocusWithKeyboardAsync(page, priority); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await priority.GetAttributeAsync("aria-selected"));
        Assert.Equal("5 3", await priority.Locator("[data-cfx-role=chord-ribbon]").GetAttributeAsync("stroke-dasharray"));
        await page.EvaluateAsync("html => orderChord.update(html)", updated.ToInteractiveHtmlFragmentWithoutAssets(Options));
        var selected = await page.EvaluateAsync<JsonElement>("() => orderChord.getSelection()");
        Assert.Contains(selected.EnumerateArray(), item => item.GetProperty("targetId").GetString() == "api-priority");
        Assert.Equal("1", await page.Locator("[data-cfx-role=chord-series]").GetAttributeAsync("data-cfx-ribbon-tension"));
        Assert.Equal("12", await page.Locator(Link("api-data")).GetAttributeAsync("data-cfx-value"));
        await FocusWithKeyboardAsync(page, page.Locator(Link("data-self"))); await page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("zero", await page.EvaluateAsync<string>("() => document.activeElement.dataset.cfxTargetId"));
        Assert.Equal("0", await page.Locator(Link("zero")).GetAttributeAsync("data-cfx-value"));
        Assert.Equal(0, await page.Locator(Link("zero") + " [data-cfx-role=chord-ribbon]").CountAsync());
        await page.SetViewportSizeAsync(width == 390 ? 824 : 414, 800);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        await page.SetViewportSizeAsync(width + 24, 800);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No Chord SVG download.")));
        var flows = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "chord-link").ToArray();
        Assert.Equal(updated.Series[0].FlowLinks.Select(flow => flow.Id), flows.Select(node => (string?)node.Attribute("data-cfx-target-id")));
        Assert.Equal("explicit", (string?)svg.Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "chord-series").Attribute("data-cfx-node-order"));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No Chord PNG download.")));
        Assert.Equal(width, png.Width); Assert.Equal(width == 390 ? 460 : 520, png.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task-owned path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "chord-order-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".runtime.json", await page.EvaluateAsync<string>("() => JSON.stringify({selection:orderChord.getSelection(),flows:[...document.querySelectorAll('[data-cfx-role=chord-link]')].map(node=>({...node.dataset}))},null,2)"));
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static Chart Model(int width, VisualThemeMode mode) => ChordDepthRecipe.Create(width == 390 ? "compact" : "wide", mode)
        .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), mode);
    private static void Options(HtmlChartInteractionOptions options) {
        options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.Tooltip.PinOnSelect = false;
    }
    private static string Link(string id) => "[data-cfx-target-kind=link][data-cfx-target-id='" + id + "']";
}
