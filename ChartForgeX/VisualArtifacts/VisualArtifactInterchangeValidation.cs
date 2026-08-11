using System;
using System.Collections.Generic;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.VisualArtifacts;

internal static class VisualArtifactInterchangeValidation {
    internal const int MaximumJsonCharacters = VisualArtifactInterchangeEnvelope.MaximumJsonCharacters;
    internal const int MaximumJsonValues = 500000;
    internal const int MaximumGroups = 10000;
    internal const int MaximumNodes = 50000;
    internal const int MaximumEdges = 100000;
    internal const int MaximumAnnotations = 50000;
    internal const int MaximumPortsPerNode = 256;
    internal const int MaximumDetailsPerNode = 1024;
    internal const int MaximumTargetIdsPerAnnotation = MaximumNodes;
    internal const int MaximumMetadataEntries = 256;
    private const int MaximumIdCharacters = 512;
    private const int MaximumTextCharacters = 65536;

    public static void Validate(VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        if (!Enum.IsDefined(typeof(VisualArtifactKind), envelope.Kind)) throw new ArgumentOutOfRangeException(nameof(envelope), envelope.Kind, "Interchange artifact kind must be defined.");
        if (!Enum.IsDefined(typeof(VisualArtifactSourceLanguage), envelope.SourceLanguage)) throw new ArgumentOutOfRangeException(nameof(envelope), envelope.SourceLanguage, "Interchange source language must be defined.");
        RequiredId(envelope.Id, "artifact id");
        Text(envelope.Title, "artifact title");
        Text(envelope.Subtitle, "artifact subtitle");
        OptionalText(envelope.Layout, "layout");
        OptionalText(envelope.Direction, "direction");
        OptionalText(envelope.AccessibleName, "accessible name");
        OptionalText(envelope.AccessibleDescription, "accessible description");
        OptionalText(envelope.Language, "language");
        PositiveOptional(envelope.Width, "artifact width");
        PositiveOptional(envelope.Height, "artifact height");
        Metadata(envelope.Metadata, "artifact metadata");
        Count(envelope.Groups.Count, MaximumGroups, "groups");
        Count(envelope.Nodes.Count, MaximumNodes, "nodes");
        Count(envelope.Edges.Count, MaximumEdges, "edges");
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
            Metadata(group.Metadata, "group metadata");
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
            Metadata(node.Metadata, "node metadata");
            Count(node.Ports.Count, MaximumPortsPerNode, "node ports");
            Count(node.Details.Count, MaximumDetailsPerNode, "node details");
            var portIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var port in node.Ports) {
                RequiredId(port.Id, "port id");
                if (!portIds.Add(port.Id)) throw new ArgumentException("Interchange port ids must be unique within a node: " + port.Id + ".", nameof(envelope));
                Text(port.Side, "port side");
                Finite(port.Offset, "port offset");
                if (port.Offset < 0 || port.Offset > 1) throw new ArgumentOutOfRangeException(nameof(envelope), port.Offset, "Port offsets must be between zero and one.");
                OptionalText(port.Label, "port label");
                Metadata(port.Metadata, "port metadata");
            }
            portsByNodeId.Add(node.Id, portIds);
            foreach (var detail in node.Details) {
                Text(detail.Label, "detail label");
                Text(detail.Value, "detail value");
                OptionalText(detail.IconId, "detail icon id");
                OptionalText(detail.Status, "detail status");
                OptionalText(detail.Color, "detail color");
                Metadata(detail.Metadata, "detail metadata");
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
            OptionalText(edge.Direction, "edge direction");
            OptionalText(edge.LineStyle, "edge line style");
            OptionalText(edge.SourcePort, "edge source port");
            OptionalText(edge.TargetPort, "edge target port");
            OptionalText(edge.SourcePortId, "edge source port id");
            OptionalText(edge.TargetPortId, "edge target port id");
            if (!string.IsNullOrWhiteSpace(edge.SourcePortId) && !portsByNodeId[edge.SourceId].Contains(edge.SourcePortId!)) throw new ArgumentException("Interchange edge '" + edge.Id + "' references unknown source port '" + edge.SourcePortId + "' on node '" + edge.SourceId + "'.", nameof(envelope));
            if (!string.IsNullOrWhiteSpace(edge.TargetPortId) && !portsByNodeId[edge.TargetId].Contains(edge.TargetPortId!)) throw new ArgumentException("Interchange edge '" + edge.Id + "' references unknown target port '" + edge.TargetPortId + "' on node '" + edge.TargetId + "'.", nameof(envelope));
            OptionalText(edge.Color, "edge color");
            SafeLink(edge.Href, "edge href", envelope);
            OptionalText(edge.Tooltip, "edge tooltip");
            if (edge.Order < 0) throw new ArgumentOutOfRangeException(nameof(envelope), edge.Order, "Edge order must not be negative.");
            Metadata(edge.Metadata, "edge metadata");
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
            Metadata(annotation.Metadata, "annotation metadata");
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

    private static void Metadata(IDictionary<string, string> values, string context) {
        Count(values.Count, MaximumMetadataEntries, context + " entries");
        foreach (var pair in values) {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > MaximumIdCharacters) throw new ArgumentException(context + " keys must be non-empty and at most " + MaximumIdCharacters + " characters.");
            Text(pair.Value, context + " value");
        }
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
