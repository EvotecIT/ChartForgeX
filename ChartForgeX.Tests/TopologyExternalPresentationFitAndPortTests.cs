using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExternalPresentationFitAndPortTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Icon,false)] [InlineData(TopologyNodeDisplayMode.Icon,true)]
    [InlineData(TopologyNodeDisplayMode.Tile,false)] [InlineData(TopologyNodeDisplayMode.Tile,true)]
    public void FittedExternalGlyphCaptionRadiusAndStrokeReuseOneCapturedUnitBasis(TopologyNodeDisplayMode mode,bool enlarge) {
        var chart=TopologyExternalCaptionPolicyTests.Source(mode);chart.WithLayout(TopologyLayoutMode.Layered);chart.WithViewport(enlarge?1000:180,enlarge?700:140,24);
        var node=chart.Nodes[0];node.CaptionPlacement=TopologyCaptionPlacement.Top;node.CaptionGap=8;node.CaptionSurface=TopologyCaptionSurface.Plate;
        node.CaptionTextStyle=new TextStyleOverride{FontFamily=BrowserProofFonts.Family,FontSize=14,FontWeight="400"};node.GlyphScale=1.5;node.CornerRadius=12;node.StrokeWidth=3;node.BorderColor="#ef4444";
        var options=chart.ResolveRenderOptions(null);options.FitContentToViewport=true;var direct=chart.Prepare(options);var svg=direct.ToSvg();var png=direct.ToPng();var json=direct.ToInterchangeEnvelope().ToJson();Capture(mode+"-"+enlarge,direct);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();Capture(mode+"-"+enlarge+"-restored",after);
        var imported=Assert.Single(restored.Nodes);var basis=TopologyPreparedNodeBasis.Resolve(imported);Assert.NotNull(basis);Assert.True(enlarge?basis!.Factor>1:basis!.Factor<1);
        Assert.Equal(png,after.ToPng());Assert.Equal(1.5,imported.GlyphScale);Assert.Equal(12*basis!.Factor,imported.CornerRadius!.Value,8);Assert.Equal(14*basis.Factor,imported.CaptionTextStyle!.FontSize!.Value,8);
        var plan=TopologyExternalCaption.Prepare(imported,restored.ResolveRenderOptions(null));Assert.NotNull(plan);imported.X+=13;imported.Y-=7;
        var translated=TopologyExternalCaption.Prepare(imported,restored.ResolveRenderOptions(null));Assert.Equal(plan!.Bounds.X+13,translated!.Bounds.X,8);Assert.Equal(plan.Bounds.Y-7,translated.Bounds.Y,8);Assert.Same(basis,TopologyPreparedNodeBasis.Resolve(imported));
        imported.Width+=1;Assert.Null(TopologyPreparedNodeBasis.Resolve(imported));Capture(mode+"-"+enlarge+"-edited",restored.Prepare());Assert.Equal(svg,direct.ToSvg());Assert.Equal(png,direct.ToPng());
        var automatic=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();automatic.LayoutMode=TopologyLayoutMode.Layered;
        var layout=TopologyLayoutEngine.Prepare(automatic,options:automatic.ResolveRenderOptions(null));Assert.Null(layout.Nodes[0].PreparedNodeBasis);
    }

    [Theory] [InlineData(TopologyIconShape.Router)] [InlineData(TopologyIconShape.Database)] [InlineData(TopologyIconShape.Cloud)] [InlineData(TopologyIconShape.Badge)]
    public void ExplicitGlyphScaleUsesActualSurfaceAndMarkBoundsBeforeSlotAdmission(TopologyIconShape shape) {
        var chart=TopologyExternalCaptionPolicyTests.Source(TopologyNodeDisplayMode.Icon);var node=chart.Nodes[0];node.ShowLabel=false;node.IconShape=shape;node.Symbol=shape==TopologyIconShape.Badge?"Q":null;node.GlyphScale=2;
        var prepared=chart.Prepare();Capture("glyph-"+shape,prepared);var glyph=TopologyGlyphBounds.Measure(node,chart.ResolveRenderOptions(null),chart.Theme.AxisStrokeWidth,chart.Theme.BarRadius);
        Assert.True(glyph.Ink.Width>0&&glyph.Ink.Height>0);Assert.True(glyph.Ink.Left>=node.X-glyph.StrokeOverflow&&glyph.Ink.Right<=node.X+node.Width+glyph.StrokeOverflow);
        Assert.Equal(prepared.ToPng(),VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson()).ToTopologyChart().ToPng());
        node.Height=12;var error=Assert.Throws<InvalidOperationException>(()=>chart.Prepare());Assert.Contains("scaled native glyph ink exceeds",error.Message,StringComparison.Ordinal);
    }

    [Theory] [InlineData(10d,10d)] [InlineData(1000d,30d)]
    public void NamedPortsAndCaptionAllocationUseTheSameClampedRoundedBody(double authored,double resolved) {
        var chart=TopologyChart.Create().WithId("rounded-external-port").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source","Review",150,170,width:100,height:60).AddNode("target","Publish",400,40,width:100,height:60)
            .AddNodePort("source","exit",TopologyEdgePort.Top,0).AddEdge("request","source","target",routing:TopologyEdgeRouting.Orthogonal);
        foreach(var node in chart.Nodes){node.ShowStatusBadge=false;node.Shape=TopologyNodeShape.Rectangle;node.PreserveDisplayModeSize=true;}chart.Nodes[0].CornerRadius=authored;chart.Edges[0].SourcePortId="exit";
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeLegend=false,FitContentToViewport=false});var prepared=chart.Prepare();Capture("radius-port-"+authored,prepared);
        var envelope=prepared.ToInterchangeEnvelope();var route=envelope.Edges[0].ResolvedRoute;var source=envelope.Nodes[0];
        Assert.NotEmpty(route);Assert.Equal(source.X!.Value,route[0].X,6);Assert.Equal(source.Y!.Value+resolved-7,route[0].Y,6);Assert.Equal("exit",envelope.Edges[0].SourcePortId);
        Assert.Equal(prepared.ToPng(),VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart().ToPng());
        var content=TopologyNodeShapeGeometry.CaptionBounds(chart.Nodes[0],0);Assert.True(content.Left>chart.Nodes[0].X);Assert.True(content.Right<chart.Nodes[0].X+chart.Nodes[0].Width);
    }

    [Fact]
    public void UndefinedOrNonfinitePortablePoliciesFailAtTheExistingTypedValidator() {
        var chart=TopologyExternalCaptionPolicyTests.Source(TopologyNodeDisplayMode.Icon);var envelope=chart.Prepare().ToInterchangeEnvelope();var node=envelope.Nodes[0].Topology!;
        node.CaptionPlacement=(TopologyCaptionPlacement)999;Assert.Throws<NotSupportedException>(()=>envelope.Validate());node.CaptionPlacement=null;
        node.CaptionGap=double.NaN;Assert.Throws<NotSupportedException>(()=>envelope.Validate());node.CaptionGap=null;
        node.GlyphScale=double.PositiveInfinity;Assert.Throws<NotSupportedException>(()=>envelope.Validate());node.GlyphScale=null;
        node.CornerRadius=-1;Assert.Throws<NotSupportedException>(()=>envelope.Validate());node.CornerRadius=null;
        node.BorderColor="url(https://example.invalid/paint)";Assert.Throws<ArgumentException>(()=>envelope.Validate());node.BorderColor=null;envelope.Validate();
    }

    private static void Capture(string name,PreparedTopology prepared)=>TopologyExternalCaptionPolicyTests.Capture(name,prepared.ToSvg(),prepared.ToPng(),prepared.ToInterchangeEnvelope().ToJson());
}
