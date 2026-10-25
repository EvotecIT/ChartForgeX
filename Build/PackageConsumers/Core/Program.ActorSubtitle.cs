using System;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX;
using ChartForgeX.Topology;
using ChartForgeX.Typography;

internal static partial class Program {
    private static void NativeActorSubtitleContracts() {
        var chart = TopologyChart.Create().WithId("package-actor-caption").WithViewport(700, 520, 24)
            .WithLayout(TopologyLayoutMode.Layered)
            .AddAutoNode("author", "Release coordinator", width: 180, height: 110, subtitle: "primary actor")
            .AddAutoNode("publish", "Publish documents", width: 180, height: 80)
            .AddEdge("request", "author", "publish", "requests", routing: TopologyEdgeRouting.Straight);
        var actor = chart.Nodes[0]; actor.Shape = TopologyNodeShape.Actor; actor.ShowStatusBadge = false;
        actor.PreserveDisplayModeSize = true; actor.DisplayMode = TopologyNodeDisplayMode.Card;
        actor.TextStyle = new TextStyleOverride { FontSize = 16, FontWeight = "500" };
        var prepared = chart.Prepare(); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var document = XDocument.Parse(svg);
        PackageAssertions.Require(document.Descendants().Any(node => (string?)node.Attribute("data-cfx-role") == "topology-node-subtitle"),
            "Packed Core Actor omitted its native subtitle owner.");
        var words = string.Join(" ", document.Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value));
        PackageAssertions.Require(words.Contains("primary actor") && words.Contains("coordinator"), "Packed Actor lost complete native caption words.");
        PackageAssertions.Require(prepared.ToInterchangeEnvelope().Nodes.Single(node => node.Id == "author").Subtitle == "primary actor",
            "Packed Actor subtitle was not retained as a source fact.");
        actor.Subtitle = "changed"; actor.TextStyle.FontSize = 60; chart.Edges.Clear(); chart.Nodes.Clear();
        PackageAssertions.Require(prepared.ToSvg() == svg && prepared.ToPng().SequenceEqual(png), "Packed Actor exports retained a mutable model or typography dependency.");
        PackageAssertions.Png(png);
    }
}
