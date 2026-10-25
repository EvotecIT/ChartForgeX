using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyExternalCaptionPolicyTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Icon, false)] [InlineData(TopologyNodeDisplayMode.Icon, true)]
    [InlineData(TopologyNodeDisplayMode.Tile, false)] [InlineData(TopologyNodeDisplayMode.Tile, true)]
    public void LiteralCaptionRoleUsesItsMeasuredFontAndOccupiedBoundsAboveOrBelow(TopologyNodeDisplayMode mode, bool top) {
        var chart=Source(mode);var node=chart.Nodes[0];node.CaptionPlacement=top?TopologyCaptionPlacement.Top:TopologyCaptionPlacement.Bottom;
        node.CaptionGap=8;node.CaptionSurface=TopologyCaptionSurface.Plain;
        node.CaptionTextStyle=new TextStyleOverride{FontFamily=BrowserProofFonts.Family,FontSize=14,FontWeight="400",Color=ChartColor.FromHex("#123456")};
        var prepared=chart.Prepare();var svg=prepared.ToSvg();var png=prepared.ToPng();var envelope=prepared.ToInterchangeEnvelope();var json=envelope.ToJson();
        Capture(mode+"-"+top,svg,png,json);
        var restored=VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();var after=restored.Prepare();Capture(mode+"-"+top+"-restored",after.ToSvg(),after.ToPng(),after.ToInterchangeEnvelope().ToJson());
        var runs=prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(mark=>mark.Role=="inline-text-run").ToArray();
        Assert.NotEmpty(runs);Assert.All(runs,mark=>Assert.Equal(14,mark.Text.Size,9));
        var imported=Assert.Single(restored.Nodes);var plan=TopologyExternalCaption.Prepare(imported,restored.ResolveRenderOptions(null));
        Assert.NotNull(plan);if(top)Assert.True(plan!.Bounds.Bottom<=imported.Y-8+.00001);else Assert.True(plan!.Bounds.Top>=imported.Y+imported.Height+8-.00001);
        Assert.Equal(14,imported.CaptionTextStyle!.FontSize);Assert.Equal(2,envelope.Version);Assert.Equal(png,after.ToPng());
        imported.CaptionTextStyle.FontSize=19;Assert.NotEqual(png,restored.ToPng());Assert.Equal(14,envelope.Nodes[0].Topology!.CaptionTextStyle!.FontSize);
    }

    [Theory] [InlineData(TopologyNodeDisplayMode.Icon)] [InlineData(TopologyNodeDisplayMode.Tile)] [InlineData(TopologyNodeDisplayMode.Artwork)]
    public void HiddenAndEmptyCaptionsKeepTheirInkAbsentEvenWhenTheRoleAndSurfaceExist(TopologyNodeDisplayMode mode) {
        var chart=Source(mode);var node=chart.Nodes[0];node.Label="";node.ShowLabel=false;node.CaptionTextStyle=new TextStyleOverride{FontSize=20};node.CaptionSurface=TopologyCaptionSurface.Plate;
        var prepared=chart.Prepare();Capture("hidden-"+mode,prepared.ToSvg(),prepared.ToPng(),prepared.ToInterchangeEnvelope().ToJson());
        Assert.DoesNotContain(prepared.Visual.Scene.Nodes,mark=>mark.Role is "topology-node-label" or "topology-node-icon-label" or "inline-text-run");
        Assert.False(prepared.ToInterchangeEnvelope().Nodes[0].Topology!.ShowLabel);
    }

    [Fact]
    public void ArtworkRoleOrSurfaceAloneDoesNotEnableTheIncumbentlyHiddenCaption() {
        var chart=Source(TopologyNodeDisplayMode.Artwork);var node=chart.Nodes[0];var original=chart.Prepare();
        node.CaptionTextStyle=new TextStyleOverride{FontSize=20};node.CaptionSurface=TopologyCaptionSurface.Plate;
        var styled=chart.Prepare();Capture("artwork-inactive-role",styled.ToSvg(),styled.ToPng(),styled.ToInterchangeEnvelope().ToJson());
        Assert.Equal(original.ToSvg(),styled.ToSvg());Assert.Equal(original.ToPng(),styled.ToPng());
        Assert.DoesNotContain(styled.Visual.Scene.Nodes,mark=>mark.Role=="topology-node-label");
    }

    [Theory] [InlineData(TopologyNodeDisplayMode.Card)] [InlineData(TopologyNodeDisplayMode.CompactCard)] [InlineData(TopologyNodeDisplayMode.Pill)]
    public void ACaptionRoleRejectsAnIncompatibleNativeModeBeforePreparation(TopologyNodeDisplayMode mode) {
        var chart=Source(mode);chart.Nodes[0].CaptionTextStyle=new TextStyleOverride{FontSize=14};
        Assert.Throws<TopologyValidationException>(()=>chart.Prepare());
    }

    [Fact]
    public void NullAndClearedExternalPoliciesKeepTheActualLegacySvgPngAndPortableBytes() {
        var chart=Source(TopologyNodeDisplayMode.Icon);var before=chart.Prepare();var svg=before.ToSvg();var png=before.ToPng();var json=before.ToInterchangeEnvelope().ToJson();
        var node=chart.Nodes[0];node.CaptionTextStyle=new TextStyleOverride{FontSize=14};node.CaptionPlacement=TopologyCaptionPlacement.Top;node.CaptionSurface=TopologyCaptionSurface.Plain;node.CaptionGap=8;
        var frozen=chart.Prepare();node.CaptionTextStyle=null;node.CaptionPlacement=null;node.CaptionSurface=null;node.CaptionGap=null;
        var after=chart.Prepare();Capture("null-caption-control",after.ToSvg(),after.ToPng(),after.ToInterchangeEnvelope().ToJson());
        Assert.Equal(svg,after.ToSvg());Assert.Equal(png,after.ToPng());Assert.Equal(json,after.ToInterchangeEnvelope().ToJson());
        Assert.Equal(14,frozen.ToInterchangeEnvelope().Nodes[0].Topology!.CaptionTextStyle!.FontSize);
    }

    internal static TopologyChart Source(TopologyNodeDisplayMode mode) {
        var chart=TopologyChart.Create().WithId("external-caption-policy").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).AddNode("node","Literal caption",210,150,width:180,height:90);
        var node=chart.Nodes[0];node.DisplayMode=mode;node.Shape=TopologyNodeShape.Rectangle;node.IconShape=TopologyIconShape.Router;
        node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;node.Color="#2563EB";
        if(mode==TopologyNodeDisplayMode.Artwork)node.Artwork=TopologyIconArtwork.Portrait(new ChartForgeX.Raster.RgbaImage(2,1,new byte[]{255,0,0,255,0,255,0,128}));
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeLegend=false,IncludeIconLabels=true,FitContentToViewport=false,MaxNodeLabelLines=4,WrapNodeLabels=true});return chart;
    }
    internal static void Capture(string name,string svg,byte[] png,string json) {
        if(Environment.GetEnvironmentVariable("CFX_FLOWCHART_ASSET_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var path=Path.Combine(output,"external-caption-"+name);File.WriteAllText(path+".svg",svg);File.WriteAllBytes(path+".png",png);File.WriteAllText(path+".json",json);
    }
}
