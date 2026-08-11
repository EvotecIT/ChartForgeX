using System;
using System.Collections.Generic;
using ChartForgeX.Topology;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.VisualArtifacts;

internal static class VisualArtifactInterchangeValidation {
    internal const int MaximumJsonCharacters = VisualArtifactInterchangeEnvelope.MaximumJsonCharacters;
    // Keep model serialization and parsed JSON materialization on the same
    // public aggregate budget, independently from the byte/character limits.
    // This prevents compact unknown structures from allocating millions of
    // GeoJsonValue instances before schema projection discards them.
    internal const int MaximumJsonValues = VisualArtifactInterchangeEnvelope.MaximumJsonValues;
    internal const int MaximumGroups = 10000;
    internal const int MaximumNodes = 50000;
    internal const int MaximumEdges = 100000;
    internal const int MaximumAnnotations = 50000;
    internal const int MaximumScenarios = 10000;
    internal const int MaximumScenarioSteps = MaximumEdges;
    internal const int MaximumPortsPerNode = 256;
    internal const int MaximumDetailsPerNode = 1024;
    internal const int MaximumMetricsPerEntity = 1024;
    internal const int MaximumTargetIdsPerAnnotation = MaximumNodes;
    // Two independently bounded source metadata collections can share the
    // projected bag, with a small reserve for typed interchange fields.
    internal const int MaximumExtensionEntries = 544;
    internal const int MaximumLegendItems = 10000;
    internal const int MaximumDashPatternValues = 64;
    internal const int MaximumWaypointsPerEdge = 10000;
    internal const int MaximumIdCharacters = 512;
    private const int MaximumTextCharacters = 65536;

    public static void Validate(VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        if (!Enum.IsDefined(typeof(VisualArtifactKind), envelope.Kind)) throw new ArgumentOutOfRangeException(nameof(envelope), envelope.Kind, "Interchange artifact kind must be defined.");
        Defined(envelope.Family, "interchange family", envelope);
        if (!Enum.IsDefined(typeof(VisualArtifactSourceLanguage), envelope.SourceLanguage)) throw new ArgumentOutOfRangeException(nameof(envelope), envelope.SourceLanguage, "Interchange source language must be defined.");
        RequiredId(envelope.Id, "artifact id");
        Text(envelope.Title, "artifact title");
        Text(envelope.Subtitle, "artifact subtitle");
        OptionalText(envelope.AccessibleName, "accessible name");
        OptionalText(envelope.AccessibleDescription, "accessible description");
        OptionalText(envelope.Language, "language");
        PositiveOptional(envelope.Width, "artifact width");
        PositiveOptional(envelope.Height, "artifact height");
        Extensions(envelope.Extensions, "artifact extensions");
        ValidateArtifactSemantics(envelope);
        Presentation(envelope.Presentation, envelope);
        Count(envelope.Groups.Count, MaximumGroups, "groups");
        Count(envelope.Nodes.Count, MaximumNodes, "nodes");
        Count(envelope.Edges.Count, MaximumEdges, "edges");
        Count(envelope.Scenarios.Count, MaximumScenarios, "scenarios");
        Count(envelope.Annotations.Count, MaximumAnnotations, "annotations");

        var entityIds = new HashSet<string>(StringComparer.Ordinal);
        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in envelope.Groups) {
            RequiredId(group.Id, "group id");
            if (!groupIds.Add(group.Id)) throw new ArgumentException("Interchange group ids must be unique: " + group.Id + ".", nameof(envelope));
            UniqueEntityId(entityIds, group.Id, "group", envelope);
            Text(group.Kind, "group kind");
            Text(group.Label, "group label");
            OptionalText(group.Subtitle, "group subtitle");
            OptionalText(group.Status, "group status");
            OptionalText(group.Color, "group color");
            SafeLink(group.Href, "group href", envelope);
            OptionalText(group.Tooltip, "group tooltip");
            FiniteOptional(group.X, "group x");
            FiniteOptional(group.Y, "group y");
            NonNegativeOptional(group.Width, "group width");
            NonNegativeOptional(group.Height, "group height");
            Extensions(group.Extensions, "group extensions");
            ValidateGroupSemantics(group, envelope);
        }

        var nodeIds = new HashSet<string>(StringComparer.Ordinal);
        var portsByNodeId = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var node in envelope.Nodes) {
            RequiredId(node.Id, "node id");
            if (!nodeIds.Add(node.Id)) throw new ArgumentException("Interchange node ids must be unique: " + node.Id + ".", nameof(envelope));
            UniqueEntityId(entityIds, node.Id, "node", envelope);
            Text(node.Kind, "node kind");
            Text(node.Label, "node label");
            OptionalText(node.Subtitle, "node subtitle");
            OptionalText(node.GroupId, "node group id");
            OptionalText(node.Status, "node status");
            OptionalText(node.IconId, "node icon id");
            OptionalText(node.Symbol, "node symbol");
            OptionalText(node.Badge, "node badge");
            OptionalText(node.Color, "node color");
            OptionalText(node.BackgroundColor, "node background color");
            SafeLink(node.Href, "node href", envelope);
            OptionalText(node.Tooltip, "node tooltip");
            FiniteOptional(node.X, "node x");
            FiniteOptional(node.Y, "node y");
            PositiveOptional(node.Width, "node width");
            PositiveOptional(node.Height, "node height");
            Extensions(node.Extensions, "node extensions");
            Metrics(node.Metrics, "node metrics");
            ValidateNodeSemantics(node, envelope);
            Count(node.Ports.Count, MaximumPortsPerNode, "node ports");
            Count(node.Details.Count, MaximumDetailsPerNode, "node details");
            var portIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var port in node.Ports) {
                RequiredId(port.Id, "port id");
                if (!portIds.Add(port.Id)) throw new ArgumentException("Interchange port ids must be unique within a node: " + port.Id + ".", nameof(envelope));
                Defined(port.Side, "port side", envelope);
                Finite(port.Offset, "port offset");
                if (port.Offset < 0 || port.Offset > 1) throw new ArgumentOutOfRangeException(nameof(envelope), port.Offset, "Port offsets must be between zero and one.");
                OptionalText(port.Label, "port label");
                Extensions(port.Extensions, "port extensions");
            }
            portsByNodeId.Add(node.Id, portIds);
            foreach (var detail in node.Details) {
                Text(detail.Label, "detail label");
                Text(detail.Value, "detail value");
                OptionalText(detail.IconId, "detail icon id");
                OptionalText(detail.Status, "detail status");
                OptionalText(detail.Color, "detail color");
                Extensions(detail.Extensions, "detail extensions");
            }
        }

        foreach (var node in envelope.Nodes) {
            if (!string.IsNullOrWhiteSpace(node.GroupId) && !groupIds.Contains(node.GroupId!)) throw new ArgumentException("Interchange node '" + node.Id + "' references unknown group '" + node.GroupId + "'.", nameof(envelope));
        }

        var edgeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var edge in envelope.Edges) {
            RequiredId(edge.Id, "edge id");
            if (!edgeIds.Add(edge.Id)) throw new ArgumentException("Interchange edge ids must be unique: " + edge.Id + ".", nameof(envelope));
            UniqueEntityId(entityIds, edge.Id, "edge", envelope);
            RequiredId(edge.SourceId, "edge source id");
            RequiredId(edge.TargetId, "edge target id");
            if (!nodeIds.Contains(edge.SourceId)) throw new ArgumentException("Interchange edge '" + edge.Id + "' references unknown source node '" + edge.SourceId + "'.", nameof(envelope));
            if (!nodeIds.Contains(edge.TargetId)) throw new ArgumentException("Interchange edge '" + edge.Id + "' references unknown target node '" + edge.TargetId + "'.", nameof(envelope));
            Text(edge.Kind, "edge kind");
            OptionalText(edge.Label, "edge label");
            OptionalText(edge.SecondaryLabel, "edge secondary label");
            OptionalText(edge.TertiaryLabel, "edge tertiary label");
            OptionalText(edge.SourceLabel, "edge source label");
            OptionalText(edge.TargetLabel, "edge target label");
            OptionalText(edge.Status, "edge status");
            OptionalText(edge.SourcePortId, "edge source port id");
            OptionalText(edge.TargetPortId, "edge target port id");
            if (!string.IsNullOrWhiteSpace(edge.SourcePortId) && !portsByNodeId[edge.SourceId].Contains(edge.SourcePortId!)) throw new ArgumentException("Interchange edge '" + edge.Id + "' references unknown source port '" + edge.SourcePortId + "' on node '" + edge.SourceId + "'.", nameof(envelope));
            if (!string.IsNullOrWhiteSpace(edge.TargetPortId) && !portsByNodeId[edge.TargetId].Contains(edge.TargetPortId!)) throw new ArgumentException("Interchange edge '" + edge.Id + "' references unknown target port '" + edge.TargetPortId + "' on node '" + edge.TargetId + "'.", nameof(envelope));
            OptionalText(edge.Color, "edge color");
            SafeLink(edge.Href, "edge href", envelope);
            OptionalText(edge.Tooltip, "edge tooltip");
            if (edge.Order < 0) throw new ArgumentOutOfRangeException(nameof(envelope), edge.Order, "Edge order must not be negative.");
            Extensions(edge.Extensions, "edge extensions");
            Metrics(edge.Metrics, "edge metrics");
            ValidateEdgeSemantics(edge, nodeIds, envelope);
        }

        var scenarioIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scenario in envelope.Scenarios) {
            RequiredId(scenario.Id, "scenario id");
            if (!scenarioIds.Add(scenario.Id)) throw new ArgumentException("Interchange scenario ids must be unique: " + scenario.Id + ".", nameof(envelope));
            if (string.IsNullOrWhiteSpace(scenario.Label)) throw new ArgumentException("Interchange scenario labels are required.", nameof(envelope));
            Text(scenario.Label, "scenario label");
            OptionalText(scenario.Description, "scenario description");
            OptionalText(scenario.Color, "scenario color");
            if (scenario.PlaybackDelayMilliseconds < 200 || scenario.PlaybackDelayMilliseconds > 60000) {
                throw new ArgumentOutOfRangeException(nameof(envelope), scenario.PlaybackDelayMilliseconds, "Scenario playback delays must be between 200 and 60000 milliseconds.");
            }
            Count(scenario.Steps.Count, MaximumScenarioSteps, "scenario steps");
            if (scenario.Steps.Count == 0) throw new ArgumentException("Interchange scenarios must contain at least one step.", nameof(envelope));
            Extensions(scenario.Extensions, "scenario extensions");
            foreach (var step in scenario.Steps) {
                RequiredId(step.TargetId, "scenario step target id");
                Defined(step.Kind, "scenario step kind", envelope);
                bool nodeStep = step.Kind == TopologyScenarioStepKind.Node;
                bool edgeStep = step.Kind == TopologyScenarioStepKind.Edge;
                if (nodeStep && !nodeIds.Contains(step.TargetId)) throw new ArgumentException("Interchange scenario references unknown node '" + step.TargetId + "'.", nameof(envelope));
                if (edgeStep && !edgeIds.Contains(step.TargetId)) throw new ArgumentException("Interchange scenario references unknown edge '" + step.TargetId + "'.", nameof(envelope));
                OptionalText(step.Label, "scenario step label");
                OptionalText(step.Description, "scenario step description");
                if (step.DurationMilliseconds.HasValue && (step.DurationMilliseconds.Value < 200 || step.DurationMilliseconds.Value > 60000)) {
                    throw new ArgumentOutOfRangeException(nameof(envelope), step.DurationMilliseconds.Value, "Scenario step durations must be between 200 and 60000 milliseconds.");
                }
                Extensions(step.Extensions, "scenario step extensions");
            }
        }

        var annotationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var annotation in envelope.Annotations) {
            RequiredId(annotation.Id, "annotation id");
            if (!annotationIds.Add(annotation.Id)) throw new ArgumentException("Interchange annotation ids must be unique: " + annotation.Id + ".", nameof(envelope));
            UniqueEntityId(entityIds, annotation.Id, "annotation", envelope);
            Text(annotation.Kind, "annotation kind");
            Text(annotation.Text, "annotation text");
            OptionalText(annotation.Placement, "annotation placement");
            Count(annotation.TargetIds.Count, MaximumTargetIdsPerAnnotation, "annotation target ids");
            if (annotation.StartIndex is < 0) throw new ArgumentOutOfRangeException(nameof(envelope), annotation.StartIndex, "Annotation start index must not be negative.");
            if (annotation.EndIndex is < 0) throw new ArgumentOutOfRangeException(nameof(envelope), annotation.EndIndex, "Annotation end index must not be negative.");
            if (annotation.StartIndex.HasValue && annotation.EndIndex.HasValue && annotation.EndIndex.Value < annotation.StartIndex.Value) throw new ArgumentException("Annotation end index must not precede its start index.", nameof(envelope));
            foreach (var targetId in annotation.TargetIds) {
                RequiredId(targetId, "annotation target id");
                if (!nodeIds.Contains(targetId)) throw new ArgumentException("Interchange annotation '" + annotation.Id + "' references unknown node '" + targetId + "'.", nameof(envelope));
            }
            Extensions(annotation.Extensions, "annotation extensions");
            ValidateAnnotationSemantics(annotation, envelope);
        }
        VisualArtifactInterchangeValueBudget.Validate(envelope);
    }

    private static void ValidateArtifactSemantics(VisualArtifactInterchangeEnvelope envelope) {
        int roots = (envelope.Topology == null ? 0 : 1) + (envelope.Flow == null ? 0 : 1) + (envelope.Sequence == null ? 0 : 1);
        switch (envelope.Family) {
            case VisualArtifactInterchangeFamily.None:
                if (roots != 0) throw new ArgumentException("An unstructured envelope cannot carry a typed artifact-family root.", nameof(envelope));
                break;
            case VisualArtifactInterchangeFamily.Topology:
                if (roots != 1 || envelope.Topology == null) throw new ArgumentException("A topology family requires only topology artifact semantics.", nameof(envelope));
                Defined(envelope.Topology.LayoutMode, "topology layout mode", envelope);
                Defined(envelope.Topology.LayoutDirection, "topology layout direction", envelope);
                break;
            case VisualArtifactInterchangeFamily.Flow:
                if (roots != 1 || envelope.Flow == null) throw new ArgumentException("A flow family requires only flow artifact semantics.", nameof(envelope));
                Defined(envelope.Flow.LayoutMode, "flow layout mode", envelope);
                Defined(envelope.Flow.LayoutDirection, "flow layout direction", envelope);
                break;
            case VisualArtifactInterchangeFamily.Sequence:
                if (roots != 1 || envelope.Sequence == null) throw new ArgumentException("A sequence family requires only sequence artifact semantics.", nameof(envelope));
                break;
        }

        VisualArtifactInterchangeGroupRole expectedGroupRole = envelope.Family switch {
            VisualArtifactInterchangeFamily.Topology => VisualArtifactInterchangeGroupRole.TopologyGroup,
            VisualArtifactInterchangeFamily.Flow => VisualArtifactInterchangeGroupRole.FlowLane,
            _ => VisualArtifactInterchangeGroupRole.Unspecified
        };
        foreach (VisualArtifactInterchangeGroup group in envelope.Groups) {
            if (group.Role != expectedGroupRole) throw new ArgumentException("Interchange group role does not belong to the selected semantic family.", nameof(envelope));
        }

        VisualArtifactInterchangeNodeRole expectedNodeRole = envelope.Family switch {
            VisualArtifactInterchangeFamily.Topology => VisualArtifactInterchangeNodeRole.TopologyNode,
            VisualArtifactInterchangeFamily.Flow => VisualArtifactInterchangeNodeRole.FlowStep,
            VisualArtifactInterchangeFamily.Sequence => VisualArtifactInterchangeNodeRole.SequenceParticipant,
            _ => VisualArtifactInterchangeNodeRole.Unspecified
        };
        foreach (VisualArtifactInterchangeNode node in envelope.Nodes) {
            if (node.Role != expectedNodeRole) throw new ArgumentException("Interchange node role does not belong to the selected semantic family.", nameof(envelope));
        }

        VisualArtifactInterchangeEdgeRole expectedEdgeRole = envelope.Family switch {
            VisualArtifactInterchangeFamily.Topology => VisualArtifactInterchangeEdgeRole.TopologyEdge,
            VisualArtifactInterchangeFamily.Flow => VisualArtifactInterchangeEdgeRole.FlowConnector,
            VisualArtifactInterchangeFamily.Sequence => VisualArtifactInterchangeEdgeRole.SequenceMessage,
            _ => VisualArtifactInterchangeEdgeRole.Unspecified
        };
        foreach (VisualArtifactInterchangeEdge edge in envelope.Edges) {
            if (edge.Role != expectedEdgeRole) throw new ArgumentException("Interchange edge role does not belong to the selected semantic family.", nameof(envelope));
        }
        foreach (VisualArtifactInterchangeAnnotation annotation in envelope.Annotations) {
            if (annotation.Role != VisualArtifactInterchangeAnnotationRole.Unspecified && envelope.Family != VisualArtifactInterchangeFamily.Sequence) {
                throw new ArgumentException("Typed sequence annotation roles require the sequence semantic family.", nameof(envelope));
            }
        }
    }

    private static void UniqueEntityId(ISet<string> ids, string id, string kind, VisualArtifactInterchangeEnvelope envelope) {
        if (!ids.Add(id)) throw new ArgumentException("Interchange " + kind + " id '" + id + "' collides with another diagram entity id.", nameof(envelope));
    }

    private static void SafeLink(string? href, string context, VisualArtifactInterchangeEnvelope envelope) {
        OptionalText(href, context);
        if (!string.IsNullOrWhiteSpace(href) && SafeHref(href) == null) {
            throw new ArgumentException(context + " must be relative or use http, https, mailto, or tel.", nameof(envelope));
        }
    }

    private static void Extensions(IDictionary<string, string> values, string context) {
        Count(values.Count, MaximumExtensionEntries, context + " entries");
        foreach (var pair in values) {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > MaximumIdCharacters) throw new ArgumentException(context + " keys must be non-empty and at most " + MaximumIdCharacters + " characters.");
            Text(pair.Value, context + " value");
        }
    }

    private static void Metrics(IReadOnlyCollection<VisualArtifactInterchangeMetric> metrics, string context) {
        Count(metrics.Count, MaximumMetricsPerEntity, context + " entries");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (VisualArtifactInterchangeMetric metric in metrics) {
            RequiredId(metric.Name, context + " name");
            if (!names.Add(metric.Name)) throw new ArgumentException(context + " names must be unique: " + metric.Name + ".");
            Text(metric.Value, context + " value");
        }
    }

    private static void Presentation(VisualArtifactInterchangePresentation? presentation, VisualArtifactInterchangeEnvelope envelope) {
        if (presentation == null) return;
        if (presentation.Theme != null) {
            Text(presentation.Theme.Background, "theme background");
            Text(presentation.Theme.Foreground, "theme foreground");
            Text(presentation.Theme.MutedForeground, "theme muted foreground");
            Text(presentation.Theme.Card, "theme card");
            Text(presentation.Theme.Surface, "theme surface");
            Text(presentation.Theme.Border, "theme border");
            Text(presentation.Theme.Accent, "theme accent");
            Text(presentation.Theme.Healthy, "theme healthy");
            Text(presentation.Theme.Warning, "theme warning");
            Text(presentation.Theme.Critical, "theme critical");
            Text(presentation.Theme.Unknown, "theme unknown");
            Text(presentation.Theme.Disabled, "theme disabled");
            Text(presentation.Theme.FontFamily, "theme font family");
        }
        if (presentation.MapViewport != null) {
            OptionalText(presentation.MapViewport.Name, "map viewport name");
            Text(presentation.MapViewport.Projection, "map viewport projection");
            Finite(presentation.MapViewport.MinimumLongitude, "map viewport minimum longitude");
            Finite(presentation.MapViewport.MaximumLongitude, "map viewport maximum longitude");
            Finite(presentation.MapViewport.MinimumLatitude, "map viewport minimum latitude");
            Finite(presentation.MapViewport.MaximumLatitude, "map viewport maximum latitude");
            if (presentation.MapViewport.MinimumLongitude < -180 || presentation.MapViewport.MaximumLongitude > 180 ||
                presentation.MapViewport.MinimumLatitude < -90 || presentation.MapViewport.MaximumLatitude > 90) {
                throw new ArgumentOutOfRangeException(nameof(envelope), "Interchange map viewport bounds must remain within longitude [-180, 180] and latitude [-90, 90].");
            }
            if (presentation.MapViewport.MinimumLongitude >= presentation.MapViewport.MaximumLongitude || presentation.MapViewport.MinimumLatitude >= presentation.MapViewport.MaximumLatitude) {
                throw new ArgumentException("Interchange map viewport minimum bounds must be strictly less than maximum bounds.", nameof(envelope));
            }
        }
        if (presentation.Legend != null) {
            OptionalText(presentation.Legend.Title, "legend title");
            Count(presentation.Legend.Items.Count, MaximumLegendItems, "legend items");
            foreach (var item in presentation.Legend.Items) {
                Text(item.Label, "legend item label");
                Defined(item.Kind, "legend item kind", envelope);
                OptionalDefined(item.Status, "legend status", envelope);
                OptionalDefined(item.NodeKind, "legend node kind", envelope);
                OptionalDefined(item.EdgeKind, "legend edge kind", envelope);
                Defined(item.LineStyle, "legend line style", envelope);
                OptionalText(item.Symbol, "legend symbol");
                OptionalText(item.IconId, "legend icon id");
                OptionalText(item.Color, "legend color");
                OptionalText(item.BackgroundColor, "legend background color");
            }
        }
    }

    private static void ValidateGroupSemantics(VisualArtifactInterchangeGroup group, VisualArtifactInterchangeEnvelope envelope) {
        Defined(group.Role, "group role", envelope);
        bool topologyRole = group.Role == VisualArtifactInterchangeGroupRole.TopologyGroup;
        if (topologyRole != (group.Topology != null)) throw new ArgumentException("Topology groups must carry exactly one typed topology semantic record.", nameof(envelope));
        if (group.Topology == null) return;
        Defined(group.Topology.Status, "topology group status", envelope);
        Defined(group.Topology.LayoutPolicy, "topology group layout policy", envelope);
        Defined(group.Topology.AppliedLayoutPolicy, "topology group applied layout policy", envelope);
        FiniteOptional(group.Topology.Longitude, "topology group longitude");
        FiniteOptional(group.Topology.Latitude, "topology group latitude");
        OptionalText(group.Topology.IconId, "topology group icon id");
        OptionalText(group.Topology.Symbol, "topology group symbol");
    }

    private static void ValidateNodeSemantics(VisualArtifactInterchangeNode node, VisualArtifactInterchangeEnvelope envelope) {
        Defined(node.Role, "node role", envelope);
        int semantics = (node.Topology == null ? 0 : 1) + (node.Flow == null ? 0 : 1) + (node.Sequence == null ? 0 : 1);
        bool requiresSemantics = node.Role != VisualArtifactInterchangeNodeRole.Unspecified;
        if (semantics != (requiresSemantics ? 1 : 0)) throw new ArgumentException("Interchange nodes must carry exactly the typed semantic record selected by their role.", nameof(envelope));
        if ((node.Role == VisualArtifactInterchangeNodeRole.TopologyNode) != (node.Topology != null) ||
            (node.Role == VisualArtifactInterchangeNodeRole.FlowStep) != (node.Flow != null) ||
            (node.Role == VisualArtifactInterchangeNodeRole.SequenceParticipant) != (node.Sequence != null)) {
            throw new ArgumentException("Interchange node role and semantic record do not match.", nameof(envelope));
        }
        if (node.Topology != null) {
            Defined(node.Topology.Kind, "topology node kind", envelope);
            Defined(node.Topology.Status, "topology node status", envelope);
            Defined(node.Topology.DisplayMode, "topology node display mode", envelope);
            FiniteOptional(node.Topology.Longitude, "topology node longitude");
            FiniteOptional(node.Topology.Latitude, "topology node latitude");
            if (node.Topology.MaximumLabelCharacters is < 1) throw new ArgumentOutOfRangeException(nameof(envelope), node.Topology.MaximumLabelCharacters, "Maximum label characters must be positive.");
            ValidateArtwork(node.Topology.Artwork, envelope);
        }
        if (node.Flow != null) Defined(node.Flow.Kind, "flow step kind", envelope);
        if (node.Sequence != null) {
            Defined(node.Sequence.Kind, "sequence participant kind", envelope);
            if (node.Sequence.Order < 0) throw new ArgumentOutOfRangeException(nameof(envelope), node.Sequence.Order, "Sequence participant order must not be negative.");
        }
    }

    private static void ValidateArtwork(VisualArtifactInterchangeArtwork? artwork, VisualArtifactInterchangeEnvelope envelope) {
        if (artwork == null) return;
        Defined(artwork.Status, "artwork status", envelope);
        OptionalText(artwork.SvgViewBox, "artwork SVG view box");
        OptionalText(artwork.PreserveAspectRatio, "artwork preserve aspect ratio");
        OptionalText(artwork.SvgBody, "artwork SVG body");
        OptionalText(artwork.SvgPath, "artwork SVG path");
        OptionalText(artwork.PreviewPath, "artwork preview path");
        OptionalText(artwork.ImageHref, "artwork image href");
        if (artwork.Status == VisualArtifactInterchangeArtworkStatus.UnsafeOmitted) {
            if (artwork.SvgBody != null || artwork.SvgPath != null || artwork.PreviewPath != null || artwork.ImageHref != null) {
                throw new ArgumentException("Unsafe omitted artwork cannot carry executable or external artwork content.", nameof(envelope));
            }
            return;
        }
        if (artwork.SvgBody != null && !TopologyIconArtwork.IsSafeSvgFragment(artwork.SvgBody)) throw new ArgumentException("Interchange contains unsafe topology artwork SVG content.", nameof(envelope));
        if (artwork.ImageHref != null && !TopologyIconArtwork.IsSafeImageHref(artwork.ImageHref)) throw new ArgumentException("Interchange contains an unsafe topology artwork image href.", nameof(envelope));
        if (artwork.SvgPath != null && !TopologyIconArtwork.IsSafeAssetPath(artwork.SvgPath)) throw new ArgumentException("Interchange contains an unsafe topology artwork SVG path.", nameof(envelope));
        if (artwork.PreviewPath != null && !TopologyIconArtwork.IsSafeAssetPath(artwork.PreviewPath)) throw new ArgumentException("Interchange contains an unsafe topology artwork preview path.", nameof(envelope));
    }

    private static void ValidateEdgeSemantics(VisualArtifactInterchangeEdge edge, ISet<string> nodeIds, VisualArtifactInterchangeEnvelope envelope) {
        Defined(edge.Role, "edge role", envelope);
        int semantics = (edge.Topology == null ? 0 : 1) + (edge.Flow == null ? 0 : 1) + (edge.Sequence == null ? 0 : 1);
        bool requiresSemantics = edge.Role != VisualArtifactInterchangeEdgeRole.Unspecified;
        if (semantics != (requiresSemantics ? 1 : 0)) throw new ArgumentException("Interchange edges must carry exactly the typed semantic record selected by their role.", nameof(envelope));
        if ((edge.Role == VisualArtifactInterchangeEdgeRole.TopologyEdge) != (edge.Topology != null) ||
            (edge.Role == VisualArtifactInterchangeEdgeRole.FlowConnector) != (edge.Flow != null) ||
            (edge.Role == VisualArtifactInterchangeEdgeRole.SequenceMessage) != (edge.Sequence != null)) {
            throw new ArgumentException("Interchange edge role and semantic record do not match.", nameof(envelope));
        }
        if (edge.Topology != null) {
            Defined(edge.Topology.Kind, "topology edge kind", envelope);
            Defined(edge.Topology.Status, "topology edge status", envelope);
            Defined(edge.Topology.Direction, "topology edge direction", envelope);
            Defined(edge.Topology.SourcePort, "topology edge source port", envelope);
            Defined(edge.Topology.TargetPort, "topology edge target port", envelope);
            Defined(edge.Topology.LineStyle, "topology edge line style", envelope);
            Defined(edge.Topology.Routing, "topology edge routing", envelope);
            Defined(edge.Topology.Emphasis, "topology edge emphasis", envelope);
            OptionalDefined(edge.Topology.SourceMarker, "topology source marker", envelope);
            OptionalDefined(edge.Topology.TargetMarker, "topology target marker", envelope);
            NonNegativeOptional(edge.Topology.StrokeWidth, "topology edge stroke width");
            FiniteOptional(edge.Topology.Opacity, "topology edge opacity");
            if (edge.Topology.Opacity is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(envelope), edge.Topology.Opacity, "Topology edge opacity must be between zero and one.");
            Count(edge.Topology.DashPattern.Count, MaximumDashPatternValues, "topology edge dash pattern");
            foreach (double value in edge.Topology.DashPattern) NonNegative(value, "topology edge dash value");
            Count(edge.Topology.Waypoints.Count, MaximumWaypointsPerEdge, "topology edge waypoints");
            foreach (var point in edge.Topology.Waypoints) ValidatePoint(point, "topology edge waypoint");
            FiniteOptional(edge.Topology.RouteLane, "topology edge route lane");
            Finite(edge.Topology.LabelOffsetX, "topology edge label x offset");
            Finite(edge.Topology.LabelOffsetY, "topology edge label y offset");
            if (edge.Topology.LabelAnchor != null) ValidatePoint(edge.Topology.LabelAnchor, "topology edge label anchor");
            OptionalText(edge.Topology.LabelAnchorNodeId, "topology edge label anchor node id");
            if (edge.Topology.LabelAnchorNodeId != null && !nodeIds.Contains(edge.Topology.LabelAnchorNodeId)) throw new ArgumentException("Topology edge label anchor references an unknown node.", nameof(envelope));
            DefinedFlags(
                edge.Topology.LayoutInference,
                TopologyEdgeLayoutInference.SourcePort | TopologyEdgeLayoutInference.TargetPort | TopologyEdgeLayoutInference.RouteLane,
                "topology edge layout inference",
                envelope);
            PositiveOptional(edge.Topology.PreferredLength, "topology edge preferred length");
            if (edge.Topology.MinimumRankSpan < 0) throw new ArgumentOutOfRangeException(nameof(envelope), edge.Topology.MinimumRankSpan, "Topology edge minimum rank span must not be negative.");
        }
        if (edge.Flow != null) {
            Defined(edge.Flow.Kind, "flow connector kind", envelope);
            Defined(edge.Flow.Direction, "flow connector direction", envelope);
        }
        if (edge.Sequence != null) {
            Defined(edge.Sequence.Kind, "sequence message kind", envelope);
            Defined(edge.Sequence.LineStyle, "sequence message line style", envelope);
        }
    }

    private static void ValidateAnnotationSemantics(VisualArtifactInterchangeAnnotation annotation, VisualArtifactInterchangeEnvelope envelope) {
        Defined(annotation.Role, "annotation role", envelope);
        bool requiresSequence = annotation.Role != VisualArtifactInterchangeAnnotationRole.Unspecified;
        if (requiresSequence != (annotation.Sequence != null)) throw new ArgumentException("Sequence annotations must carry exactly one typed sequence semantic record.", nameof(envelope));
        if (annotation.Sequence == null) return;
        OptionalDefined(annotation.Sequence.NotePlacement, "sequence note placement", envelope);
        OptionalDefined(annotation.Sequence.BlockKind, "sequence block kind", envelope);
        OptionalDefined(annotation.Sequence.ParentBlockKind, "sequence parent block kind", envelope);
        OptionalText(annotation.Sequence.BranchKind, "sequence branch kind");
        if (annotation.Sequence.Depth < 0) throw new ArgumentOutOfRangeException(nameof(envelope), annotation.Sequence.Depth, "Sequence annotation depth must not be negative.");

        bool activation = annotation.Sequence.ActivationState.HasValue;
        bool note = annotation.Sequence.NotePlacement.HasValue;
        bool block = annotation.Sequence.BlockKind.HasValue;
        bool branch = annotation.Sequence.ParentBlockKind.HasValue || annotation.Sequence.BranchKind != null;
        switch (annotation.Role) {
            case VisualArtifactInterchangeAnnotationRole.SequenceActivation:
                if (!activation || note || block || branch || annotation.Sequence.Depth != 0 || annotation.Sequence.IsEmpty) {
                    throw new ArgumentException("Sequence activation annotations may carry only typed activation semantics.", nameof(envelope));
                }
                if (annotation.TargetIds.Count != 1 || !annotation.StartIndex.HasValue ||
                    annotation.EndIndex.HasValue && annotation.EndIndex.Value != annotation.StartIndex.Value) {
                    throw new ArgumentException("Sequence activation annotations require one participant target and one semantic step index.", nameof(envelope));
                }
                break;
            case VisualArtifactInterchangeAnnotationRole.SequenceNote:
                if (activation || !note || block || branch || annotation.Sequence.Depth != 0 || annotation.Sequence.IsEmpty) {
                    throw new ArgumentException("Sequence note annotations may carry only typed note semantics.", nameof(envelope));
                }
                if (!annotation.StartIndex.HasValue || annotation.EndIndex.HasValue && annotation.EndIndex.Value != annotation.StartIndex.Value) {
                    throw new ArgumentException("Sequence note annotations require one semantic step index.", nameof(envelope));
                }
                // A note whose mutated source target no longer resolves is retained as a
                // targetless semantic note. Adapters must diagnose that it cannot be placed.
                break;
            case VisualArtifactInterchangeAnnotationRole.SequenceBlock:
                if (activation || note || !block || branch || annotation.Sequence.Depth != 0) {
                    throw new ArgumentException("Sequence block annotations may carry only typed block semantics.", nameof(envelope));
                }
                ValidateSequenceSpan(annotation, "block", envelope);
                break;
            case VisualArtifactInterchangeAnnotationRole.SequenceBranch:
                if (activation || note || block || !annotation.Sequence.ParentBlockKind.HasValue ||
                    string.IsNullOrWhiteSpace(annotation.Sequence.BranchKind)) {
                    throw new ArgumentException("Sequence branch annotations may carry only typed branch semantics.", nameof(envelope));
                }
                ValidateSequenceSpan(annotation, "branch", envelope);
                break;
        }
    }

    private static void ValidateSequenceSpan(VisualArtifactInterchangeAnnotation annotation, string kind, VisualArtifactInterchangeEnvelope envelope) {
        if (!annotation.StartIndex.HasValue) {
            throw new ArgumentException("Sequence " + kind + " annotations require a start index.", nameof(envelope));
        }
        if (annotation.Sequence!.IsEmpty) {
            if (annotation.EndIndex.HasValue) throw new ArgumentException("Empty sequence " + kind + " annotations must not claim a covered end index.", nameof(envelope));
        } else if (!annotation.EndIndex.HasValue) {
            throw new ArgumentException("Non-empty sequence " + kind + " annotations require an end index.", nameof(envelope));
        }
    }

    private static void ValidatePoint(VisualArtifactInterchangePoint point, string context) {
        if (point == null) throw new ArgumentNullException(context);
        Finite(point.X, context + " x");
        Finite(point.Y, context + " y");
    }

    private static void Defined<TEnum>(TEnum value, string context, VisualArtifactInterchangeEnvelope envelope) where TEnum : struct {
        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(nameof(envelope), value, context + " must be defined.");
    }

    private static void DefinedFlags<TEnum>(TEnum value, TEnum allowedFlags, string context, VisualArtifactInterchangeEnvelope envelope) where TEnum : struct {
        ulong allowed = Convert.ToUInt64(allowedFlags);
        ulong actual = Convert.ToUInt64(value);
        if ((actual & ~allowed) != 0) throw new ArgumentOutOfRangeException(nameof(envelope), value, context + " contains undefined flag bits.");
    }

    private static void OptionalDefined<TEnum>(TEnum? value, string context, VisualArtifactInterchangeEnvelope envelope) where TEnum : struct {
        if (value.HasValue) Defined(value.Value, context, envelope);
    }

    private static void NonNegative(double value, string context) {
        Finite(value, context);
        if (value < 0) throw new ArgumentOutOfRangeException(context, value, context + " must not be negative.");
    }

    private static void Count(int count, int maximum, string context) {
        if (count > maximum) throw new ArgumentOutOfRangeException(context, count, context + " must not exceed " + maximum + ".");
    }

    private static void RequiredId(string value, string context) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(context + " is required.");
        if (value.Length > MaximumIdCharacters) throw new ArgumentException(context + " must not exceed " + MaximumIdCharacters + " characters.");
    }

    private static void Text(string value, string context) {
        if (value == null) throw new ArgumentNullException(context);
        if (value.Length > MaximumTextCharacters) throw new ArgumentException(context + " must not exceed " + MaximumTextCharacters + " characters.");
    }

    private static void OptionalText(string? value, string context) {
        if (value != null) Text(value, context);
    }

    private static void Finite(double value, string context) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(context, value, context + " must be finite.");
    }

    private static void FiniteOptional(double? value, string context) {
        if (value.HasValue) Finite(value.Value, context);
    }

    private static void PositiveOptional(double? value, string context) {
        if (!value.HasValue) return;
        Finite(value.Value, context);
        if (value.Value <= 0) throw new ArgumentOutOfRangeException(context, value.Value, context + " must be greater than zero.");
    }

    private static void NonNegativeOptional(double? value, string context) {
        if (!value.HasValue) return;
        Finite(value.Value, context);
        if (value.Value < 0) throw new ArgumentOutOfRangeException(context, value.Value, context + " must not be negative.");
    }
}
