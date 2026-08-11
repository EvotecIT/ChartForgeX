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
        var envelope = Common(artifact, out var artifactMetadataKeys);
        switch (artifact.Model) {
            case TopologyChart topology:
                MapTopology(
                    envelope,
                    artifact,
                    VisualArtifactRendering.TopologyModel(artifact, topology),
                    VisualArtifactRendering.TopologyOptions(artifact, renderOptions));
                break;
            case FlowArtifact flow:
                MapFlow(envelope, flow, artifactMetadataKeys);
                break;
            case SequenceArtifact sequence:
                MapSequence(envelope, sequence, artifactMetadataKeys);
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

    private static VisualArtifactInterchangeEnvelope Common(VisualArtifact artifact, out Dictionary<string, string> artifactMetadataKeys) {
        var envelope = new VisualArtifactInterchangeEnvelope {
            Id = BoundedGeneratedId(artifact.Id, "artifact"),
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
        artifactMetadataKeys = Copy(artifact.Metadata, envelope.Metadata);
        return envelope;
    }

    private static void MapTopology(
        VisualArtifactInterchangeEnvelope envelope,
        VisualArtifact artifact,
        TopologyChart topology,
        TopologyRenderOptions? renderOptions) {
        var options = (renderOptions ?? new TopologyRenderOptions()).CloneForRendering();
        var prepared = PrepareValidatedTopology(topology, options, detachOmittedSourceGroups: options.View != null);
        RefreshTopologyAccessibility(envelope, artifact, prepared);
        var ids = new InterchangeIdScope();
        foreach (var group in prepared.Groups) ids.AddGroup(group.Id);
        foreach (var node in prepared.Nodes) {
            ids.AddNode(node.Id);
            foreach (var port in node.Ports) ids.AddPort(node.Id, port.Id);
        }
        foreach (var edge in prepared.Edges) ids.AddEdge(edge.Id);
        if (!string.IsNullOrWhiteSpace(prepared.Id)) {
            string preparedId = prepared.Id!;
            envelope.Id = BoundedGeneratedId(preparedId, !string.IsNullOrWhiteSpace(options.View?.Id) ? "topology-view" : "topology");
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
        if (prepared.LayoutMode == TopologyLayoutMode.Geographic) {
            envelope.Metadata["topology.mapViewport.name"] = prepared.MapViewport.Name;
            envelope.Metadata["topology.mapViewport.projection"] = "Equirectangular";
            envelope.Metadata["topology.mapViewport.minimumLongitude"] = InvariantNumber(prepared.MapViewport.MinimumLongitude);
            envelope.Metadata["topology.mapViewport.maximumLongitude"] = InvariantNumber(prepared.MapViewport.MaximumLongitude);
            envelope.Metadata["topology.mapViewport.minimumLatitude"] = InvariantNumber(prepared.MapViewport.MinimumLatitude);
            envelope.Metadata["topology.mapViewport.maximumLatitude"] = InvariantNumber(prepared.MapViewport.MaximumLatitude);
        }

        foreach (var group in prepared.Groups) envelope.Groups.Add(MapGroup(group, ids.Group(group.Id), "TopologyGroup"));
        foreach (var node in prepared.Nodes) {
            envelope.Nodes.Add(MapNode(node, ids.Node(node.Id), ids.OptionalGroup(node.GroupId), ids, EffectiveNodeDisplayMode(node, options)));
        }
        for (var index = 0; index < prepared.Edges.Count; index++) {
            TopologyEdge edge = prepared.Edges[index];
            envelope.Edges.Add(MapEdge(
                edge,
                ids.Edge(edge.Id),
                ids.Node(edge.SourceNodeId),
                ids.Node(edge.TargetNodeId),
                ids.OptionalPort(edge.SourceNodeId, edge.SourcePortId),
                ids.OptionalPort(edge.TargetNodeId, edge.TargetPortId),
                index,
                ids));
        }
        foreach (var scenario in prepared.Scenarios) {
            VisualArtifactInterchangeScenario? mappedScenario = MapScenario(scenario, ids);
            if (mappedScenario != null) envelope.Scenarios.Add(mappedScenario);
        }
    }

    private static void RefreshTopologyAccessibility(
        VisualArtifactInterchangeEnvelope envelope,
        VisualArtifact artifact,
        TopologyChart prepared) {
        if (!artifact.HasModelAccessibilitySnapshot) return;
        envelope.AccessibleName = string.Equals(artifact.Accessibility.Name, artifact.ModelAccessibilitySnapshot.Name, StringComparison.Ordinal)
            ? prepared.Accessibility.Name
            : artifact.Accessibility.Name;
        envelope.AccessibleDescription = string.Equals(artifact.Accessibility.Description, artifact.ModelAccessibilitySnapshot.Description, StringComparison.Ordinal)
            ? prepared.Accessibility.Description
            : artifact.Accessibility.Description;
        envelope.Language = string.Equals(artifact.Accessibility.Language, artifact.ModelAccessibilitySnapshot.Language, StringComparison.Ordinal)
            ? prepared.Accessibility.Language
            : artifact.Accessibility.Language;
        envelope.IsDecorative = artifact.Accessibility.IsDecorative == artifact.ModelAccessibilitySnapshot.IsDecorative
            ? prepared.Accessibility.IsDecorative
            : artifact.Accessibility.IsDecorative;
    }

    private static void MapFlow(VisualArtifactInterchangeEnvelope envelope, FlowArtifact flow, IReadOnlyDictionary<string, string> artifactMetadataKeys) {
        envelope.Id = BoundedGeneratedId(flow.Id, "flow");
        envelope.Title = flow.Title;
        envelope.Subtitle = flow.Subtitle;
        envelope.Layout = flow.LayoutMode.ToString();
        envelope.Direction = flow.Direction.ToString();
        CopyMissing(flow.Metadata, envelope.Metadata, artifactMetadataKeys);
        envelope.Metadata["flow.lanes"] = flow.Lanes.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["flow.steps"] = flow.Steps.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["flow.connectors"] = flow.Connectors.Count.ToString(CultureInfo.InvariantCulture);

        var flowTopology = flow.ToTopologyChart();
        var prepared = PrepareValidatedTopology(flowTopology, new TopologyRenderOptions { IncludeLegend = false }, detachOmittedSourceGroups: false);
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

    private static void MapSequence(VisualArtifactInterchangeEnvelope envelope, SequenceArtifact sequence, IReadOnlyDictionary<string, string> artifactMetadataKeys) {
        var ids = new InterchangeIdScope();
        var participantIds = new List<string>();
        foreach (var participant in sequence.Participants) participantIds.Add(ids.AddNodeOccurrence(participant.Id));
        envelope.Id = BoundedGeneratedId(sequence.Id, "sequence");
        envelope.Title = sequence.Title;
        envelope.Subtitle = sequence.Subtitle;
        envelope.Layout = "Sequence";
        envelope.Direction = "TopToBottom";
        VisualArtifactSize naturalSize = SequenceArtifactRendering.CalculateNaturalSize(sequence);
        envelope.Width = naturalSize.Width;
        envelope.Height = naturalSize.Height;
        CopyMissing(sequence.Metadata, envelope.Metadata, artifactMetadataKeys);
        envelope.Metadata["sequence.participants"] = sequence.Participants.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["sequence.messages"] = sequence.Messages.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["sequence.activations"] = sequence.Activations.Count.ToString(CultureInfo.InvariantCulture);
        envelope.Metadata["sequence.notes"] = sequence.Notes.Count.ToString(CultureInfo.InvariantCulture);

        for (var index = 0; index < sequence.Participants.Count; index++) {
            var participant = sequence.Participants[index];
            var node = new VisualArtifactInterchangeNode {
                Id = participantIds[index],
                Kind = participant.Kind.ToString(),
                Label = participant.Label,
                Href = SafeHref(participant.Href)
            };
            Copy(participant.Metadata, node.Metadata);
            node.Metadata["sequence.order"] = index.ToString(CultureInfo.InvariantCulture);
            node.Metadata["sequence.implicit"] = participant.IsImplicit ? "true" : "false";
            envelope.Nodes.Add(node);
        }

        for (var index = 0; index < sequence.Messages.Count; index++) {
            var message = sequence.Messages[index];
            if (!ids.TryNode(message.SourceId, out string sourceId) || !ids.TryNode(message.TargetId, out string targetId)) continue;
            string edgeId = ids.AddEdge("message-" + (index + 1).ToString(CultureInfo.InvariantCulture));
            var edge = new VisualArtifactInterchangeEdge {
                Id = edgeId,
                Kind = "SequenceMessage",
                SourceId = sourceId,
                TargetId = targetId,
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
            foreach (string participantId in note.ParticipantIds) {
                if (ids.TryNode(participantId, out string targetId)) annotation.TargetIds.Add(targetId);
            }
            envelope.Annotations.Add(annotation);
        }

        for (var index = 0; index < sequence.Activations.Count; index++) {
            var activation = sequence.Activations[index];
            if (!ids.TryNode(activation.ParticipantId, out string targetId)) continue;
            int stepIndex = Math.Max(0, activation.StepIndex);
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = ids.AddAnnotation("activation-" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                Kind = activation.Active ? "SequenceActivation" : "SequenceDeactivation",
                Text = string.Empty,
                StartIndex = stepIndex,
                EndIndex = stepIndex
            };
            annotation.TargetIds.Add(targetId);
            Copy(activation.Metadata, annotation.Metadata);
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

    private static TopologyChart PrepareValidatedTopology(TopologyChart topology, TopologyRenderOptions options, bool detachOmittedSourceGroups) {
        var validator = new TopologyChartValidator();
        var sourceValidation = validator.ValidateScenarioReferences(topology);
        if (!sourceValidation.IsValid) throw new TopologyValidationException(sourceValidation);

        var prepared = TopologyLayoutEngine.Prepare(topology, options.View, options);
        if (detachOmittedSourceGroups) DetachOmittedSourceGroups(topology, prepared);
        var preparedValidation = validator.Validate(prepared, validateScenarioReferences: false, options);
        if (!preparedValidation.IsValid) throw new TopologyValidationException(preparedValidation);
        return prepared;
    }

    private static void DetachOmittedSourceGroups(TopologyChart source, TopologyChart prepared) {
        var sourceGroupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in source.Groups) sourceGroupIds.Add(group.Id);
        var preparedGroupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in prepared.Groups) preparedGroupIds.Add(group.Id);
        foreach (var node in prepared.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.GroupId) && sourceGroupIds.Contains(node.GroupId!) && !preparedGroupIds.Contains(node.GroupId!)) {
                node.GroupId = null;
            }
        }
    }

    private static VisualArtifactInterchangeNode MapNode(
        TopologyNode node,
        string id,
        string? groupId,
        InterchangeIdScope ids,
        TopologyNodeDisplayMode displayMode) {
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
        var metadataKeys = Copy(node.Metadata, mapped.Metadata);
        CopyWithPrefix(node.Metrics, mapped.Metadata, "metric.", metadataKeys);
        mapped.Metadata["topology.displayMode"] = displayMode.ToString();
        if (!node.ShowStatusBadge) mapped.Metadata["topology.showStatusBadge"] = bool.FalseString;
        if (node.MaximumLabelCharacters.HasValue) mapped.Metadata["topology.maximumLabelCharacters"] = node.MaximumLabelCharacters.Value.ToString(CultureInfo.InvariantCulture);
        foreach (var port in node.Ports) {
            var mappedPort = new VisualArtifactInterchangePort { Id = ids.Port(node.Id, port.Id), Side = port.Side.ToString(), Offset = port.Offset, Label = port.Label };
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

    private static VisualArtifactInterchangeEdge MapEdge(
        TopologyEdge edge,
        string id,
        string sourceId,
        string targetId,
        string? sourcePortId,
        string? targetPortId,
        int order,
        InterchangeIdScope ids) {
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
            SourcePortId = sourcePortId,
            TargetPortId = targetPortId,
            Color = edge.Color,
            Href = SafeHref(edge.Href),
            Tooltip = edge.Tooltip,
            Order = order
        };
        var metadataKeys = Copy(edge.Metadata, mapped.Metadata);
        CopyWithPrefix(edge.Metrics, mapped.Metadata, "metric.", metadataKeys);
        mapped.Metadata["topology.routing"] = edge.Routing.ToString();
        mapped.Metadata["topology.emphasis"] = edge.Emphasis.ToString();
        if (edge.SourceMarker.HasValue) mapped.Metadata["topology.sourceMarker"] = edge.SourceMarker.Value.ToString();
        if (edge.TargetMarker.HasValue) mapped.Metadata["topology.targetMarker"] = edge.TargetMarker.Value.ToString();
        if (edge.StrokeWidth.HasValue) mapped.Metadata["topology.strokeWidth"] = InvariantNumber(edge.StrokeWidth.Value);
        if (edge.Opacity.HasValue) mapped.Metadata["topology.opacity"] = InvariantNumber(edge.Opacity.Value);
        if (edge.DashPattern.Count > 0) mapped.Metadata["topology.dashPattern"] = InvariantNumbers(edge.DashPattern);
        if (edge.Waypoints.Count > 0) mapped.Metadata["topology.waypoints"] = InvariantWaypoints(edge);
        if (edge.IsMuted) mapped.Metadata["topology.muted"] = bool.TrueString;
        if (edge.RoutingPriority != 0) mapped.Metadata["topology.routingPriority"] = edge.RoutingPriority.ToString(CultureInfo.InvariantCulture);
        if (edge.RouteLane != 0) mapped.Metadata["topology.routeLane"] = InvariantNumber(edge.RouteLane);
        if (edge.LabelOffsetX != 0) mapped.Metadata["topology.labelOffsetX"] = InvariantNumber(edge.LabelOffsetX);
        if (edge.LabelOffsetY != 0) mapped.Metadata["topology.labelOffsetY"] = InvariantNumber(edge.LabelOffsetY);
        if (edge.HasLabelAnchorOverride) {
            mapped.Metadata["topology.labelAnchor"] = InvariantNumber(edge.LabelAnchorX) + "," + InvariantNumber(edge.LabelAnchorY);
        }
        if (!string.IsNullOrWhiteSpace(edge.LabelAnchorNodeId) && ids.TryNode(edge.LabelAnchorNodeId!, out string labelAnchorNodeId)) {
            mapped.Metadata["topology.labelAnchorNodeId"] = labelAnchorNodeId;
        }
        if (edge.LayoutInference != TopologyEdgeLayoutInference.None) mapped.Metadata["topology.layoutInference"] = edge.LayoutInference.ToString();
        return mapped;
    }

    private static VisualArtifactInterchangeScenario? MapScenario(TopologyScenario scenario, InterchangeIdScope ids) {
        var mapped = new VisualArtifactInterchangeScenario {
            Id = ids.AddScenario(scenario.Id),
            Label = scenario.Label,
            Description = scenario.Description,
            Color = scenario.Color,
            PlaybackDelayMilliseconds = scenario.PlaybackDelayMilliseconds,
            LoopPlayback = scenario.LoopPlayback,
            AutoPlay = scenario.AutoPlay,
            Spotlight = scenario.Spotlight
        };
        Copy(scenario.Metadata, mapped.Metadata);
        foreach (var step in scenario.Steps) {
            string targetId;
            if (step.Kind == TopologyScenarioStepKind.Node) {
                if (!ids.TryNode(step.Id, out targetId)) continue;
            } else {
                if (!ids.TryEdge(step.Id, out targetId)) continue;
            }
            var mappedStep = new VisualArtifactInterchangeScenarioStep {
                TargetId = targetId,
                Kind = step.Kind.ToString(),
                Label = step.Label,
                Description = step.Description,
                DurationMilliseconds = step.DurationMilliseconds
            };
            Copy(step.Metadata, mappedStep.Metadata);
            mapped.Steps.Add(mappedStep);
        }
        return mapped.Steps.Count == 0 ? null : mapped;
    }

    private static string InvariantNumber(double value) => value.ToString("G17", CultureInfo.InvariantCulture);

    private static string InvariantNumbers(IEnumerable<double> values) {
        var parts = new List<string>();
        foreach (double value in values) parts.Add(InvariantNumber(value));
        return string.Join(",", parts);
    }

    private static string InvariantWaypoints(TopologyEdge edge) {
        var parts = new List<string>();
        foreach (var point in edge.Waypoints) parts.Add(InvariantNumber(point.X) + "," + InvariantNumber(point.Y));
        return string.Join(";", parts);
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

    private static Dictionary<string, string> Copy(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, string> target) {
        var projectedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        var pairs = new List<KeyValuePair<string, string>>(source);
        foreach (var pair in pairs) {
            if (pair.Key.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters) continue;
            target[pair.Key] = pair.Value;
            projectedKeys[pair.Key] = pair.Key;
        }
        foreach (var pair in pairs) {
            if (pair.Key.Length <= VisualArtifactInterchangeValidation.MaximumIdCharacters) continue;
            string key = AllocateMetadataKey(target, pair.Key);
            target[key] = pair.Value;
            projectedKeys[pair.Key] = key;
        }
        return projectedKeys;
    }

    private static void CopyMissing(IEnumerable<KeyValuePair<string, string>> source, IDictionary<string, string> target, IReadOnlyDictionary<string, string> reservedSourceKeys) {
        var pairs = new List<KeyValuePair<string, string>>(source);
        foreach (var pair in pairs) {
            if (pair.Key.Length > VisualArtifactInterchangeValidation.MaximumIdCharacters || reservedSourceKeys.ContainsKey(pair.Key)) continue;
            if (target.ContainsKey(pair.Key)) target[AllocateMetadataKey(target, pair.Key)] = pair.Value;
            else target[pair.Key] = pair.Value;
        }
        foreach (var pair in pairs) {
            if (pair.Key.Length <= VisualArtifactInterchangeValidation.MaximumIdCharacters || reservedSourceKeys.ContainsKey(pair.Key)) continue;
            target[AllocateMetadataKey(target, pair.Key)] = pair.Value;
        }
    }

    private static void CopyWithPrefix(
        IEnumerable<KeyValuePair<string, string>> source,
        IDictionary<string, string> target,
        string prefix,
        IReadOnlyDictionary<string, string> directMetadataKeys) {
        foreach (var pair in source) {
            string sourceKey = prefix + pair.Key;
            if (directMetadataKeys.TryGetValue(sourceKey, out string? projectedKey)) target[projectedKey] = pair.Value;
            else target[AllocateMetadataKey(target, sourceKey)] = pair.Value;
        }
    }

    private static string AllocateMetadataKey(IDictionary<string, string> target, string sourceKey) {
        var ordinal = 1;
        string candidate = BoundedGeneratedId(sourceKey, "metadata-key", ordinal);
        while (target.ContainsKey(candidate)) {
            ordinal++;
            candidate = BoundedGeneratedId(sourceKey, "metadata-key", ordinal);
        }
        return candidate;
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
        private readonly Dictionary<string, string> _scenarios = new(StringComparer.Ordinal);
        private readonly HashSet<string> _usedScenarios = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Dictionary<string, string>> _ports = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> _usedPorts = new(StringComparer.Ordinal);

        public string AddGroup(string sourceId) => Add(_groups, sourceId, "group");
        public string AddNode(string sourceId) => Add(_nodes, sourceId, "node");
        public string AddNodeOccurrence(string sourceId) {
            string allocated = Allocate(sourceId, "node");
            if (!_nodes.ContainsKey(sourceId)) _nodes.Add(sourceId, allocated);
            return allocated;
        }
        public string AddEdge(string sourceId) => Add(_edges, sourceId, "edge");
        public string AddScenario(string sourceId) {
            string allocated = AllocateLocal(_usedScenarios, sourceId, "scenario");
            _scenarios.Add(sourceId, allocated);
            return allocated;
        }
        public string AddAnnotation(string sourceId) => Allocate(sourceId, "annotation");
        public string AddPort(string nodeSourceId, string sourceId) {
            if (!_ports.TryGetValue(nodeSourceId, out var ports)) {
                ports = new Dictionary<string, string>(StringComparer.Ordinal);
                _ports.Add(nodeSourceId, ports);
                _usedPorts.Add(nodeSourceId, new HashSet<string>(StringComparer.Ordinal));
            }
            string allocated = AllocateLocal(_usedPorts[nodeSourceId], sourceId, "port");
            ports.Add(sourceId, allocated);
            return allocated;
        }
        public string Group(string sourceId) => _groups[sourceId];
        public string Node(string sourceId) => _nodes[sourceId];
        public string Edge(string sourceId) => _edges[sourceId];
        public string Port(string nodeSourceId, string sourceId) => _ports[nodeSourceId][sourceId];
        public bool TryNode(string sourceId, out string mappedId) => _nodes.TryGetValue(sourceId, out mappedId!);
        public bool TryEdge(string sourceId, out string mappedId) => _edges.TryGetValue(sourceId, out mappedId!);
        public string? OptionalPort(string nodeSourceId, string? sourceId) =>
            !string.IsNullOrWhiteSpace(sourceId) && _ports.TryGetValue(nodeSourceId, out var ports) && ports.TryGetValue(sourceId!, out string? mappedId)
                ? mappedId
                : null;
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
            string sourceCandidate = BoundedGeneratedId(sourceId, category);
            if (_used.Add(sourceCandidate)) return sourceCandidate;
            string preferred = category + "-" + sourceId;
            string candidate = BoundedGeneratedId(preferred, category);
            int ordinal = 2;
            while (!_used.Add(candidate)) {
                candidate = BoundedGeneratedId(preferred, category, ordinal);
                ordinal++;
            }
            return candidate;
        }

        private static string AllocateLocal(ISet<string> used, string sourceId, string category) {
            string sourceCandidate = BoundedGeneratedId(sourceId, category);
            if (used.Add(sourceCandidate)) return sourceCandidate;
            string preferred = category + "-" + sourceId;
            string candidate = BoundedGeneratedId(preferred, category);
            int ordinal = 2;
            while (!used.Add(candidate)) {
                candidate = BoundedGeneratedId(preferred, category, ordinal);
                ordinal++;
            }
            return candidate;
        }
    }
}
