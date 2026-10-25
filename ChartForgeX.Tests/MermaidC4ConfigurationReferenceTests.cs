using ChartForgeX.VisualArtifacts;
using System.Text.Json;
using ChartForgeX.Mermaid;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory]
    [Trait("Category","MermaidReference")]
    [InlineData(false,390)] [InlineData(true,1100)]
    public async Task CurrentPinnedC4ConfigurationExposesRealFontPaletteWrapAndGridInputs(bool dark,int width) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));
        var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);
        using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        var source=MermaidC4ConfigurationDepthTests.Fixture();var rendered=MermaidRenderer.Render(source,MermaidC4ConfigurationDepthTests.Options(dark,width-32));Assert.False(rendered.HasErrors);
        var artifact=rendered.Artifact!;var svg=artifact.ToSvg();var png=artifact.ToPng();
        var body="<style>body{margin:0;padding:16px;box-sizing:border-box;background:"+(dark?"#171b23;color:#edf2f7":"#fff;color:#182235")+";font-family:'"+BrowserProofFonts.Family+"'}h2{font-size:18px}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style>"
            +"<h2>Pinned Mermaid "+reference.GetProperty("version").GetString()+" / C4 configuration</h2><section id='reference'></section><h2>Native SVG</h2><section id='native'>"+svg+"</section><h2>Native PNG</h2><section><img id='png' src='data:image/png;base64,"+Convert.ToBase64String(png)+"'></section>";
        await using var session=await BrowserProofFonts.OpenAsync(body,width,1000);var pageErrors=new List<string>();session.Page.PageError+=(_,error)=>pageErrors.Add(error);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions {Path=Path.Combine(bundle,"dist","mermaid.min.js")});
        var observed=await ObserveAsync(session.Page,source,"",dark,new Dictionary<string,int>{{"topology-node",3},{"topology-group",1},{"topology-edge",2}});
        var raw=await session.Page.EvaluateAsync<string>("""
            async source=>{
                const db=(await mermaid.mermaidAPI.getDiagramFromText(source)).db,config=db.getConfig(),svg=document.querySelector('#reference svg');
                const rows=[...svg.querySelectorAll('.c4-shape')].map(node=>{
                    const label=node.querySelector('.label'),name=node.querySelector('.c4-name text'),shape=node.querySelector('rect.basic,path.basic,rect,path');
                    const style=getComputedStyle(label),paint=shape?getComputedStyle(shape):null,box=node.getBoundingClientRect();
                    return {id:node.id,classes:node.getAttribute('class'),text:node.textContent,fontFamily:style.fontFamily,fontSize:style.fontSize,fontWeight:style.fontWeight,
                        nameWeight:name?getComputedStyle(name).fontWeight:null,fill:paint?.fill,stroke:paint?.stroke,bounds:{x:box.x,y:box.y,width:box.width,height:box.height},rows:node.querySelectorAll('tspan.text-outer-tspan').length};
                });
                return JSON.stringify({rows,shapeLimit:db.getC4ShapeInRow(),boundaryLimit:db.getC4BoundaryInRow(),config:{width:config.width,padding:config.c4ShapePadding,margin:config.c4ShapeMargin,indent:config.nextLinePaddingX,wrap:config.wrap,containerSize:config.containerFontSize},
                    captions:db.getC4ShapeArray().map(shape=>({id:shape.alias,type:shape.typeC4Shape.text,label:shape.label.text})),boundaries:db.getBoundaries().map(group=>({id:group.alias,parent:group.parentBoundary}))});
            }
            """,source);
        var name="c4-config-reference12-"+width+(dark?"-dark":"-light");await CaptureAsync(session.Page,name,svg,png,artifact.ToInterchangeJson(),observed);
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)File.WriteAllText(Path.Combine(output,name+".configuration.json"),raw);
        using var json=JsonDocument.Parse(raw);var facts=json.RootElement;Assert.Equal(2,facts.GetProperty("shapeLimit").GetInt32());Assert.Equal(1,facts.GetProperty("boundaryLimit").GetInt32());
        var config=facts.GetProperty("config");Assert.Equal(216,config.GetProperty("width").GetDouble());Assert.Equal(18,config.GetProperty("padding").GetDouble());Assert.Equal(30,config.GetProperty("margin").GetDouble());Assert.Equal(12,config.GetProperty("indent").GetDouble());Assert.True(config.GetProperty("wrap").GetBoolean());
        var rows=facts.GetProperty("rows").EnumerateArray().ToArray();Assert.Equal(3,rows.Length);Assert.All(rows,row=>Assert.Contains(BrowserProofFonts.Family,row.GetProperty("fontFamily").GetString()));
        var service=rows.Single(row=>row.GetProperty("classes").GetString()!.Split(' ').Contains("c4-container"));Assert.Equal("20px",service.GetProperty("fontSize").GetString());Assert.Equal("rgb(229, 238, 248)",service.GetProperty("fill").GetString());Assert.Equal("rgb(51, 85, 119)",service.GetProperty("stroke").GetString());
        foreach(var word in new[]{"Service platform","Operations","coordinator","Publication","orchestration","External","delivery","validated","documents"})foreach(var surface in new[]{"referenceText","nativeText"})Assert.Contains(word,observed.GetProperty(surface).GetString());
        Assert.True(observed.GetProperty("referencePaintElements").GetInt32()>0);Assert.Empty(observed.GetProperty("clip").EnumerateArray());Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());Assert.Equal(0,observed.GetProperty("nativeScripts").GetInt32());Assert.Empty(pageErrors);InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
