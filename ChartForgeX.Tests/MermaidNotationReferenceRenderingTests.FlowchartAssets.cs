using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidNotationReferenceRenderingTests {
    [MermaidReferenceTheory] [Trait("Category","MermaidReference")]
    [InlineData(false)] [InlineData(true)]
    public async Task PinnedImageHandlerAndOwnedNativeImageShareAspectSizeAndCaptionDirection(bool top) {
        var corpus=Path.Combine(TestRepository.Root,"tests","mermaid-conformance");
        using var manifest=JsonDocument.Parse(File.ReadAllText(Path.Combine(corpus,"compatibility.json")));
        var reference=manifest.RootElement.GetProperty("references").GetProperty("12");var bundle=Path.Combine(corpus,"node_modules",reference.GetProperty("package").GetString()!);
        using var installed=JsonDocument.Parse(File.ReadAllText(Path.Combine(bundle,"package.json")));
        Assert.Equal(reference.GetProperty("version").GetString(),installed.RootElement.GetProperty("version").GetString());
        var pixels=FlowchartAssetRecipe.Image();var uri="data:image/png;base64,"+Convert.ToBase64String(PngWriter.WriteRgba(pixels));
        var source="flowchart LR\nimage@{ img: '"+uri+"', label: 'Proof image', pos: '"+(top?"t":"b")+"', h: 40, constraint: 'on' }\nstyle image fill:#fef2f2,stroke:#475569,color:#1e293b";
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject,
            Flowchart=new MermaidFlowchartRenderOptions{Width=640,Height=400,ImageBindings=new Dictionary<string,RgbaImage>{{uri,pixels}}},
            Presentation=new MermaidPresentationOptions{FontFamily=BrowserProofFonts.Family,TopologySizing=MermaidTopologySizing.Natural}});
        var artifact=result.Artifact??throw new InvalidOperationException("The finite owned image must produce a native artifact. Diagnostics: " + string.Join("; ", result.Diagnostics.Select(d => d.Message)) + "");
        var native=Assert.IsType<TopologyChart>(artifact.Model).Prepare();var image=Assert.Single(native.Visual.Scene.Nodes.OfType<VisualSceneImage>());
        var captions=native.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(mark=>mark.Text.Lines.Any(line=>line.Text=="Proof image")).ToArray();
        var svg=artifact.ToSvg();var png=artifact.ToPng();var html="<style>body{margin:0;padding:16px;box-sizing:border-box}section{max-width:100%;overflow:auto;margin-bottom:24px}svg,img{display:block;max-width:none}</style><h2>Pinned Mermaid</h2><section id='reference'></section><h2>ChartForgeX SVG</h2><section id='native'>"+svg+"</section><h2>ChartForgeX PNG</h2><section><img id='png' src='data:image/png;base64,"+Convert.ToBase64String(png)+"'></section>";
        await using var session=await BrowserProofFonts.OpenAsync(html,640,1000);var errors=new List<string>();session.Page.PageError+=(_,error)=>errors.Add(error);
        await session.Page.AddScriptTagAsync(new PageAddScriptTagOptions{Path=Path.Combine(bundle,"dist","mermaid.min.js")});
        var observed=await session.Page.EvaluateReferenceJsonAsync("""
            async ({source,fontFamily}) => {
                mermaid.initialize({startOnLoad:false,securityLevel:'strict',htmlLabels:false,fontFamily,theme:'default',look:'classic',flowchart:{htmlLabels:false}});
                document.querySelector('#reference').innerHTML=(await mermaid.render('image-reference-diagram',source)).svg;
                await document.fonts.ready;await document.querySelector('#png').decode();
                const reference=document.querySelector('#reference svg'),native=document.querySelector('#native svg');
                for(const svg of [reference,native]){const v=svg.viewBox.baseVal;svg.style.width=v.width+'px';svg.style.height=v.height+'px';svg.style.maxWidth='none';}
                const images=[...reference.querySelectorAll('image')];if(images.length!==1)throw Error('Expected the single source-authored reference image');const image=images[0],label=[...reference.querySelectorAll('text')].find(node=>node.textContent.trim()==='Proof image');
                const ib=image.getBoundingClientRect(),lb=label.getBoundingClientRect(),frame=reference.getBoundingClientRect();
                return {width:Number(image.getAttribute('width')),height:Number(image.getAttribute('height')),caption:label.textContent,
                    top:lb.bottom<=ib.top+0.5,bottom:lb.top>=ib.bottom-0.5,
                    clips:lb.left<frame.left-1.5||lb.right>frame.right+1.5||lb.top<frame.top-1.5||lb.bottom>frame.bottom+1.5,
                    nativeImages:native.querySelectorAll('image').length,nativeActive:native.querySelectorAll('script,foreignObject').length,
                    horizontalOverflow:document.documentElement.scrollWidth>innerWidth};
            }
            """,new{source,fontFamily=BrowserProofFonts.Family});
        var name="asset-reference-12-"+(top?"top":"bottom");await CaptureAsync(session.Page,name,svg,png,artifact.ToInterchangeJson(),observed);
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is {Length:>0} output)File.WriteAllText(Path.Combine(output,name+".mmd"),source);
        Assert.Empty(result.Diagnostics);Assert.Equal(80,image.Bounds.Width,6);Assert.Equal(40,image.Bounds.Height,6);Assert.NotEmpty(captions);
        Assert.All(captions,mark=>Assert.Equal(16,mark.Text.Size,6));
        if(top)Assert.All(captions,mark=>Assert.True(mark.Baseline<=image.Bounds.Top-7.5));else Assert.All(captions,mark=>Assert.True(mark.Baseline-mark.Text.Ascent>=image.Bounds.Bottom+7.5));
        Assert.Equal(80,observed.GetProperty("width").GetDouble());Assert.Equal(40,observed.GetProperty("height").GetDouble());Assert.Contains("Proof image",observed.GetProperty("caption").GetString()!,StringComparison.Ordinal);
        Assert.True(observed.GetProperty(top?"top":"bottom").GetBoolean());Assert.False(observed.GetProperty("clips").GetBoolean());Assert.Equal(1,observed.GetProperty("nativeImages").GetInt32());
        Assert.Equal(0,observed.GetProperty("nativeActive").GetInt32());Assert.False(observed.GetProperty("horizontalOverflow").GetBoolean());Assert.Empty(errors);InteractiveChartBrowser.AssertNoConsoleErrors(session);
    }
}
