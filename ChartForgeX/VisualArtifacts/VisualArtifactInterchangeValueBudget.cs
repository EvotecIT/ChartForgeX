using System;

namespace ChartForgeX.VisualArtifacts;

internal static class VisualArtifactInterchangeValueBudget {
    public static void Validate(VisualArtifactInterchangeEnvelope envelope) {
        long count = 19L + Optional(envelope.Width) + Optional(envelope.Height) +
                     Optional(envelope.AccessibleName) + Optional(envelope.AccessibleDescription) + Optional(envelope.Language) +
                     envelope.Extensions.Count + Presentation(envelope.Presentation) + (envelope.Sequence == null ? 0L : 2L);
        Ensure(count, envelope);

        foreach (var group in envelope.Groups) {
            count += 5L + Optional(group.Subtitle) + Optional(group.Status) + Optional(group.Color) + Optional(group.Href) +
                     Optional(group.Tooltip) + Optional(group.X) + Optional(group.Y) + Optional(group.Width) + Optional(group.Height) +
                     group.Extensions.Count + 1L + (group.Topology == null ? 0L : 12L);
            Ensure(count, envelope);
        }

        foreach (var node in envelope.Nodes) {
            count += 7L + Optional(node.Subtitle) + Optional(node.GroupId) + Optional(node.Status) + Optional(node.IconId) +
                     Optional(node.Symbol) + Optional(node.Badge) + Optional(node.Color) + Optional(node.BackgroundColor) +
                     Optional(node.Href) + Optional(node.Tooltip) + Optional(node.X) + Optional(node.Y) + Optional(node.Width) +
                     Optional(node.Height) + node.Extensions.Count + 1L + NodeSemantics(node);
            count += node.Metrics.Count * 4L;
            foreach (var port in node.Ports) {
                count += 5L + Optional(port.Label) + port.Extensions.Count;
                Ensure(count, envelope);
            }
            foreach (var detail in node.Details) {
                count += 4L + Optional(detail.IconId) + Optional(detail.Status) + Optional(detail.Color) + detail.Extensions.Count;
                Ensure(count, envelope);
            }
            Ensure(count, envelope);
        }

        foreach (var edge in envelope.Edges) {
            count += 7L + Optional(edge.Label) + Optional(edge.SecondaryLabel) + Optional(edge.TertiaryLabel) +
                     Optional(edge.SourceLabel) + Optional(edge.TargetLabel) + Optional(edge.Status) +
                     Optional(edge.SourcePortId) + Optional(edge.TargetPortId) + Optional(edge.Color) + Optional(edge.Href) +
                     Optional(edge.Tooltip) + edge.Extensions.Count + 1L + EdgeSemantics(edge);
            count += edge.Metrics.Count * 4L;
            Ensure(count, envelope);
        }

        foreach (var scenario in envelope.Scenarios) {
            count += 9L + Optional(scenario.Description) + Optional(scenario.Color) + scenario.Extensions.Count;
            foreach (var step in scenario.Steps) {
                count += 4L + Optional(step.Label) + Optional(step.Description) + Optional(step.DurationMilliseconds) + step.Extensions.Count;
                Ensure(count, envelope);
            }
            Ensure(count, envelope);
        }

        foreach (var annotation in envelope.Annotations) {
            count += 6L + Optional(annotation.Placement) + Optional(annotation.StartIndex) + Optional(annotation.EndIndex) +
                     annotation.TargetIds.Count + annotation.Extensions.Count + 1L + (annotation.Sequence == null ? 0L : 10L);
            Ensure(count, envelope);
        }
    }

    private static int Optional(string? value) => value == null ? 0 : 1;
    private static int Optional(double? value) => value.HasValue ? 1 : 0;
    private static int Optional(int? value) => value.HasValue ? 1 : 0;

    private static long Presentation(VisualArtifactInterchangePresentation? presentation) {
        if (presentation == null) return 0;
        long count = 1;
        if (presentation.Theme != null) count += 15;
        if (presentation.MapViewport != null) count += 8 + Optional(presentation.MapViewport.Name);
        if (presentation.Legend != null) {
            count += 4 + Optional(presentation.Legend.Title);
            foreach (var item in presentation.Legend.Items) {
                count += 12 + Optional(item.Status) + Optional(item.NodeKind) + Optional(item.EdgeKind) +
                         Optional(item.Symbol) + Optional(item.IconId) + Optional(item.Color) + Optional(item.BackgroundColor);
            }
        }
        return count;
    }

    private static long NodeSemantics(VisualArtifactInterchangeNode node) {
        if (node.Topology != null) {
            return 14L + Optional(node.Topology.Longitude) + Optional(node.Topology.Latitude) +
                   Optional(node.Topology.MaximumLabelCharacters) + (node.Topology.Artwork == null ? 0L : 12L);
        }
        if (node.Flow != null) return 3L;
        if (node.Sequence != null) return 5L;
        return 0L;
    }

    private static long EdgeSemantics(VisualArtifactInterchangeEdge edge) {
        if (edge.Topology != null) {
            return 30L + edge.Topology.DashPattern.Count + edge.Topology.Waypoints.Count * 4L +
                   Optional(edge.Topology.SourceMarker) + Optional(edge.Topology.TargetMarker) +
                   Optional(edge.Topology.StrokeWidth) + Optional(edge.Topology.Opacity) + Optional(edge.Topology.RouteLane) +
                   Optional(edge.Topology.LabelAnchorNodeId) + Optional(edge.Topology.PreferredLength) +
                   (edge.Topology.LabelAnchor == null ? 0L : 4L);
        }
        if (edge.Flow != null) return 3L;
        if (edge.Sequence != null) return 6L;
        return 0L;
    }

    private static int Optional<TEnum>(TEnum? value) where TEnum : struct => value.HasValue ? 1 : 0;

    private static void Ensure(long count, VisualArtifactInterchangeEnvelope envelope) {
        if (count > VisualArtifactInterchangeEnvelope.MaximumJsonValues) {
            throw new ArgumentOutOfRangeException(nameof(envelope), count, "Interchange envelopes must not exceed " + VisualArtifactInterchangeEnvelope.MaximumJsonValues + " materialized JSON values.");
        }
    }
}
