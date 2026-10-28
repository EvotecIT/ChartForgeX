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
public sealed class MapPeriodicRouteCopyBrowserTests {
    [BrowserTheory]
    [InlineData(390, VisualThemeMode.Light)]
    [InlineData(800, VisualThemeMode.Dark)]
    public async Task NearWorldPeerClustersKeepTwoPaintedFragmentsAndOneSourceIdentityInBrowserAndDownloads(int width, VisualThemeMode mode) {
        if (!Enabled) return;
        var chart = MapPeriodicRouteCopyTests.Clustered(width, mode).WithDataLabels();
        await using var session = await OpenAsync(chart.ToInteractiveHtmlPage(Options), width + 24, 650);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var source = page.Locator("[data-cfx-role='dotted-map-connector-source']"); Assert.Equal(1, await source.CountAsync());
        Assert.Equal("periodic-route", await source.GetAttributeAsync("data-cfx-feature-id"));
        Assert.Equal("140,0;-130,0", await source.GetAttributeAsync("data-cfx-route-coordinates"));
        Assert.Equal("true", await source.GetAttributeAsync("data-cfx-route-view-clipped"));
        Assert.Equal(4, await page.Locator("[data-cfx-role='dotted-map-point-source']").CountAsync());
        Assert.Equal(1, await page.Locator("[data-cfx-role='dotted-map-connector-label-source']").CountAsync());
        var defects = await page.EvaluateAsync<string[]>("""
            () => {
                const defects = [];
                const paths = [...document.querySelectorAll('[data-cfx-role="dotted-map-connector"]')];
                const clusters = [...document.querySelectorAll('[data-cfx-role="dotted-map-cluster"]')]
                    .map(node => ({x: node.cx.baseVal.value, y: node.cy.baseVal.value, r: node.rx.baseVal.value})).sort((a,b) => a.x-b.x);
                if (paths.length !== 2 || clusters.length !== 2) return ['native fragment/cluster count'];
                const middle = (clusters[0].x + clusters[1].x) / 2;
                for (const cluster of clusters) {
                    const matches = paths.filter(path => { const box = path.getBBox(); return cluster.x < middle ? box.x + box.width < middle : box.x > middle; });
                    if (matches.length !== 1) { defects.push('a periodic fragment crosses or misses its endpoint side'); continue; }
                    const path = matches[0], length = path.getTotalLength();
                    if (!(length > 0)) defects.push('empty native fragment');
                    const start = path.getPointAtLength(0), end = path.getPointAtLength(length);
                    const distance = Math.min(Math.hypot(start.x-cluster.x,start.y-cluster.y), Math.hypot(end.x-cluster.x,end.y-cluster.y));
                    if (!(distance >= cluster.r + 2 && distance <= cluster.r + 16)) defects.push('periodic cluster clearance');
                    if ((path.getAttribute('d').match(/M/g) || []).length !== 1) defects.push('disconnected native decoration path');
                }
                return defects;
            }
            """);
        var svgDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export='svg']").ClickAsync());
        var svgPath = await svgDownload.PathAsync() ?? throw new InvalidOperationException("No periodic map SVG download.");
        var svg = XDocument.Parse(await File.ReadAllTextAsync(svgPath));
        Assert.Equal(1, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-connector-source"));
        Assert.Equal(2, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-connector"));
        Assert.Equal(1, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-connector-label-source"));
        Assert.Equal(4, svg.Descendants().Count(node => (string?)node.Attribute("data-cfx-role") == "dotted-map-point-source"));
        var pngDownload = await page.RunAndWaitForDownloadAsync(() => page.Locator("[data-cfx-export='png']").ClickAsync());
        var pngPath = await pngDownload.PathAsync() ?? throw new InvalidOperationException("No periodic map PNG download.");
        var image = RasterImageDecoder.Decode(await File.ReadAllBytesAsync(pngPath)); Assert.Equal(width, image.Width); Assert.Equal(360, image.Height);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            if (!Path.IsPathRooted(directory)) throw new InvalidOperationException("Captures require an absolute task path.");
            Directory.CreateDirectory(directory); var stem = Path.Combine(directory, "near-world-peer-cluster-fragments-" + width + "-" + mode);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            await svgDownload.SaveAsAsync(stem + ".export.svg"); await pngDownload.SaveAsAsync(stem + ".export.png");
            await File.WriteAllTextAsync(stem + ".json", JsonSerializer.Serialize(new { width, mode, defects, errors, session.ConsoleLog }));
        }
        Assert.Empty(defects); Assert.Empty(errors); AssertNoConsoleErrors(session);
    }
    private static void Options(HtmlChartInteractionOptions options) {
        options.Interaction.Enable(ChartInteractionFeatures.Export); options.ResponsiveLayout = HtmlChartResponsiveLayout.Fit;
    }
}
