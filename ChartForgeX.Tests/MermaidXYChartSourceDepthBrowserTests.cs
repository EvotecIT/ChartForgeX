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
public sealed class MermaidXYChartSourceDepthBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390, false)] [InlineData(false, 390, true)] [InlineData(false, 1100, false)] [InlineData(false, 1100, true)]
    [InlineData(true, 390, false)] [InlineData(true, 390, true)] [InlineData(true, 1100, false)] [InlineData(true, 1100, true)]
    public async Task NamedSeriesAndDatumCaptionsKeepNativeFactsSelectionAndCurrentExports(bool horizontal, int width, bool dark) {
        if (!Enabled) return;
        var parsed = new ChartForgeX.Mermaid.MermaidParser().ParseXYChart(MermaidXYChartSourceDepthTests.Source(horizontal)); Assert.Empty(parsed.Diagnostics);
        var chart = ChartForgeX.Mermaid.MermaidXYChartRendering.ToChart(parsed.Document!, MermaidXYChartSourceDepthTests.Options(width, dark).XYChart);
        chart.WithLineMarkers(ChartLineMarkerMode.All);
        var html = chart.ToInteractiveHtmlPage(options => options.Interaction.Enable(ChartInteractionFeatures.ReportReview | ChartInteractionFeatures.Export));
        await using var session = await BrowserProofFonts.OpenAsync(html, width + 24, 800); var page = session.Page;
        var errors = new List<string>(); page.PageError += (_, message) => errors.Add(message);
        await page.WaitForSelectorAsync(Point(1, 0));
        Assert.Equal(new[] { "Intake", "Completed" }, await page.Locator("[data-cfx-role=legend-label]").AllTextContentsAsync());
        var captions = await page.Locator("[data-cfx-role=data-label-source][data-cfx-full-label]").EvaluateAllAsync<string[]>("nodes=>nodes.map(node=>node.dataset.cfxFullLabel)");
        Assert.Equal(new[] { "Opened", "Reviewed, ready", "Released" }.OrderBy(text => text), captions.OrderBy(text => text));
        var visibleCaptions = await page.Locator("[data-cfx-role=data-label-source][data-cfx-full-label]")
            .EvaluateAllAsync<string[]>("""
                nodes => nodes.map(node => [...node.querySelectorAll('text')].flatMap(text => {
                  const rows=[...text.querySelectorAll('tspan')]; return rows.length ? rows.map(row=>row.textContent) : [text.textContent];
                }).join(' ').replace(/\s+/g,' ').trim())
                """);
        Assert.Equal(new[] { "Opened", "Reviewed, ready", "Released" }.OrderBy(text => text), visibleCaptions.OrderBy(text => text));
        await page.Locator(Point(1, 2)).FocusAsync(); await page.Keyboard.PressAsync("Space");
        Assert.Equal("true", await page.Locator(Point(1, 2)).GetAttributeAsync("aria-selected"));
        Assert.Equal("33", await page.Locator(Point(1, 2)).GetAttributeAsync("data-cfx-y"));
        await MoveToAsync(page, Point(1, 2));
        await page.WaitForFunctionAsync("()=>{const tip=document.querySelector('.cfx-tooltip');return tip&&!tip.hidden}");
        Assert.Contains("33", await TooltipTextAsync(page)); await MoveAwayAsync(page);
        var observed = await page.EvaluateAsync<JsonElement>("""
            () => { const svg=document.querySelector('.cfx-interactive-chart .cfx-stage svg'), box=svg.getBoundingClientRect();
              return {labels:[...svg.querySelectorAll('[data-cfx-role="data-label-source"][data-cfx-full-label]')].map(n=>n.dataset.cfxFullLabel),
                clippedLabels:[...svg.querySelectorAll('[data-cfx-role="data-label"]')].filter(n=>{const b=n.getBoundingClientRect();return b.left<box.left-1||b.top<box.top-1||b.right>box.right+1||b.bottom>box.bottom+1}).map(n=>n.textContent),
                values:[...svg.querySelectorAll('[data-cfx-role="point"]')].map(n=>Number(n.dataset.cfxY)),
                activeSvgElements:svg.querySelectorAll('script,image,foreignObject').length}; }
            """);
        var vector = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var vectorPath = await vector.PathAsync() ?? throw new InvalidOperationException("XY SVG download was not persisted.");
        var exported = XDocument.Load(vectorPath);
        Assert.Equal(new[] { "Opened", "Reviewed, ready", "Released" }.OrderBy(text => text), exported.Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "data-label-source").Select(node => (string)node.Attribute("data-cfx-full-label")!).OrderBy(text => text));
        Assert.Equal(new[] { "Opened", "Reviewed, ready", "Released" }.OrderBy(text => text),
            (await BrowserProofCaptions.ExportedAsync(page, exported, "data-label-source")).OrderBy(text => text));
        var pixels = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var pixelPath = await pixels.PathAsync() ?? throw new InvalidOperationException("XY PNG download was not persisted.");
        var image = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(pixelPath)); Assert.Equal(width, image.Width); Assert.Equal(520, image.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "xy-source-depth-" + (horizontal ? "horizontal-" : "vertical-") + width + (dark ? "-dark" : "-light"));
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await vector.SaveAsAsync(stem + ".export.svg"); await pixels.SaveAsAsync(stem + ".export.png");
            File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, visibleCaptions, errors, session.ConsoleLog }));
            File.WriteAllText(stem + ".source.mmd", MermaidXYChartSourceDepthTests.Source(horizontal));
        }
        Assert.Empty(observed.GetProperty("clippedLabels").EnumerateArray()); Assert.Equal(0, observed.GetProperty("activeSvgElements").GetInt32());
        Assert.Equal(new[] { 18d, 70, 44, 85, 12, 60, 33, 75 }.OrderBy(value => value), observed.GetProperty("values").EnumerateArray().Select(value => value.GetDouble()).OrderBy(value => value));
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
}
