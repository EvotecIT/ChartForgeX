using System;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void VisualArtifactInterchangeRoundTripsTopologySemantics() {
        var topology = TopologyChart.Create();
        topology.Id = "directory";
        topology.Title = "Directory topology";
        topology.LayoutMode = TopologyLayoutMode.Manual;
        topology.Groups.Add(new TopologyGroup { Id = "site", Label = "Site A", X = 20, Y = 30, Width = 420, Height = 220, Href = "https://example.test/site" });
        var source = new TopologyNode { Id = "dc1", Label = "DC 1", Kind = TopologyNodeKind.Server, GroupId = "site", X = 80, Y = 100, Width = 120, Height = 64, Href = "https://example.test/dc1" };
        source.Metadata["role"] = "primary";
        source.Ports.Add(new TopologyNodePort { Id = "ldap", Side = TopologyEdgePort.Right, Offset = 0.35, Label = "LDAP" });
        source.Details.Add(new TopologyNodeDetail { Label = "Site", Value = "A", Status = TopologyHealthStatus.Healthy });
        topology.Nodes.Add(source);
        topology.Nodes.Add(new TopologyNode { Id = "dc2", Label = "DC 2", Kind = TopologyNodeKind.Server, GroupId = "site", X = 280, Y = 100, Width = 120, Height = 64, Href = "javascript:alert(1)" });
        topology.Edges.Add(new TopologyEdge {
            Id = "replication", SourceNodeId = "dc1", TargetNodeId = "dc2", Kind = TopologyEdgeKind.Replication,
            Direction = VisualLinkDirection.Bidirectional, SourcePortId = "ldap", Label = "AD replication", Href = "https://example.test/link"
        });

        var artifact = topology.ToVisualArtifact();
        artifact.Metadata["owner"] = "directory-team";
        artifact.Metadata["Owner"] = "Platform";
        artifact.Accessibility.WithTextAlternative("Directory topology", "Two domain controllers replicate in Site A.", "en-US");
        var envelope = artifact.ToInterchangeEnvelope();
        string json = envelope.ToJson();
        var roundTrip = VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json());

        Assert(roundTrip.Kind == VisualArtifactKind.Topology, "Topology interchange should preserve artifact kind.");
        Assert(roundTrip.Metadata["owner"] == "directory-team", "Topology interchange should preserve artifact metadata.");
        Assert(roundTrip.Metadata["Owner"] == "Platform", "Topology interchange should preserve case-distinct metadata keys without data loss.");
        Assert(roundTrip.AccessibleDescription == "Two domain controllers replicate in Site A.", "Topology interchange should preserve accessibility metadata.");
        Assert(roundTrip.Groups.Single().Href == "https://example.test/site", "Topology interchange should preserve safe group hyperlinks.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc1").Ports.Single().Id == "ldap", "Topology interchange should preserve named node ports.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc1").Details.Single().Value == "A", "Topology interchange should preserve typed node details.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc2").Href == null, "Topology interchange should reject unsafe node hyperlinks.");
        Assert(roundTrip.Edges.Single().SourcePortId == "ldap", "Topology interchange should preserve named edge endpoints.");
        Assert(roundTrip.ToJson() == json, "Topology interchange JSON should be deterministic after round trip.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Json), "Topology artifacts should declare their implemented semantic JSON export.");
    }

    private static void VisualArtifactInterchangePreservesFlowAndSequenceSemantics() {
        var flow = FlowArtifact.Create("approval")
            .WithTitle("Approval")
            .AddLane("requester", "Requester", VisualStatus.Positive)
            .AddStep("submit", "Submit", FlowArtifactStepKind.Start, "requester")
            .AddStep("approve", "Approve?", FlowArtifactStepKind.Decision, "requester")
            .AddConnector("submit", "approve", "Review", FlowArtifactConnectorKind.Flow);
        flow.WithStep("submit", step => step.Metadata["owner"] = "requester");
        var flowRoundTrip = VisualArtifactInterchangeEnvelope.FromJson(flow.ToVisualArtifact().ToInterchangeJson());
        Assert(flowRoundTrip.Groups.Single().Kind == "FlowLane", "Flow interchange should preserve lane semantics.");
        Assert(flowRoundTrip.Nodes.Single(node => node.Id == "approve").Kind == "Decision", "Flow interchange should preserve step kinds.");
        Assert(flowRoundTrip.Nodes.Single(node => node.Id == "submit").Metadata["owner"] == "requester", "Flow interchange should preserve step metadata.");
        Assert(flowRoundTrip.Edges.Single().Label == "Review", "Flow interchange should preserve connector labels.");

        var sequence = SequenceArtifact.Create("authentication")
            .WithTitle("Authentication")
            .AddParticipant("user", "User", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("api", "API", SequenceArtifactParticipantKind.Control)
            .AddMessage("user", "api", "Sign in")
            .AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "api" }, "Validate token")
            .AddBlock(SequenceArtifactBlockKind.Opt, "MFA", 0, 0);
        sequence.Messages[0].ActivatesTarget = true;
        sequence.Participants[0].Metadata["sequence.order"] = "99";
        sequence.Participants[0].Metadata["sequence.implicit"] = "true";
        sequence.Messages[0].Metadata["sequence.activatesTarget"] = "false";
        var sequenceRoundTrip = VisualArtifactInterchangeEnvelope.FromJson(sequence.ToVisualArtifact().ToInterchangeJson());
        Assert(sequenceRoundTrip.Nodes.Single(node => node.Id == "user").Kind == "Actor", "Sequence interchange should preserve participant kinds.");
        Assert(sequenceRoundTrip.Nodes.Single(node => node.Id == "user").Metadata["sequence.order"] == "0", "Typed participant order should override colliding arbitrary metadata.");
        Assert(sequenceRoundTrip.Nodes.Single(node => node.Id == "user").Metadata["sequence.implicit"] == "false", "Typed participant state should override colliding arbitrary metadata.");
        Assert(sequenceRoundTrip.Edges.Single().Metadata["sequence.activatesTarget"] == "true", "Sequence interchange should preserve activation semantics.");
        Assert(sequenceRoundTrip.Annotations.Any(annotation => annotation.Kind == "SequenceNote" && annotation.TargetIds.Single() == "api"), "Sequence interchange should preserve participant notes.");
        Assert(sequenceRoundTrip.Annotations.Any(annotation => annotation.Kind == "SequenceBlock:Opt"), "Sequence interchange should preserve block spans.");
        Assert(sequence.ToVisualArtifact().SupportsExport(VisualArtifactExportFormat.Json), "Sequence artifacts should declare their implemented semantic JSON export.");
    }

    private static void VisualArtifactInterchangeRejectsInvalidContracts() {
        AssertThrows<NotSupportedException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":2,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\"}"),
            "Interchange parsing should reject unsupported schema versions.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":7}"),
            "Interchange parsing should reject non-string contract properties instead of coercing them.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"ID\":\"x\"}"),
            "Interchange parsing should keep schema property names case-sensitive.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\",\"decorative\":\"false\"}"),
            "Interchange parsing should reject string values where the schema requires booleans.");
        string deeplyNested = new string('[', 33) + new string(']', 33);
        AssertThrows<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(deeplyNested), "Interchange parsing should reject excessive JSON nesting before recursive parsing.");

        var invalid = new VisualArtifactInterchangeEnvelope { Id = "invalid", Kind = VisualArtifactKind.Topology };
        invalid.Nodes.Add(new VisualArtifactInterchangeNode { Id = "a", Label = "A" });
        invalid.Edges.Add(new VisualArtifactInterchangeEdge { Id = "missing", SourceId = "a", TargetId = "b" });
        AssertThrows<ArgumentException>(() => invalid.ToJson(), "Interchange serialization should reject edges that reference missing nodes.");

        var unsafeLink = new VisualArtifactInterchangeEnvelope { Id = "unsafe", Kind = VisualArtifactKind.Topology };
        unsafeLink.Nodes.Add(new VisualArtifactInterchangeNode { Id = "node", Label = "Node", Href = "file:///secret.txt" });
        AssertThrows<ArgumentException>(() => unsafeLink.ToJson(), "Interchange validation should reject unsafe hyperlink schemes from external envelopes.");

        var collidingIds = new VisualArtifactInterchangeEnvelope { Id = "collision", Kind = VisualArtifactKind.Topology };
        collidingIds.Nodes.Add(new VisualArtifactInterchangeNode { Id = "item", Label = "Item" });
        collidingIds.Edges.Add(new VisualArtifactInterchangeEdge { Id = "item", SourceId = "item", TargetId = "item" });
        AssertThrows<ArgumentException>(() => collidingIds.ToJson(), "Interchange entity ids should share one diagram-wide namespace.");

        var missingPort = new VisualArtifactInterchangeEnvelope { Id = "ports", Kind = VisualArtifactKind.Topology };
        missingPort.Nodes.Add(new VisualArtifactInterchangeNode { Id = "source", Label = "Source" });
        missingPort.Nodes.Add(new VisualArtifactInterchangeNode { Id = "target", Label = "Target" });
        missingPort.Edges.Add(new VisualArtifactInterchangeEdge { Id = "edge", SourceId = "source", TargetId = "target", SourcePortId = "missing" });
        AssertThrows<ArgumentException>(() => missingPort.ToJson(), "Interchange validation should reject named ports that do not exist on their endpoint node.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => VisualArtifactInterchangeEnvelope.FromUtf8Json(new byte[VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes + 1]),
            "Interchange UTF-8 byte limits should be enforced before decoding the payload.");
    }
}
