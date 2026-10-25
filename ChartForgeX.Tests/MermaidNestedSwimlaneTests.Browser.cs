using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNestedSwimlaneTests {
    [BrowserTheory]
    [InlineData("LR", false, 390)] [InlineData("RL", true, 1100)] [InlineData("TB", false, 1100)] [InlineData("BT", true, 390)]
    public async Task NestedResponsibilityShellsAndCompleteCaptionsSurviveDirectAndFencedScrollableHosts(string direction, bool dark, int width) {
        var source = Fixture(direction); var options = Options(dark, width); var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        await ObserveBrowserAsync(direct.Artifact!, direction, dark, width, "direct");
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors);
        await ObserveBrowserAsync(Assert.Single(fence.Artifacts), direction, dark, width, "fenced");
    }

    private static async Task ObserveBrowserAsync(VisualArtifact artifact, string direction, bool dark, int width, string surface) {
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 850);
        var name = "swimlane-nesting-" + direction + "-" + surface + "-" + width + (dark ? "-dark" : "-light"); Capture(artifact, name);
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']"); var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            await host.EvaluateAsync("element=>element.scrollLeft=element.scrollWidth");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true }); await host.EvaluateAsync("element=>element.scrollLeft=0");
        }
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clip=[];
                const box=element=>{const r=element.getBoundingClientRect();return {x:r.left,y:r.top,right:r.right,bottom:r.bottom}};
                const groups=[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(group=>({id:group.getAttribute('data-group-id'),parent:group.getAttribute('data-parent-group-id'),...box(group.querySelector('[data-cfx-role="topology-group-surface"]'))}));
                for(const text of svg.querySelectorAll('text')) {const r=text.getBoundingClientRect();if(r.width&&r.height&&text.textContent.trim()&&(r.left<frame.left-1.5||r.top<frame.top-1.5||r.right>frame.right+1.5||r.bottom>frame.bottom+1.5))clip.push(text.textContent)}
                return {groups,labels:[...svg.querySelectorAll('text')].map(text=>text.textContent),clip,nodes:svg.querySelectorAll('[data-cfx-role="topology-node"]').length,
                    edges:svg.querySelectorAll('[data-cfx-role="topology-edge"]').length,scripts:svg.querySelectorAll('script,image,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".json"), observed.GetRawText());
        var groups = observed.GetProperty("groups").EnumerateArray().ToArray(); Assert.Equal(5, groups.Length);
        foreach (var child in groups.Where(group => !string.IsNullOrEmpty(group.GetProperty("parent").GetString()))) {
            var parent = groups.Single(group => group.GetProperty("id").GetString() == child.GetProperty("parent").GetString());
            Assert.True(child.GetProperty("x").GetDouble() >= parent.GetProperty("x").GetDouble() - 1 && child.GetProperty("y").GetDouble() >= parent.GetProperty("y").GetDouble() - 1
                && child.GetProperty("right").GetDouble() <= parent.GetProperty("right").GetDouble() + 1 && child.GetProperty("bottom").GetDouble() <= parent.GetProperty("bottom").GetDouble() + 1);
        }
        var labels = observed.GetProperty("labels").EnumerateArray().Select(label => label.GetString()).ToArray();
        foreach (var caption in new[] { "Product delivery", "API", "Storage", "Operations", "Partner", "Plan", "Accept", "Build", "Persist", "Check", "Deliver", "Observe", "External" }) Assert.Contains(caption, labels);
        Assert.Equal(8, observed.GetProperty("nodes").GetInt32()); Assert.Equal(7, observed.GetProperty("edges").GetInt32()); Assert.Empty(observed.GetProperty("clip").EnumerateArray());
        Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        if (await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=\"preserve-size\"]').scrollLeft>0");
            Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
