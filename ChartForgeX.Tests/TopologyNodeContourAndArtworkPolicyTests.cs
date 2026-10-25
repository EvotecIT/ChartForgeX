using System.Xml.Linq;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyNodeContourAndArtworkPolicyTests {
    [Theory] [InlineData("#ef4444")] [InlineData("none")] [InlineData("transparent")]
    public void ExplicitContourColorLeavesTheNativeGlyphAndCaptionPaintIndependent(string border) {
        var chart=TopologyExternalCaptionPolicyTests.Source(TopologyNodeDisplayMode.Icon);var node=chart.Nodes[0];var ordinary=chart.Prepare();
        var oldSvg=ordinary.ToSvg();var oldPng=ordinary.ToPng();var oldJson=ordinary.ToInterchangeEnvelope().ToJson();
        node.BorderColor=border;var prepared=chart.Prepare();var svg=prepared.ToSvg();var png=prepared.ToPng();var json=prepared.ToInterchangeEnvelope().ToJson();
        Capture("border-"+border.TrimStart('#'),prepared);var envelope=VisualArtifactInterchangeEnvelope.FromJson(json);var restored=envelope.ToTopologyChart().Prepare();Capture("border-restored-"+border.TrimStart('#'),restored);
        var exterior=Marks(svg,"topology-node-surface");Assert.NotEmpty(exterior);
        if(border[0]=='#')Assert.All(exterior,mark=>Assert.Equal(border,(string?)mark.Attribute("stroke"),ignoreCase:true));else Assert.All(exterior,mark=>Assert.True(NoStroke(mark)));
        Assert.Equal(PaintFacts(oldSvg,"topology-icon-surface"),PaintFacts(svg,"topology-icon-surface"));Assert.Equal(PaintFacts(oldSvg,"topology-node-icon"),PaintFacts(svg,"topology-node-icon"));
        Assert.NotEmpty(Marks(svg,"topology-icon-surface"));Assert.NotEmpty(Marks(svg,"topology-node-icon"));Assert.Equal(border,envelope.Nodes[0].Topology!.BorderColor);Assert.Equal(2,envelope.Version);Assert.Equal(png,restored.ToPng());
        envelope.Nodes[0].Topology!.BorderColor="#111111";Assert.Equal(border,prepared.ToInterchangeEnvelope().Nodes[0].Topology!.BorderColor);
        node.BorderColor=null;var cleared=chart.Prepare();Assert.Equal(oldSvg,cleared.ToSvg());Assert.Equal(oldPng,cleared.ToPng());Assert.Equal(oldJson,cleared.ToInterchangeEnvelope().ToJson());
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void AnExplicitArtworkFramePaintsBehindCapturedPixelsAndKeepsTheirActualColors(bool opaque) {
        var chart=TopologyExternalCaptionPolicyTests.Source(TopologyNodeDisplayMode.Artwork);var node=chart.Nodes[0];node.ShowLabel=false;node.Artwork=TopologyIconArtwork.Portrait(new RgbaImage(1,1,new byte[]{255,0,0,opaque?(byte)255:(byte)0}));
        var ordinary=chart.Prepare();var oldPixel=CenterPixel(ordinary);node.BackgroundColor="#00ff00";node.BorderColor="#2563eb";node.CornerRadius=0;
        var framed=chart.Prepare();Capture("artwork-frame-"+opaque,framed);var restored=VisualArtifactInterchangeEnvelope.FromJson(framed.ToInterchangeEnvelope().ToJson()).ToTopologyChart().Prepare();Capture("artwork-frame-restored-"+opaque,restored);
        Assert.NotEmpty(Marks(framed.ToSvg(),"topology-node-surface"));Assert.NotEmpty(Marks(framed.ToSvg(),"topology-node-artwork"));Assert.Equal(framed.ToPng(),restored.ToPng());
        var pixel=CenterPixel(framed);if(opaque){Assert.Equal(new byte[]{255,0,0,255},pixel);Assert.Equal(oldPixel,pixel);}else{Assert.Equal(new byte[]{0,255,0,255},pixel);Assert.NotEqual(oldPixel,pixel);}
    }

    [Fact]
    public void OrdinaryArtworkColorShapeAndWholeObjectOpacityDoNotConjureAFrame() {
        var chart=TopologyExternalCaptionPolicyTests.Source(TopologyNodeDisplayMode.Artwork);var ordinary=chart.Prepare();chart.Nodes[0].Color="#ef4444";chart.Nodes[0].Shape=TopologyNodeShape.Rectangle;chart.Nodes[0].Opacity=.6;
        var prepared=chart.Prepare();Capture("artwork-legacy-no-frame",prepared);Assert.Empty(Marks(prepared.ToSvg(),"topology-node-surface"));
        Assert.Equal(PaintFacts(ordinary.ToSvg(),"topology-node-artwork"),PaintFacts(prepared.ToSvg(),"topology-node-artwork"));
    }

    [Theory] [InlineData(TopologyNodeShape.Actor)] [InlineData(TopologyNodeShape.ActorIcon)]
    public void NewExternalCaptionAndGlyphPoliciesRejectActorModesWithoutChangingLegacyNullAdmission(TopologyNodeShape shape) {
        var chart=TopologyExternalCaptionPolicyTests.Source(TopologyNodeDisplayMode.Icon);chart.Nodes[0].Shape=shape;var ordinary=chart.Prepare();Capture("actor-null-"+shape,ordinary);
        chart.Nodes[0].GlyphScale=1;Assert.Throws<TopologyValidationException>(()=>chart.Prepare());chart.Nodes[0].GlyphScale=null;chart.Nodes[0].CaptionPlacement=TopologyCaptionPlacement.Bottom;
        Assert.Throws<TopologyValidationException>(()=>chart.Prepare());chart.Nodes[0].CaptionPlacement=null;Assert.Equal(ordinary.ToPng(),chart.ToPng());
    }

    private static byte[] CenterPixel(PreparedTopology prepared){var node=prepared.ToInterchangeEnvelope().Nodes[0];var image=prepared.ToRgba();var x=(int)Math.Floor(node.X!.Value+node.Width!.Value/2);var y=(int)Math.Floor(node.Y!.Value+node.Height!.Value/2);var index=(y*image.Width+x)*4;return image.Pixels.Skip(index).Take(4).ToArray();}
    private static XElement[] Marks(string svg,string role)=>XDocument.Parse(svg).Descendants().Where(mark=>(string?)mark.Attribute("data-cfx-role")==role).ToArray();
    private static string[] PaintFacts(string svg,string role)=>Marks(svg,role).Select(mark=>mark.Name.LocalName+"|"+string.Join("|",new[]{"d","x","y","width","height","cx","cy","rx","ry","fill","stroke","stroke-width","opacity"}.Select(key=>key+"="+(string?)mark.Attribute(key)))).ToArray();
    private static bool NoStroke(XElement mark)=>(string?)mark.Attribute("stroke") is null or "none" or "transparent" or "#00000000" or "rgba(0,0,0,0)"||(string?)mark.Attribute("stroke-opacity")=="0";
    private static void Capture(string name,PreparedTopology prepared)=>TopologyExternalCaptionPolicyTests.Capture(name,prepared.ToSvg(),prepared.ToPng(),prepared.ToInterchangeEnvelope().ToJson());
}
