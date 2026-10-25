using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite","Compatibility")]
public sealed class MermaidSwimlaneOrderingBrowserTests {
    [BrowserTheory]
    [InlineData(false,390)] [InlineData(true,1100)]
    public async Task ActualBrowserUsesTheSameOrderedNativeSceneAndScriptFreeExports(bool dark,int width) {
        var result=MermaidRenderer.Render(MermaidSwimlaneOrderingCausalTests.Source("LR",true),MermaidSwimlaneLayoutDepthTests.Options(dark,width));Assert.Empty(result.Diagnostics);
        var artifact=result.Artifact!;MermaidNestedSwimlaneTests.Capture(artifact,"swimlane-auto-order-browser-"+width+"-"+dark);
        await using var session=await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions {HtmlSizing=VisualArtifactHtmlSizing.PreserveSize}),width,950);
        var order=await session.Page.EvaluateAsync<string[]>("() => [...document.querySelectorAll('[data-cfx-role=\"topology-group\"]')].sort((a,b)=>a.getBoundingClientRect().top-b.getBoundingClientRect().top).map(node=>node.getAttribute('data-group-id'))");
        var output=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(!string.IsNullOrWhiteSpace(output)){Directory.CreateDirectory(output);await session.Page.ScreenshotAsync(new PageScreenshotOptions {Path=Path.Combine(output,"swimlane-auto-order-"+width+"-"+dark+".browser.png"),FullPage=true});}
        Assert.True(MermaidSwimlaneOrderingCausalTests.Cost(order)<12);Assert.Equal(0,await session.Page.Locator("svg script,svg image,svg foreignObject").CountAsync());InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
