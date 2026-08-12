using System;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualArtifactInterchangeEnforcesDeterministicExactTopologyContracts() {
        var metricOrderA = TopologyChart.Create();
        var metricNodeA = new TopologyNode { Id = "metrics", Label = "Metrics" };
        metricNodeA.Metrics["zeta"] = "2";
        metricNodeA.Metrics["alpha"] = "1";
        metricOrderA.Nodes.Add(metricNodeA);
        metricOrderA.Nodes.Add(new TopologyNode { Id = "target", Label = "Target" });
        var metricEdgeA = new TopologyEdge { Id = "edge", SourceNodeId = "metrics", TargetNodeId = "target" };
        metricEdgeA.Metrics["zeta"] = "4";
        metricEdgeA.Metrics["alpha"] = "3";
        metricOrderA.Edges.Add(metricEdgeA);
        var metricOrderB = TopologyChart.Create();
        var metricNodeB = new TopologyNode { Id = "metrics", Label = "Metrics" };
        metricNodeB.Metrics["alpha"] = "1";
        metricNodeB.Metrics["zeta"] = "2";
        metricOrderB.Nodes.Add(metricNodeB);
        metricOrderB.Nodes.Add(new TopologyNode { Id = "target", Label = "Target" });
        var metricEdgeB = new TopologyEdge { Id = "edge", SourceNodeId = "metrics", TargetNodeId = "target" };
        metricEdgeB.Metrics["alpha"] = "3";
        metricEdgeB.Metrics["zeta"] = "4";
        metricOrderB.Edges.Add(metricEdgeB);
        Assert(metricOrderA.ToVisualArtifact().ToInterchangeJson() == metricOrderB.ToVisualArtifact().ToInterchangeJson(),
            "Equivalent metric dictionaries should produce identical interchange JSON regardless of insertion order.");

        VisualArtifactInterchangeEnvelope automaticNamedPort = InterchangeTopology("automatic-port");
        VisualArtifactInterchangeNode automaticPortNode = InterchangeTopologyNode("node", "Node");
        automaticPortNode.Ports.Add(new VisualArtifactInterchangePort { Id = "named", Side = TopologyEdgePort.Auto, Offset = 0.5 });
        automaticNamedPort.Nodes.Add(automaticPortNode);
        AssertThrows<ArgumentException>(() => automaticNamedPort.ToJson(),
            "Named interchange ports should require an explicit boundary side.");

        VisualArtifactInterchangeEnvelope incompleteCoordinates = InterchangeTopology("incomplete-coordinates");
        VisualArtifactInterchangeNode incompleteCoordinateNode = InterchangeTopologyNode("node", "Node");
        incompleteCoordinateNode.Topology!.Longitude = 20;
        incompleteCoordinates.Nodes.Add(incompleteCoordinateNode);
        AssertThrows<ArgumentException>(() => incompleteCoordinates.ToJson(),
            "Topology geographic coordinates should be supplied as a complete longitude/latitude pair.");
        incompleteCoordinateNode.Topology.Latitude = 95;
        AssertThrows<ArgumentOutOfRangeException>(() => incompleteCoordinates.ToJson(),
            "Topology geographic coordinates should remain inside their longitude/latitude domains.");
        VisualArtifactInterchangeEnvelope incompleteGroupCoordinates = InterchangeTopology("incomplete-group-coordinates");
        incompleteGroupCoordinates.Groups.Add(new VisualArtifactInterchangeGroup {
            Id = "region",
            Role = VisualArtifactInterchangeGroupRole.TopologyGroup,
            Kind = "TopologyGroup",
            Label = "Region",
            Topology = new VisualArtifactInterchangeTopologyGroup { Longitude = 20 }
        });
        AssertThrows<ArgumentException>(() => incompleteGroupCoordinates.ToJson(),
            "Topology group geographic coordinates should be supplied as a complete longitude/latitude pair.");

        VisualArtifactInterchangeEnvelope invalidDash = InterchangeTopology("invalid-dash");
        invalidDash.Nodes.Add(InterchangeTopologyNode("source", "Source"));
        invalidDash.Nodes.Add(InterchangeTopologyNode("target", "Target"));
        VisualArtifactInterchangeEdge invalidDashEdge = InterchangeTopologyEdge("edge", "source", "target");
        invalidDashEdge.Topology!.DashPattern.AddRange(new[] { 4D, 2D, 0D });
        invalidDash.Edges.Add(invalidDashEdge);
        AssertThrows<ArgumentException>(() => invalidDash.ToJson(),
            "Topology dash patterns should contain positive alternating dash/gap pairs.");
        invalidDashEdge.Topology.DashPattern.Clear();
        invalidDashEdge.Topology.DashPattern.AddRange(new[] { 4D, 0D });
        AssertThrows<ArgumentOutOfRangeException>(() => invalidDash.ToJson(),
            "Topology dash and gap lengths should be strictly positive.");

        VisualArtifactInterchangeEnvelope pointEnvelope = InterchangeTopology("points");
        pointEnvelope.Nodes.Add(InterchangeTopologyNode("source", "Source"));
        pointEnvelope.Nodes.Add(InterchangeTopologyNode("target", "Target"));
        VisualArtifactInterchangeEdge pointEdge = InterchangeTopologyEdge("edge", "source", "target");
        pointEdge.Topology!.Waypoints.Add(new VisualArtifactInterchangePoint { X = 123.25, Y = 456.5 });
        pointEdge.Topology.LabelAnchor = new VisualArtifactInterchangePoint { X = 789.25, Y = 654.5 };
        pointEnvelope.Edges.Add(pointEdge);
        string pointJson = pointEnvelope.ToJson();
        AssertThrows<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(
                pointJson.Replace("{\"x\":123.25,\"y\":456.5}", "{\"y\":456.5}", StringComparison.Ordinal)),
            "External waypoint objects should require both typed coordinates.");
        AssertThrows<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(
                pointJson.Replace("{\"x\":789.25,\"y\":654.5}", "{\"x\":789.25}", StringComparison.Ordinal)),
            "External label-anchor objects should require both typed coordinates.");

        VisualArtifactInterchangeEnvelope nestedBlock = SequenceArtifact.Create("nested-block")
            .AddParticipant("worker", "Worker")
            .AddBlock(SequenceArtifactBlockKind.Opt, "Nested", 0, 0, depth: 1)
            .ToVisualArtifact()
            .ToInterchangeEnvelope();
        Assert(nestedBlock.Annotations[0].Sequence!.Depth == 1 && VisualArtifactInterchangeEnvelope.FromJson(nestedBlock.ToJson()).Annotations[0].Sequence!.Depth == 1,
            "Sequence block nesting depth should remain an explicit typed interchange semantic.");
    }
}
