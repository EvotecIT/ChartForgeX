using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Creates portable semantic interchange envelopes from ChartForgeX visual artifacts.</summary>
public static class VisualArtifactInterchangeMapping {
    /// <summary>
    /// Creates a versioned semantic interchange envelope for an artifact.
    /// </summary>
    /// <param name="artifact">The artifact to project.</param>
    /// <param name="renderOptions">Optional topology preparation options used to capture deterministic coordinates.</param>
    /// <returns>A portable semantic envelope. Unsupported visual kinds retain common metadata and use their separately rendered SVG fallback.</returns>
    public static VisualArtifactInterchangeEnvelope ToInterchangeEnvelope(this VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions = null) {
        if (artifact == null) throw new ArgumentNullException(nameof(artifact));
        var envelope = Common(artifact);
        switch (artifact.Model) {
            case TopologyChart topology:
                MapTopology(envelope, topology, renderOptions?.Topology);
                break;
            case FlowArtifact flow:
                MapFlow(envelope, flow);
                break;
            case SequenceArtifact sequence:
                MapSequence(envelope, sequence);
                break;
        }

        VisualArtifactInterchangeValidation.Validate(envelope);
        return envelope;
    }

    /// <summary>Serializes an artifact's semantic interchange envelope to deterministic JSON.</summary>
    public static string ToInterchangeJson(this VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions = null) =>
        artifact.ToInterchangeEnvelope(renderOptions).ToJson();

    /// <summary>Serializes an artifact's semantic interchange envelope to deterministic UTF-8 JSON bytes.</summary>
    public static byte[] ToInterchangeUtf8Json(this VisualArtifact artifact, VisualArtifactRenderOptions? renderOptions = null) =>
        artifact.ToInterchangeEnvelope(renderOptions).ToUtf8Json();

    private static VisualArtifactInterchangeEnvelope Common(VisualArtifact artifact) {
        var envelope = new VisualArtifactInterchangeEnvelope {
            Id = artifact.Id,
            Kind = artifact.Kind,
            SourceLanguage = artifact.SourceLanguage,
            Title = artifact.Title,
            Subtitle = artifact.Subtitle,
            AccessibleName = artifact.Accessibility.Name,
            AccessibleDescription = artifact.Accessibility.Description,
            Language = artifact.Accessibility.Language,
            IsDecorative = artifact.Accessibility.IsDecorative,
            Width = artifact.NaturalSize?.Width,
            Height = artifact.NaturalSize?.Height
        };
        Copy(artifact.Metadata, envelope.Metadata);
        return envelope;
    }

    private static void MapTopology(VisualArtifactInterchangeEnvelope envelope, TopologyChart topology, TopologyRenderOptions? renderOptions) {
        var options = renderOptions?.Clone() ?? new TopologyRenderOptions();
        var prepared = TopologyLayoutEngine.Prepare(topology, options.View, options);
        envelope.Layout = topology.LayoutMode.ToString();
        envelope.Direction = topology.LayoutDirection.ToString();
        envelope.Width = prepared.Viewport.Width;
        envelope.Height = prepared.Viewport.Height;

        foreach (var group in prepared.Groups) envelope.Groups.Add(MapGroup(group, "TopologyGroup"));
        foreach (var node in prepared.Nodes) envelope.Nodes.Add(MapNode(node));
        for (var index = 0; index < prepared.Edges.Count; index++) envelope.Edges.Add(MapEdge(prepared.Edges[index], index));
    }

    private static void MapFlow(VisualArtifactInterchangeEnvelope envelope, FlowArtifact flow) {
        envelope.Layout = flow.LayoutMode.ToString();
        envelope.Direction = flow.Direction.ToString();
        envelope.Width = flow.Width;
        envelope.Height = flow.Height;
        Copy(flow.Metadata, envelope.Metadata);

        var prepared = TopologyLayoutEngine.Prepare(flow.ToTopologyChart(), options: new TopologyRenderOptions { IncludeLegend = false });
        var preparedGroups = GroupsById(prepared.Groups);
        var preparedNodes = NodesById(prepared.Nodes);

        foreach (var lane in flow.Lanes) {
            preparedGroups.TryGetValue(lane.Id, out var preparedGroup);
            var group = new VisualArtifactInterchangeGroup {
                Id = lane.Id,
                Kind = "FlowLane",
                Label = lane.Label,
                Status = lane.Status.ToString(),
                Color = lane.Color,
                X = preparedGroup?.X,
                Y = preparedGroup?.Y,
                Width = preparedGroup?.Width,
                Height = preparedGroup?.Height
            };
            Copy(lane.Metadata, group.Metadata);
            envelope.Groups.Add(group);
        }

        foreach (var step in flow.Steps) {
            preparedNodes.TryGetValue(step.Id, out var preparedNode);
            var node = new VisualArtifactInterchangeNode {
                Id = step.Id,
                Kind = step.Kind.ToString(),
                Label = step.Label,
                Subtitle = step.Subtitle,
                GroupId = step.LaneId,
                Status = step.Status.ToString(),
                IconId = step.Icon,
                Symbol = step.Symbol,
                Badge = step.Badge,
                Color = step.Color,
                X = preparedNode?.X,
                Y = preparedNode?.Y,
                Width = preparedNode?.Width ?? step.Width,
                Height = preparedNode?.Height ?? step.Height
            };
            Copy(step.Metadata, node.Metadata);
            envelope.Nodes.Add(node);
        }

        for (var index = 0; index < flow.Connectors.Count; index++) {
            var connector = flow.Connectors[index];
            var edge = new VisualArtifactInterchangeEdge {
                Id = connector.Id,
                Kind = connector.Kind.ToString(),
                SourceId = connector.SourceId,
                TargetId = connector.TargetId,
                Label = connector.Label,
                Status = connector.Status.ToString(),
                Direction = connector.Direction.ToString(),
                Color = connector.Color,
                Order = index
            };
            Copy(connector.Metadata, edge.Metadata);
            envelope.Edges.Add(edge);
        }
    }

    private static void MapSequence(VisualArtifactInterchangeEnvelope envelope, SequenceArtifact sequence) {
        envelope.Layout = "Sequence";
        envelope.Direction = "TopToBottom";
        envelope.Width = sequence.Width;
        envelope.Height = sequence.Height;
        Copy(sequence.Metadata, envelope.Metadata);

        for (var index = 0; index < sequence.Participants.Count; index++) {
            var participant = sequence.Participants[index];
            var node = new VisualArtifactInterchangeNode {
                Id = participant.Id,
                Kind = participant.Kind.ToString(),
                Label = participant.Label
            };
            Copy(participant.Metadata, node.Metadata);
            node.Metadata["sequence.order"] = index.ToString(CultureInfo.InvariantCulture);
            node.Metadata["sequence.implicit"] = participant.IsImplicit ? "true" : "false";
            envelope.Nodes.Add(node);
        }

        for (var index = 0; index < sequence.Messages.Count; index++) {
            var message = sequence.Messages[index];
            var edge = new VisualArtifactInterchangeEdge {
                Id = "message-" + (index + 1).ToString(CultureInfo.InvariantCulture),
                Kind = "SequenceMessage",
                SourceId = message.SourceId,
                TargetId = message.TargetId,
                Label = message.Text,
                Direction = "Forward",
                LineStyle = message.LineStyle.ToString(),
                Order = index
            };
            Copy(message.Metadata, edge.Metadata);
            edge.Metadata["sequence.activatesTarget"] = message.ActivatesTarget ? "true" : "false";
            edge.Metadata["sequence.deactivates"] = message.Deactivates ? "true" : "false";
            envelope.Edges.Add(edge);
        }

        for (var index = 0; index < sequence.Notes.Count; index++) {
            var note = sequence.Notes[index];
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = "note-" + (index + 1).ToString(CultureInfo.InvariantCulture),
                Kind = "SequenceNote",
                Text = note.Text,
                Placement = note.Placement.ToString(),
                StartIndex = note.StepIndex,
                EndIndex = note.StepIndex
            };
            annotation.TargetIds.AddRange(note.ParticipantIds);
            envelope.Annotations.Add(annotation);
        }

        for (var index = 0; index < sequence.Blocks.Count; index++) {
            var block = sequence.Blocks[index];
            envelope.Annotations.Add(new VisualArtifactInterchangeAnnotation {
                Id = "block-" + (index + 1).ToString(CultureInfo.InvariantCulture),
                Kind = "SequenceBlock:" + block.Kind,
                Text = block.Text,
                StartIndex = block.StartStepIndex,
                EndIndex = block.EndStepIndex
            });
        }
    }

    private static VisualArtifactInterchangeGroup MapGroup(TopologyGroup group, string kind) {
        var mapped = new VisualArtifactInterchangeGroup {
            Id = group.Id,
            Kind = kind,
            Label = group.Label,
            Subtitle = group.Subtitle,
            Status = group.Status.ToString(),
            Color = group.Color,
            Href = SafeHref(group.Href),
            Tooltip = group.Tooltip,
            X = group.X,
            Y = group.Y,
            Width = group.Width,
            Height = group.Height
        };
        if (!string.IsNullOrWhiteSpace(group.IconId)) mapped.Metadata["iconId"] = group.IconId!;
        if (!string.IsNullOrWhiteSpace(group.Symbol)) mapped.Metadata["symbol"] = group.Symbol!;
        Copy(group.Metadata, mapped.Metadata);
        return mapped;
    }

    private static VisualArtifactInterchangeNode MapNode(TopologyNode node) {
        var mapped = new VisualArtifactInterchangeNode {
            Id = node.Id,
            Kind = node.Kind.ToString(),
            Label = node.Label,
            Subtitle = node.Subtitle,
            GroupId = node.GroupId,
            Status = node.Status.ToString(),
            IconId = node.IconId,
            Symbol = node.Symbol,
            Badge = node.Badge,
            Color = node.Color,
            BackgroundColor = node.BackgroundColor,
            Href = SafeHref(node.Href),
            Tooltip = node.Tooltip,
            X = node.X,
            Y = node.Y,
            Width = node.Width,
            Height = node.Height
        };
        Copy(node.Metadata, mapped.Metadata);
        CopyWithPrefix(node.Metrics, mapped.Metadata, "metric.");
        foreach (var port in node.Ports) {
            var mappedPort = new VisualArtifactInterchangePort { Id = port.Id, Side = port.Side.ToString(), Offset = port.Offset, Label = port.Label };
            Copy(port.Metadata, mappedPort.Metadata);
            mapped.Ports.Add(mappedPort);
        }
        foreach (var detail in node.Details) {
            var mappedDetail = new VisualArtifactInterchangeDetail {
                Label = detail.Label,
                Value = detail.Value,
                IconId = detail.IconId,
                Status = detail.Status?.ToString(),
                Color = detail.Color
            };
            Copy(detail.Metadata, mappedDetail.Metadata);
            mapped.Details.Add(mappedDetail);
        }
        return mapped;
    }

    private static VisualArtifactInterchangeEdge MapEdge(TopologyEdge edge, int order) {
        var mapped = new VisualArtifactInterchangeEdge {
            Id = edge.Id,
            Kind = edge.Kind.ToString(),
            SourceId = edge.SourceNodeId,
            TargetId = edge.TargetNodeId,
            Label = edge.Label,
            SecondaryLabel = edge.SecondaryLabel,
            TertiaryLabel = edge.TertiaryLabel,
            SourceLabel = edge.SourceLabel,
            TargetLabel = edge.TargetLabel,
            Status = edge.Status.ToString(),
            Direction = edge.Direction.ToString(),
            LineStyle = edge.LineStyle.ToString(),
            SourcePort = edge.SourcePort.ToString(),
            TargetPort = edge.TargetPort.ToString(),
            SourcePortId = edge.SourcePortId,
            TargetPortId = edge.TargetPortId,
            Color = edge.Color,
            Href = SafeHref(edge.Href),
            Tooltip = edge.Tooltip,
            Order = order
        };
        Copy(edge.Metadata, mapped.Metadata);
        CopyWithPrefix(edge.Metrics, mapped.Metadata, "metric.");
        return mapped;
    }

    private static Dictionary<string, TopologyGroup> GroupsById(IEnumerable<TopologyGroup> groups) {
        var result = new Dictionary<string, TopologyGroup>(StringComparer.Ordinal);
        foreach (var group in groups) result[group.Id] = group;
        return result;
    }

    private static Dictionary<string, TopologyNode> NodesById(IEnumerable<TopologyNode> nodes) {
        var result = new Dictionary<string, TopologyNode>(StringComparer.Ordinal);
        foreach (var node in nodes) result[node.Id] = node;
        return result;
    }

    private static void Copy(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, string> target) {
        foreach (var pair in source) target[pair.Key] = pair.Value;
    }

    private static void CopyWithPrefix(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, string> target, string prefix) {
        foreach (var pair in source) target[prefix + pair.Key] = pair.Value;
    }
}
