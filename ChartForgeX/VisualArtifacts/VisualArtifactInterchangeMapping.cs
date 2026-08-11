using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Topology;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Creates portable semantic interchange envelopes from ChartForgeX visual artifacts.</summary>
public static partial class VisualArtifactInterchangeMapping {
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
        artifactMetadataKeys = Copy(artifact.Metadata, envelope.Extensions);
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
        envelope.Topology = new VisualArtifactInterchangeTopologyArtifact {
            LayoutMode = prepared.LayoutMode,
            LayoutDirection = prepared.LayoutDirection
        };
        envelope.Family = VisualArtifactInterchangeFamily.Topology;
        envelope.Width = prepared.Viewport.Width;
        envelope.Height = prepared.Viewport.Height;
        envelope.Presentation = new VisualArtifactInterchangePresentation {
            Theme = MapTheme(prepared.Theme ?? TopologyTheme.Light()),
            Legend = MapLegend(prepared.Legend)
        };
        if (prepared.LayoutMode == TopologyLayoutMode.Geographic) {
            envelope.Presentation.MapViewport = new VisualArtifactInterchangeMapViewport {
                Name = prepared.MapViewport.Name,
                Projection = "Equirectangular",
                MinimumLongitude = prepared.MapViewport.MinimumLongitude,
                MaximumLongitude = prepared.MapViewport.MaximumLongitude,
                MinimumLatitude = prepared.MapViewport.MinimumLatitude,
                MaximumLatitude = prepared.MapViewport.MaximumLatitude
            };
        }

        foreach (var group in prepared.Groups) envelope.Groups.Add(MapGroup(group, ids.Group(group.Id), "TopologyGroup"));
        foreach (var node in prepared.Nodes) {
            TopologyNodeDisplayMode displayMode = EffectiveNodeDisplayMode(node, options);
            envelope.Nodes.Add(MapNode(node, ids.Node(node.Id), ids.OptionalGroup(node.GroupId), ids, displayMode,
                options.IncludeStatusBadges && ShouldRenderNodeStatusBadge(node, options)));
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
                ids,
                options));
        }
        foreach (var scenario in prepared.Scenarios) {
            VisualArtifactInterchangeScenario? mappedScenario = MapScenario(scenario, ids);
            if (mappedScenario != null) envelope.Scenarios.Add(mappedScenario);
        }
    }

    private static void MapFlow(VisualArtifactInterchangeEnvelope envelope, FlowArtifact flow, IReadOnlyDictionary<string, string> artifactMetadataKeys) {
        envelope.Id = BoundedGeneratedId(flow.Id, "flow");
        envelope.Title = flow.Title;
        envelope.Subtitle = flow.Subtitle;
        envelope.Flow = new VisualArtifactInterchangeFlowArtifact {
            LayoutMode = flow.LayoutMode,
            LayoutDirection = flow.Direction
        };
        envelope.Family = VisualArtifactInterchangeFamily.Flow;
        CopyMissing(flow.Metadata, envelope.Extensions, artifactMetadataKeys);

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
                Role = VisualArtifactInterchangeGroupRole.FlowLane,
                Kind = "FlowLane",
                Label = lane.Label,
                Status = lane.Status.ToString(),
                Color = lane.Color,
                X = preparedGroup?.X,
                Y = preparedGroup?.Y,
                Width = preparedGroup?.Width,
                Height = preparedGroup?.Height
            };
            Copy(lane.Metadata, group.Extensions);
            envelope.Groups.Add(group);
        }

        foreach (var step in flow.Steps) {
            preparedNodes.TryGetValue(step.Id, out var preparedNode);
            var node = new VisualArtifactInterchangeNode {
                Id = ids.Node(step.Id),
                Role = VisualArtifactInterchangeNodeRole.FlowStep,
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
                Height = preparedNode?.Height ?? step.Height,
                Flow = new VisualArtifactInterchangeFlowNode { Kind = step.Kind }
            };
            Copy(step.Metadata, node.Extensions);
            envelope.Nodes.Add(node);
        }

        for (var index = 0; index < flow.Connectors.Count; index++) {
            var connector = flow.Connectors[index];
            var edge = new VisualArtifactInterchangeEdge {
                Id = ids.Edge(connector.Id),
                Role = VisualArtifactInterchangeEdgeRole.FlowConnector,
                Kind = connector.Kind.ToString(),
                SourceId = ids.Node(connector.SourceId),
                TargetId = ids.Node(connector.TargetId),
                Label = connector.Label,
                Status = connector.Status.ToString(),
                Color = connector.Color,
                Order = index,
                Flow = new VisualArtifactInterchangeFlowEdge { Kind = connector.Kind, Direction = connector.Direction }
            };
            Copy(connector.Metadata, edge.Extensions);
            envelope.Edges.Add(edge);
        }
    }

    private static void MapSequence(VisualArtifactInterchangeEnvelope envelope, SequenceArtifact sequence, IReadOnlyDictionary<string, string> artifactMetadataKeys) {
        envelope.Family = VisualArtifactInterchangeFamily.Sequence;
        envelope.Sequence = new VisualArtifactInterchangeSequenceArtifact();
        var ids = new InterchangeIdScope();
        var participantIds = new List<string>();
        foreach (var participant in sequence.Participants) participantIds.Add(ids.AddNodeOccurrence(participant.Id));
        envelope.Id = BoundedGeneratedId(sequence.Id, "sequence");
        envelope.Title = sequence.Title;
        envelope.Subtitle = sequence.Subtitle;
        VisualArtifactSize naturalSize = SequenceArtifactRendering.CalculateNaturalSize(sequence);
        envelope.Width = naturalSize.Width;
        envelope.Height = naturalSize.Height;
        CopyMissing(sequence.Metadata, envelope.Extensions, artifactMetadataKeys);

        for (var index = 0; index < sequence.Participants.Count; index++) {
            var participant = sequence.Participants[index];
            var node = new VisualArtifactInterchangeNode {
                Id = participantIds[index],
                Role = VisualArtifactInterchangeNodeRole.SequenceParticipant,
                Kind = participant.Kind.ToString(),
                Label = participant.Label,
                Href = SafeHref(participant.Href),
                Sequence = new VisualArtifactInterchangeSequenceNode {
                    Kind = participant.Kind,
                    Order = index,
                    IsImplicit = participant.IsImplicit
                }
            };
            Copy(participant.Metadata, node.Extensions);
            envelope.Nodes.Add(node);
        }

        for (var index = 0; index < sequence.Messages.Count; index++) {
            var message = sequence.Messages[index];
            if (!ids.TryNode(message.SourceId, out string sourceId) || !ids.TryNode(message.TargetId, out string targetId)) continue;
            string edgeId = ids.AddEdge("message-" + (index + 1).ToString(CultureInfo.InvariantCulture));
            var edge = new VisualArtifactInterchangeEdge {
                Id = edgeId,
                Role = VisualArtifactInterchangeEdgeRole.SequenceMessage,
                Kind = "SequenceMessage",
                SourceId = sourceId,
                TargetId = targetId,
                Label = message.Text,
                Order = index,
                Sequence = new VisualArtifactInterchangeSequenceEdge {
                    Kind = message.Kind,
                    LineStyle = message.LineStyle,
                    ActivatesTarget = message.ActivatesTarget,
                    Deactivates = message.Deactivates
                }
            };
            Copy(message.Metadata, edge.Extensions);
            envelope.Edges.Add(edge);
        }
        for (var index = 0; index < sequence.Notes.Count; index++) {
            var note = sequence.Notes[index];
            int stepIndex = Math.Max(0, note.StepIndex);
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = ids.AddAnnotation("note-" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                Role = VisualArtifactInterchangeAnnotationRole.SequenceNote,
                Kind = "SequenceNote",
                Text = note.Text,
                Placement = note.Placement.ToString(),
                StartIndex = stepIndex,
                EndIndex = stepIndex,
                Sequence = new VisualArtifactInterchangeSequenceAnnotation { NotePlacement = note.Placement }
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
                Role = VisualArtifactInterchangeAnnotationRole.SequenceActivation,
                Kind = activation.Active ? "SequenceActivation" : "SequenceDeactivation",
                Text = string.Empty,
                StartIndex = stepIndex,
                EndIndex = stepIndex,
                Sequence = new VisualArtifactInterchangeSequenceAnnotation { ActivationState = activation.Active }
            };
            annotation.TargetIds.Add(targetId);
            Copy(activation.Metadata, annotation.Extensions);
            envelope.Annotations.Add(annotation);
        }

        for (var index = 0; index < sequence.Blocks.Count; index++) {
            var block = sequence.Blocks[index];
            int start = Math.Max(0, block.StartStepIndex);
            int end = Math.Max(start, block.EndStepIndex);
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = ids.AddAnnotation("block-" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                Role = VisualArtifactInterchangeAnnotationRole.SequenceBlock,
                Kind = "SequenceBlock:" + block.Kind,
                Text = block.Text,
                StartIndex = start,
                EndIndex = block.IsEmpty ? null : end,
                Sequence = new VisualArtifactInterchangeSequenceAnnotation {
                    BlockKind = block.Kind,
                    IsEmpty = block.IsEmpty
                }
            };
            envelope.Annotations.Add(annotation);
        }
        for (var index = 0; index < sequence.Branches.Count; index++) {
            var branch = sequence.Branches[index];
            int start = Math.Max(0, branch.StartStepIndex);
            int end = Math.Max(start, branch.EndStepIndex);
            var annotation = new VisualArtifactInterchangeAnnotation {
                Id = ids.AddAnnotation("branch-" + (index + 1).ToString(CultureInfo.InvariantCulture)),
                Role = VisualArtifactInterchangeAnnotationRole.SequenceBranch,
                Kind = "SequenceBranch:" + branch.Kind,
                Text = branch.Text,
                Placement = branch.ParentKind.ToString(),
                StartIndex = start,
                EndIndex = branch.IsEmpty ? null : end,
                Sequence = new VisualArtifactInterchangeSequenceAnnotation {
                    ParentBlockKind = branch.ParentKind,
                    BranchKind = branch.Kind,
                    Depth = branch.Depth,
                    IsEmpty = branch.IsEmpty
                }
            };
            envelope.Annotations.Add(annotation);
        }
    }

    private static VisualArtifactInterchangeGroup MapGroup(TopologyGroup group, string id, string kind) {
        var mapped = new VisualArtifactInterchangeGroup {
            Id = id,
            Role = VisualArtifactInterchangeGroupRole.TopologyGroup,
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
            Height = group.Height,
            Topology = MapGroupPresentation(group)
        };
        Copy(group.Metadata, mapped.Extensions);
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
        TopologyNodeDisplayMode displayMode,
        bool showStatusBadge) {
        var mapped = new VisualArtifactInterchangeNode {
            Id = id,
            Role = VisualArtifactInterchangeNodeRole.TopologyNode,
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
            Height = node.Height,
            Topology = MapNodePresentation(node, displayMode, showStatusBadge)
        };
        Copy(node.Metadata, mapped.Extensions);
        CopyMetrics(node.Metrics, mapped.Metrics);
        foreach (var port in node.Ports) {
            var mappedPort = new VisualArtifactInterchangePort { Id = ids.Port(node.Id, port.Id), Side = port.Side, Offset = port.Offset, Label = port.Label };
            Copy(port.Metadata, mappedPort.Extensions);
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
            Copy(detail.Metadata, mappedDetail.Extensions);
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
        InterchangeIdScope ids,
        TopologyRenderOptions options) {
        var mapped = new VisualArtifactInterchangeEdge {
            Id = id,
            Role = VisualArtifactInterchangeEdgeRole.TopologyEdge,
            Kind = edge.Kind.ToString(),
            SourceId = sourceId,
            TargetId = targetId,
            Label = EdgeLabel(edge, options.EdgeLabelMetricKey, edge.Label),
            SecondaryLabel = EdgeLabel(edge, options.EdgeSecondaryLabelMetricKey, edge.SecondaryLabel),
            TertiaryLabel = EdgeLabel(edge, options.EdgeTertiaryLabelMetricKey, edge.TertiaryLabel),
            SourceLabel = edge.SourceLabel,
            TargetLabel = edge.TargetLabel,
            Status = edge.Status.ToString(),
            SourcePortId = sourcePortId,
            TargetPortId = targetPortId,
            Color = edge.Color,
            Href = SafeHref(edge.Href),
            Tooltip = edge.Tooltip,
            Order = order,
            Topology = MapEdgePresentation(edge, ids)
        };
        Copy(edge.Metadata, mapped.Extensions);
        CopyMetrics(edge.Metrics, mapped.Metrics);
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
        Copy(scenario.Metadata, mapped.Extensions);
        foreach (var step in scenario.Steps) {
            string targetId;
            if (step.Kind == TopologyScenarioStepKind.Node) {
                if (!ids.TryNode(step.Id, out targetId)) continue;
            } else {
                if (!ids.TryEdge(step.Id, out targetId)) continue;
            }
            var mappedStep = new VisualArtifactInterchangeScenarioStep {
                TargetId = targetId,
                Kind = step.Kind,
                Label = step.Label,
                Description = step.Description,
                DurationMilliseconds = step.DurationMilliseconds
            };
            Copy(step.Metadata, mappedStep.Extensions);
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

    private static void CopyMetrics(IEnumerable<KeyValuePair<string, string>> source, ICollection<VisualArtifactInterchangeMetric> target) {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (VisualArtifactInterchangeMetric metric in target) names.Add(metric.Name);
        foreach (var pair in source) {
            var ordinal = 1;
            string name = BoundedGeneratedId(pair.Key, "metric-name", ordinal);
            while (!names.Add(name)) {
                ordinal++;
                name = BoundedGeneratedId(pair.Key, "metric-name", ordinal);
            }
            target.Add(new VisualArtifactInterchangeMetric { Name = name, Value = pair.Value });
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
