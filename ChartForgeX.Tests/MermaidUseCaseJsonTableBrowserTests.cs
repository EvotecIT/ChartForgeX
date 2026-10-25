using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed class MermaidUseCaseJsonTableBrowserTests {
    [BrowserTheory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1000, false)] [InlineData(1000, true)]
    public async Task RealLinkedTableHasVisibleScalarCellsInsideItsNativeNodeAndRetainedNote(int width, bool dark) {
        if (!InteractiveChartBrowser.Enabled) return;
        var source = UseCaseJsonTableFixture.Read(width == 390);
        var result = MermaidRenderer.Render(source, MermaidUseCaseJsonTableTests.Options(width, dark));
        var artifact = result.Artifact!;
        await using var session = await BrowserProofFonts.OpenAsync(artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), width + 24, 1200);
        var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            () => {
              const svg=document.querySelector('svg'),node=svg.querySelector('[data-cfx-role=topology-node][data-node-id=Details]'),
                table=node.querySelector('[data-cfx-role=topology-node-table]'),frame=svg.getBoundingClientRect(),box=node.getBoundingClientRect();
              const texts=[...table.querySelectorAll('text')],cells=[...table.querySelectorAll('[data-cfx-role=table-cell]')];
              const textFacts=texts.map(text=>{const b=text.getBoundingClientRect(),cell=text.closest('[data-cfx-role=table-cell]');
                return {text:text.textContent,row:cell?.dataset.rowKey,column:cell?.dataset.columnId,
                  box:{left:b.left,top:b.top,right:b.right,bottom:b.bottom,width:b.width,height:b.height},
                  outside:b.left<box.left-1||b.right>box.right+1||b.top<box.top-1||b.bottom>box.bottom+1||b.left<frame.left-1||b.right>frame.right+1||b.top<frame.top-1||b.bottom>frame.bottom+1};});
              return {nodeCount:svg.querySelectorAll('[data-cfx-role=topology-node]').length,tableId:table.dataset.tableId,
                cells:cells.map(cell=>({row:cell.dataset.rowKey,column:cell.dataset.columnId,text:cell.textContent})),
                visible:texts.map(text=>text.textContent),textFacts,
                rawOutside:textFacts.filter(text=>text.outside),
                outside:textFacts.filter(text=>text.text.length>0&&text.outside),
                noteTarget:svg.querySelector('[data-cfx-role=topology-annotation]').dataset.targetId,
                note:[...svg.querySelectorAll('[data-cfx-role=topology-annotation] text')].map(text=>text.textContent).join(' '),
                edgeCount:svg.querySelectorAll('[data-cfx-role=topology-edge]').length,active:svg.querySelectorAll('script,image,foreignObject').length};
            }
            """);
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "usecase-json-" + width + (dark ? "-dark" : "-light"));
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = stem + ".browser.png", FullPage = true });
            File.WriteAllText(stem + ".observed.json", JsonSerializer.Serialize(new { observed, errors, session.ConsoleLog }));
            File.WriteAllText(stem + ".source.mmd", source);
            File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".native.png", artifact.ToPng());
            File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
        }
        Assert.Empty(result.Diagnostics);
        Assert.Equal(3, observed.GetProperty("nodeCount").GetInt32()); Assert.Equal("Details", observed.GetProperty("tableId").GetString());
        Assert.Equal(18, observed.GetProperty("cells").GetArrayLength()); Assert.Equal(2, observed.GetProperty("edgeCount").GetInt32());
        var cells = observed.GetProperty("cells").EnumerateArray().ToArray(); Assert.Equal(18, cells.Select(cell => (cell.GetProperty("row").GetString(), cell.GetProperty("column").GetString())).Distinct().Count());
        Assert.Equal("1.00", cells.Single(cell => cell.GetProperty("row").GetString() == "/version" && cell.GetProperty("column").GetString() == "value").GetProperty("text").GetString());
        Assert.Equal(new (string? Row, string? Column)[] { ("/memo", "value"), ("/tags/1", "property") }, cells.Where(cell => cell.GetProperty("text").GetString() == "")
            .Select(cell => (cell.GetProperty("row").GetString(), cell.GetProperty("column").GetString())));
        var visible = observed.GetProperty("visible").EnumerateArray().Select(text => text.GetString()).ToArray();
        foreach (var value in new[] { "Property", "Value", "1.00", "false", "Aster", "null", "ready", "signed", "kept", "[]" }) Assert.Contains(value, visible);
        Assert.All(observed.GetProperty("textFacts").EnumerateArray().Where(text => text.GetProperty("text").GetString()!.Length > 0), text => {
            Assert.True(text.GetProperty("box").GetProperty("width").GetDouble() > 0); Assert.True(text.GetProperty("box").GetProperty("height").GetDouble() > 0);
        });
        Assert.Equal("Details", observed.GetProperty("noteTarget").GetString()); Assert.Contains("Captured source facts stay linked", observed.GetProperty("note").GetString());
        Assert.Empty(observed.GetProperty("outside").EnumerateArray()); Assert.Equal(0, observed.GetProperty("active").GetInt32()); Assert.Empty(errors);
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
