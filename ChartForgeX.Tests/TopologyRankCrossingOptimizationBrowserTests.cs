using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed class TopologyRankCrossingOptimizationBrowserTests {
    [BrowserTheory]
    [InlineData(false,390)] [InlineData(true,1100)]
    public async Task RepairedRankSceneRetainsNativeActualGeometryAcrossBrowserSvgPngAndTypedRestoration(bool dark,int width) {
        var rendered=MermaidRenderer.Render(TopologyRankOptimizationCausalTests.Source("LR"),MermaidSwimlaneLayoutDepthTests.Options(dark,width));Assert.Empty(rendered.Diagnostics);
        var chart=Assert.IsType<TopologyChart>(rendered.Artifact!.Model).WithId("rank-crossing-optimized");TopologyRankOptimizationCausalTests.SetMinima(chart);var prepared=chart.Prepare();
        Assert.Equal(0,TopologyRankOptimizationCausalTests.Crossings(prepared.ToInterchangeEnvelope()));var artifact=chart.ToVisualArtifact();
        MermaidNestedSwimlaneTests.Capture(artifact,"rank-crossing-browser-"+width+"-"+dark);
        await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions {HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,950);
        Assert.Equal(5,await session.Page.Locator("[data-cfx-role='topology-node']").CountAsync());Assert.Equal(2,await session.Page.Locator("[data-cfx-role='topology-edge']").CountAsync());
        Assert.Equal(0,await session.Page.Locator("svg script,svg image,svg foreignObject").CountAsync());
        var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(!string.IsNullOrWhiteSpace(output)){Directory.CreateDirectory(output);await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path=Path.Combine(output,"rank-crossing-"+width+"-"+dark+".browser.png"),FullPage=true});}
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
