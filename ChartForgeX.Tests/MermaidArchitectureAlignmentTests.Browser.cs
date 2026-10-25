using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidArchitectureAlignmentTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(false, 1100)] [InlineData(true, 390)] [InlineData(true, 1100)]
    public async Task AuthoredRowsColumnsAndNestedBodiesRemainVisibleAndScrollableInBothConsumers(bool dark, int width) {
        var source = Fixture(); var options = MermaidArchitectureNativeDepthTests.Options(dark, width);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        await ObserveAlignedAsync(direct.Artifact!, dark, width, "direct");
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fenced.HasErrors);
        await ObserveAlignedAsync(Assert.Single(fenced.Artifacts), dark, width, "fenced");
    }

    private static async Task ObserveAlignedAsync(VisualArtifact artifact, bool dark, int width, string consumer) {
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 800);
        var name = "architecture-alignments-" + consumer + "-" + width + (dark ? "-dark" : "-light"); Capture(artifact, name);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            await host.EvaluateAsync("element=>element.scrollLeft=element.scrollWidth");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
            await host.EvaluateAsync("element=>element.scrollLeft=0");
        }
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clip=[];
                const bounds=element=>{const r=element.getBoundingClientRect();return {x:r.x,y:r.y,right:r.right,bottom:r.bottom,cx:r.x+r.width/2,cy:r.y+r.height/2}};
                const nodes=[...svg.querySelectorAll('[data-cfx-role="topology-node"]')].map(node=>({id:node.dataset.nodeId,...bounds(node.querySelector('[data-cfx-role="topology-node-surface"]'))}));
                const groups=[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(group=>({id:group.dataset.groupId,parent:group.dataset.parentGroupId??null,...bounds(group.querySelector('[data-cfx-role="topology-group-surface"]'))}));
                for(const text of svg.querySelectorAll('text')){const r=text.getBoundingClientRect();if(r.width&&r.height&&text.textContent.trim()&&(r.left<frame.left-1.5||r.top<frame.top-1.5||r.right>frame.right+1.5||r.bottom>frame.bottom+1.5))clip.push(text.textContent)}
                return {nodes,groups,clip,text:svg.textContent,edges:svg.querySelectorAll('[data-cfx-role="topology-edge"]').length,scripts:svg.querySelectorAll('script,image,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".observed.json"), observed.GetRawText());
        AssertGeometry(artifact.ToInterchangeEnvelope()); Assert.Equal(7, observed.GetProperty("nodes").GetArrayLength()); Assert.Equal(4, observed.GetProperty("groups").GetArrayLength()); Assert.Equal(4, observed.GetProperty("edges").GetInt32());
        foreach (var declaration in artifact.ToInterchangeEnvelope().Topology!.Alignments) {
            var members = declaration.NodeIds.Select(id => observed.GetProperty("nodes").EnumerateArray().Single(node => node.GetProperty("id").GetString() == id)).ToArray();
            for (var i = 1; i < members.Length; i++) {
                if (declaration.Kind == ChartForgeX.Topology.TopologyAlignmentKind.Row) {
                    Assert.InRange(Math.Abs(members[0].GetProperty("cy").GetDouble() - members[i].GetProperty("cy").GetDouble()), 0, .1);
                    Assert.True(members[i - 1].GetProperty("right").GetDouble() < members[i].GetProperty("x").GetDouble());
                } else {
                    Assert.InRange(Math.Abs(members[0].GetProperty("cx").GetDouble() - members[i].GetProperty("cx").GetDouble()), 0, .1);
                    Assert.True(members[i - 1].GetProperty("bottom").GetDouble() < members[i].GetProperty("y").GetDouble());
                }
            }
        }
        foreach (var label in new[] { "Platform", "Applications", "Data", "Partner", "Client", "API", "Queue", "Worker", "Audit", "Remote" }) Assert.Contains(label, observed.GetProperty("text").GetString());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        if (await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=\"preserve-size\"]').scrollLeft>0"); Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
