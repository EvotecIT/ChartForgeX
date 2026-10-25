using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidArchitectureNativeDepthTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(false, 1100)] [InlineData(true, 390)] [InlineData(true, 1100)]
    public async Task RealNestedArchitectureAndBoundaryRoutesRemainVisibleInDirectAndFencedHosts(bool dark, int width) {
        var source = Fixture(); var options = Options(dark, width);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        await ObserveBrowserAsync(direct.Artifact!, dark, width, "direct");
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fenced.HasErrors);
        await ObserveBrowserAsync(Assert.Single(fenced.Artifacts), dark, width, "fenced");
    }

    private static async Task ObserveBrowserAsync(VisualArtifact artifact, bool dark, int width, string surface) {
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 800);
        var name = "architecture-boundaries-" + surface + "-" + width + (dark ? "-dark" : "-light");
        Capture(artifact, name);
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        // Preserve initial and scrolled real surfaces before meaningful assertions.
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            await host.EvaluateAsync("element => element.scrollLeft=element.scrollWidth");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
            await host.EvaluateAsync("element => element.scrollLeft=0");
        }
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clip=[];
                for(const text of svg.querySelectorAll('text')) {
                    const box=text.getBoundingClientRect();
                    if(box.width&&box.height&&text.textContent.trim()&&(box.left<frame.left-1.5||box.top<frame.top-1.5||box.right>frame.right+1.5||box.bottom>frame.bottom+1.5)) clip.push(text.textContent);
                }
                const groups=[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(group=>({id:group.getAttribute('data-group-id'),parent:group.getAttribute('data-parent-group-id')}));
                return {groups,clip,text:svg.textContent,nodes:svg.querySelectorAll('[data-cfx-role="topology-node"]').length,
                    edges:[...svg.querySelectorAll('[data-cfx-role="topology-edge"]')].map(edge=>({source:edge.getAttribute('data-source-node-id'),target:edge.getAttribute('data-target-node-id'),sourcePort:edge.getAttribute('data-source-port'),targetPort:edge.getAttribute('data-target-port')})),
                    scripts:svg.querySelectorAll('script,foreignObject,image').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".json"), observed.GetRawText());
        Assert.Equal(5, observed.GetProperty("nodes").GetInt32()); Assert.Equal(4, observed.GetProperty("groups").GetArrayLength());
        Assert.Equal("platform", observed.GetProperty("groups").EnumerateArray().Single(group => group.GetProperty("id").GetString() == "private").GetProperty("parent").GetString());
        Assert.Equal("private", observed.GetProperty("groups").EnumerateArray().Single(group => group.GetProperty("id").GetString() == "data").GetProperty("parent").GetString());
        var edges = observed.GetProperty("edges"); Assert.Equal(4, edges.GetArrayLength());
        Assert.Equal("data", edges[3].GetProperty("source").GetString()); Assert.Equal("partner", edges[3].GetProperty("target").GetString());
        Assert.Equal("Right", edges[3].GetProperty("sourcePort").GetString()); Assert.Equal("Left", edges[3].GetProperty("targetPort").GetString());
        Assert.Equal("split", edges[1].GetProperty("source").GetString()); Assert.Equal("Bottom", edges[1].GetProperty("sourcePort").GetString());
        foreach (var label in new[] { "Platform", "Private", "Data", "Partner", "Gateway", "Worker", "Storage", "Remote" }) Assert.Contains(label, observed.GetProperty("text").GetString());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("overflow").GetBoolean());
        Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        if (await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=\"preserve-size\"]').scrollLeft>0");
            Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
