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
                MapTopology(
                    envelope,
                    VisualArtifactRendering.TopologyModel(artifact, topology),
                    VisualArtifactRendering.TopologyOptions(artifact, renderOptions));
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
        var options = (renderOptions ?? new TopologyRenderOptions()).CloneForRendering();
        var prepared = TopologyLayoutEngine.Prepare(topology, options.View, options);
        var ids = new InterchangeIdScope();
        foreach (var group in prepared.Groups) ids.AddGroup(group.Id);
        foreach (var node in prepared.Nodes) ids.AddNode(node.Id);
        foreach (var edge in prepared.Edges) ids.AddEdge(edge.Id);
        if (!string.IsNullOrWhiteSpace(prepared.Id)) {
            string preparedId = prepared.Id!;
            envelope.Id = !string.IsNullOrWhiteSpace(options.View?.Id)
                ? BoundedGeneratedId(preparedId, "topology-view")
                : preparedId;
        }
        envelope.Title = prepared.Title ?? string.Empty;
        envelope.Subtitle = prepared.Subtitle ?? string.Empty;
        envelope.Layout = prepared.LayoutMode.ToString();
        envelope.Direction = prepared.LayoutDirection.ToString();
        envelope.Width = prepared.Viewport.Width;
        envelope.Height = prepared.Viewport.Height;
        envelope.Metadata["topology.layout"] = prepared.LayoutMode.ToString();
        envelope.Metadata["topology.nodes"] = prepared.Nodes.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["topology.edges"] = prepared.Edges.Count.ToString(CultureInfo.InvariantCulture);

        foreach (var group in prepared.Groups) envelope.Groups.Add(MapGroup(group, ids.Group(group.Id), "TopologyGroup"));
        foreach (var node in prepared.Nodes) envelope.Nodes.Add(MapNode(node, ids.Node(node.Id), ids.OptionalGroup(node.GroupId)));
        for (var index = 0; index < prepared.Edges.Count; index++) {
            TopologyEdge edge = prepared.Edges[index];
            envelope.Edges.Add(MapEdge(edge, ids.Edge(edge.Id), ids.Node(edge.SourceNodeId), ids.Node(edge.TargetNodeId), index));
        }
    }

    private static void MapFlow(VisualArtifactInterchangeEnvelope envelope, FlowArtifact flow) {
        envelope.Id = flow.Id;
        envelope.Title = flow.Title;
        envelope.Subtitle = flow.Subtitle;
        envelope.Layout = flow.LayoutMode.ToString();
        envelope.Direction = flow.Direction.ToString();
        CopyMissing(flow.Metadata, envelope.Metadata);
        envelope.Metadata["flow.lanes"] = flow.Lanes.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["flow.steps"] = flow.Steps.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["flow.connectors"] = flow.Connectors.Count.ToString(CultureInfo.InvariantCulture);

        var prepared = TopologyLayoutEngine.Prepare(flow.ToTopologyChart(), options: new TopologyRenderOptions { IncludeLegend = false });
        envelope.Width = prepared.Viewport.Width;
        envelope.Height = prepared.Viewport.Height;
        var preparedGroups = GroupsById(prepared.Groups);
        var preparedNodes = NodesById(prepared.Nodes);
        var ids = new InterchangeIdScope();
        foreach (var lane in flow.Lanes) ids.AddGroup(lane.Id);
        foreach (var step in flow.Steps) ids.AddNode(step.Id);
        foreach (var connector in flow.Connectors) ids.AddEdge(connector.Id);

        foreach (var lane in flow.Lanes) {
            preparedGroups.TryGetValue(lane.Id, out var preparedGroup);
            var group = new VisualArtifactInterchangeGroup {
                Id = ids.Group(lane.Id),
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
                Id = ids.Node(step.Id),
                Kind = step.Kind.ToString(),
                Label = step.Label,
                Subtitle = step.Subtitle,
                GroupId = ids.OptionalGroup(step.LaneId),
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
                Id = ids.Edge(connector.Id),
                Kind = connector.Kind.ToString(),
                SourceId = ids.Node(connector.SourceId),
                TargetId = ids.Node(connector.TargetId),
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
        var ids = new InterchangeIdScope();
        foreach (var participant in sequence.Participants) ids.AddNode(participant.Id);
        envelope.Id = sequence.Id;
        envelope.Title = sequence.Title;
        envelope.Subtitle = sequence.Subtitle;
        envelope.Layout = "Sequence";
        envelope.Direction = "TopToBottom";
        VisualArtifactSize naturalSize = SequenceArtifactRendering.CalculateNaturalSize(sequence);
        envelope.Width = naturalSize.Width;
        envelope.Height = naturalSize.Height;
        CopyMissing(sequence.Metadata, envelope.Metadata);
        envelope.Metadata["sequence.participants"] = sequence.Participants.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["sequence.messages"] = sequence.Messages.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["sequence.notes"] = sequence.Notes.Count.ToString(CultureInfo.InvariantCulture);

        for (var index = 0; index < sequence.Participants.Count; index++) {
            var participant = sequence.Participants[index];
            var node = new VisualArtifactInterchangeNode {
                Id = ids.Node(participant.Id),
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
            string edgeId = ids.AddEdge("message-" + (index + 1).ToString(CultureInfo.InvariantCulture));
            var edge = new VisualArtifactInterchangeEdge {
                Id = edgeId,
                Kind = "SequenceMessage",
                SourceId = ids.Node(message.SourceId),
                TargetId = ids.Node(message.TargetId),
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
            int stepIndex = Math.Max(0, note.StepIndex);
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = ids.AddAnnotation("note-" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                Kind = "SequenceNote",
                Text = note.Text,
                Placement = note.Placement.ToString(),
                StartIndex = stepIndex,
                EndIndex = stepIndex
            };
            foreach (string participantId in note.ParticipantIds) annotation.TargetIds.Add(ids.Node(participantId));
            envelope.Annotations.Add(annotation);
        }

        for (var index = 0; index < sequence.Blocks.Count; index++) {
            var block = sequence.Blocks[index];
            int start = Math.Max(0, block.StartStepIndex);
            int end = Math.Max(start, block.EndStepIndex);
            envelope.Annotations.Add(new VisualArtifactInterchangeAnnotation {
                Id = ids.AddAnnotation("block-" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                Kind = "SequenceBlock:" + block.Kind,
                Text = block.Text,
                StartIndex = start,
                EndIndex = end
            });
        }
    }

    private static VisualArtifactInterchangeGroup MapGroup(TopologyGroup group, string id, string kind) {
        var mapped = new VisualArtifactInterchangeGroup {
            Id = id,
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
        Copy(group.Metadata, mapped.Metadata);
        if (!string.IsNullOrWhiteSpace(group.IconId)) mapped.Metadata["iconId"] = group.IconId!;
        if (!string.IsNullOrWhiteSpace(group.Symbol)) mapped.Metadata["symbol"] = group.Symbol!;
        return mapped;
    }

    private static VisualArtifactInterchangeNode MapNode(TopologyNode node, string id, string? groupId) {
        var mapped = new VisualArtifactInterchangeNode {
            Id = id,
            Kind = node.Kind.ToString(),
            Label = node.Label,
            Subtitle = node.Subtitle,
            GroupId = groupId,
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

    private static VisualArtifactInterchangeEdge MapEdge(TopologyEdge edge, string id, string sourceId, string targetId, int order) {
        var mapped = new VisualArtifactInterchangeEdge {
            Id = id,
            Kind = edge.Kind.ToString(),
            SourceId = sourceId,
            TargetId = targetId,
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

    private static void CopyMissing(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, string> target) {
        foreach (var pair in source) if (!target.ContainsKey(pair.Key)) target[pair.Key] = pair.Value;
    }

    private static void CopyWithPrefix(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, string> target, string prefix) {
        foreach (var pair in source) {
            string key = BoundedGeneratedId(prefix + pair.Key, "metadata-key");
            target[key] = pair.Value;
        }
    }

    private static string BoundedGeneratedId(string value, string discriminator, int ordinal = 1) {
        string suffix = ordinal <= 1 ? string.Empty : "-" + ordinal.ToString(CultureInfo.InvariantCulture);
        if (value.Length + suffix.Length <= VisualArtifactInterchangeValidation.MaximumIdCharacters) return value + suffix;

        string tail = "-" + StableHash(discriminator, value) + suffix;
        int prefixLength = VisualArtifactInterchangeValidation.MaximumIdCharacters - tail.Length;
        if (prefixLength > 0 && prefixLength < value.Length && char.IsHighSurrogate(value[prefixLength - 1]) && char.IsLowSurrogate(value[prefixLength])) {
            prefixLength--;
        }
        return value.Substring(0, Math.Max(0, prefixLength)) + tail;
    }

    private static string StableHash(string discriminator, string value) {
        unchecked {
            uint hash = 2166136261;
            AddHash(ref hash, discriminator);
            AddHash(ref hash, value);
            return hash.ToString("x8", CultureInfo.InvariantCulture);
        }
    }

    private static void AddHash(ref uint hash, string value) {
        foreach (char character in value) {
            hash ^= character;
            hash *= 16777619;
        }
    }

    private sealed class InterchangeIdScope {
        private readonly HashSet<string> _used = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _groups = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _nodes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _edges = new(StringComparer.Ordinal);

        public string AddGroup(string sourceId) => Add(_groups, sourceId, "group");
        public string AddNode(string sourceId) => Add(_nodes, sourceId, "node");
        public string AddEdge(string sourceId) => Add(_edges, sourceId, "edge");
        public string AddAnnotation(string sourceId) => Allocate(sourceId, "annotation");
        public string Group(string sourceId) => _groups[sourceId];
        public string Node(string sourceId) => _nodes[sourceId];
        public string Edge(string sourceId) => _edges[sourceId];
        public string? OptionalGroup(string? sourceId) =>
            !string.IsNullOrWhiteSpace(sourceId) && _groups.TryGetValue(sourceId!, out string? mappedId)
                ? mappedId
                : null;

        private string Add(IDictionary<string, string> map, string sourceId, string category) {
            string allocated = Allocate(sourceId, category);
            map.Add(sourceId, allocated);
            return allocated;
        }

        private string Allocate(string sourceId, string category) {
            if (_used.Add(sourceId)) return sourceId;
            string preferred = category + "-" + sourceId;
            string candidate = BoundedGeneratedId(preferred, category);
            int ordinal = 2;
            while (!_used.Add(candidate)) {
                candidate = BoundedGeneratedId(preferred, category, ordinal);
                ordinal++;
            }
            return candidate;
        }
    }
}
