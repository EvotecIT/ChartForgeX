using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(false, 390)] [InlineData(true, 1100)]
    public async Task CurrentPinnedKanbanKeepsConfiguredColumnsAndTaskFactsBesideNativeDenseGeometry(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json"))); Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        // Native accessibility declarations are separately qualified; this pinned parser treats them as Kanban sections.
        var source = string.Join("\n", MermaidKanbanSourceGeometryTests.Fixture().Split('\n').Where(line => !line.StartsWith("accTitle:", StringComparison.Ordinal) && !line.StartsWith("accDescr:", StringComparison.Ordinal))); var rendered = MermaidRenderer.Render(source, MermaidKanbanSourceGeometryTests.Options(dark, width));
        Assert.Empty(rendered.Diagnostics); var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{padding:12px;box-sizing:border-box;font-family:'" + BrowserProofFonts.Family + "';background:" + (dark ? "#171b23;color:#edf2f7" : "white;color:#182235")
            + "}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:20px}svg,img{display:block;max-width:none}</style><h2>Pinned Mermaid "
            + reference.GetProperty("version").GetString() + "</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new Dictionary<string, int> { ["topology-node"] = 13, ["topology-group"] = 3, ["topology-edge"] = 0 });
        var facts = await session.Page.EvaluateReferenceJsonAsync("""
            async source => {
              const data=(await mermaid.mermaidAPI.getDiagramFromText(source)).db.getData();
              return {padding:data.config.kanban.padding,sectionWidth:data.config.kanban.sectionWidth,
                groups:data.nodes.filter(node=>node.isGroup).map(node=>node.id),
                tasks:data.nodes.filter(node=>!node.isGroup).map(node=>({id:node.id,parent:node.parentId,ticket:node.ticket,assigned:node.assigned,priority:node.priority})),
                actualReferenceSections:[...document.querySelectorAll('#reference .sections rect')].map(node=>{const b=node.getBBox();return{x:b.x,y:b.y,width:b.width,height:b.height}})};
            }
            """, source);
        var name = "kanban-source-reference12-" + width + "-" + dark;
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) File.WriteAllText(Path.Combine(output, name + ".source-facts.json"), facts.GetRawText());
        Assert.Equal(36, facts.GetProperty("padding").GetDouble()); Assert.Equal(240, facts.GetProperty("sectionWidth").GetDouble());
        Assert.Equal(new[] { "empty", "work", "done" }, facts.GetProperty("groups").EnumerateArray().Select(group => group.GetString()));
        Assert.Equal(13, facts.GetProperty("tasks").GetArrayLength());
        var parsed = new MermaidParser().ParseKanban(source).Document!;
        foreach (var column in parsed.Columns) foreach (var task in column.Tasks) {
            var actual = facts.GetProperty("tasks").EnumerateArray().Single(item => item.GetProperty("id").GetString() == task.Id);
            Assert.Equal(column.Id, actual.GetProperty("parent").GetString()); Assert.Equal(task.Assigned, actual.GetProperty("assigned").GetString());
            Assert.Equal(task.Ticket, actual.GetProperty("ticket").GetString()); Assert.Equal(task.Priority, actual.GetProperty("priority").GetString());
        }
        MermaidKanbanSourceGeometryTests.AssertContained(artifact.ToInterchangeEnvelope());
        foreach (var text in new[] { "Empty queue", "In progress", "Case 12", "Delivery accepted" }) foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(text, observed.GetProperty(surface).GetString());
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
