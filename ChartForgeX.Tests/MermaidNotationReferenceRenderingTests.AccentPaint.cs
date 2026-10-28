using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
[MermaidReferenceTheory] [Trait("Category", "MermaidReference")] [InlineData("12")]
public async Task PinnedMarkerPaintOmittedProperties(string referenceId) {
    var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
    using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
    var pin = manifest.RootElement.GetProperty("references").GetProperty(referenceId);
    var bundle = Path.Combine(corpus, "node_modules", pin.GetProperty("package").GetString()!);
    using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
    Assert.Equal("12.2.0", installed.RootElement.GetProperty("version").GetString());
    Assert.Equal(pin.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
    await using var session = await BrowserProofFonts.OpenAsync("<section id='reference'></section><section id='native'></section>", 900, 700);
    var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
    await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
    var observations = new List<JsonElement>(); var nativeResults = new List<MermaidRenderResult>();
    var variants = new[] { (Name: "default", Style: ""), (Name: "fill-only", Style: "fill:#ff0000"),
        (Name: "stroke-only", Style: "stroke:#0000ff"), (Name: "both", Style: "fill:#ff0000,stroke:#0000ff") };
    foreach (var shape in new[] { "f-circ", "fr-circ", "fork" }) foreach (var variant in variants) {
        var name = "marker-paint-reference-12-" + shape + "-" + variant.Name;
        var source = "flowchart LR\nwork@{ shape: " + shape + ", label: 'Work' }" + (variant.Style.Length == 0 ? "" : "\nstyle work " + variant.Style);
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Flowchart = new MermaidFlowchartRenderOptions { Title = "", Width = 400, Height = 260 },
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family }
        });
        nativeResults.Add(result);
        var artifact = result.Artifact ?? throw new InvalidOperationException("Ordinary marker source was rejected: " + name);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var semantic = artifact.ToInterchangeJson();
        var scene = Assert.IsType<ChartForgeX.Topology.TopologyChart>(artifact.Model).Prepare().Visual.Scene;
        var nativePaint = scene.Nodes.OfType<ChartForgeX.Rendering.VisualScenePath>().Where(mark => mark.Role == "topology-node-surface")
            .Select(mark => new { Fill = mark.Fill?.ToHexRgba(), Stroke = mark.Stroke?.ToHexRgba(), mark.StrokeWidth, mark.Opacity }).ToArray();
        var observed = await session.Page.EvaluateReferenceJsonAsync("""
            async ({source,nativeSvg,renderId,fontFamily}) => {
                document.querySelector('#native').innerHTML=nativeSvg;
                mermaid.initialize({startOnLoad:false,securityLevel:'strict',htmlLabels:false,fontFamily,theme:'default',look:'classic',flowchart:{htmlLabels:false}});
                document.querySelector('#reference').innerHTML=(await mermaid.render(renderId,source)).svg;
                await document.fonts.ready;
                const svg=document.querySelector('#reference svg'), nodes=[...svg.querySelectorAll('g.node')], node=nodes[0];
                const own=node?[...node.querySelectorAll('rect,circle,ellipse,path,polygon,line')]
                    .filter(mark=>!mark.closest('.label')&&mark.closest('g.node')===node):[];
                const paint=mark=>{const c=getComputedStyle(mark);return {kind:mark.tagName,fill:c.fill,stroke:c.stroke,opacity:Number(c.opacity),
                    strokeWidth:Number.parseFloat(c.strokeWidth),fillAttribute:mark.getAttribute('fill'),strokeAttribute:mark.getAttribute('stroke'),style:mark.getAttribute('style')};};
                const roles=mermaid.mermaidAPI.getConfig().themeVariables;
                return {referenceNodeCount:nodes.length,referenceElements:own.map(paint),referenceSvg:svg.outerHTML,
                    roles:{mainBkg:roles.mainBkg,nodeBorder:roles.nodeBorder,lineColor:roles.lineColor,stateBorder:roles.stateBorder},
                    referenceActive:svg.querySelectorAll('script,foreignObject').length,
                    nativeElements:[...document.querySelectorAll('#native [data-cfx-role="topology-node-surface"]')].map(paint)};
            }
            """, new { source, nativeSvg = svg, renderId = "marker-" + shape + "-" + variant.Name, fontFamily = BrowserProofFonts.Family });
        observations.Add(observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
            File.WriteAllText(stem + ".mmd", source); File.WriteAllText(stem + ".native.svg", svg);
            File.WriteAllBytes(stem + ".native.png", png); File.WriteAllText(stem + ".native.json", semantic);
            File.WriteAllText(stem + ".reference.svg", observed.GetProperty("referenceSvg").GetString());
            File.WriteAllText(stem + ".observed.json", observed.GetRawText());
            File.WriteAllText(stem + ".native-paint.json", JsonSerializer.Serialize(nativePaint));
            File.WriteAllText(stem + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics));
        }
    }
    // Save every case before any paint judgment; this probe observes omitted/default paint first.
    Assert.Equal(12, observations.Count); Assert.All(nativeResults, result => Assert.False(result.HasErrors));
    Assert.All(observations, observed => {
        Assert.Equal(1, observed.GetProperty("referenceNodeCount").GetInt32());
        Assert.NotEmpty(observed.GetProperty("referenceElements").EnumerateArray());
        Assert.Equal(0, observed.GetProperty("referenceActive").GetInt32());
    });
    Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
}
}
