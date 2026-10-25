using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Microsoft.Playwright;
using Xunit;
using static ChartForgeX.Tests.InteractiveChartBrowser;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class SeamPolygonMapBrowserTests {
    [BrowserTheory]
    [InlineData("compact", VisualThemeMode.Light)]
    [InlineData("wide", VisualThemeMode.Dark)]
    [InlineData("compact-mercator", VisualThemeMode.Dark)]
    [InlineData("mercator", VisualThemeMode.Light)]
    public async Task NativeSeamRingsAndFeatureSnapshotsSurviveHiddenTargetExclusionSelectionUpdateAndExports(string variant, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = SeamPolygonMapRecipe.Create(variant, mode); var width = variant.StartsWith("compact", StringComparison.Ordinal) ? 390 : 800;
        var prepared = MapLabelClusterDepthTests.Prepare(chart);
        var map = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "region-map-surface").Bounds;
        var viewport = chart.Options.MapViewport; var projection = viewport.GeographicProjection!.Value;
        var source = ChartGeographicProjection.Bounds(viewport, projection);
        double[] At(double longitude, double latitude) {
            var point = ChartGeographicProjection.Project(longitude, latitude, projection, viewport);
            return new[] { map.Left + (point.X - source.Left) / source.Width * map.Width, map.Top + (point.Y - source.Top) / source.Height * map.Height };
        }
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), width + 24, 900);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        await page.EvaluateAsync("() => { window.seamPolygonChart = ChartForgeXCharts.get(document.querySelector('.cfx-interactive-chart')); }");
        var hidden = page.Locator(Region("outside-observation"));
        Assert.Equal("false", await hidden.GetAttributeAsync("data-cfx-visible")); Assert.Null(await hidden.GetAttributeAsync("tabindex")); Assert.NotEqual("button", await hidden.GetAttributeAsync("role"));
        Assert.Equal("outside-source", await hidden.GetAttributeAsync("data-cfx-feature-id"));
        var island = page.Locator(Region("island-observation"));
        Assert.Equal("123456789012345678901", await island.GetAttributeAsync("data-cfx-feature-id-json"));
        var raw = JsonDocument.Parse(await island.GetAttributeAsync("data-cfx-geojson-geometry") ?? throw new InvalidOperationException("Missing seam source rings."));
        Assert.Equal("175.00", raw.RootElement.GetProperty("coordinates")[0][0][0].GetRawText()); Assert.Equal("1e+1", raw.RootElement.GetProperty("coordinates")[0][0][2].GetRawText());
        var path = island.Locator("[data-cfx-role='region-map-region']");
        Assert.False(await path.EvaluateAsync<bool>("(node, p) => node.isPointInFill(new DOMPoint(p[0],p[1]))", At(180, -15)));
        Assert.True(await path.EvaluateAsync<bool>("(node, p) => node.isPointInFill(new DOMPoint(p[0],p[1]))", At(176, -15)));
        await FocusWithKeyboardAsync(page, island); await page.Keyboard.PressAsync("Space");
        Assert.Contains((await Selection(page)).EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "island-observation");
        Assert.DoesNotContain((await Selection(page)).EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "123456789012345678901");
        chart.Series[0].UpdateRegionValue(0, new ChartRegionMapItem("I", 95));
        await page.EvaluateAsync("html => seamPolygonChart.update(html)", chart.ToInteractiveHtmlFragmentWithoutAssets(Options));
        Assert.Equal("95", await page.Locator(Region("island-observation")).GetAttributeAsync("data-cfx-value"));
        Assert.Contains((await Selection(page)).EnumerateArray(), item => item.TryGetProperty("observationId", out var id) && id.GetString() == "island-observation");
        Assert.Equal(raw.RootElement.GetRawText(), await page.Locator(Region("island-observation")).GetAttributeAsync("data-cfx-geojson-geometry"));
        await page.SetViewportSizeAsync(width == 390 ? 824 : 414, 900);
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth + 1"));
        Assert.Equal("false", await page.Locator(Region("outside-observation")).GetAttributeAsync("data-cfx-visible"));
        await page.SetViewportSizeAsync(width + 24, 900);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=svg]").ClickAsync());
        var svg = XDocument.Parse(await File.ReadAllTextAsync(await svgDownload.PathAsync() ?? throw new InvalidOperationException("No seam polygon SVG download.")));
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
        var exported = svg.Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "region-map-region-source").ToArray(); Assert.Equal(3, exported.Length);
        Assert.Equal("false", (string?)exported.Single(node => (string?)node.Attribute("data-cfx-region") == "O").Attribute("data-cfx-visible"));
        Assert.Equal(raw.RootElement.GetRawText(), (string?)exported.Single(node => (string?)node.Attribute("data-cfx-region") == "I").Attribute("data-cfx-geojson-geometry"));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export=png]").ClickAsync());
        var png = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(await pngDownload.PathAsync() ?? throw new InvalidOperationException("No seam polygon PNG download.")));
        Assert.Equal(width, png.Width); Assert.Equal(540, png.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "seam-polygon-map-" + variant + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".selection.json", (await Selection(page)).GetRawText());
        }
        Assert.Empty(errors); AssertNoConsoleErrors(session);
    }

    private static string Region(string id) => "[data-cfx-role='region-map-region-source'][data-cfx-observation-id='" + id + "']";
    private static Task<JsonElement> Selection(IPage page) => page.EvaluateAsync<JsonElement>("() => seamPolygonChart.getSelection()");
    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit; options.Tooltip.PinOnSelect = false;
    }
}
