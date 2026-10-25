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
    public async Task PinnedAndNativeAutomaticResponsibilityOrdersHaveActualNonWorseWeightedDistance(bool dark,int width) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));
        var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);
        using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        var source=MermaidSwimlaneOrderingCausalTests.Source("LR",true);var rendered=MermaidRenderer.Render(source,MermaidSwimlaneLayoutDepthTests.Options(dark,width-32));Assert.Empty(rendered.Diagnostics);
        var artifact=rendered.Artifact!;var svg=artifact.ToSvg();var png=artifact.ToPng();
        await using var session=await BrowserProofFonts.OpenAsync("<style>body{margin:0;padding:16px;box-sizing:border-box}section{max-width:100%;overflow:auto}</style><h2>Reference</h2><section id='reference'></section><h2>Native</h2><section>"+svg+"</section>",width,1000);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions {Path=Path.Combine(bundle,"dist","mermaid.min.js")});
        var observed=await session.Page.EvaluateAsync<JsonElement>("""
            async({source,dark,fontFamily})=>{
                mermaid.initialize({startOnLoad:false,deterministicIds:true,securityLevel:'strict',fontFamily,theme:dark?'dark':'default',look:'classic',layout:'swimlane',flowchart:{htmlLabels:false}});
                const rendered=await mermaid.render('ordered-responsibilities',source);document.querySelector('#reference').innerHTML=rendered.svg;await document.fonts.ready;
                const ids={Alpha:'A',Bravo:'B',Charlie:'C',Delta:'D'};
                return {order:[...document.querySelectorAll('#reference .cluster')].map(group=>({label:group.querySelector(':scope > .cluster-label').textContent.trim(),y:group.getBoundingClientRect().top})).sort((a,b)=>a.y-b.y).map(group=>ids[group.label]),text:document.querySelector('#reference').textContent};
            }
            """,new {source,dark,fontFamily=BrowserProofFonts.Family});
        await CaptureAsync(session.Page,"swimlane-auto-order-reference12-"+width+"-"+dark,svg,png,artifact.ToInterchangeJson(),observed);
        var order=observed.GetProperty("order").EnumerateArray().Select(value=>value.GetString()!).ToArray();Assert.Equal(4,order.Length);
        Assert.True(MermaidSwimlaneOrderingCausalTests.Cost(order)<=12);Assert.True(MermaidSwimlaneOrderingCausalTests.Cost(MermaidSwimlaneOrderingCausalTests.Order(artifact.ToInterchangeEnvelope(),"LR"))<12);
        foreach(var text in new[]{"Alpha","Bravo","Charlie","Delta","Accept","Build","Check","Deliver"})Assert.Contains(text,observed.GetProperty("text").GetString());InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
