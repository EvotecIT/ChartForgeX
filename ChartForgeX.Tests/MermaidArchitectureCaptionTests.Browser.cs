using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidArchitectureCaptionTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(false, 1100)] [InlineData(true, 390)] [InlineData(true, 1100)]
    public async Task OptionalCaptionsAndNativeBuiltinsStayVisibleAsAuthoredInDirectAndFencedHosts(bool dark, int width) {
        var source = Fixture(); var options = MermaidArchitectureNativeDepthTests.Options(dark, width);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        await ObserveBrowserAsync(direct.Artifact!, dark, width, "direct");
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors);
        await ObserveBrowserAsync(Assert.Single(fence.Artifacts), dark, width, "fenced");
    }

    private static async Task ObserveBrowserAsync(VisualArtifact artifact, bool dark, int width, string surface) {
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 800);
        var name = "architecture-captions-" + surface + "-" + width + (dark ? "-dark" : "-light"); Capture(artifact, name);
        var host = session.Page.Locator("[data-cfx-html-sizing='preserve-size']"); var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".browser.png"), FullPage = true });
            await host.EvaluateAsync("element=>element.scrollLeft=element.scrollWidth");
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, name + ".scrolled.browser.png"), FullPage = true });
            await host.EvaluateAsync("element=>element.scrollLeft=0");
        }
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
                const svg=document.querySelector('svg'),frame=svg.getBoundingClientRect(),clip=[];
                for(const text of svg.querySelectorAll('text')) {
                    const box=text.getBoundingClientRect();
                    if(box.width&&box.height&&text.textContent.trim()&&(box.left<frame.left-1.5||box.top<frame.top-1.5||box.right>frame.right+1.5||box.bottom>frame.bottom+1.5)) clip.push(text.textContent);
                }
                return {clip,labels:[...svg.querySelectorAll('text')].map(text=>text.textContent),
                    groups:[...svg.querySelectorAll('[data-cfx-role="topology-group"]')].map(group=>({id:group.getAttribute('data-group-id'),show:group.getAttribute('data-group-show-label')!=='false',icon:group.getAttribute('data-group-icon-id')})),
                    nodes:svg.querySelectorAll('[data-cfx-role="topology-node"]').length,edges:svg.querySelectorAll('[data-cfx-role="topology-edge"]').length,
                    scripts:svg.querySelectorAll('script,image,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """);
        if (!string.IsNullOrWhiteSpace(output)) File.WriteAllText(Path.Combine(output, name + ".json"), observed.GetRawText());
        Assert.Equal(4, observed.GetProperty("nodes").GetInt32()); Assert.Equal(3, observed.GetProperty("edges").GetInt32());
        var groups = observed.GetProperty("groups"); Assert.Equal(3, groups.GetArrayLength());
        Assert.False(groups[0].GetProperty("show").GetBoolean()); Assert.False(groups[1].GetProperty("show").GetBoolean()); Assert.True(groups[2].GetProperty("show").GetBoolean());
        Assert.Equal("cloud:cloud", groups[0].GetProperty("icon").GetString()); Assert.Equal("common:server", groups[1].GetProperty("icon").GetString());
        var labels = observed.GetProperty("labels").EnumerateArray().Select(value => value.GetString()).ToArray();
        foreach (var caption in new[] { "Facts", "Orders", "Plain" }) Assert.Contains(caption, labels);
        foreach (var id in new[] { "root", "inner", "web", "worker" }) Assert.DoesNotContain(id, labels);
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32());
        if (await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth")) {
            await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight");
            await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=\"preserve-size\"]').scrollLeft>0");
            Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        }
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
