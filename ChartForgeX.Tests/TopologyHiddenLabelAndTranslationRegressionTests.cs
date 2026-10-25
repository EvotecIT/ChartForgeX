using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyHiddenLabelAndTranslationRegressionTests {
    [Theory] [InlineData("")] [InlineData("  \t")]
    public void HiddenEmptySemanticLabelsKeepTheirExactFactsAndNoCaptionInk(string label) {
        var chart = Source(TopologyIconShape.Router); var node = chart.Nodes[0]; node.Label = label; node.ShowLabel = false;
        var before = chart.Prepare(); var saved = before.ToInterchangeEnvelope(); Capture("hidden-" + label.Length, before);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart(); var after = restored.Prepare(); Capture("hidden-restored-" + label.Length, after);
        Assert.Equal(label, saved.Nodes[0].Label); Assert.Equal(label, restored.Nodes[0].Label); Assert.False(restored.Nodes[0].ShowLabel);
        Assert.DoesNotContain(before.Visual.Scene.Nodes, mark => mark.Role is "topology-node-icon-label" or "topology-node-label" or "inline-text-run");
        Assert.Equal(before.ToPng(), after.ToPng());
        node.ShowLabel = true; Assert.Throws<TopologyValidationException>(() => chart.Prepare());
        node.ShowLabel = false; Assert.Throws<ArgumentNullException>(() => node.Label = null!);
    }

    [Theory] [InlineData(TopologyIconShape.Server)] [InlineData(TopologyIconShape.Database)] [InlineData(TopologyIconShape.Router)]
    public void AUnitScaleFrameTranslationRetainsTheOriginalQuantizedGlyphPose(TopologyIconShape icon) {
        var chart = Source(icon); chart.Title = "Source heading";
        var before = chart.Prepare(); var saved = before.ToInterchangeEnvelope(); Capture("translated-" + icon, before);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart(); var after = restored.Prepare(); Capture("translated-restored-" + icon, after);
        var basis = Assert.IsType<TopologyPreparedNodeBasis>(saved.Nodes[0].Topology!.PreparedNodeBasis);
        var importedBasis=Assert.IsType<TopologyPreparedNodeBasis>(TopologyPreparedNodeBasis.Resolve(restored.Nodes[0]));
        Assert.Equal(1, basis.Factor); Assert.Equal(basis.Factor,importedBasis.Factor); Assert.Equal(before.ToPng(), after.ToPng());
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg()));
        restored.Nodes[0].Y += 11; var moved = restored.Prepare(); Capture("translated-edited-" + icon, moved);
        Assert.Same(importedBasis, TopologyPreparedNodeBasis.Resolve(restored.Nodes[0])); Assert.NotEqual(before.ToPng(), moved.ToPng());
        restored.Nodes[0].IconShape = TopologyIconShape.Cloud; Assert.Null(TopologyPreparedNodeBasis.Resolve(restored.Nodes[0]));
    }

    [Fact]
    public void UnitScalePlainEdgePaintRetainsItsActualLineLayersWhileTheOrdinaryRequestStaysVersionOne() {
        var chart = TopologyChart.Create().WithId("plain-route-capture").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("from","From",80,120,width:100,height:60).AddNode("to","To",380,120,width:100,height:60)
            .AddEdge("link","from","to",routing:TopologyEdgeRouting.Orthogonal);
        foreach (var node in chart.Nodes) { node.Shape=TopologyNodeShape.Rectangle; node.ShowStatusBadge=false; node.PreserveDisplayModeSize=true; }
        chart.WithRenderOptions(new TopologyRenderOptions { IncludeTitle=false, IncludeLegend=false, FitContentToViewport=false });
        var ordinary = chart.Prepare(); var ordinaryJson=ordinary.ToInterchangeEnvelope().ToJson(); Capture("ordinary-route", ordinary);
        Assert.Null(ordinary.ToInterchangeEnvelope().Presentation!.TopologyCaptions); Assert.Equal(1,ordinary.ToInterchangeEnvelope().Version);
        var options=chart.ResolveRenderOptions(null).Clone(); options.EdgeVisualStyle=ChartLineVisualStyle.Plain();
        var plain=chart.Prepare(options); var saved=plain.ToInterchangeEnvelope(); Capture("plain-route",plain);
        var after=VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare();Capture("plain-route-restored",after);
        Assert.NotNull(saved.Presentation!.TopologyCaptions!.Routes); Assert.Equal(2,saved.Version);
        Assert.Equal(plain.ToPng(),after.ToPng());
        static string[] EdgeRoles(string svg)=>XDocument.Parse(svg).Descendants().Select(mark=>(string?)mark.Attribute("data-cfx-role"))
            .Where(role=>role!=null&&role.StartsWith("topology-edge-line",StringComparison.Ordinal)).Cast<string>().ToArray();
        Assert.Equal(new[]{"topology-edge-line"},EdgeRoles(plain.ToSvg()));Assert.Equal(EdgeRoles(plain.ToSvg()),EdgeRoles(after.ToSvg()));
        Assert.Equal(ordinaryJson,chart.Prepare().ToInterchangeEnvelope().ToJson());
    }

    private static TopologyChart Source(TopologyIconShape icon) {
        var chart=TopologyChart.Create().WithId("hidden-glyph-and-unit-pose").WithTitle("").WithViewport(640,400,24).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("node","Native caption",111.1875,83.46770833333333,width:63,height:63);
        chart.Theme=VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family);
        var node=chart.Nodes[0];node.DisplayMode=TopologyNodeDisplayMode.Icon;node.Shape=TopologyNodeShape.Rounded;node.IconShape=icon;
        node.PreserveDisplayModeSize=true;node.ShowStatusBadge=false;node.GlyphScale=48d/26;
        chart.WithRenderOptions(new TopologyRenderOptions{IncludeLegend=false,IncludeIconLabels=true,FitContentToViewport=false});return chart;
    }
    private static void Capture(string name,PreparedTopology prepared) {
        if(Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not {Length:>0} output)return;
        Directory.CreateDirectory(output);var stem=Path.Combine(output,"topology182-"+name);
        File.WriteAllText(stem+".svg",prepared.ToSvg());File.WriteAllBytes(stem+".png",prepared.ToPng());File.WriteAllText(stem+".json",prepared.ToInterchangeEnvelope().ToJson());
    }
}
