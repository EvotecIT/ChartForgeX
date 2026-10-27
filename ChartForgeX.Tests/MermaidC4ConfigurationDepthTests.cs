using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed partial class MermaidC4ConfigurationDepthTests {
    [Theory]
    [InlineData("Person","person")] [InlineData("Person_Ext","external_person")]
    [InlineData("System","system")] [InlineData("System_Ext","external_system")]
    [InlineData("SystemDb","system_db")] [InlineData("SystemDb_Ext","external_system_db")]
    [InlineData("SystemQueue","system_queue")] [InlineData("SystemQueue_Ext","external_system_queue")]
    [InlineData("Container","container")] [InlineData("Container_Ext","external_container")]
    [InlineData("ContainerDb","container_db")] [InlineData("ContainerDb_Ext","external_container_db")]
    [InlineData("ContainerQueue","container_queue")] [InlineData("ContainerQueue_Ext","external_container_queue")]
    [InlineData("Component","component")] [InlineData("Component_Ext","external_component")]
    [InlineData("ComponentDb","component_db")] [InlineData("ComponentDb_Ext","external_component_db")]
    [InlineData("ComponentQueue","component_queue")] [InlineData("ComponentQueue_Ext","external_component_queue")]
    public void EveryPinnedElementTypeUsesItsOwnPaletteAndCapturedFont(string call,string type) {
        var family=BrowserProofFonts.Family;
        var source="%%{init: {'c4':{'"+type+"FontFamily':'"+family+"','"+type+"FontSize':'18px','"+type+"FontWeight':500,'"+type+"_bg_color':'#123456','"+type+"_border_color':'#654321'}}}%%\nC4Context\n"+call+"(item, \"Observed caption\")";
        var chart=Parse(source).ToTopologyChart();chart.WithTheme(chart.Theme.WithFontFamily(BrowserProofFonts.Family));var node=Assert.Single(chart.Nodes);
        Assert.Equal("#123456",node.BackgroundColor);Assert.Equal("#654321",node.Color);
        Assert.Equal(family,node.TextStyle!.FontFamily);Assert.Equal(18,node.TextStyle.FontSize);Assert.Equal("500",node.TextStyle.FontWeight);
        var prepared=chart.Prepare();var labels=prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(text=>text.Role=="topology-node-label").ToArray();
        Assert.NotEmpty(labels);Assert.All(labels,text=>{Assert.Equal(family,text.Text.Style.Font.Family);Assert.True(text.Text.Size>=18-.001);});
        var image=RasterImageDecoder.Decode(prepared.ToPng());Assert.True(image.Width>0&&image.Height>0);
    }
    [Fact]
    public void ExplicitElementStylesAndHostFontOverridesWinFamilyDefaults() {
        var source="%%{init: {'c4':{'containerFontFamily':'Source font','containerFontSize':18,'container_bg_color':'#123456','container_border_color':'#654321'}}}%%\nC4Container\nContainer(api, \"API\")\nUpdateElementStyle(api,$bgColor=\"#abcdef\",$borderColor=\"#fedcba\")";
        var host=new MermaidPresentationOptions {FontFamily=BrowserProofFonts.Family,FontSize=22};
        var chart=Parse(source).ToTopologyChart(new MermaidTopologyRenderOptions {Presentation=host});chart.WithTheme(chart.Theme.WithFontFamily(BrowserProofFonts.Family));var node=Assert.Single(chart.Nodes);
        Assert.Equal("#abcdef",node.BackgroundColor);Assert.Equal("#fedcba",node.Color);Assert.Equal(host.FontFamily,node.TextStyle!.FontFamily);Assert.Equal(22,node.TextStyle.FontSize);
    }
    [Fact]
    public void SourceGridSpacingUsesTheGenericImmutablePlannerAndExistingSourceRowLimits() {
        var source="%%{init: {'c4':{'c4ShapeMargin':30,'nextLinePaddingX':12,'diagramMarginX':16,'diagramMarginY':20}}}%%\nC4Context\nSystem(a,\"A\")\nSystem(b,\"B\")\nSystem(c,\"C\")\nUpdateLayoutConfig(2,1)";
        var chart=Parse(source).ToTopologyChart();chart.WithTheme(chart.Theme.WithFontFamily(BrowserProofFonts.Family));var policy=chart.GridLayout!;
        Assert.Equal(2,policy.MaximumNodesPerRow);Assert.Equal(1,policy.MaximumGroupsPerRow);Assert.Equal(30,policy.HorizontalSpacing);Assert.Equal(60,policy.VerticalSpacing);Assert.Equal(12,policy.RowIndent);
        var facts=chart.Prepare().ToInterchangeEnvelope();var a=facts.Nodes.Single(n=>n.Id=="a");var b=facts.Nodes.Single(n=>n.Id=="b");var c=facts.Nodes.Single(n=>n.Id=="c");
        Assert.Equal(a.Y,b.Y);Assert.True(c.Y>a.Y);Assert.Equal(30,b.X!.Value-a.X!.Value-a.Width!.Value,6);Assert.Equal(12,c.X!.Value-a.X!.Value,6);
    }
    [Theory]
    [InlineData("containerFontSize","'2em'")] [InlineData("containerFontSize","0")]
    [InlineData("containerFontWeight","'bold;fill:red'")] [InlineData("containerFontFamily","'Arial;fill:red'")]
    [InlineData("container_bg_color","'url(https://example.test/x)'")] [InlineData("c4ShapeMargin","-1")]
    [InlineData("width","0")] [InlineData("wrap","'true'")]
    public void UnsupportedScalarDomainsHaveLocatedErrorsWithoutCssOrExternalFetch(string key,string value) {
        var parsed=new MermaidParser().ParseC4("%%{init: {'c4':{'"+key+"':"+value+"}}}%%\nC4Context\nContainer(api,\"API\")");
        Assert.True(parsed.HasErrors);Assert.Contains(parsed.Diagnostics,item=>item.Severity==MermaidDiagnosticSeverity.Error&&item.Span.Line==1);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NaturalWrapGrowsHeightAtMeasuredWidthAndFixedFramesRetainTheirContract(bool fixedFrame) {
        var family=BrowserProofFonts.Family;
        var source="%%{init: {'c4':{'containerFontFamily':'"+family+"','containerFontSize':18,'width':200,'c4ShapePadding':20,'wrap':true}}}%%\nC4Container\nContainer(api,\"Complete measured publication caption\",\"Managed runtime\",\"Every source word is retained across native measured rows\")";
        var options=new MermaidTopologyRenderOptions {Width=390,Height=700,Presentation=new MermaidPresentationOptions {TopologySizing=fixedFrame?MermaidTopologySizing.FixedViewport:MermaidTopologySizing.Natural}};
        var chart=Parse(source).ToTopologyChart(options);chart.WithTheme(chart.Theme.WithFontFamily(BrowserProofFonts.Family));var original=chart.Nodes.Select(node=>(node.Width,node.Height)).ToArray();var prepared=chart.Prepare();
        Assert.Equal(original,chart.Nodes.Select(node=>(node.Width,node.Height)).ToArray());
        var rows=prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(text=>text.Role is "topology-node-label" or "topology-node-subtitle").ToArray();
        Assert.True(rows.Length>2);var text=string.Join(" ",rows.SelectMany(row=>row.Text.Lines).Select(line=>line.Text));
        foreach(var word in new[]{"Complete","measured","publication","caption","Every","source","word","retained","native","rows"})Assert.Contains(word,text);
        Assert.DoesNotContain("…",text);
        var logicalOptions=chart.DefaultRenderOptions!.Clone();logicalOptions.FitContentToViewport=false;
        var logical=fixedFrame?chart.Prepare(logicalOptions):prepared;
        var logicalNode=Assert.Single(logical.ToInterchangeEnvelope().Nodes);var outputNode=Assert.Single(prepared.ToInterchangeEnvelope().Nodes);
        var outputScale=outputNode.Width!.Value/logicalNode.Width!.Value;
        Assert.True(!double.IsNaN(outputScale)&&!double.IsInfinity(outputScale)&&outputScale>0);
        Assert.Equal(logicalNode.Height!.Value*outputScale,outputNode.Height!.Value,6);
        // MaximumTextWidth is logical; a fixed frame scales the completed node and its text together.
        Assert.All(logical.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(row=>row.Role is "topology-node-label" or "topology-node-subtitle"),
            row=>Assert.True(row.Text.Metrics.Width<=160.001));
        Assert.All(rows,row=>Assert.True(row.Text.Metrics.Width/outputScale<=160.001));
        if(fixedFrame){Assert.Equal(390,prepared.Width);Assert.Equal(700,prepared.Height);}
        var image=RasterImageDecoder.Decode(prepared.ToPng());Assert.Equal((int)Math.Ceiling(prepared.Width),image.Width);Assert.Equal((int)Math.Ceiling(prepared.Height),image.Height);
        Capture(chart.ToVisualArtifact(),"c4-config-wrap-"+(fixedFrame?"fixed":"natural"));
    }
    [Fact]
    public void PreparedNodeHeaderAndRelationshipTypographyAndPortablePoliciesAreDetached() {
        var family=BrowserProofFonts.Family;var chart=Parse(Fixture()).ToTopologyChart();chart.WithTheme(chart.Theme.WithFontFamily(BrowserProofFonts.Family));
        Assert.All(chart.Groups,group=>Assert.Equal(family,group.TextStyle!.FontFamily));Assert.All(chart.Edges,edge=>Assert.Equal(15,edge.TextStyle!.FontSize));
        var prepared=chart.Prepare();var svg=prepared.ToSvg();var png=prepared.ToPng();var facts=prepared.ToInterchangeEnvelope();
        foreach(var node in chart.Nodes)if(node.TextStyle!=null){node.TextStyle.FontFamily="Missing later face";node.TextStyle.FontSize=90;}
        chart.Groups[0].TextStyle!.FontSize=90;chart.Edges[0].TextStyle!.FontSize=90;
        Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());
        try {FontRegistry.Clear();Assert.Equal(svg,prepared.ToSvg());Assert.Equal(png,prepared.ToPng());}
        finally {_=BrowserProofFonts.Family;}
        var restored=VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart();
        Assert.Equal(family,restored.Nodes.Single(node=>node.Id=="service").TextStyle!.FontFamily);Assert.Equal(18,restored.Groups[0].TextStyle!.FontSize);Assert.Equal(15,restored.Edges[0].TextStyle!.FontSize);
        var restoredFacts=restored.Prepare().ToInterchangeEnvelope();Assert.Equal(facts.Nodes.Select(node=>(node.Id,node.X,node.Y,node.Width,node.Height)),restoredFacts.Nodes.Select(node=>(node.Id,node.X,node.Y,node.Width,node.Height)));
    }
    [Theory]
    [InlineData(false,390)] [InlineData(true,390)] [InlineData(false,1100)] [InlineData(true,1100)]
    public void ConfiguredDirectFenceAndPortableArtifactsShareSourcePolicyAndNativeExports(bool dark,int width) {
        _=BrowserProofFonts.Family;var source=Fixture();var options=Options(dark,width);
        var direct=MermaidRenderer.Render(source,options);Assert.False(direct.HasErrors);var fenced=new MermaidVisualMarkupParser(options).Parse("```mermaid\n"+source+"\n```");Assert.False(fenced.HasErrors);
        var artifact=direct.Artifact!;var other=Assert.Single(fenced.Artifacts);Assert.Equal(artifact.ToSvg(),other.ToSvg());Assert.Equal(artifact.ToPng(),other.ToPng());
        var facts=artifact.ToInterchangeEnvelope();Assert.Equal(facts.ToJson(),VisualArtifactInterchangeEnvelope.FromUtf8Json(facts.ToUtf8Json()).ToJson());
        Assert.All(facts.Nodes,node=>Assert.NotNull(node.Topology!.TextStyle));Assert.All(facts.Groups,group=>Assert.NotNull(group.Topology!.TextStyle));Assert.All(facts.Edges,edge=>Assert.NotNull(edge.Topology!.TextStyle));
        var xml=XDocument.Parse(artifact.ToSvg());Assert.DoesNotContain(xml.Descendants(), node=>node.Name.LocalName is "script" or "image" or "foreignObject");
        Capture(artifact,"c4-config-direct-"+width+(dark?"-dark":"-light"));
    }
    [Fact]
    public void DeclaredButUnconsumedPinnedGeometryFieldsStayLocatedAndUnapplied() {
        const string body="C4Container\nContainer(api,\"API\")";
        const string init="%%{init: {'c4':{'height':100,'boxMargin':10,'wrapPadding':10,'c4ShapeInRow':1,'c4BoundaryInRow':1}}}%%\n";
        var parsed=new MermaidParser().ParseC4(init+body);Assert.False(parsed.HasErrors);
        foreach(var key in new[]{"height","boxMargin","wrapPadding","c4ShapeInRow","c4BoundaryInRow"}) {
            Assert.Contains(parsed.Document!.Configuration.Settings,item=>item.Path=="c4."+key);
            Assert.Contains(parsed.Diagnostics,item=>item.Code==MermaidDiagnosticCodes.UnsupportedConfiguration&&item.Span.Line==1&&item.Message.Contains("c4."+key,StringComparison.Ordinal));
        }
        var configured=parsed.Document!.ToTopologyChart();Assert.Null(configured.GridLayout);
        Assert.Equal(Parse(body).ToSvg(),configured.ToSvg());
    }
    [Fact]
    public void NativeTextAndGridPoliciesRejectNonFiniteOrNegativeGeometryBeforePreparation() {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TopologyNode {MaximumTextWidth=double.NaN});
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TopologyNode {MaximumTextWidth=0});
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TopologyNode {ContentPadding=-1});
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TopologyGridLayout(horizontalSpacing:double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new TopologyGridLayout(paddingY:double.NaN));
    }
    internal static MermaidC4Document Parse(string source) {var result=new MermaidParser().ParseC4(source);Assert.False(result.HasErrors,string.Join("; ",result.Diagnostics.Select(item=>item.Message)));return result.Document!;}
    internal static string Fixture() {DirectoryInfo? root=new(AppContext.BaseDirectory);while(root!=null&&!File.Exists(Path.Combine(root.FullName,"ChartForgeX.sln")))root=root.Parent;return File.ReadAllText(Path.Combine(root!.FullName,"tests","mermaid-conformance","fixtures","c4-config-typography-native.mmd")).Replace("Carlito",BrowserProofFonts.Family,StringComparison.Ordinal);}
    internal static MermaidRenderOptions Options(bool dark,int width)=>new(){C4=new MermaidTopologyRenderOptions{Width=width,Height=850},Presentation=new MermaidPresentationOptions{FontFamily=BrowserProofFonts.Family,ThemeMode=dark?VisualThemeMode.Dark:VisualThemeMode.Light}};
    internal static void Capture(VisualArtifact artifact,string name) {var root=Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");if(string.IsNullOrWhiteSpace(root))return;if(!Path.IsPathRooted(root))throw new InvalidOperationException("Capture root must be absolute.");Directory.CreateDirectory(root);var stem=Path.Combine(root,name);File.WriteAllText(stem+".svg",artifact.ToSvg());File.WriteAllBytes(stem+".png",artifact.ToPng());File.WriteAllText(stem+".json",artifact.ToInterchangeJson());}
}
