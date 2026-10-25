using ChartForgeX.VisualArtifacts;
using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData(false, 390)] [InlineData(true, 1100)]
    public async Task PinnedIshikawaTreeAndSourcePaintRemainObservedBesideMeasuredNativeCaptions(bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json"))); Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidIshikawaDepthTests.Fixture(dark); var rendered = MermaidRenderer.Render(source, MermaidIshikawaDepthTests.Options(dark, width - 32, BrowserProofFonts.Family)); Assert.Empty(rendered.Diagnostics);
        var artifact = rendered.Artifact!; var block = (FishboneDiagramBlock)artifact.Model!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + ";font-family:'" + BrowserProofFonts.Family + "'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / Ishikawa cause tree</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1100);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await ObserveAsync(session.Page, source, "", dark, new Dictionary<string, int> { ["fishbone-cause"] = 28 });
        var facts = await session.Page.EvaluateAsync<JsonElement>("""
            async source => {
              const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db,root=db.getRoot(),nodes=[];
              const visit=(node,id,parent)=>{nodes.push({id,parent,text:node.text});node.children.forEach((child,i)=>visit(child,id+'.'+i,id))};
              root.children.forEach((node,i)=>visit(node,'cause-'+i,null));
              const svg=document.querySelector('#reference svg'),spine=svg.querySelector('.ishikawa-spine'),head=svg.querySelector('.ishikawa-head'),label=svg.querySelector('text');
              return {effect:root.text,nodes,paint:{line:getComputedStyle(spine).stroke,head:getComputedStyle(head).fill,text:getComputedStyle(label).fill},
                referenceLabels:[...svg.querySelectorAll('text')].map(n=>n.textContent.replace(/\s+/g,' ').trim())};
            }
            """, source);
        var name = "ishikawa-depth-reference12-" + width + (dark ? "-dark" : "-light");
        await CaptureAsync(session.Page, name, svg, png, artifact.ToInterchangeJson(), observed);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) File.WriteAllText(Path.Combine(output, name + ".tree-paint.json"), facts.GetRawText());
        Assert.Equal(block.Effect, facts.GetProperty("effect").GetString()); var expected = FishboneNaturalLayoutTests.Facts(block); var actual = facts.GetProperty("nodes").EnumerateArray().ToArray(); Assert.Equal(expected.Count, actual.Length);
        for (var index = 0; index < expected.Count; index++) {
            Assert.Equal(expected[index].Id, actual[index].GetProperty("id").GetString()); Assert.Equal(expected[index].Parent, actual[index].GetProperty("parent").GetString()); Assert.Equal(expected[index].Text, actual[index].GetProperty("text").GetString());
            foreach (var surface in new[] { "referenceText", "nativeText" }) Assert.Contains(expected[index].Text, observed.GetProperty(surface).GetString());
        }
        Assert.Equal("rgb(124, 58, 237)", facts.GetProperty("paint").GetProperty("line").GetString());
        Assert.Equal(dark ? "rgb(36, 50, 71)" : "rgb(226, 232, 240)", facts.GetProperty("paint").GetProperty("head").GetString());
        Assert.Equal(dark ? "rgb(241, 245, 249)" : "rgb(24, 34, 53)", facts.GetProperty("paint").GetProperty("text").GetString());
        Assert.Equal(28, observed.GetProperty("nativeRegions").GetProperty("fishbone-cause").GetInt32()); Assert.Equal(0, observed.GetProperty("nativeScripts").GetInt32()); Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());
        // Both paints are retained for physical inspection. Different upstream/native geometry is expected.
        Assert.DoesNotContain(observed.GetProperty("clip").EnumerateArray(), value => value.GetString()!.StartsWith("native:", StringComparison.Ordinal));
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
