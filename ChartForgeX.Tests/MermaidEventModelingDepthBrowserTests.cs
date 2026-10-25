using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidEventModelingDepthBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 390)]
    [InlineData(false, 1200)] [InlineData(true, 1200)]
    public async Task CompleteTimelineKeepsEveryTypedRowAndLaneCaptionInsideNativeSurfacesAndScrollableHtml(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidEventModelingDepthTests.Source, MermaidEventModelingDepthTests.Options(dark, width));
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 760);
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => {
                const nodes=[...document.querySelectorAll('[data-cfx-role="topology-node"]')];
                const within=(a,b)=>b.x>=a.x-.2&&b.y>=a.y-.2&&b.x+b.width<=a.x+a.width+.2&&b.y+b.height<=a.y+a.height+.2;
                if(nodes.length!==9||!nodes.every(node=>{
                    const surface=node.querySelector('[data-cfx-role="topology-node-surface"]')?.getBBox();
                    return surface&&[...node.querySelectorAll('text')].every(text=>within(surface,text.getBBox()));
                }))return false;
                const groups=[...document.querySelectorAll('[data-cfx-role="topology-group"]')];
                if(groups.length!==6||!groups.every(group=>{
                    const surface=group.querySelector('[data-cfx-role="topology-group-surface"]')?.getBBox();
                    const captions=[...group.querySelectorAll('[data-cfx-role="topology-group-label"]')];
                    return surface&&captions.length===1&&captions.every(text=>within(surface,text.getBBox()));
                }))return false;
                const order=['30','10','20','05','06','04','08','01','02'].map(id=>nodes.find(node=>node.dataset.nodeId===id).getBoundingClientRect());
                return order.slice(1).every((bounds,index)=>bounds.x>=order[index].right-1);
            }
            """));
        var text = string.Join("\n", await session.Page.Locator("svg text").AllTextContentsAsync());
        foreach (var expected in new[] { "String: ABC", "Alex Morgan", "/escaped~1key/~0value", "Boolean: true", "Null: null", "reset" }) Assert.Contains(expected, text);
        var host = session.Page.Locator("[data-cfx-html-sizing=preserve-size]");
        Assert.True(await host.EvaluateAsync<bool>("element=>element.scrollWidth>element.clientWidth"));
        await host.FocusAsync(); await session.Page.Keyboard.PressAsync("ArrowRight");
        await session.Page.WaitForFunctionAsync("() => document.querySelector('[data-cfx-html-sizing=preserve-size]').scrollLeft > 0");
        Assert.True(await host.EvaluateAsync<double>("element=>element.scrollLeft") > 0);
        Assert.True(await session.Page.EvaluateAsync<bool>("()=>document.documentElement.scrollWidth<=innerWidth"));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, "event-native-browser-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
    [BrowserTheory]
    [InlineData(false)] [InlineData(true)]
    public async Task LongNamespaceCaptionIsCompleteInsideTheNativeLaneInBrowserGeometry(bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var name = string.Concat(Enumerable.Repeat("Operations", 20));
        var source = "eventmodeling\ntf 30 ui " + name + ".X\ntf 10 cmd Save\ntf 20 ui " + name + ".Y";
        var result = MermaidRenderer.Render(source, MermaidEventModelingDepthTests.Options(dark, 390)); Assert.False(result.HasErrors);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var label = chart.Groups.Single(group => group.Label.Contains(name)).Label;
        await using var session = await BrowserProofFonts.OpenAsync(result.Artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), 390, 760);
        var caption = session.Page.Locator("[data-cfx-role=topology-group-label]").Filter(new LocatorFilterOptions { HasText = name });
        Assert.Equal(label, await caption.TextContentAsync());
        Assert.True(await caption.EvaluateAsync<bool>("""
            text => {
                const a=text.closest('[data-cfx-role="topology-group"]').querySelector('[data-cfx-role="topology-group-surface"]').getBBox();
                const b=text.getBBox();
                return b.x>=a.x-.2&&b.y>=a.y-.2&&b.x+b.width<=a.x+a.width+.2&&b.y+b.height<=a.y+a.height+.2;
            }
            """));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory); var path = Path.Combine(directory, "event-long-lane-browser-" + (dark ? "dark" : "light"));
            File.WriteAllText(path + ".svg", result.Artifact.ToSvg()); File.WriteAllBytes(path + ".native.png", result.Artifact.ToPng());
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }

}
