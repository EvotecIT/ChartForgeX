using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidKanbanNativeBoardBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)]
    [InlineData(true, 390)]
    [InlineData(false, 1200)]
    [InlineData(true, 1200)]
    public async Task FactualBoardKeepsAllColumnsCardsAndMetadataInsideTheNativeScrollableHost(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidKanbanNativeBoardTests.Source, MermaidKanbanNativeBoardTests.Options(dark, width));
        Assert.False(result.HasErrors); var artifact = result.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize });
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 800);
        Assert.Equal(0, await session.Page.Locator("script, image, foreignObject").CountAsync());
        Assert.Equal(4, await session.Page.Locator("[data-cfx-role=topology-group-surface]").CountAsync());
        Assert.Equal(6, await session.Page.Locator("[data-cfx-role=topology-node]").CountAsync());
        Assert.Equal(0, await session.Page.Locator("[data-cfx-role=topology-edge-line]").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("""
            () => {
                const panels = [...document.querySelectorAll('[data-cfx-role="topology-group"]')];
                if (panels.map(panel=>panel.dataset.groupId).join(',') !== 'todo,doing,waiting,done') return false;
                const rects = panels.map(panel=>panel.querySelector('[data-cfx-role="topology-group-surface"]').getBBox());
                if (!rects.every((r,i)=>i===0||r.x>=rects[i-1].x+rects[i-1].width)) return false;
                return [...document.querySelectorAll('[data-cfx-role="topology-node"]')].every(node=>{
                    const surface=node.querySelector('[data-cfx-role="topology-node-surface"]').getBBox();
                    return [...node.querySelectorAll('text')].every(text=>{
                        const b=text.getBBox(); return b.x>=surface.x-1 && b.x+b.width<=surface.x+surface.width+1
                            && b.y>=surface.y-1 && b.y+b.height<=surface.y+surface.height+1;
                    });
                });
            }
            """));
        foreach (var text in new[] { "Priority: High", "Assigned: Alex Morgan", "Ticket: CFX/7 ?", "estimate: 2 points", "icon: fa:bug", "0 tasks" })
            Assert.Contains(text, await session.Page.Locator("svg").TextContentAsync());
        Assert.Equal(1, await session.Page.Locator("a[href='https://tickets.example.invalid/item/CFX%2F7%20%3F']").CountAsync());
        Assert.True(await session.Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= innerWidth"));
        var host = session.Page.Locator("[data-cfx-html-sizing=preserve-size]"); await host.FocusAsync(); await session.Page.Keyboard.PressAsync("End");
        Assert.True(await host.EvaluateAsync<double>("element=>{element.scrollLeft=element.scrollWidth;return element.scrollLeft;}") > 0);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "kanban-board-browser-" + width + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".native.png", png);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = path + ".browser.png", FullPage = true });
        }
    }
}
