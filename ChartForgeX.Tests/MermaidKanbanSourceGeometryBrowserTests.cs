using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidKanbanSourceGeometryBrowserTests {
    [BrowserTheory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public async Task DenseSourceSizedBoardKeepsEveryCardReadableAndAvailableByHostScrolling(bool dark, int width) {
        if (!InteractiveChartBrowser.Enabled) return;
        var result = MermaidRenderer.Render(MermaidKanbanSourceGeometryTests.Fixture(), MermaidKanbanSourceGeometryTests.Options(dark, width));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width, 850);
        var page = session.Page; var errors = new List<string>(); page.PageError += (_, message) => errors.Add(message);
        var observed = await page.EvaluateAsync<JsonElement>("""
            () => {
              const panels=[...document.querySelectorAll('[data-cfx-role=topology-group]')];
              const boxes=panels.map(panel=>{const r=panel.querySelector('[data-cfx-role=topology-group-surface]').getBBox();return{id:panel.dataset.groupId,x:r.x,y:r.y,width:r.width,height:r.height}});
              const cards=[...document.querySelectorAll('[data-cfx-role=topology-node]')].map(node=>{
                const b=node.querySelector('[data-cfx-role=topology-node-surface]').getBBox();
                const full=[...node.querySelectorAll('text')].every(text=>{const t=text.getBBox();return t.x>=b.x-.01&&t.y>=b.y-.01&&t.x+t.width<=b.x+b.width+.01&&t.y+t.height<=b.y+b.height+.01});
                return{id:node.dataset.nodeId,group:node.dataset.groupId,x:b.x,y:b.y,width:b.width,height:b.height,text:node.textContent,full};
              });
              return {boxes,cards,text:document.querySelector('svg').textContent,pageOverflow:document.documentElement.scrollWidth>innerWidth,active:document.querySelectorAll('script,foreignObject,image').length};
            }
            """);
        await Capture(page, width, dark, "first", svg, png, observed);
        Assert.False(observed.GetProperty("pageOverflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("active").GetInt32());
        var panels = observed.GetProperty("boxes").EnumerateArray().ToArray(); Assert.Equal(new[] { "empty", "work", "done" }, panels.Select(panel => panel.GetProperty("id").GetString()));
        Assert.All(panels, panel => Assert.True(panel.GetProperty("width").GetDouble() >= 240));
        var cards = observed.GetProperty("cards").EnumerateArray().ToArray(); Assert.Equal(13, cards.Length); Assert.All(cards, card => Assert.True(card.GetProperty("full").GetBoolean(), card.GetProperty("id").GetString()));
        Assert.Equal(Enumerable.Range(1, 12).Select(index => "task" + index), cards.Where(card => card.GetProperty("group").GetString() == "work").Select(card => card.GetProperty("id").GetString()));
        foreach (var group in panels) {
            var members = cards.Where(card => card.GetProperty("group").GetString() == group.GetProperty("id").GetString()).ToArray();
            for (var index = 0; index < members.Length; index++) {
                var card = members[index]; var x = card.GetProperty("x").GetDouble(); var y = card.GetProperty("y").GetDouble();
                Assert.True(x >= group.GetProperty("x").GetDouble() + 23.999 && x + card.GetProperty("width").GetDouble() <= group.GetProperty("x").GetDouble() + group.GetProperty("width").GetDouble() - 23.999);
                Assert.True(y >= group.GetProperty("y").GetDouble() && y + card.GetProperty("height").GetDouble() <= group.GetProperty("y").GetDouble() + group.GetProperty("height").GetDouble());
                if (index > 0) Assert.True(y >= members[index - 1].GetProperty("y").GetDouble() + members[index - 1].GetProperty("height").GetDouble() + 15.999);
            }
        }
        foreach (var text in new[] { "Case 12", "0 tasks", "Assigned: Avery Quinn", "Ticket: CFX-12" }) Assert.Contains(text, observed.GetProperty("text").GetString());
        var host = page.Locator("[data-cfx-html-sizing=preserve-size]"); await host.FocusAsync(); await page.Keyboard.PressAsync("End");
        var position = await host.EvaluateAsync<double>("element=>{element.scrollLeft=element.scrollWidth;return element.scrollLeft}");
        if (width == 390) Assert.True(position > 0);
        await Capture(page, width, dark, "last-column", svg, png, observed); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
    private static async Task Capture(IPage page, int width, bool dark, string phase, string svg, byte[] png, JsonElement facts) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        if (!Path.IsPathRooted(output)) throw new InvalidOperationException("Captures require an absolute task-owned directory.");
        Directory.CreateDirectory(output); var prefix = Path.Combine(output, "kanban-source-browser-" + width + "-" + dark + "-" + phase);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = prefix + ".png", FullPage = true });
        File.WriteAllText(prefix + ".json", facts.GetRawText()); File.WriteAllText(prefix + ".svg", svg); File.WriteAllBytes(prefix + ".native.png", png);
    }
}
