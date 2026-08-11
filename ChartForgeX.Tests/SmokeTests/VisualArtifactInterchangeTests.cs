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
        topology.Groups[0].IconId = "microsoft-ad:site";
        topology.Groups[0].Metadata["iconId"] = "untrusted-override";

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
        Assert(roundTrip.Groups.Single().Metadata["iconId"] == "microsoft-ad:site", "Typed group icon ids should override colliding arbitrary metadata.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc1").Ports.Single().Id == "ldap", "Topology interchange should preserve named node ports.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc1").Details.Single().Value == "A", "Topology interchange should preserve typed node details.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc2").Href == null, "Topology interchange should reject unsafe node hyperlinks.");
        Assert(roundTrip.Edges.Single().SourcePortId == "ldap", "Topology interchange should preserve named edge endpoints.");
        Assert(roundTrip.ToJson() == json, "Topology interchange JSON should be deterministic after round trip.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Json), "Topology artifacts should declare their implemented semantic JSON export.");

        var lazyOptions = new TopologyRenderOptions {
            Preset = TopologyViewPreset.Ungrouped,
            LayoutPreset = TopologyLayoutPreset.Presentation
        };
        var explicitOptions = new TopologyRenderOptions();
        explicitOptions.ApplyPreset(TopologyViewPreset.Ungrouped);
        explicitOptions.ApplyLayoutPreset(TopologyLayoutPreset.Presentation);
        var lazyEnvelope = artifact.ToInterchangeEnvelope(new VisualArtifactRenderOptions { Topology = lazyOptions });
        var explicitEnvelope = artifact.ToInterchangeEnvelope(new VisualArtifactRenderOptions { Topology = explicitOptions });
        Assert(lazyEnvelope.Width == explicitEnvelope.Width && lazyEnvelope.Height == explicitEnvelope.Height && lazyEnvelope.Groups.Count == explicitEnvelope.Groups.Count,
            "Interchange topology preparation should apply pending view and layout presets exactly like rendering.");

        artifact.NaturalSize = new VisualArtifactSize(1777, 1333);
        artifact.PreserveNaturalSize = true;
        var naturalEnvelope = artifact.ToInterchangeEnvelope();
        Assert(naturalEnvelope.Width == 1777 && naturalEnvelope.Height == 1333,
            $"Topology interchange should preserve an artifact's explicit natural viewport exactly like static rendering (actual {naturalEnvelope.Width}x{naturalEnvelope.Height}).");

        var collidingTopology = TopologyChart.Create();
        collidingTopology.LayoutMode = TopologyLayoutMode.Manual;
        collidingTopology.Groups.Add(new TopologyGroup { Id = "shared", Label = "Group", X = 0, Y = 0, Width = 400, Height = 200 });
        collidingTopology.Nodes.Add(new TopologyNode { Id = "shared", Label = "Source", GroupId = "shared", X = 30, Y = 70, Width = 100, Height = 50 });
        collidingTopology.Nodes.Add(new TopologyNode { Id = "target", Label = "Target", GroupId = "shared", X = 220, Y = 70, Width = 100, Height = 50 });
        collidingTopology.Edges.Add(new TopologyEdge { Id = "shared", SourceNodeId = "shared", TargetNodeId = "target" });
        var collidingTopologyEnvelope = collidingTopology.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(collidingTopologyEnvelope.Groups.Single().Id == "shared", "The first topology entity should keep its stable source id.");
        Assert(collidingTopologyEnvelope.Nodes.Single(node => node.Label == "Source").Id != "shared" && collidingTopologyEnvelope.Edges.Single().Id != "shared",
            "Topology cross-category ids should be deterministically namespaced into the interchange envelope.");
        Assert(collidingTopologyEnvelope.Edges.Single().SourceId == collidingTopologyEnvelope.Nodes.Single(node => node.Label == "Source").Id,
            "Topology remapping should update edge references to the namespaced node id.");

        var highCardinality = new VisualArtifactInterchangeEnvelope { Id = "high-cardinality", Kind = VisualArtifactKind.Topology };
        highCardinality.Nodes.Add(new VisualArtifactInterchangeNode { Id = "source", Label = "Source" });
        highCardinality.Nodes.Add(new VisualArtifactInterchangeNode { Id = "target", Label = "Target" });
        for (var index = 0; index < 72000; index++) {
            highCardinality.Edges.Add(new VisualArtifactInterchangeEdge {
                Id = "e" + index,
                SourceId = "source",
                TargetId = "target"
            });
        }
        string highCardinalityJson = highCardinality.ToJson();
        var highCardinalityRoundTrip = VisualArtifactInterchangeEnvelope.FromJson(highCardinalityJson);
        Assert(highCardinalityRoundTrip.Edges.Count == highCardinality.Edges.Count,
            "Every envelope accepted by serialization limits should remain parseable at the same edge cardinality.");
    }

    private static void VisualArtifactInterchangePreservesFlowAndSequenceSemantics() {
        var flow = FlowArtifact.Create("approval")
            .WithTitle("Approval")
            .AddLane("requester", "Requester", VisualStatus.Positive)
            .AddStep("submit", "Submit", FlowArtifactStepKind.Start, "requester")
            .AddStep("approve", "Approve?", FlowArtifactStepKind.Decision, "requester")
            .AddConnector("submit", "approve", "Review", FlowArtifactConnectorKind.Flow);
        flow.WithStep("submit", step => step.Metadata["owner"] = "requester");
        flow.Metadata["scope"] = "model";
        flow.Metadata["model-only"] = "preserved";
        var flowArtifact = flow.ToVisualArtifact();
        flowArtifact.Metadata["scope"] = "artifact";
        var flowRoundTrip = VisualArtifactInterchangeEnvelope.FromJson(flowArtifact.ToInterchangeJson());
        TopologyChart preparedFlow = TopologyLayoutEngine.Prepare(flow.ToTopologyChart(), options: new TopologyRenderOptions { IncludeLegend = false });
        Assert(flowRoundTrip.Groups.Single().Kind == "FlowLane", "Flow interchange should preserve lane semantics.");
        Assert(flowRoundTrip.Nodes.Single(node => node.Id == "approve").Kind == "Decision", "Flow interchange should preserve step kinds.");
        Assert(flowRoundTrip.Nodes.Single(node => node.Id == "submit").Metadata["owner"] == "requester", "Flow interchange should preserve step metadata.");
        Assert(flowRoundTrip.Edges.Single().Label == "Review", "Flow interchange should preserve connector labels.");
        Assert(flowRoundTrip.Metadata["scope"] == "artifact" && flowRoundTrip.Metadata["model-only"] == "preserved",
            "Explicit artifact metadata should override same-key flow metadata while retaining model-only values.");
        Assert(flowRoundTrip.Width == preparedFlow.Viewport.Width && flowRoundTrip.Height == preparedFlow.Viewport.Height,
            "Flow interchange should publish the prepared topology viewport instead of its unprepared configured canvas.");

        var collidingFlow = FlowArtifact.Create("colliding-flow")
            .AddLane("shared", "Lane")
            .AddStep("shared", "Step", laneId: "shared")
            .AddStep("target", "Target", laneId: "shared")
            .AddConnector("shared", "target");
        var collidingFlowEnvelope = collidingFlow.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(collidingFlowEnvelope.Groups.Single().Id == "shared", "The first source entity should keep its stable id.");
        Assert(collidingFlowEnvelope.Nodes.Single(node => node.Label == "Step").Id != "shared", "Cross-category source id collisions should be deterministically namespaced.");
        Assert(collidingFlowEnvelope.Nodes.Single(node => node.Label == "Step").GroupId == "shared", "Remapped nodes should retain their group reference.");
        Assert(collidingFlowEnvelope.Edges.Single().SourceId == collidingFlowEnvelope.Nodes.Single(node => node.Label == "Step").Id,
            "Remapped edges should retain their node references.");

        var sequence = SequenceArtifact.Create("authentication")
            .WithTitle("Authentication")
            .AddParticipant("user", "User", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("api", "API", SequenceArtifactParticipantKind.Control)
            .AddMessage("user", "api", "Sign in")
            .AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "api" }, "Validate token")
            .AddBlock(SequenceArtifactBlockKind.Opt, "MFA", 0, 0);
        sequence.Messages[0].ActivatesTarget = true;
        sequence.Notes[0].StepIndex = -2;
        sequence.Participants[0].Metadata["sequence.order"] = "99";
        sequence.Participants[0].Metadata["sequence.implicit"] = "true";
        sequence.Messages[0].Metadata["sequence.activatesTarget"] = "false";
        sequence.Metadata["scope"] = "model";
        sequence.Metadata["model-only"] = "preserved";
        var sequenceArtifact = sequence.ToVisualArtifact();
        sequenceArtifact.Metadata["scope"] = "artifact";
        var sequenceRoundTrip = VisualArtifactInterchangeEnvelope.FromJson(sequenceArtifact.ToInterchangeJson());
        Assert(sequenceRoundTrip.Nodes.Single(node => node.Id == "user").Kind == "Actor", "Sequence interchange should preserve participant kinds.");
        Assert(sequenceRoundTrip.Nodes.Single(node => node.Id == "user").Metadata["sequence.order"] == "0", "Typed participant order should override colliding arbitrary metadata.");
        Assert(sequenceRoundTrip.Nodes.Single(node => node.Id == "user").Metadata["sequence.implicit"] == "false", "Typed participant state should override colliding arbitrary metadata.");
        Assert(sequenceRoundTrip.Edges.Single().Metadata["sequence.activatesTarget"] == "true", "Sequence interchange should preserve activation semantics.");
        Assert(sequenceRoundTrip.Metadata["scope"] == "artifact" && sequenceRoundTrip.Metadata["model-only"] == "preserved",
            "Explicit artifact metadata should override same-key sequence metadata while retaining model-only values.");
        Assert(sequenceRoundTrip.Annotations.Any(annotation => annotation.Kind == "SequenceNote" && annotation.TargetIds.Single() == "api"), "Sequence interchange should preserve participant notes.");
        Assert(sequenceRoundTrip.Annotations.Single(annotation => annotation.Kind == "SequenceNote").StartIndex == 0,
            "Sequence note rows should use the renderer's non-negative normalization.");
        Assert(sequenceRoundTrip.Annotations.Any(annotation => annotation.Kind == "SequenceBlock:Opt"), "Sequence interchange should preserve block spans.");
        Assert(sequence.ToVisualArtifact().SupportsExport(VisualArtifactExportFormat.Json), "Sequence artifacts should declare their implemented semantic JSON export.");

        var collidingSequence = SequenceArtifact.Create("colliding-sequence")
            .AddParticipant("message-1", "Caller")
            .AddParticipant("target", "Target")
            .AddMessage("message-1", "target", "Call")
            .AddBlock(SequenceArtifactBlockKind.Opt, "normalized", -2, -5);
        var collidingSequenceEnvelope = collidingSequence.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(collidingSequenceEnvelope.Edges.Single().Id != "message-1", "Generated sequence ids should not collide with participant ids.");
        Assert(collidingSequenceEnvelope.Edges.Single().SourceId == "message-1", "Sequence message references should retain participant ids.");
        Assert(collidingSequenceEnvelope.Annotations.Single().StartIndex == 0 && collidingSequenceEnvelope.Annotations.Single().EndIndex == 0,
            "Sequence block spans should use the renderer's non-negative ordered normalization.");

        var wideSequence = SequenceArtifact.Create("wide").WithSize(320, 240);
        for (var index = 0; index < 10; index++) wideSequence.AddParticipant("participant-" + index, "Participant " + index);
        var wideArtifact = wideSequence.ToVisualArtifact();
        var wideEnvelope = wideArtifact.ToInterchangeEnvelope();
        VisualArtifactSize naturalSize = wideArtifact.NaturalSize.GetValueOrDefault();
        Assert(wideArtifact.NaturalSize.HasValue && naturalSize.Width > wideSequence.Width,
            "The sequence fixture should calculate a wider natural layout than its configured minimum.");
        Assert(wideEnvelope.Width == naturalSize.Width && wideEnvelope.Height == naturalSize.Height,
            "Sequence interchange should preserve calculated natural dimensions instead of overwriting them with configured minimums.");
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
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":01,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\"}"),
            "Interchange parsing should reject JSON numbers with leading zeros.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\u00A0\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\"}"),
            "Interchange parsing should reject non-JSON whitespace outside strings.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"0\",\"sourceLanguage\":\"Native\",\"id\":\"x\"}"),
            "Interchange parsing should accept only declared enum names, not numeric enum tokens.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\",\"id\":\"y\"}"),
            "Interchange parsing should reject exact duplicate properties instead of applying last-wins semantics.");
        string excessivePorts = string.Join(",", Enumerable.Repeat("{\"id\":\"p\",\"side\":\"Left\",\"offset\":0}", 257));
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\",\"nodes\":[{\"id\":\"n\",\"ports\":[" + excessivePorts + "]}]}"),
            "Interchange parsing should enforce schema collection limits while constructing the JSON value tree.");
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

        var excessiveAnnotationTargets = new VisualArtifactInterchangeEnvelope { Id = "annotation-targets", Kind = VisualArtifactKind.Topology };
        excessiveAnnotationTargets.Nodes.Add(new VisualArtifactInterchangeNode { Id = "node", Label = "Node" });
        var excessiveTargets = new VisualArtifactInterchangeAnnotation { Id = "annotation", Text = "Targets" };
        for (var index = 0; index < 50001; index++) excessiveTargets.TargetIds.Add("node");
        excessiveAnnotationTargets.Annotations.Add(excessiveTargets);
        AssertThrows<ArgumentOutOfRangeException>(() => excessiveAnnotationTargets.ToJson(),
            "In-memory annotation target limits should match parser limits so serialized envelopes remain parseable.");

        AssertThrows<ArgumentOutOfRangeException>(
            () => VisualArtifactInterchangeEnvelope.FromUtf8Json(new byte[VisualArtifactInterchangeEnvelope.MaximumJsonUtf8Bytes + 1]),
            "Interchange UTF-8 byte limits should be enforced before decoding the payload.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\",\"title\":\"line\nbreak\"}"),
            "Interchange parsing should reject unescaped control characters inside JSON strings.");

        var unpairedSurrogate = new VisualArtifactInterchangeEnvelope { Id = "surrogate", Kind = VisualArtifactKind.Topology, Title = "bad\uD800value" };
        AssertThrows<ArgumentException>(() => unpairedSurrogate.ToUtf8Json(),
            "Interchange UTF-8 export should reject unpaired UTF-16 surrogate characters instead of replacing them.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\",\"title\":\"bad\uD800value\"}"),
            "Interchange parsing should reject raw unpaired UTF-16 surrogate characters.");
        AssertThrows<ArgumentException>(
            () => VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\",\"title\":\"bad\\uD800value\"}"),
            "Interchange parsing should reject escaped unpaired UTF-16 surrogate characters.");
        var rawPair = VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"raw-pair\",\"title\":\"\uD83D\uDE00\"}");
        var escapedPair = VisualArtifactInterchangeEnvelope.FromJson("{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"escaped-pair\",\"title\":\"\\uD83D\\uDE00\"}");
        Assert(rawPair.Title == escapedPair.Title && rawPair.ToUtf8Json().Length > 0,
            "Interchange parsing should preserve valid raw and escaped UTF-16 surrogate pairs.");

        var oversized = new VisualArtifactInterchangeEnvelope { Id = "oversized", Kind = VisualArtifactKind.Topology };
        string largeLabel = new string('x', 65536);
        for (var index = 0; index < 130; index++) {
            oversized.Nodes.Add(new VisualArtifactInterchangeNode { Id = "node-" + index, Label = largeLabel });
        }
        AssertThrows<InvalidOperationException>(() => oversized.ToJson(),
            "Interchange serialization should enforce the JSON character limit while the output buffer is being written.");

        var unknownKind = new VisualArtifactInterchangeEnvelope { Id = "unknown-kind", Kind = (VisualArtifactKind)999 };
        AssertThrows<ArgumentOutOfRangeException>(() => unknownKind.ToJson(), "Interchange serialization should reject undefined artifact kinds.");
        var unknownLanguage = new VisualArtifactInterchangeEnvelope { Id = "unknown-language", Kind = VisualArtifactKind.Topology, SourceLanguage = (VisualArtifactSourceLanguage)999 };
        AssertThrows<ArgumentOutOfRangeException>(() => unknownLanguage.ToJson(), "Interchange serialization should reject undefined source languages.");
    }
}
