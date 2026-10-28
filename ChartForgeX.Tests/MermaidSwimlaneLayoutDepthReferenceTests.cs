using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category","MermaidReference")]
    [InlineData(false,390)] [InlineData(true,1100)]
    public async Task PinnedSwimlaneObservesConfiguredSpacingRealParentsAndEffectiveLocalDirection(bool dark,int width) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));
        var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);
        using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        var source=MermaidSwimlaneLayoutDepthTests.Source();var result=MermaidRenderer.Render(source,MermaidSwimlaneLayoutDepthTests.Options(dark,width-32));Assert.Empty(result.Diagnostics);
        var artifact=result.Artifact!;var svg=artifact.ToSvg();var png=artifact.ToPng();
        var html="<style>body{margin:0;padding:16px;box-sizing:border-box}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style><h2>Pinned Mermaid "+reference.GetProperty("version").GetString()+"</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>"+svg+"</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64,"+Convert.ToBase64String(png)+"'></section>";
        await using var session=await BrowserProofFonts.OpenAsync(html,width,1100);await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions {Path=Path.Combine(bundle,"dist","mermaid.min.js")});
        var observed=await session.Page.EvaluateReferenceJsonAsync("""
            async({source,dark,fontFamily})=>{
                mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic',layout:'swimlane',flowchart:{htmlLabels:false}});
                const rendered=await mermaid.render('swimlane-layout-depth-reference',source);document.querySelector('#reference').innerHTML=rendered.svg;
                await document.fonts.ready;await document.querySelector('#png').decode();
                const diagram=await mermaid.mermaidAPI.getDiagramFromText(source),facts=diagram.db.getData().nodes.map(node=>({id:node.id,parent:node.parentId??null,group:!!node.isGroup}));
                const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');
                const boxes=[...reference.querySelectorAll('.node')].map(node=>{const r=node.getBoundingClientRect();return {text:node.textContent.replace(/\s+/g,' ').trim(),x:r.left,y:r.top,right:r.right,bottom:r.bottom}});
                return {facts,boxes,referenceText:reference.textContent,nativeText:native.textContent,config:mermaid.mermaidAPI.getConfig().flowchart,
                    invalid:/NaN|Infinity/.test(reference.outerHTML+native.outerHTML),active:native.querySelectorAll('script,image,foreignObject').length,overflow:document.documentElement.scrollWidth>innerWidth};
            }
            """,new {source,dark,fontFamily=BrowserProofFonts.Family});
        await CaptureAsync(session.Page,"swimlane-layout-depth-reference12-"+width+"-"+dark,svg,png,artifact.ToInterchangeJson(),observed);
        var facts=observed.GetProperty("facts").EnumerateArray().ToArray();Assert.Equal("Product",facts.Single(item=>item.GetProperty("id").GetString()=="API").GetProperty("parent").GetString());
        Assert.Equal("API",facts.Single(item=>item.GetProperty("id").GetString()=="A").GetProperty("parent").GetString());
        var boxes=observed.GetProperty("boxes").EnumerateArray().ToArray();var a=boxes.Single(item=>item.GetProperty("text").GetString()=="Accept");var b=boxes.Single(item=>item.GetProperty("text").GetString()=="Build");
        // Pinned global lane geometry ignores this local vertical direction; native source geometry retains it.
        Assert.Equal(a.GetProperty("y").GetDouble(),b.GetProperty("y").GetDouble(),5);
        Assert.True(b.GetProperty("x").GetDouble()>a.GetProperty("right").GetDouble());
        var native=artifact.ToInterchangeEnvelope();MermaidNestedSwimlaneTests.AssertContainment(native);
        Assert.True(native.Nodes.Single(node=>node.Id=="B").Y!.Value>native.Nodes.Single(node=>node.Id=="A").Y!.Value);
        foreach(var caption in new[]{"Product delivery","API","Partner","Plan","Accept","Build","Check","Deliver"})foreach(var surface in new[]{"referenceText","nativeText"})Assert.Contains(caption,observed.GetProperty(surface).GetString());
        Assert.False(observed.GetProperty("invalid").GetBoolean());Assert.False(observed.GetProperty("overflow").GetBoolean());Assert.Equal(0,observed.GetProperty("active").GetInt32());InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
