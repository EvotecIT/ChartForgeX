using ChartForgeX.Themes;
using ChartForgeX.Svg;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyExplicitLabelMarkerBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 1200)]
    public async Task RestoredPinnedBaselineAndFillOnlyMarkersUseTheNativeSvgAndPortableFacts(bool dark, int width) {
        var source = TopologyExplicitLabelMarkerTests.Source().WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family), dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var saved = source.Prepare().ToInterchangeEnvelope(); var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        var artifact = restored.ToVisualArtifact();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.FitToWidth }), width, 720);
        var y = await session.Page.Locator("[data-cfx-role=topology-edge-label-text] text").GetAttributeAsync("y");
        Assert.Equal(SvgMarkupWriter.FormatNumber(saved.Edges[0].Topology!.LabelPosition!.Point.Y), y);
        Assert.Equal(2, await session.Page.Locator("[data-cfx-role=topology-marker][stroke-width='0']").CountAsync());
        Assert.Equal(0, await session.Page.Locator("svg script,svg image,svg foreignObject").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth")); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "explicit-label-markers-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", artifact.ToPng()); File.WriteAllText(path + ".portable.json", artifact.ToInterchangeJson());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}
