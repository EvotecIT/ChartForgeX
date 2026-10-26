using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category", "MermaidReference")]
    [InlineData("flowchart", false, 390)] [InlineData("flowchart", true, 1100)]
    [InlineData("swimlane-beta", false, 390)] [InlineData("swimlane-beta", true, 1100)]
    public async Task CurrentPinnedParentFactsRetainTheFirstClosedOwnerWithLaterAncestorAndSiblingReferences(string family, bool dark, int width) {
        var corpus = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance"); using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus, "compatibility.json")));
        var reference = manifest.RootElement.GetProperty("references").GetProperty("12"); var bundle = Path.Combine(corpus, "node_modules", reference.GetProperty("package").GetString()!);
        using var installed = JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle, "package.json"))); Assert.Equal(reference.GetProperty("version").GetString(), installed.RootElement.GetProperty("version").GetString());
        var source = MermaidFlowchartMembershipTests.Fixture(family); var direct = MermaidRenderer.Render(source, MermaidFlowchartMembershipTests.Options(dark, width - 32)); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var html = "<style>body{margin:0;padding:16px;box-sizing:border-box;font-family:'" + BrowserProofFonts.Family + "';background:" + (dark ? "#171b23;color:#edf2f7" : "#fff;color:#182235")
            + "}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            + "<h2>Pinned Mermaid " + reference.GetProperty("version").GetString() + " / membership " + family + "</h2><section id='reference'></section>"
            + "<h2>Native SVG</h2><section id='native'>" + svg + "</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64," + Convert.ToBase64String(png) + "'></section>";
        await using var session = await BrowserProofFonts.OpenAsync(html, width, 1050); await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = Path.Combine(bundle, "dist", "mermaid.min.js") });
        var observed = await session.Page.EvaluateReferenceJsonAsync("""
            async ({source,family,dark,fontFamily}) => {
                mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic',layout:family==='swimlane-beta'?'swimlane':'dagre',flowchart:{htmlLabels:false}});
                const result=await mermaid.render('membership-reference',source);document.querySelector('#reference').innerHTML=result.svg;
                const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');for(const svg of [reference,native]){const view=svg.viewBox.baseVal;svg.style.width=view.width+'px';svg.style.height=view.height+'px';svg.style.maxWidth='none'}
                await document.fonts.ready;await document.querySelector('#png').decode();const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db;
                const facts=db.getData().nodes.filter(node=>!node.isGroup).map(node=>({id:node.id,parent:node.parentId??null}));const normalize=text=>text.replace(/\s+/g,' ').trim(),clip=[];
                const text=svg=>normalize([...svg.querySelectorAll('text,foreignObject')].map(node=>node.textContent).join(' '));
                for(const [surface,svg] of [['reference',reference],['native',native]]){const frame=svg.getBoundingClientRect();for(const label of svg.querySelectorAll('text,foreignObject')){const r=label.getBoundingClientRect();if(r.width&&r.height&&label.textContent.trim()&&(r.left<frame.left-1.5||r.top<frame.top-1.5||r.right>frame.right+1.5||r.bottom>frame.bottom+1.5))clip.push(surface+':'+normalize(label.textContent))}}
                return {facts,clip,referenceText:text(reference),nativeText:text(native),overflow:document.documentElement.scrollWidth>innerWidth,scripts:native.querySelectorAll('script,image,foreignObject').length};
            }
            """, new { source, family, dark, fontFamily = BrowserProofFonts.Family });
        await CaptureAsync(session.Page, "membership-reference12-" + family + "-" + width + (dark ? "-dark" : "-light"), svg, png, artifact.ToInterchangeJson(), observed);
        var facts = observed.GetProperty("facts").EnumerateArray().ToArray(); var saved = artifact.ToInterchangeEnvelope();
        foreach (var pair in new[] { ("N", "Inner"), ("Q", "Inner"), ("X", "Outer"), ("T", "Outer"), ("Y", "Sibling") }) {
            Assert.Equal(pair.Item2, facts.Single(node => node.GetProperty("id").GetString() == pair.Item1).GetProperty("parent").GetString());
            Assert.Equal(pair.Item2, saved.Nodes.Single(node => node.Id == pair.Item1).GroupId);
        }
        foreach (var surface in new[] { "referenceText", "nativeText" }) { Assert.Contains("Updated", observed.GetProperty(surface).GetString()); Assert.Contains("Persist", observed.GetProperty(surface).GetString()); Assert.DoesNotContain("Original", observed.GetProperty(surface).GetString()); }
        Assert.Empty(observed.GetProperty("clip").EnumerateArray()); Assert.False(observed.GetProperty("overflow").GetBoolean()); Assert.Equal(0, observed.GetProperty("scripts").GetInt32()); InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
