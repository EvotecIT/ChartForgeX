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
        var observed = await session.Page.EvaluateReferenceJsonAsync("""
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
        // The pinned route detours around the other edge. Hop requests do not invent a crossing.
        Assert.DoesNotContain(referencePaths, path => path.Contains('A') || path.Count(character => character == 'M') > 1);
        var polylines = observed.GetProperty("referencePaths").EnumerateArray().Select(path => {
            using var points = JsonDocument.Parse(Convert.FromBase64String(path.GetProperty("points").GetString()!));
            return points.RootElement.EnumerateArray().Select(point => (X: point.GetProperty("x").GetDouble(), Y: point.GetProperty("y").GetDouble())).ToArray();
        }).ToArray();
        for (var a = 1; a < polylines[0].Length; a++) for (var b = 1; b < polylines[1].Length; b++)
            Assert.False(ProperCrossing(polylines[0][a - 1], polylines[0][a], polylines[1][b - 1], polylines[1][b]));
        // Native positive crossing paint is independently covered by the native crossing contracts.
        Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
        static bool ProperCrossing((double X, double Y) a, (double X, double Y) b, (double X, double Y) c, (double X, double Y) d) {
            static double Side((double X, double Y) p, (double X, double Y) q, (double X, double Y) r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
            return Side(a, b, c) * Side(a, b, d) < 0 && Side(c, d, a) * Side(c, d, b) < 0;
        }
    }
}
