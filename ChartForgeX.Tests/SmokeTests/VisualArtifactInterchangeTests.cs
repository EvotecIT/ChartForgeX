using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
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
        topology.WithNodeDisplay("dc1", TopologyNodeDisplayMode.Pill);
        topology.Edges.Add(new TopologyEdge {
            Id = "replication", SourceNodeId = "dc1", TargetNodeId = "dc2", Kind = TopologyEdgeKind.Replication,
            Direction = VisualLinkDirection.Bidirectional, SourcePortId = "ldap", Label = "AD replication", Href = "https://example.test/link",
            SourceMarker = TopologyMarkerKind.Circle, TargetMarker = TopologyMarkerKind.Diamond, Routing = TopologyEdgeRouting.Curved,
            Emphasis = TopologyEdgeEmphasis.Strong, IsMuted = true
        });
        topology.Edges[0].Metadata["topology.sourceMarker"] = "untrusted-override";
        topology.Edges[0].Metadata["topology.routing"] = "untrusted-override";
        topology.WithEdgeStroke("replication", 2.5, 0.4, 6, 2);
        topology.WithEdgeWaypoints("replication", new ChartPoint(180, 120), new ChartPoint(220, 120));
        var scenario = new TopologyScenario {
            Id = "replication-review", Label = "Replication review", Description = "Inspect the primary route", Color = "#336699",
            PlaybackDelayMilliseconds = 1200, LoopPlayback = true, Spotlight = true
        };
        scenario.Metadata["owner"] = "directory-team";
        scenario.Steps.Add(new TopologyScenarioStep { Id = "dc1", Kind = TopologyScenarioStepKind.Node, Label = "Start", DurationMilliseconds = 800 });
        var edgeStep = new TopologyScenarioStep { Id = "replication", Kind = TopologyScenarioStepKind.Edge, Description = "Follow replication" };
        edgeStep.Metadata["phase"] = "transport";
        scenario.Steps.Add(edgeStep);
        topology.Scenarios.Add(scenario);
        topology.Groups[0].IconId = "microsoft-ad:site";
        topology.Groups[0].Metadata["iconId"] = "untrusted-override";

        var artifact = topology.ToVisualArtifact();
        artifact.Metadata["owner"] = "directory-team";
        artifact.Metadata["Owner"] = "Platform";
        artifact.Accessibility.WithTextAlternative("Directory topology", "Two domain controllers replicate in Site A.", "en-US");
        var envelope = artifact.ToInterchangeEnvelope();
        string json = envelope.ToJson();
        var roundTrip = VisualArtifactInterchangeEnvelope.FromUtf8Json(envelope.ToUtf8Json());
        TopologyEdge preparedEdge = TopologyLayoutEngine.Prepare(topology, options: new TopologyRenderOptions()).Edges.Single();
        string expectedWaypoints = string.Join(";", preparedEdge.Waypoints.Select(point =>
            point.X.ToString("R", CultureInfo.InvariantCulture) + "," + point.Y.ToString("R", CultureInfo.InvariantCulture)));

        Assert(roundTrip.Kind == VisualArtifactKind.Topology, "Topology interchange should preserve artifact kind.");
        Assert(roundTrip.Metadata["owner"] == "directory-team", "Topology interchange should preserve artifact metadata.");
        Assert(roundTrip.Metadata["Owner"] == "Platform", "Topology interchange should preserve case-distinct metadata keys without data loss.");
        Assert(roundTrip.AccessibleDescription == "Two domain controllers replicate in Site A.", "Topology interchange should preserve accessibility metadata.");
        Assert(roundTrip.Groups.Single().Href == "https://example.test/site", "Topology interchange should preserve safe group hyperlinks.");
        Assert(roundTrip.Groups.Single().Metadata["iconId"] == "microsoft-ad:site", "Typed group icon ids should override colliding arbitrary metadata.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc1").Ports.Single().Id == "ldap", "Topology interchange should preserve named node ports.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc1").Details.Single().Value == "A", "Topology interchange should preserve typed node details.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc2").Href == null, "Topology interchange should reject unsafe node hyperlinks.");
        Assert(roundTrip.Nodes.Single(node => node.Id == "dc1").Metadata["topology.displayMode"] == "Pill",
            "Topology interchange should preserve the prepared node display mode selected by the model or render options.");
        Assert(roundTrip.Edges.Single().SourcePortId == "ldap", "Topology interchange should preserve named edge endpoints.");
        Assert(roundTrip.Edges.Single().Metadata["topology.sourceMarker"] == "Circle" && roundTrip.Edges.Single().Metadata["topology.targetMarker"] == "Diamond",
            "Topology interchange should preserve explicit endpoint marker semantics in reserved edge metadata.");
        Assert(roundTrip.Edges.Single().Metadata["topology.routing"] == "Curved" && roundTrip.Edges.Single().Metadata["topology.waypoints"] == expectedWaypoints,
            "Topology interchange should preserve explicit route mode and deterministic waypoint coordinates.");
        Assert(roundTrip.Edges.Single().Metadata["topology.strokeWidth"] == "2.5" && roundTrip.Edges.Single().Metadata["topology.opacity"] == "0.40000000000000002" &&
               roundTrip.Edges.Single().Metadata["topology.dashPattern"] == "6,2" && roundTrip.Edges.Single().Metadata["topology.emphasis"] == "Strong" &&
               roundTrip.Edges.Single().Metadata["topology.muted"] == bool.TrueString,
            "Topology interchange should preserve explicit stroke and presentation semantics.");
        Assert(roundTrip.Scenarios.Single().Label == "Replication review" && roundTrip.Scenarios.Single().Steps.Select(step => step.TargetId).SequenceEqual(new[] { "dc1", "replication" }) &&
               roundTrip.Scenarios.Single().Steps[1].Metadata["phase"] == "transport" && roundTrip.Scenarios.Single().Spotlight,
            "Topology interchange should preserve scenario identity, playback policy, ordered remapped targets, and step metadata.");
        Assert(roundTrip.ToJson() == json, "Topology interchange JSON should be deterministic after round trip.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Json), "Topology artifacts should declare their implemented semantic JSON export.");

        var mutableAccessibilityTopology = TopologyChart.Create()
            .WithAccessibility(accessibility => accessibility.WithTextAlternative("Initial name", "Initial description", "en"));
        mutableAccessibilityTopology.Nodes.Add(new TopologyNode { Id = "current", Label = "Current" });
        VisualArtifact mutableAccessibilityArtifact = mutableAccessibilityTopology.ToVisualArtifact();
        mutableAccessibilityTopology.Accessibility.WithTextAlternative("Current name", "Current description", "pl-PL");
        mutableAccessibilityTopology.Accessibility.IsDecorative = true;
        VisualArtifactInterchangeEnvelope currentAccessibility = mutableAccessibilityArtifact.ToInterchangeEnvelope();
        Assert(currentAccessibility.AccessibleName == "Current name" && currentAccessibility.AccessibleDescription == "Current description" &&
               currentAccessibility.Language == "pl-PL" && currentAccessibility.IsDecorative,
            "Topology interchange should refresh accessibility from the current model after wrapper creation.");
        mutableAccessibilityArtifact.Accessibility.Name = "Artifact override";
        Assert(mutableAccessibilityArtifact.ToInterchangeEnvelope().AccessibleName == "Artifact override",
            "Topology interchange should retain an intentional artifact-level accessibility override.");

        var geographicTopology = TopologyChart.Create();
        geographicTopology.LayoutMode = TopologyLayoutMode.Geographic;
        geographicTopology.MapViewport = ChartMapViewport.Europe();
        geographicTopology.Nodes.Add(new TopologyNode { Id = "warsaw", Label = "Warsaw", Longitude = 21.0122, Latitude = 52.2297 });
        VisualArtifactInterchangeEnvelope geographicEnvelope = geographicTopology.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(geographicEnvelope.Metadata["topology.mapViewport.name"] == "Europe" && geographicEnvelope.Metadata["topology.mapViewport.projection"] == "Equirectangular" &&
               geographicEnvelope.Metadata["topology.mapViewport.minimumLongitude"] == "-11" && geographicEnvelope.Metadata["topology.mapViewport.maximumLatitude"] == "72",
            "Geographic topology interchange should preserve the prepared map viewport identity, projection, and bounds.");

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

        topology.Groups[0].Status = TopologyHealthStatus.Warning;
        var groupFilteredEnvelope = artifact.ToInterchangeEnvelope(new VisualArtifactRenderOptions {
            Topology = new TopologyRenderOptions {
                View = new TopologyView {
                    Id = "focused",
                    Title = "Focused directory",
                    Subtitle = "Healthy nodes",
                    IncludeNodeGroups = false,
                    HealthStatuses = { TopologyHealthStatus.Unknown }
                }
            }
        });
        Assert(groupFilteredEnvelope.Id == "directory-focused" && groupFilteredEnvelope.Title == "Focused directory" && groupFilteredEnvelope.Subtitle == "Healthy nodes",
            "Interchange topology views should expose the prepared view identity used by static rendering.");
        Assert(groupFilteredEnvelope.Groups.Count == 0 && groupFilteredEnvelope.Nodes.Count == 2,
            "Interchange topology views should retain eligible nodes when their groups are omitted.");
        Assert(groupFilteredEnvelope.Nodes.All(node => node.GroupId == null),
            "Interchange topology views should detach references to groups omitted during preparation.");
        Assert(groupFilteredEnvelope.Metadata["topology.nodes"] == "2" && groupFilteredEnvelope.Metadata["topology.edges"] == "1",
            "Interchange topology view metadata should describe the prepared model instead of stale wrapper counts.");

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

        string maximumId = new string('x', 512);
        var maximumIdTopology = TopologyChart.Create();
        maximumIdTopology.LayoutMode = TopologyLayoutMode.Manual;
        maximumIdTopology.Groups.Add(new TopologyGroup { Id = maximumId, Label = "Group", X = 0, Y = 0, Width = 400, Height = 200 });
        var maximumIdSource = new TopologyNode { Id = maximumId, Label = "Source", GroupId = maximumId, X = 30, Y = 70, Width = 100, Height = 50 };
        string longPortId = new string('p', 600);
        string longMetadataKey = new string('m', 600);
        maximumIdSource.Ports.Add(new TopologyNodePort { Id = longPortId, Side = TopologyEdgePort.Right, Offset = 0.5 });
        maximumIdSource.Metadata[longMetadataKey] = "bounded";
        maximumIdSource.Metrics[maximumId] = "1";
        maximumIdTopology.Nodes.Add(maximumIdSource);
        maximumIdTopology.Nodes.Add(new TopologyNode { Id = "target", Label = "Target", GroupId = maximumId, X = 220, Y = 70, Width = 100, Height = 50 });
        maximumIdTopology.Edges.Add(new TopologyEdge { Id = "edge", SourceNodeId = maximumId, TargetNodeId = "target", SourcePortId = longPortId });
        var maximumIdEnvelope = maximumIdTopology.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(maximumIdEnvelope.Nodes.Single(node => node.Label == "Source").Id.Length <= 512,
            "Generated collision ids should remain within the interchange schema limit.");
        Assert(maximumIdEnvelope.Edges.Single().SourceId == maximumIdEnvelope.Nodes.Single(node => node.Label == "Source").Id && maximumIdEnvelope.ToJson().Length > 0,
            "Bounded collision ids should preserve references and produce a valid interchange payload.");
        Assert(maximumIdEnvelope.Nodes.Single(node => node.Label == "Source").Metadata.Single(pair => pair.Value == "1").Key.Length <= 512,
            "Generated prefixed metadata keys should remain within the interchange schema limit.");
        Assert(maximumIdEnvelope.Nodes.Single(node => node.Label == "Source").Metadata.Single(pair => pair.Value == "bounded").Key.Length <= 512,
            "Direct source metadata keys should remain within the interchange schema limit.");
        Assert(maximumIdEnvelope.Nodes.Single(node => node.Label == "Source").Ports.Single().Id.Length <= 512 &&
               maximumIdEnvelope.Edges.Single().SourcePortId == maximumIdEnvelope.Nodes.Single(node => node.Label == "Source").Ports.Single().Id,
            "Bounded topology port ids should preserve named edge references.");
        var mappedMaximumSource = maximumIdEnvelope.Nodes.Single(node => node.Label == "Source");
        string boundedDirectMetadataKey = mappedMaximumSource.Metadata.Single(pair => pair.Value == "bounded").Key;
        string boundedMetricMetadataKey = mappedMaximumSource.Metadata.Single(pair => pair.Value == "1").Key;
        maximumIdSource.Metadata[boundedDirectMetadataKey] = "direct-collision";
        maximumIdSource.Metadata[boundedMetricMetadataKey] = "metric-collision";
        var collidingMetadata = maximumIdTopology.ToVisualArtifact().ToInterchangeEnvelope().Nodes.Single(node => node.Label == "Source").Metadata;
        Assert(collidingMetadata[boundedDirectMetadataKey] == "direct-collision" && collidingMetadata.Values.Contains("bounded"),
            "Bounding should preserve a valid direct metadata key that collides with an over-limit key and retain both values.");
        Assert(collidingMetadata[boundedMetricMetadataKey] == "metric-collision" && collidingMetadata.Values.Contains("1"),
            "Bounding should preserve a valid metadata key that collides with a prefixed metric key and retain both values.");
        string prefixedCollisionSourceKey = "metric." + new string('c', 600);
        maximumIdSource.Metadata[prefixedCollisionSourceKey] = "metadata-over-limit";
        string prefixedCollisionBoundedKey = maximumIdTopology.ToVisualArtifact().ToInterchangeEnvelope().Nodes
            .Single(node => node.Label == "Source").Metadata.Single(pair => pair.Value == "metadata-over-limit").Key;
        maximumIdSource.Metrics[prefixedCollisionBoundedKey.Substring("metric.".Length)] = "metric-valid";
        maximumIdSource.Metadata["metric.exact"] = "metadata-exact";
        maximumIdSource.Metrics["exact"] = "metric-exact";
        var prefixedCollisionMetadata = maximumIdTopology.ToVisualArtifact().ToInterchangeEnvelope().Nodes.Single(node => node.Label == "Source").Metadata;
        Assert(prefixedCollisionMetadata[prefixedCollisionBoundedKey] == "metadata-over-limit" && prefixedCollisionMetadata.Values.Contains("metric-valid"),
            "Prefixed metrics should not overwrite a different over-limit metadata key that projects to the same bounded key.");
        Assert(prefixedCollisionMetadata["metric.exact"] == "metric-exact" && !prefixedCollisionMetadata.Values.Contains("metadata-exact"),
            "Typed metrics should retain precedence when their logical prefixed key exactly matches direct metadata.");
        maximumIdTopology.Id = maximumId;
        var maximumViewEnvelope = maximumIdTopology.ToVisualArtifact().ToInterchangeEnvelope(new VisualArtifactRenderOptions {
            Topology = new TopologyRenderOptions { View = new TopologyView { Id = maximumId } }
        });
        Assert(maximumViewEnvelope.Id.Length <= 512 && maximumViewEnvelope.ToJson().Length > 0,
            "Generated topology view ids should remain within the interchange schema limit.");

        maximumIdTopology.Id = new string('t', 600);
        var boundedTopologyEnvelope = maximumIdTopology.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(boundedTopologyEnvelope.Id.Length <= 512 && boundedTopologyEnvelope.ToJson().Length > 0,
            "Topology interchange should deterministically bound over-limit top-level source ids.");

        var autoGroupTopology = TopologyChart.Create();
        autoGroupTopology.Groups.Add(new TopologyGroup { Id = "auto-group", Label = "Auto group", Width = 0, Height = 0 });
        autoGroupTopology.Nodes.Add(new TopologyNode { Id = "auto-node", Label = "Auto node", GroupId = "auto-group" });
        var autoGroupEnvelope = autoGroupTopology.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(autoGroupEnvelope.Groups.Single().Width > 0 && autoGroupEnvelope.Groups.Single().Height > 0,
            "Interchange topology validation should accept auto-sized source groups after preparation assigns their dimensions.");

        var mergedMetadataTopology = TopologyChart.Create();
        mergedMetadataTopology.LayoutMode = TopologyLayoutMode.Manual;
        var mergedMetadataSource = new TopologyNode { Id = "metadata-source", Label = "Source", X = 20, Y = 20, Width = 100, Height = 50 };
        var mergedMetadataTarget = new TopologyNode { Id = "metadata-target", Label = "Target", X = 180, Y = 20, Width = 100, Height = 50 };
        var mergedMetadataEdge = new TopologyEdge { Id = "metadata-edge", SourceNodeId = "metadata-source", TargetNodeId = "metadata-target" };
        for (var index = 0; index < 256; index++) {
            mergedMetadataSource.Metadata["node-" + index] = index.ToString();
            mergedMetadataEdge.Metadata["edge-" + index] = index.ToString();
        }
        mergedMetadataSource.Metrics["latency"] = "42ms";
        mergedMetadataEdge.Metrics["throughput"] = "8Gbps";
        mergedMetadataTopology.Nodes.Add(mergedMetadataSource);
        mergedMetadataTopology.Nodes.Add(mergedMetadataTarget);
        mergedMetadataTopology.Edges.Add(mergedMetadataEdge);
        var mergedMetadataEnvelope = mergedMetadataTopology.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(mergedMetadataEnvelope.Nodes.Single(node => node.Id == "metadata-source").Metadata["metric.latency"] == "42ms" &&
               mergedMetadataEnvelope.Edges.Single().Metadata["metric.throughput"] == "8Gbps" && mergedMetadataEnvelope.ToJson().Length > 0,
            "Interchange projection should retain independently valid topology metadata and metric collections in the merged metadata bag.");

        var invalidGroupTopology = TopologyChart.Create();
        invalidGroupTopology.Nodes.Add(new TopologyNode { Id = "orphan", Label = "Orphan", GroupId = "missing" });
        AssertThrows<TopologyValidationException>(() => invalidGroupTopology.ToVisualArtifact().ToInterchangeEnvelope(new VisualArtifactRenderOptions {
                Topology = new TopologyRenderOptions {
                    View = new TopologyView { IncludeNodeGroups = false, HealthStatuses = { TopologyHealthStatus.Unknown } }
                }
            }),
            "Interchange projection should reject source topology references that a prepared view could otherwise erase.");

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
        flow.Id = "approval-current";
        flow.Title = "Current approval";
        flow.AddStep("archive", "Archive", FlowArtifactStepKind.Process, "requester");
        flow.AddConnector("approve", "archive", "Store", FlowArtifactConnectorKind.Flow);
        var flowRoundTrip = VisualArtifactInterchangeEnvelope.FromJson(flowArtifact.ToInterchangeJson());
        Assert(flowRoundTrip.Id == "approval-current" && flowRoundTrip.Title == "Current approval",
            "Flow interchange should project current model identity after wrapper creation.");
        Assert(flowRoundTrip.Metadata["flow.steps"] == "3" && flowRoundTrip.Metadata["flow.connectors"] == "2",
            "Flow interchange should recompute typed model counts after wrapper creation.");
        TopologyChart preparedFlow = TopologyLayoutEngine.Prepare(flow.ToTopologyChart(), options: new TopologyRenderOptions { IncludeLegend = false });
        Assert(flowRoundTrip.Groups.Single().Kind == "FlowLane", "Flow interchange should preserve lane semantics.");
        Assert(flowRoundTrip.Nodes.Single(node => node.Id == "approve").Kind == "Decision", "Flow interchange should preserve step kinds.");
        Assert(flowRoundTrip.Nodes.Single(node => node.Id == "submit").Metadata["owner"] == "requester", "Flow interchange should preserve step metadata.");
        Assert(flowRoundTrip.Edges.Single(edge => edge.Label == "Review").SourceId == "submit", "Flow interchange should preserve connector labels.");
        Assert(flowRoundTrip.Metadata["scope"] == "artifact" && flowRoundTrip.Metadata["model-only"] == "preserved",
            "Explicit artifact metadata should override same-key flow metadata while retaining model-only values.");
        Assert(flowRoundTrip.Width == preparedFlow.Viewport.Width && flowRoundTrip.Height == preparedFlow.Viewport.Height,
            "Flow interchange should publish the prepared topology viewport instead of its unprepared configured canvas.");

        string longFlowMetadataKey = new string('z', 600);
        flow.Metadata[longFlowMetadataKey] = "model-long";
        var modelMetadataEnvelope = flowArtifact.ToInterchangeEnvelope();
        string boundedFlowMetadataKey = modelMetadataEnvelope.Metadata.Single(pair => pair.Value == "model-long").Key;
        flowArtifact.Metadata[boundedFlowMetadataKey] = "artifact-short";
        flowArtifact.Metadata[longFlowMetadataKey] = "artifact-long";
        var collidingFlowMetadata = flowArtifact.ToInterchangeEnvelope().Metadata;
        Assert(collidingFlowMetadata[boundedFlowMetadataKey] == "artifact-short" && collidingFlowMetadata.Values.Contains("artifact-long") &&
               !collidingFlowMetadata.Values.Contains("model-long"),
            "Artifact metadata should keep same-key precedence while collision-aware bounding retains distinct valid and over-limit keys.");

        var crossLayerFlow = FlowArtifact.Create("cross-layer-metadata").AddStep("step", "Step");
        var crossLayerArtifact = crossLayerFlow.ToVisualArtifact();
        string crossLayerLongKey = new string('v', 600);
        crossLayerArtifact.Metadata[crossLayerLongKey] = "artifact-over-limit";
        string crossLayerBoundedKey = crossLayerArtifact.ToInterchangeEnvelope().Metadata.Single(pair => pair.Value == "artifact-over-limit").Key;
        crossLayerFlow.Metadata[crossLayerBoundedKey] = "model-valid";
        var crossLayerMetadata = crossLayerArtifact.ToInterchangeEnvelope().Metadata;
        Assert(crossLayerMetadata[crossLayerBoundedKey] == "artifact-over-limit" && crossLayerMetadata.Values.Contains("model-valid"),
            "Collision-aware merging should retain a valid model key that matches a different bounded artifact key.");

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

        string longSourceId = new string('s', 300);
        string longTargetId = new string('t', 300);
        var longIdFlowEnvelope = FlowArtifact.Create(new string('f', 600))
            .AddStep(longSourceId, "Long source")
            .AddStep(longTargetId, "Long target")
            .AddConnector(longSourceId, longTargetId)
            .ToVisualArtifact()
            .ToInterchangeEnvelope();
        Assert(longIdFlowEnvelope.Id.Length <= 512 && longIdFlowEnvelope.Nodes.All(node => node.Id.Length <= 512) && longIdFlowEnvelope.Edges.Single().Id.Length <= 512 &&
               longIdFlowEnvelope.Edges.Single().SourceId == longIdFlowEnvelope.Nodes.Single(node => node.Label == "Long source").Id &&
               longIdFlowEnvelope.ToJson().Length > 0,
            "Top-level and allocated source or generated flow ids should remain within the interchange schema limit.");

        var invalidLaneFlow = FlowArtifact.Create("invalid-lane").AddStep("step", "Step", laneId: "missing");
        AssertThrows<TopologyValidationException>(() => invalidLaneFlow.ToVisualArtifact().ToInterchangeEnvelope(),
            "Flow interchange should reject missing lane references consistently with the static topology fallback.");

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

        var duplicateSequence = SequenceArtifact.Create(new string('q', 600))
            .AddParticipant("first", "First")
            .AddParticipant("second", "Second")
            .AddMessage("first", "first", "Self call");
        duplicateSequence.Participants[1].Id = "first";
        var duplicateSequenceEnvelope = duplicateSequence.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(duplicateSequenceEnvelope.Id.Length <= 512 && duplicateSequenceEnvelope.Nodes.Select(node => node.Id).Distinct(StringComparer.Ordinal).Count() == 2,
            "Sequence interchange should bound its top-level id and allocate distinct nodes for participant ids duplicated by mutation.");
        Assert(duplicateSequenceEnvelope.Edges.Single().SourceId == duplicateSequenceEnvelope.Nodes[0].Id &&
               duplicateSequenceEnvelope.Edges.Single().TargetId == duplicateSequenceEnvelope.Nodes[0].Id && duplicateSequenceEnvelope.ToJson().Length > 0,
            "Sequence message references should retain the renderer's first-participant match when later participant ids collide.");

        var danglingSequence = SequenceArtifact.Create("dangling-sequence")
            .AddParticipant("caller", "Caller")
            .AddParticipant("service", "Service")
            .AddMessage("caller", "service", "Call")
            .AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "service" }, "Observe");
        danglingSequence.Participants[0].Id = "renamed-caller";
        danglingSequence.Notes[0].ParticipantIds[0] = "removed-service";
        var danglingSequenceEnvelope = danglingSequence.ToVisualArtifact().ToInterchangeEnvelope();
        Assert(danglingSequenceEnvelope.Edges.Count == 0 && danglingSequenceEnvelope.Metadata["sequence.messages"] == "1",
            "Sequence interchange should omit messages whose mutated endpoints no longer resolve, matching static layout behavior.");
        Assert(danglingSequenceEnvelope.Annotations.Single().TargetIds.Count == 0 && danglingSequenceEnvelope.ToJson().Length > 0,
            "Sequence interchange should retain notes while omitting participant targets that no longer resolve, matching static note placement behavior.");

        var wideSequence = SequenceArtifact.Create("wide").WithTitle("Initial").WithSize(320, 240).AddParticipant("participant-0", "Participant 0");
        var wideArtifact = wideSequence.ToVisualArtifact();
        VisualArtifactSize initialNaturalSize = wideArtifact.NaturalSize.GetValueOrDefault();
        wideSequence.Id = "wide-current";
        wideSequence.Title = "Current";
        for (var index = 1; index < 10; index++) wideSequence.AddParticipant("participant-" + index, "Participant " + index);
        wideSequence.AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "participant-0" }, "Late note");
        wideSequence.Notes[0].StepIndex = 20;
        var wideEnvelope = wideArtifact.ToInterchangeEnvelope();
        VisualArtifactSize currentNaturalSize = wideSequence.ToVisualArtifact().NaturalSize.GetValueOrDefault();
        Assert(currentNaturalSize.Width > initialNaturalSize.Width && currentNaturalSize.Height > initialNaturalSize.Height,
            "The sequence fixture should grow in both dimensions after wrapper creation.");
        Assert(wideEnvelope.Id == "wide-current" && wideEnvelope.Title == "Current",
            "Sequence interchange should project current model identity after wrapper creation.");
        Assert(wideEnvelope.Width == currentNaturalSize.Width && wideEnvelope.Height == currentNaturalSize.Height,
            "Sequence interchange should recalculate natural dimensions from the current model.");
        Assert(wideEnvelope.Metadata["sequence.participants"] == "10" && wideEnvelope.Metadata["sequence.notes"] == "1",
            "Sequence interchange should recompute typed model counts after wrapper creation.");
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
        string compactUnknownValues = string.Join(",", Enumerable.Repeat("null", 100000));
        string compactUnknownArrays = string.Join(",", Enumerable.Repeat("[" + compactUnknownValues + "]", 6));
        string compactUnknownPayload = "{\"schema\":\"chartforgex.visual-artifact\",\"version\":1,\"kind\":\"Topology\",\"sourceLanguage\":\"Native\",\"id\":\"x\",\"unknown\":[" + compactUnknownArrays + "]}";
        Assert(compactUnknownPayload.Length < VisualArtifactInterchangeEnvelope.MaximumJsonCharacters,
            "The unknown-structure fixture should remain below the interchange character limit.");
        AssertThrows<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(compactUnknownPayload),
            "Interchange parsing should cap total materialized JSON values before discarding unknown schema properties.");
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

        var excessiveJsonValues = new VisualArtifactInterchangeEnvelope { Id = "json-values", Kind = VisualArtifactKind.Topology };
        excessiveJsonValues.Nodes.Add(new VisualArtifactInterchangeNode { Id = "node", Label = "Node" });
        for (var annotationIndex = 0; annotationIndex < 12; annotationIndex++) {
            var annotation = new VisualArtifactInterchangeAnnotation { Id = "annotation-" + annotationIndex, Text = "Targets" };
            for (var targetIndex = 0; targetIndex < 50000; targetIndex++) annotation.TargetIds.Add("node");
            excessiveJsonValues.Annotations.Add(annotation);
        }
        AssertThrows<InvalidOperationException>(() => excessiveJsonValues.ToJson(),
            "Interchange serialization should enforce the same total JSON value budget as parsing.");

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
        var stableNumber = new VisualArtifactInterchangeEnvelope { Id = "stable-number", Kind = VisualArtifactKind.Topology, Width = 0.84551240822557006 };
        Assert(stableNumber.ToJson().Contains("\"width\":0.84551240822557006", StringComparison.Ordinal),
            "Interchange JSON should use cross-target-stable 17-digit floating-point formatting.");
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
