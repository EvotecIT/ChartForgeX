using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyTransparentDefaultFrameCausalTests {
    [Fact]
    public void ExplicitDefaultFrameRetainsItsRealBackgroundSceneAndSvgMarkUnderATransparentTheme() {
        var chart=Source(true);var options=chart.ResolveRenderOptions(null);
        var before=chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640,400),24),chart.Theme,frame:new VisualFrame()),options);
        var saved=before.ToArtifact().ToInterchangeEnvelope();var after=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare();Capture("explicit-direct",before);Capture("explicit-restored",after.Visual);
        Assert.Single(before.Scene.Nodes,mark=>mark.Role=="background");Assert.Single(after.Visual.Scene.Nodes,mark=>mark.Role=="background");
        Assert.Equal(Background(before.ToSvg()),Background(after.ToSvg()));Assert.Equal(before.ToPng(),after.ToPng());
    }
    [Fact]
    public void TransparentConvenienceStillOmitsItsCanonicalFrameAndBackgroundMark() {
        var chart=Source(true);var before=chart.Prepare();var saved=before.ToInterchangeEnvelope();var after=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare();Capture("transparent-convenience",before.Visual);Capture("transparent-convenience-restored",after.Visual);
        Assert.DoesNotContain(before.Visual.Scene.Nodes,mark=>mark.Role=="background");Assert.DoesNotContain(after.Visual.Scene.Nodes,mark=>mark.Role=="background");
        Assert.DoesNotContain("\"frame\"",saved.ToJson(),StringComparison.Ordinal);Assert.Equal(before.ToPng(),after.ToPng());
        Assert.NotNull(saved.Presentation!.Theme!.Definition);Assert.Equal(2,saved.Version);
    }
    [Fact]
    public void OrdinaryOpaqueDefaultFrameStillKeepsTheActualVersionOneContract() {
        var chart=Source(false);chart.Theme=VisualTheme.Graphite();var before=chart.Prepare();var saved=before.ToInterchangeEnvelope();Capture("opaque-default",before.Visual);
        Assert.Equal(1,saved.Version);Assert.DoesNotContain("\"frame\"",saved.ToJson(),StringComparison.Ordinal);
        Assert.Single(before.Visual.Scene.Nodes,mark=>mark.Role=="background");
    }
    private static string[] Background(string svg)=>XDocument.Parse(svg).Descendants().Where(mark=>(string?)mark.Attribute("data-cfx-role")=="background").Select(mark=>{var copy=new XElement(mark);copy.Attributes().Where(attribute=>attribute.Name.LocalName=="id").Remove();return copy.ToString(SaveOptions.DisableFormatting);}).ToArray();
    private static TopologyChart Source(bool transparent) {
        var chart=TopologyChart.Create().WithId("transparent-default-frame").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual).AddNode("node","Node",180,140,width:120,height:70);
        chart.Theme=VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);if(transparent)chart.Theme=chart.Theme.Configure(builder=>{builder.Light.Background=ChartColor.Transparent;builder.Dark.Background=ChartColor.Transparent;});
        chart.Nodes[0].Shape=TopologyNodeShape.Rectangle;chart.Nodes[0].ShowStatusBadge=false;chart.Nodes[0].PreserveDisplayModeSize=true;
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeTitle=false,IncludeLegend=false,FitContentToViewport=false});return chart;
    }
    private static void Capture(string name,PreparedVisual value) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not{Length:>0} output)return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,"transparent-frame-"+name);File.WriteAllText(stem+".svg",value.ToSvg());File.WriteAllBytes(stem+".png",value.ToPng());File.WriteAllText(stem+".json",value.ToArtifact().ToInterchangeJson());
    }
}
