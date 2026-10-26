using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidRepeatedSubgraphTests {
    [BrowserTheory]
    [InlineData("flowchart", false, 390)] [InlineData("flowchart", true, 1100)]
    [InlineData("swimlane-beta", false, 390)] [InlineData("swimlane-beta", true, 1100)]
    public async Task MergedSubgraphsPaintOnlyCanonicalCaptionsAndActualParentShells(string family, bool dark, int width) {
        var source = Fixture(family); var options = Options(dark, width); var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        await ObserveBrowserAsync(direct.Artifact!, family, dark, width, "direct");
        var markup = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(markup.HasErrors);
        await ObserveBrowserAsync(Assert.Single(markup.Artifacts), family, dark, width, "fenced");
    }

    private static async Task ObserveBrowserAsync(VisualArtifact artifact, string family, bool dark, int width, string surface) {
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 850);
        var name = "flowchart-repeated-subgraphs-" + family + "-" + surface + "-" + width + (dark ? "-dark" : "-light"); Capture(artifact, name);
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']"); var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            await host.EvaluateAsync("element=>element.scrollLeft=element.scrollWidth");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true }); await host.EvaluateAsync("element=>element.scrollLeft=0");
        }
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clip=[];
                for(const text of svg.querySelectorAll('text')){const r=text.getBoundingClientRect();if(r.width&&r.height&&text.textContent.trim()&&(r.left<frame.left-1.5||r.top<frame.top-1.5||r.right>frame.right+1.5||r.bottom>frame.bottom+1.5))clip.push(text.textContent)}
                const groups=[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(group=>{const r=group.querySelector('[data-cfx-role="topology-group-surface"]').getBoundingClientRect();return {id:group.getAttribute('data-group-id'),parent:group.getAttribute('data-parent-group-id'),x:r.left,y:r.top,right:r.right,bottom:r.bottom}});
                return {groups,nodes:[...svg.querySelectorAll('[data-cfx-role="topology-node"]')].map(node=>({id:node.getAttribute('data-node-id'),parent:node.getAttribute('data-group-id')})),
                    labels:[...svg.querySelectorAll('text')].map(node=>node.textContent),clip,scripts:svg.querySelectorAll('script,image,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".json"), observed.GetRawText());
        var nodes = observed.GetProperty("nodes").EnumerateArray().ToArray(); Assert.Equal(9, nodes.Length);
        foreach (var pair in new[] { ("A", "API"), ("B", "API"), ("S", "Service"), ("T", "Service"), ("O", "Ops"), ("D", "Ops") })
            Assert.Equal(pair.Item2, nodes.Single(node => node.GetProperty("id").GetString() == pair.Item1).GetProperty("parent").GetString());
        var groups = observed.GetProperty("groups").EnumerateArray().ToArray(); Assert.Equal(5, groups.Length);
        foreach (var pair in new[] { ("API", "Product"), ("Service", "Wrapper") }) {
            var child = groups.Single(group => group.GetProperty("id").GetString() == pair.Item1); var parent = groups.Single(group => group.GetProperty("id").GetString() == pair.Item2);
            Assert.Equal(pair.Item2, child.GetProperty("parent").GetString());
            Assert.True(child.GetProperty("x").GetDouble() >= parent.GetProperty("x").GetDouble() - 1 && child.GetProperty("y").GetDouble() >= parent.GetProperty("y").GetDouble() - 1
                && child.GetProperty("right").GetDouble() <= parent.GetProperty("right").GetDouble() + 1 && child.GetProperty("bottom").GetDouble() <= parent.GetProperty("bottom").GetDouble() + 1);
        }
        var labels = observed.GetProperty("labels").EnumerateArray().Select(label => label.GetString()).ToArray();
        foreach (var caption in new[] { "Product delivery", "API", "Service", "Review", "Operations", "Accepted", "Build", "Serve", "Track", "Review work", "Observe", "Deliver", "Qualify" }) Assert.Contains(caption, labels);
        Assert.DoesNotContain(labels, caption => caption?.Contains("Ignored", StringComparison.Ordinal) == true);
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); Assert.False(observed.GetProperty("overflow").GetBoolean());
        if (await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight",new KeyboardPressOptions{Delay=100});
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=\"preserve-size\"]').scrollLeft>0"); Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
