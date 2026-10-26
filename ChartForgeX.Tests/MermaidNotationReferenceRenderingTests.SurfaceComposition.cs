using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory] [Trait("Category", "MermaidReference")]
    [InlineData("rect")] [InlineData("person")] [InlineData("dbl-circ")] [InlineData("bucket")] [InlineData("stadium")]
    public async Task PinnedClassicElementTreeKeepsGroupElementAndFixedDetailOpacityDistinct(string shape) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = "flowchart LR\nwork@{ shape: " + shape + ", label: 'Work' }\nstyle work fill:#ff0000,stroke:#0000ff,opacity:0.5";
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Flowchart = new MermaidFlowchartRenderOptions { Width = 600, Height = 440 }, Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family }
        });
        var artifact = result.Artifact ?? throw new InvalidOperationException("The current located opacity warning must preserve the native graph.");
        var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid</h2><section id='reference'></section><h2>Native source before appearance lowering</h2><section id='native'>" + svg
            + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, 900, 1000); var errors = new List<string>(); session.Page.PageError += (_, error) => errors.Add(error);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async ({source,fontFamily}) => {
                mermaid.initialize({startOnLoad:false,securityLevel:'strict',htmlLabels:false,fontFamily,theme:'default',look:'classic',flowchart:{htmlLabels:false}});
                document.querySelector('#reference').innerHTML=(await mermaid.render('element-composition-reference',source)).svg;
                await document.fonts.ready;await document.querySelector('#png').decode();
                const svg=document.querySelector('#reference svg'),node=[...svg.querySelectorAll('g.node')].find(n=>n.querySelector('.label')?.textContent.trim()==='Work');
                // Keep an unexpected selector result in the retained observation before its assertion.
                const nodeFound=!!node;
                const own=node?[...node.querySelectorAll('rect,circle,ellipse,path,polygon,line')].filter(n=>!n.closest('.label')&&n.closest('g.node')===node):[];
                const opacity=n=>Number(getComputedStyle(n).opacity),paint=n=>({kind:n.tagName,opacity:opacity(n),fill:getComputedStyle(n).fill,stroke:getComputedStyle(n).stroke,style:n.getAttribute('style'),path:n.getAttribute('d'),points:n.getAttribute('points')});
                const elements=own.map(n=>{const ancestors=[];let effective=1;for(let a=n;a&&a!==node.parentElement;a=a.parentElement){effective*=opacity(a);if(a!==n)ancestors.push({kind:a.tagName,classes:a.getAttribute('class'),opacity:opacity(a)});}return {...paint(n),effective,ancestors};});
                // effective records only the arithmetic product of computed opacity along this ancestor chain.
                // The screenshot and serialized SVG retain actual isolated overlap composition.
                return {nodeFound,elements,nodeOpacity:node?opacity(node):null,labelOpacity:node?[...node.querySelectorAll('.label text,.label span')].map(opacity):[],referenceActive:svg.querySelectorAll('script,foreignObject').length,
                    nativeActive:document.querySelectorAll('#native script,#native foreignObject').length};
            }
            """, new { source, fontFamily = BrowserProofFonts.Family });
        var name = "surface-element-composition-reference-12-" + shape; await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".mmd"), source); File.WriteAllText(Path.Combine(output, name + ".diagnostics.json"), JsonSerializer.Serialize(result.Diagnostics));
        }
        Assert.False(result.HasErrors); Assert.True(observed.GetProperty("nodeFound").GetBoolean(), "Missing ordinary labeled source node");
        Assert.Equal(1, observed.GetProperty("nodeOpacity").GetDouble());
        var elements = observed.GetProperty("elements").EnumerateArray().ToArray(); Assert.Equal(shape is "person" or "dbl-circ" or "bucket" or "stadium" ? 2 : 1, elements.Length);
        if (shape == "bucket") {
            var body = Assert.Single(elements, element => element.GetProperty("kind").GetString() == "path"); var rim = Assert.Single(elements, element => element.GetProperty("kind").GetString() == "ellipse");
            Assert.Equal(.5, body.GetProperty("opacity").GetDouble()); Assert.Equal(.5, body.GetProperty("effective").GetDouble()); Assert.Equal(1, rim.GetProperty("effective").GetDouble()); Assert.Equal("none", rim.GetProperty("fill").GetString());
        } else foreach (var element in elements) {
            Assert.Equal(.5, element.GetProperty("opacity").GetDouble()); Assert.Equal(shape == "dbl-circ" ? .25 : .5, element.GetProperty("effective").GetDouble());
            if (shape == "dbl-circ") Assert.Contains(element.GetProperty("ancestors").EnumerateArray(), ancestor => ancestor.GetProperty("opacity").GetDouble() == .5);
        }
        var labels = observed.GetProperty("labelOpacity").EnumerateArray().ToArray(); Assert.NotEmpty(labels); Assert.All(labels, label => Assert.Equal(1, label.GetDouble()));
        Assert.Equal(0, observed.GetProperty("referenceActive").GetInt32()); Assert.Equal(0, observed.GetProperty("nativeActive").GetInt32()); Assert.Empty(errors); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
