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
    public async Task CurrentPinnedAlignmentOccurrencesAndActualGlobalGeometryAreObservedBesideNativeOutput(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json"))); Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidArchitectureAlignmentTests.Fixture(); var result = MermaidRenderer.Render(source, MermaidArchitectureNativeDepthTests.Options(dark, width - 32));
        Assert.Empty(result.Diagnostics); var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / ordered alignments</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new Dictionary<string, int> { ["topology-node"] = 7, ["topology-group"] = 4, ["topology-edge"] = 4 });
        var facts = await session.Page.EvaluateAsync<JsonElement>("""
            async source => {
                const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db,svg=document.querySelector('#reference svg'),size=db.getConfigField('iconSize');
                const nodes=[...db.getServices(),...db.getJunctions()].map(node=>{
                    const element=[...svg.querySelectorAll('g.architecture-service')].find(element=>element.id.endsWith('-service-'+node.id))??[...svg.querySelectorAll('rect[id]')].find(element=>element.id.endsWith('-node-'+node.id))?.parentElement.parentElement;
                    if(!element)return {id:node.id,missing:true};const point=svg.createSVGPoint();point.x=size/2;point.y=size/2;const center=point.matrixTransform(element.getScreenCTM());
                    return {id:node.id,x:center.x,y:center.y};
                });
                return {hints:db.getLayoutHints().map(hint=>({direction:hint.direction,members:[...hint.members]})),nodes,
                    groups:db.getGroups().map(group=>({id:group.id,parent:group.in??null})),paths:svg.querySelectorAll('path.edge').length};
            }
            """, source);
        var name = "architecture-alignments-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) File.WriteAllText(Path.Combine(output, name + ".alignments.json"), facts.GetRawText());
        var document = new MermaidParser().ParseArchitecture(source).Document!; var hints = facts.GetProperty("hints"); Assert.Equal(document.Alignments.Count, hints.GetArrayLength());
        for (var i = 0; i < document.Alignments.Count; i++) {
            var declaration = document.Alignments[i]; var hint = hints[i]; Assert.Equal(declaration.Kind.ToString().ToLowerInvariant(), hint.GetProperty("direction").GetString());
            Assert.Equal(declaration.Members, hint.GetProperty("members").EnumerateArray().Select(value => value.GetString()));
            var members = declaration.Members.Select(id => facts.GetProperty("nodes").EnumerateArray().Single(node => node.GetProperty("id").GetString() == id)).ToArray();
            for (var m = 1; m < members.Length; m++) {
                var equal = declaration.Kind == ChartForgeX.Topology.TopologyAlignmentKind.Row ? "y" : "x"; var ordered = equal == "y" ? "x" : "y";
                Assert.InRange(Math.Abs(members[0].GetProperty(equal).GetDouble() - members[m].GetProperty(equal).GetDouble()), 0, .5);
                Assert.True(members[m - 1].GetProperty(ordered).GetDouble() < members[m].GetProperty(ordered).GetDouble());
            }
        }
        MermaidArchitectureAlignmentTests.AssertGeometry(artifact.ToInterchangeEnvelope()); Assert.Equal(4, facts.GetProperty("paths").GetInt32());
        foreach (var label in new[] { "Platform", "Applications", "Data", "Partner", "Client", "API", "Queue", "Worker", "Audit", "Remote" })
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(label, observed.GetProperty(surface).GetString());
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
