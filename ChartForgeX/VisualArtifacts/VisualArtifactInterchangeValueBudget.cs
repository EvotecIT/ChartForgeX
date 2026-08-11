using System;

namespace ChartForgeX.VisualArtifacts;

internal static class VisualArtifactInterchangeValueBudget {
    public static void Validate(VisualArtifactInterchangeEnvelope envelope) {
        long count = 17L + Optional(envelope.Width) + Optional(envelope.Height) +
                     Optional(envelope.AccessibleName) + Optional(envelope.AccessibleDescription) + Optional(envelope.Language) +
                     envelope.Metadata.Count;
        Ensure(count, envelope);

        foreach (var group in envelope.Groups) {
            count += 5L + Optional(group.Subtitle) + Optional(group.Status) + Optional(group.Color) + Optional(group.Href) +
                     Optional(group.Tooltip) + Optional(group.X) + Optional(group.Y) + Optional(group.Width) + Optional(group.Height) +
                     group.Metadata.Count;
            Ensure(count, envelope);
        }

        foreach (var node in envelope.Nodes) {
            count += 7L + Optional(node.Subtitle) + Optional(node.GroupId) + Optional(node.Status) + Optional(node.IconId) +
                     Optional(node.Symbol) + Optional(node.Badge) + Optional(node.Color) + Optional(node.BackgroundColor) +
                     Optional(node.Href) + Optional(node.Tooltip) + Optional(node.X) + Optional(node.Y) + Optional(node.Width) +
                     Optional(node.Height) + node.Metadata.Count;
            foreach (var port in node.Ports) {
                count += 5L + Optional(port.Label) + port.Metadata.Count;
                Ensure(count, envelope);
            }
            foreach (var detail in node.Details) {
                count += 4L + Optional(detail.IconId) + Optional(detail.Status) + Optional(detail.Color) + detail.Metadata.Count;
                Ensure(count, envelope);
            }
            Ensure(count, envelope);
        }

        foreach (var edge in envelope.Edges) {
            count += 7L + Optional(edge.Label) + Optional(edge.SecondaryLabel) + Optional(edge.TertiaryLabel) +
                     Optional(edge.SourceLabel) + Optional(edge.TargetLabel) + Optional(edge.Status) + Optional(edge.Direction) +
                     Optional(edge.LineStyle) + Optional(edge.SourcePort) + Optional(edge.TargetPort) + Optional(edge.SourcePortId) +
                     Optional(edge.TargetPortId) + Optional(edge.Color) + Optional(edge.Href) + Optional(edge.Tooltip) + edge.Metadata.Count;
            Ensure(count, envelope);
        }

        foreach (var scenario in envelope.Scenarios) {
            count += 9L + Optional(scenario.Description) + Optional(scenario.Color) + scenario.Metadata.Count;
            foreach (var step in scenario.Steps) {
                count += 4L + Optional(step.Label) + Optional(step.Description) + Optional(step.DurationMilliseconds) + step.Metadata.Count;
                Ensure(count, envelope);
            }
            Ensure(count, envelope);
        }

        foreach (var annotation in envelope.Annotations) {
            count += 6L + Optional(annotation.Placement) + Optional(annotation.StartIndex) + Optional(annotation.EndIndex) +
                     annotation.TargetIds.Count + annotation.Metadata.Count;
            Ensure(count, envelope);
        }
    }

    private static int Optional(string? value) => value == null ? 0 : 1;
    private static int Optional(double? value) => value.HasValue ? 1 : 0;
    private static int Optional(int? value) => value.HasValue ? 1 : 0;

    private static void Ensure(long count, VisualArtifactInterchangeEnvelope envelope) {
        if (count > VisualArtifactInterchangeEnvelope.MaximumJsonValues) {
            throw new ArgumentOutOfRangeException(nameof(envelope), count, "Interchange envelopes must not exceed " + VisualArtifactInterchangeEnvelope.MaximumJsonValues + " materialized JSON values.");
        }
    }
}
