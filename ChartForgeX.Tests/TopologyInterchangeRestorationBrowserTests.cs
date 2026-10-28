using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyInterchangeRestorationBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 1200)]
    public async Task LoadedAndEditedEventDataKeepsSourceIdentityVisibleInScriptFreeNativeHtml(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidEventModelingDepthTests.Source, MermaidEventModelingDepthTests.Options(dark, width));
        Assert.False(result.HasErrors); var original = result.Artifact!;
        var json = original.ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        var chart = envelope.ToTopologyChart(); chart.Nodes.Single(node => node.Id == "20").Details[0].Value = "String: RESTORED EDIT";
        var artifact = chart.ToVisualArtifact(); var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 760);
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.Equal(9, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => {
                const nodes=[...document.querySelectorAll('[data-cfx-role="topology-node"]')];
                const edited=nodes.find(node=>node.dataset.nodeId==='20');
                if(!edited||!edited.textContent.includes('RESTORED EDIT'))return false;
                const surface=edited.querySelector('[data-cfx-role="topology-node-surface"]')?.getBBox();
                if(!surface)return false;
                return [...edited.querySelectorAll('text')].every(text=>{
                    const b=text.getBBox();return b.x>=surface.x-.2&&b.y>=surface.y-.2&&b.x+b.width<=surface.x+surface.width+.2&&b.y+b.height<=surface.y+surface.height+.2;
                });
            }
            """));
        var text = string.Join("\n", await session.Page.Locator("svg text").AllTextContentsAsync());
        Assert.Contains("RESTORED EDIT", text); Assert.Contains("/escaped~1key/~0value", text); Assert.Contains("reset", text);
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        Assert.Equal(json, envelope.ToJson()); Assert.DoesNotContain("RESTORED EDIT", original.ToSvg());
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, "topology-restored-event-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}
