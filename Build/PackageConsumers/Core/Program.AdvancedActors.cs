using System;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void NativeAdvancedActorContracts() {
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite().WithFontFamily(PackageAssertions.OutlineFontFamily)).WithId("packed-native-actors").WithViewport(600, 420, 0).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("business", "Business participant", 40, 50, width: 190, height: 130)
            .AddNode("router", "Native router", 350, 50, width: 190, height: 130).AddEdge("request", "business", "router");
        chart.Nodes[0].Shape = TopologyNodeShape.ActorBusinessHollow; chart.Nodes[1].Shape = TopologyNodeShape.ActorIcon;
        chart.Nodes[1].IconShape = TopologyIconShape.Router;
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.ShowStatusBadge = false; node.Subtitle = "«external»"; node.PreserveDisplayModeSize = true; }
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng(); var envelope = prepared.ToInterchangeEnvelope();
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();
        PackageAssertions.Require(envelope.Version == 2 && loaded.Nodes[0].Shape == TopologyNodeShape.ActorBusinessHollow
            && loaded.Nodes[1].IconShape == TopologyIconShape.Router && loaded.Nodes[1].IconId == null && loaded.Edges.Single().Id == "request",
            "Packed Core actor authoring lost finite contour/glyph/relationship facts during native JSON editing.");
        loaded.Nodes[0].Subtitle = "«reviewed»"; chart.Nodes.Clear(); chart.Edges.Clear();
        PackageAssertions.Require(svg == prepared.ToSvg() && png.SequenceEqual(prepared.ToPng()), "Packed native actor export retained mutable chart ownership.");
        PackageAssertions.Png(png); PackageAssertions.Png(loaded.ToPng());
    }
}
