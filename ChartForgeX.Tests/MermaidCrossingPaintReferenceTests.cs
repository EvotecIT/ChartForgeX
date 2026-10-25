using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData("basis", false, 390)] [InlineData("basis", true, 1100)]
    [InlineData("false", false, 390)] [InlineData("false", true, 1100)]
    [InlineData("arc", false, 390)] [InlineData("arc", true, 1100)]
    [InlineData("gap", false, 390)] [InlineData("gap", true, 1100)]
    public async Task PinnedLineHopsKeepsActualSourceDefaultsAndHonestNativeOwnershipDifference(string form, bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = SwimlaneCrossingPaintRecipe.Source(form); var result = MermaidRenderer.Render(source, MermaidNestedSwimlaneTests.Options(dark, width - 32));
        var artifact = result.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box}section{max-width:100%;overflow:auto}svg,img{display:block;max-width:none}</style><section id='reference'></section><section id='native'>"
            + svg + "</section><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1050);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateAsync<JsonElement>("""
            async ({source,dark,fontFamily})=>{
                mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic',layout:'swimlane',flowchart:{htmlLabels:false}});
                const rendered=await mermaid.render('crossing-reference',source);document.querySelector('#reference').innerHTML=rendered.svg;
                await document.fonts.ready;await document.querySelector('#png').decode();
                const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');
                const referencePaths=[...reference.querySelectorAll('path[data-id]')].filter(path=>path.getAttribute('data-points')).map(path=>({id:path.getAttribute('data-id'),d:path.getAttribute('d'),points:path.getAttribute('data-points')}));
                const nativePaths=[...native.querySelectorAll('[data-cfx-role="topology-edge-line"]')].map(path=>({id:path.closest('[data-edge-id]').getAttribute('data-edge-id'),d:path.getAttribute('d')}));
                const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db;
                return {referencePaths,nativePaths,nodes:db.getData().nodes.map(node=>({id:node.id,parent:node.parentId??null})),
                    referenceText:reference.textContent,nativeText:native.textContent,overflow:document.documentElement.scrollWidth>innerWidth,
                    scripts:native.querySelectorAll('script,image,foreignObject').length};}
            """, new { source, dark, fontFamily = BrowserProofFonts.Family });
        await CaptureAsync(session.Page, "crossing-reference12-" + form + "-" + width + "-" + dark, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            File.WriteAllText(Path.Combine(output, "crossing-reference12-" + form + "-" + width + "-" + dark + ".source.mmd"), source);
        Assert.Empty(result.Diagnostics); Assert.Equal(2, observed.GetProperty("referencePaths").GetArrayLength()); Assert.Equal(2, observed.GetProperty("nativePaths").GetArrayLength());
        foreach (var caption in new[] { "Accept", "Build", "Check", "Deliver" }) {
            Assert.Contains(caption, observed.GetProperty("referenceText").GetString()); Assert.Contains(caption, observed.GetProperty("nativeText").GetString());
        }
        var referencePaths = observed.GetProperty("referencePaths").EnumerateArray().Select(path => path.GetProperty("d").GetString()!).ToArray();
        if (form is "basis" or "false") Assert.DoesNotContain(referencePaths, path => path.Contains('A') || path.Count(character => character == 'M') > 1);
        if (form == "arc") Assert.Contains(referencePaths, path => path.Contains('A'));
        if (form == "gap") Assert.Contains(referencePaths, path => path.Count(character => character == 'M') > 1);
        // Actual path records and screenshots are retained for Arc/Gap inspection. Native later-paint ownership
        // deliberately differs from the pinned horizontal preference; no whole-path or pixel equality assertion.
        Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
