using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory] [Trait("Category", "MermaidReference")]
    [InlineData("flowchart", false)] [InlineData("flowchart", true)]
    [InlineData("agentflow", false)] [InlineData("agentflow", true)]
    public async Task PinnedDirectElementsExposeLiteralPaletteAndOpacityRoles(string family, bool explicitRoles) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12");
        var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = "---\ntitle: Source heading\n---\n" + MermaidTopologyPaletteRoleTests.Source(family, false, explicitRoles);
        var result = MermaidRenderer.Render(source, MermaidTopologyPaletteRoleTests.Options());
        var artifact = result.Artifact ?? throw new InvalidOperationException("The ordinary source graph must remain natively admitted.");
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid</h2><section id='reference'></section><h2>ChartForgeX SVG</h2><section id='native'>" + svg
            + "</section><h2>ChartForgeX PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, 1100, 1000);
        var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateReferenceJsonAsync("""
            async ({source,fontFamily}) => {
                mermaid.initialize({startOnLoad:false,securityLevel:'strict',htmlLabels:false,fontFamily,theme:'default',look:'classic',flowchart:{htmlLabels:false},agentflow:{htmlLabels:false}});
                document.querySelector('#reference').innerHTML=(await mermaid.render('palette-reference-diagram',source)).svg;
                await document.fonts.ready; await document.querySelector('#png').decode();
                const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');
                for(const svg of [reference,native]){const v=svg.viewBox.baseVal;svg.style.width=v.width+'px';svg.style.height=v.height+'px';svg.style.maxWidth='none';}
                const node=[...reference.querySelectorAll('g.node')].find(n=>n.querySelector('.label')?.textContent.trim()==='Work');
                const group=[...reference.querySelectorAll('g.cluster')].find(n=>n.querySelector('.cluster-label')?.textContent.trim()==='Scope');
                const surface=node?.querySelector('rect.basic'),label=node?.querySelector('.label text'),groupSurface=group?.querySelector(':scope > rect'),groupLabel=group?.querySelector('.cluster-label text'),edgeLabel=reference.querySelector('.edgeLabel rect'),edgeText=reference.querySelector('.edgeLabel text'),heading=reference.querySelector('.flowchartTitleText,.agentflowTitleText');
                const paint=element=>{if(!element)throw Error('Missing required direct palette element');const c=getComputedStyle(element);return {fill:c.fill.replaceAll(' ',''),stroke:c.stroke.replaceAll(' ',''),opacity:Number(c.opacity),strokeWidth:Number.parseFloat(c.strokeWidth)};};
                return {node:paint(surface),label:paint(label),group:paint(groupSurface),groupLabel:paint(groupLabel),edgeLabel:paint(edgeLabel),edgeText:paint(edgeText),heading:paint(heading),
                    nodeTag:surface.tagName,groupTag:groupSurface.tagName,edgeLabelTag:edgeLabel.tagName,
                    nativeActive:native.querySelectorAll('script,foreignObject').length,
                    nativeNodeCount:native.querySelectorAll('[data-cfx-role="topology-node"]').length,
                    resolvedRoles:mermaid.mermaidAPI.getConfig().themeVariables};
            }
            """, new { source, fontFamily = BrowserProofFonts.Family });
        var name = "palette-direct-reference-12-" + family + (explicitRoles ? "-explicit" : "-default");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".mmd"), source);
            File.WriteAllText(Path.Combine(output, name + ".diagnostics.json"), JsonSerializer.Serialize(result.Diagnostics));
        }
        Assert.Equal("rect", observed.GetProperty("nodeTag").GetString()); Assert.Equal("rect", observed.GetProperty("groupTag").GetString()); Assert.Equal("rect", observed.GetProperty("edgeLabelTag").GetString());
        Paint("node", explicitRoles ? "rgb(238,238,255)" : "rgb(236,236,255)", explicitRoles ? "rgb(18,52,86)" : "rgb(147,112,219)", 1);
        Assert.Equal("rgb(51,51,51)", observed.GetProperty("label").GetProperty("fill").GetString());
        var nativeLabel = System.Xml.Linq.XDocument.Parse(svg).Descendants().Single(e => e.Name.LocalName == "text" && e.Value == "Work");
        Assert.Equal(explicitRoles ? "#234567" : "#333333", (string?)nativeLabel.Attribute("fill"), ignoreCase: true);
        Paint("group", family == "agentflow" ? "none" : explicitRoles ? "rgb(255,237,221)" : "rgb(255,255,222)", explicitRoles ? "rgb(135,101,67)" : "rgb(170,170,51)", 1);
        Assert.Equal(family == "agentflow" ? .75 : 1, observed.GetProperty("group").GetProperty("strokeWidth").GetDouble());
        Assert.Equal(explicitRoles ? "rgb(86,120,144)" : "rgb(51,51,51)", observed.GetProperty("groupLabel").GetProperty("fill").GetString());
        Paint("edgeLabel", explicitRoles ? "rgb(204,221,238)" : "rgba(232,232,232,0.8)", null, .5);
        // Pinned Mermaid retains its default edge-caption color even with textColor configured.
        Assert.Equal("rgb(51,51,51)", observed.GetProperty("edgeText").GetProperty("fill").GetString());
        var nativePaint = System.Xml.Linq.XDocument.Parse(svg).Descendants().Where(e => (string?)e.Attribute("data-cfx-role") == "topology-edge-label").SelectMany(e => e.Descendants()).Where(e => e.Name.LocalName == "text").Select(e => (string?)e.Attribute("fill")).ToArray();
        Assert.NotEmpty(nativePaint);
        Assert.All(nativePaint, fill => Assert.Equal(explicitRoles ? "#234567" : "#333333", fill, ignoreCase: true));
        Assert.Equal("rgb(51,51,51)", observed.GetProperty("heading").GetProperty("fill").GetString());
        Assert.Equal(0, observed.GetProperty("nativeActive").GetInt32()); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        void Paint(string role, string fill, string? stroke, double opacity) {
            var value = observed.GetProperty(role); Assert.Equal(fill, value.GetProperty("fill").GetString());
            if (stroke != null) Assert.Equal(stroke, value.GetProperty("stroke").GetString()); Assert.Equal(opacity, value.GetProperty("opacity").GetDouble());
        }
    }
}
