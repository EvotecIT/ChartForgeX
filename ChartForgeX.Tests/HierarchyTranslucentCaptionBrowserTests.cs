using ChartForgeX.Primitives;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class HierarchyTranslucentCaptionBrowserTests {
    public static IEnumerable<object[]> Cases() {
        foreach (var kind in new[] { ChartSeriesKind.Tree, ChartSeriesKind.Treemap, ChartSeriesKind.Sunburst })
            foreach (var width in new[] { 390, 1100 }) foreach (var dark in new[] { false, true }) yield return new object[] { kind, width, dark };
    }

    [BrowserTheory]
    [MemberData(nameof(Cases))]
    public async Task AutomaticInkMatchesThePreparedSurfaceInCompactWideLightAndDarkHosts(ChartSeriesKind kind, int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var chart = HierarchyTranslucentCaptionCausalTests.Single(kind).WithSize(width, 460)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light).WithTransparentBackground(false);
        chart.ConfigureDataLabelStyle(style => style.FontFamily = BrowserProofFonts.Family);
        chart.Series[0].WithPointColor(0, (dark ? ChartColor.White : ChartColor.Black).WithAlpha(24));
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context); var artifact = chart.ToVisualArtifact();
        var nativeInk = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role is "tree-node-label" or "treemap-label" or "sunburst-label").Color;
        var actualSurface = HierarchyCaptionSurfaceTestFacts.FrameSurface(prepared);
        var mark = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneMark>(), item => item.Role is "tree-node-mark" or "treemap-tile-mark" or "sunburst-segment-mark");
        actualSurface = ChartColorMath.CompositeOverOpaque(mark.Fill!.Value, actualSurface);
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(), width, 560);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, error) => errors.Add(error);
        var caption = page.Locator("[data-cfx-role='tree-node-label'],[data-cfx-role='treemap-label'],[data-cfx-role='sunburst-label']");
        var name = "hierarchy-translucent-" + kind + "-" + width + "-" + (dark ? "dark" : "light");
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, name + ".svg"), artifact.ToSvg());
            File.WriteAllBytes(Path.Combine(directory, name + ".native.png"), artifact.ToPng());
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(directory, name + ".browser.png"), FullPage = true });
        }
        Assert.True(await caption.IsVisibleAsync());
        var paintedInks = await caption.Locator("text").EvaluateAllAsync<string[]>("nodes => nodes.map(node => getComputedStyle(node).fill)");
        Assert.NotEmpty(paintedInks);
        foreach (var paintedInk in paintedInks) {
            Assert.True(SvgRasterColor.TryParse(paintedInk, out var browserInk));
            Assert.Equal(nativeInk, browserInk);
            Assert.True(ChartColorMath.ContrastRatio(browserInk, actualSurface) >= 4.5);
        }
        Assert.Equal(0, await page.Locator("script,foreignObject").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }


}
