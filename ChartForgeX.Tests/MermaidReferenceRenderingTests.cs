using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Observed upstream/native comparisons; upstream engines remain test-time inputs.</summary>
public sealed class MermaidReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData("10", "mermaid-10", "10.9.8", 640, false)]
    [InlineData("11", "mermaid-11", "11.17.2", 1100, false)]
    [InlineData("12", "mermaid", "12.2.0", 640, false)]
    [InlineData("12", "mermaid", "12.2.0", 1100, true)]
    public async Task PinnedEnginesAndNativeOutputPreserveLabelsAndCompoundRelationships(string id, string package,
        string version, int width, bool dark) {
        Assert.True(InteractiveChartBrowser.Enabled);
        var referenceRoot = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "node_modules", package);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(referenceRoot, "package.json")));
        Assert.Equal(version, installed.RootElement.GetProperty("version").GetString());
        foreach (var fixture in new[] { "flowchart-multiline-markdown", "flowchart-compound-long-edge", "flowchart-link-style-replacement" }) {
            var source = File.ReadAllText(MermaidFlowchartLabelDepthTests.Fixture(fixture + ".mmd"));
            var options = new MermaidRenderOptions { Flowchart = new MermaidFlowchartRenderOptions { Width = width - 32, Height = 440 } };
            var artifact = MermaidRenderer.Render((dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + source, options).Artifact!;
            var svg = artifact.ToSvg(); var png = artifact.ToPng();
            var background = dark ? "#171b23" : "#fff";
            var color = dark ? "#edf2f7" : "#182235";
            var body = "<style>body{margin:0;padding:16px;font-family:Arial,sans-serif;background:" + background + ";color:" + color
                + "}h2{font-size:18px}section{margin-bottom:24px}svg,img{max-width:100%;height:auto;display:block}</style>"
                + "<h2>Mermaid " + version + " / " + fixture + "</h2><section id='reference'></section>"
                + "<h2>ChartForgeX SVG</h2><section id='native'>" + svg + "</section>"
                + "<h2>ChartForgeX PNG</h2><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'>";
            await using var session = await InteractiveChartBrowser.OpenAsync(body, width, 1100);
            var page = session.Page;
            var errors = new List<string>();
            page.PageError += (_, error) => errors.Add(error);
            await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(referenceRoot, "dist", "mermaid.min.js") });
            await page.EvaluateAsync("""
                async ({source,dark}) => {
                    mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',
                        theme:dark?'dark':'default',layout:'dagre',look:'classic',flowchart:{htmlLabels:true}});
                    const result=await mermaid.render('reference-diagram',source);
                    document.querySelector('#reference').innerHTML=result.svg;
                    await document.fonts.ready;
                    await document.querySelector('#png').decode();
                }
                """, new { source, dark });
            await BrowserProofCaptions.InstallAsync(page);
            var observed = await page.EvaluateReferenceJsonAsync("""
                () => {
                    const normalize=text=>text.replace(/\s+/g,' ').trim();
                    const labelText=node=> {
                        const copy=node.cloneNode(true);
                        for(const br of copy.querySelectorAll('br')) br.replaceWith(' ');
                        return normalize(copy.textContent);
                    };
                    const reference=document.querySelector('#reference svg');
                    const native=document.querySelector('#native svg');
                    const nativeLabels=[...native.querySelectorAll('[data-cfx-role="topology-node"]')].map(n=>[...n.querySelectorAll('[data-cfx-role="topology-node-label"]')].map(window.cfxReferenceLabelText).join(' '));
                    const referenceLabels=[...reference.querySelectorAll('.node .label')].map(labelText);
                    const clip=[];
                    for(const node of native.querySelectorAll('[data-cfx-role="topology-node"]')) {
                        const surface=node.querySelector('[data-cfx-role="topology-node-surface"]').getBBox();
                        for(const text of node.querySelectorAll('[data-cfx-role="topology-node-label"] text,[data-cfx-role="topology-node-label"][data-cfx-caption]')) {
                            const box=text.getBBox();
                            if(box.x<surface.x-.5||box.y<surface.y-.5||box.x+box.width>surface.x+surface.width+.5||box.y+box.height>surface.y+surface.height+.5) clip.push(node.getAttribute('data-node-id'));
                        }
                    }
                    return {referenceLabels,nativeLabels,clip,referenceEdges:reference.querySelectorAll('.flowchart-link').length,
                        nativeEdges:native.querySelectorAll('[data-cfx-role="topology-edge"]').length,
                        referenceLayout:'dagre',referenceLook:'classic',
                        horizontalOverflow:document.documentElement.scrollWidth>innerWidth,
                        nativeScripts:native.querySelectorAll('script,foreignObject').length};
                }
                """);
            var nativeLabels = observed.GetProperty("nativeLabels").EnumerateArray().Select(value => value.GetString()).ToArray();
            var referenceLabels = observed.GetProperty("referenceLabels").EnumerateArray().Select(value => value.GetString()).ToArray();
            Assert.Equal(nativeLabels.OrderBy(value => value), referenceLabels.OrderBy(value => value));
            Assert.Empty(observed.GetProperty("clip").EnumerateArray());
            Assert.Equal(observed.GetProperty("referenceEdges").GetInt32(), observed.GetProperty("nativeEdges").GetInt32());
            Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
            Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32());
            Assert.Empty(errors);
            InteractiveChartBrowser.AssertNoConsoleErrors(session);
            if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } captures) {
                Directory.CreateDirectory(captures);
                var name = "reference-" + id + "-" + fixture + "-" + width + (dark ? "-dark" : "-light");
                File.WriteAllText(Path.Combine(captures, name + ".html"), await page.EvaluateAsync<string>("""
                    () => { const copy=document.documentElement.cloneNode(true); for(const script of copy.querySelectorAll('script')) script.remove(); return '<!doctype html>'+copy.outerHTML; }
                    """));
                File.WriteAllText(Path.Combine(captures, name + ".json"), observed.GetRawText());
                File.WriteAllText(Path.Combine(captures, name + "-native.svg"), svg);
                File.WriteAllBytes(Path.Combine(captures, name + "-native.png"), png);
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(captures, name + ".png"), FullPage = true });
            }
        }
    }
}

/// <summary>Keeps the pinned upstream runtime out of ordinary native/browser test runs.</summary>
public sealed class MermaidReferenceTheoryAttribute : TheoryAttribute {
    public MermaidReferenceTheoryAttribute() {
        if (Environment.GetEnvironmentVariable("CFX_TEST_MERMAID_REFERENCE") != "1")
            Skip = "Run Build/Test-MermaidReferenceRendering.ps1 for pinned upstream render proof.";
    }
}
