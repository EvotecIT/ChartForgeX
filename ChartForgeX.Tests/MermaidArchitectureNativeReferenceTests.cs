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
    public async Task CurrentPinnedArchitectureExposesNestedGroupsAndAuthoredSidesWithoutInventedGeometryEquality(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidArchitectureNativeDepthTests.Fixture(); var rendered = MermaidRenderer.Render(source, MermaidArchitectureNativeDepthTests.Options(dark, width - 32));
        Assert.Empty(rendered.Diagnostics); var artifact = rendered.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / architecture</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark,
            new Dictionary<string, int> { ["topology-node"] = 5, ["topology-group"] = 4, ["topology-edge"] = 4 });
        var facts = await session.Page.EvaluateAsync<JsonElement>("""
            async source => {
                const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db;
                const frame=document.querySelector('#reference svg');
                const groups=db.getGroups().map(group=>{
                    const rect=[...frame.querySelectorAll('rect[id]')].find(rect=>rect.id.endsWith('-group-'+group.id));
                    const box=rect?.getBoundingClientRect();
                    return {id:group.id,parent:group.in??null,bounds:box?{x:box.x,y:box.y,width:box.width,height:box.height}:null};
                });
                return {groups,services:db.getServices().map(node=>({id:node.id,group:node.in??null})),
                    junctions:db.getJunctions().map(node=>({id:node.id,group:node.in??null})),
                    edges:db.getEdges().map(edge=>({left:edge.lhsId,right:edge.rhsId,leftSide:edge.lhsDir,rightSide:edge.rhsDir,leftBoundary:!!edge.lhsGroup,rightBoundary:!!edge.rhsGroup,leftArrow:!!edge.lhsInto,rightArrow:!!edge.rhsInto})),
                    paths:[...frame.querySelectorAll('path.edge')].map(path=>({id:path.id,d:path.getAttribute('d')}))};
            }
            """, source);
        var name = "architecture-boundaries-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            File.WriteAllText(Path.Combine(output, name + ".architecture.json"), facts.GetRawText());
        var groups = facts.GetProperty("groups"); Assert.Equal(4, groups.GetArrayLength());
        Assert.Equal("platform", groups.EnumerateArray().Single(group => group.GetProperty("id").GetString() == "private").GetProperty("parent").GetString());
        Assert.Equal("private", groups.EnumerateArray().Single(group => group.GetProperty("id").GetString() == "data").GetProperty("parent").GetString());
        foreach (var group in groups.EnumerateArray()) { Assert.True(group.GetProperty("bounds").GetProperty("width").GetDouble() > 0); Assert.True(group.GetProperty("bounds").GetProperty("height").GetDouble() > 0); }
        Assert.Equal(4, facts.GetProperty("services").GetArrayLength()); Assert.Single(facts.GetProperty("junctions").EnumerateArray());
        var edges = facts.GetProperty("edges"); Assert.Equal(4, edges.GetArrayLength());
        Assert.Equal("worker", edges[1].GetProperty("left").GetString()); Assert.Equal("split", edges[1].GetProperty("right").GetString());
        Assert.True(edges[1].GetProperty("leftArrow").GetBoolean()); Assert.False(edges[1].GetProperty("rightArrow").GetBoolean());
        Assert.Equal("T", edges[1].GetProperty("leftSide").GetString()); Assert.Equal("B", edges[1].GetProperty("rightSide").GetString());
        Assert.True(edges[3].GetProperty("leftBoundary").GetBoolean()); Assert.True(edges[3].GetProperty("rightBoundary").GetBoolean());
        Assert.Equal(4, facts.GetProperty("paths").GetArrayLength());
        var native = artifact.ToInterchangeEnvelope(); Assert.Equal("data", native.Edges[3].SourceId); Assert.Equal("partner", native.Edges[3].TargetId);
        Assert.Equal("store", native.Edges[3].Extensions["mermaid.source.id"]); Assert.Equal("private", native.Groups.Single(group => group.Id == "data").Topology!.ParentGroupId);
        foreach (var label in new[] { "Platform", "Private", "Data", "Partner", "Gateway", "Worker", "Storage", "Remote" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(label, observed.GetProperty(surface).GetString());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
