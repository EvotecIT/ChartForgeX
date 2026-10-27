using System.Xml.Linq;
using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidBoundImageCaptionTests {
    [Theory] [InlineData(false)] [InlineData(true)]
    public void FiniteBoundSourceImageAndLiteralCaptionUseTheSameDirectFenceAndPortableNativeOwner(bool top) {
        var source="flowchart LR\npicture@{img:'owned-image',label:'Proof caption',pos:"+(top?"t":"b")+",constraint:on,w:40,h:20}";
        var pixels=new byte[]{255,0,0,255,0,255,0,128};var options=Options(pixels);var parser=new MermaidVisualMarkupParser(options);
        var direct=MermaidRenderer.Render(source,options);Capture(source,direct,"bound-image-"+top);
        var artifact=direct.Artifact??throw new InvalidOperationException("A bound source image must produce its native artifact.");
        var svg=artifact.ToSvg();var png=artifact.ToPng();var json=artifact.ToInterchangeJson();
        var fenced=parser.Parse("```mermaid\n"+source+"\n```");
        if(Environment.GetEnvironmentVariable("CFX_FLOWCHART_ASSET_CAPTURE_DIRECTORY") is {Length:>0} fenceOutput) {Directory.CreateDirectory(fenceOutput);File.WriteAllText(Path.Combine(fenceOutput,"bound-image-"+top+".fence-diagnostics.json"),JsonSerializer.Serialize(fenced.Diagnostics.Select(item=>new{item.Code,item.Message,item.Severity,item.Line,item.Column})));}
        var fence=Assert.Single(fenced.Artifacts);TopologyExternalCaptionPolicyTests.Capture("image-fence-"+top,fence.ToSvg(),fence.ToPng(),fence.ToInterchangeJson());
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();TopologyExternalCaptionPolicyTests.Capture("image-restored-"+top,restored.ToSvg(),restored.ToPng(),restored.ToInterchangeEnvelope().ToJson());
        Assert.False(direct.HasErrors);Assert.Empty(direct.Diagnostics);Assert.False(fenced.HasErrors);Assert.Empty(fenced.Diagnostics);
        var node=Assert.Single(Assert.IsType<TopologyChart>(artifact.Model).Nodes);Assert.Equal(TopologyNodeDisplayMode.Artwork,node.DisplayMode);
        Assert.Equal(40,node.Width);Assert.Equal(20,node.Height);Assert.Equal(top?TopologyCaptionPlacement.Top:TopologyCaptionPlacement.Bottom,node.CaptionPlacement);
        Assert.Equal(8,node.CaptionGap);Assert.Equal(16,node.CaptionTextStyle!.FontSize);Assert.Equal("Proof caption",node.Label);Assert.True(node.ShowLabel);
        Assert.Equal(svg,fence.ToSvg());Assert.Equal(png,fence.ToPng());Assert.Equal(png,restored.ToPng());
        pixels[0]=0;options.Flowchart!.ImageBindings!.Clear();Assert.Equal(svg,artifact.ToSvg());Assert.Equal(png,artifact.ToPng());
        var repeat=Assert.Single(parser.Parse("```mermaid\n"+source+"\n```").Artifacts);Assert.Equal(svg,repeat.ToSvg());Assert.Equal(png,repeat.ToPng());
    }

    [Theory] [InlineData("", "picture")] [InlineData(",label:''", "")] [InlineData(",label:'picture'", "picture")]
    public void MissingEmptyAndAuthoredIdentityLabelsRemainDistinctNativeFacts(string label,string retained) {
        var source="flowchart LR\npicture@{img:'owned-image'"+label+"}";var result=MermaidRenderer.Render(source,Options(new byte[]{255,0,0,255,0,255,0,255}));Capture(source,result,"image-label-"+label.Length);
        Assert.False(result.HasErrors);Assert.Empty(result.Diagnostics);var node=Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes);
        Assert.Equal(retained,node.Label);Assert.Equal(label.Contains("picture",StringComparison.Ordinal),node.ShowLabel);
        if(!node.ShowLabel){Assert.Null(node.CaptionGap);Assert.Null(node.CaptionPlacement);Assert.Null(node.CaptionTextStyle);}
    }

    [Theory] [InlineData("fill")] [InlineData("stroke")]
    public void UnsupportedAssetPaintStaysLocatedAndInertThroughTheExistingColorResolver(string property) {
        var source="flowchart LR\nserver@{icon:'common:server',label:'Server'}\nstyle server "+property+":url(https://example.invalid/paint)";
        var result=MermaidRenderer.Render(source,new MermaidRenderOptions{Presentation=new MermaidPresentationOptions{FontFamily=BrowserProofFonts.Family},Flowchart=new MermaidFlowchartRenderOptions{Width=480,Height=320,FitContent=false}});Capture(source,result,"inert-asset-paint-"+property);
        Assert.False(result.HasErrors);var warning=Assert.Single(result.Diagnostics);Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement,warning.Code);Assert.Equal(3,warning.Span.Line);
        var node=Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes);Assert.Equal("url(https://example.invalid/paint)",node.Metadata["mermaid.style."+property]);
        var paints=System.Xml.Linq.XDocument.Parse(result.Artifact.ToSvg()).Descendants().Attributes().Where(value=>value.Name.LocalName is "fill" or "stroke");Assert.DoesNotContain(paints,value=>value.Value.StartsWith("url(",StringComparison.OrdinalIgnoreCase));
        Assert.True(MermaidRenderer.Render(source,new MermaidRenderOptions{WarningPolicy=MermaidWarningPolicy.Reject}).HasErrors);
    }

    private static MermaidRenderOptions Options(byte[] pixels)=>new(){Flowchart=new MermaidFlowchartRenderOptions{Width=480,Height=320,FitContent=false,ImageBindings=new(){["owned-image"]=new RgbaImage(2,1,pixels)}},
        Presentation=new MermaidPresentationOptions{FontFamily=BrowserProofFonts.Family,ThemeMode=VisualThemeMode.Light},WarningPolicy=MermaidWarningPolicy.Reject};
    private static void Capture(string source,MermaidRenderResult result,string name){
        if(Environment.GetEnvironmentVariable("CFX_FLOWCHART_ASSET_CAPTURE_DIRECTORY") is {Length:>0} output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,name+".mmd"),source);File.WriteAllText(Path.Combine(output,name+".diagnostics.json"),JsonSerializer.Serialize(result.Diagnostics.Select(item=>new{item.Code,item.Message,item.Severity,item.Span.Line,item.Span.Column,item.Span.Length})));}
        if(result.Artifact!=null)TopologyExternalCaptionPolicyTests.Capture(name,result.Artifact.ToSvg(),result.Artifact.ToPng(),result.Artifact.ToInterchangeJson());
    }
}
