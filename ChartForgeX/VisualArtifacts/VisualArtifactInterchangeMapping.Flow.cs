using System;
using ChartForgeX.Accessibility;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    /// <summary>Restores a detached editable flow through its existing ordered lane, step and connector compiler.</summary>
    /// <remarks>Envelope IDs and typed source dimensions are authoritative. Prepared bounds/routes remain observations;
    /// automatic layout is recomputed from authored dimensions. Captured theme, font and caller frame feed convenience
    /// exports; an explicit Prepare(context) remains authoritative. Size/padding edits invalidate the retained layout.</remarks>
    /// <exception cref="NotSupportedException">A carried fact has no editable Flow representation.</exception>
    public static FlowArtifact ToFlowArtifact(this VisualArtifactInterchangeEnvelope envelope) {
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));
        envelope.Validate(); RequireEditableSourcePresentation(envelope);
        if (envelope.Family != VisualArtifactInterchangeFamily.Flow || envelope.Flow == null)
            throw new NotSupportedException("Only a typed Flow-family envelope can restore an editable flow.");
        if (envelope.Scenarios.Count != 0 || envelope.Annotations.Count != 0 || envelope.Presentation?.MapViewport != null
            || envelope.Presentation?.Legend != null || envelope.Presentation?.TopologyCaptions != null)
            throw new NotSupportedException("Editable Flow has no scenario, annotation, geographic or topology legend/caption owner.");
        var flow = FlowArtifact.Create(envelope.Id).WithTitle(envelope.Title).WithSubtitle(envelope.Subtitle);
        flow.LayoutMode = envelope.Flow.LayoutMode; flow.Direction = envelope.Flow.LayoutDirection;
        var layout = new VisualLayoutOptions(new VisualSize(Bound(envelope.Width, "flow width"), Bound(envelope.Height, "flow height")),
            envelope.Flow.ViewportPadding ?? ChartPadding.All(24));
        flow.WithSize(layout.Size.Width, layout.Size.Height, layout.Padding);
        flow.ExportFormats = envelope.Flow.ExportFormats ?? FlowArtifact.DefaultExportFormats;
        Copy(envelope.Extensions, flow.Metadata);
        foreach (var group in envelope.Groups) {
            if (group.Role != VisualArtifactInterchangeGroupRole.FlowLane || group.Topology != null || group.Kind != "FlowLane"
                || group.Subtitle != null || group.BackgroundColor != null || group.BorderColor != null || group.LabelColor != null || group.Href != null || group.Tooltip != null)
                throw new NotSupportedException("Editable Flow lanes require their native lane facts without unsupported paint/navigation fields.");
            flow.AddLane(group.Id, group.Label, Status(group.Status), group.Color);
            Copy(group.Extensions, flow.Lanes[flow.Lanes.Count - 1].Metadata);
        }
        foreach (var node in envelope.Nodes) {
            var source = node.Flow;
            if (node.Role != VisualArtifactInterchangeNodeRole.FlowStep || source == null || node.Topology != null || node.Sequence != null
                || node.Kind != source.Kind.ToString() || node.Ports.Count != 0 || node.Details.Count != 0 || node.Metrics.Count != 0
                || node.BackgroundColor != null || node.LabelColor != null || node.Href != null || node.Tooltip != null)
                throw new NotSupportedException("Editable Flow steps require their native kind and supported source fields.");
            flow.AddStep(node.Id, node.Label, source.Kind, node.GroupId, Status(node.Status));
            flow.ConfigureStep(node.Id, step => {
                step.Subtitle = node.Subtitle; step.Icon = node.IconId; step.Symbol = node.Symbol; step.Color = node.Color; step.Badge = node.Badge;
                step.Width = source.AuthoredWidth ?? Bound(node.Width, "flow step width"); step.Height = source.AuthoredHeight ?? Bound(node.Height, "flow step height");
                step.FitLabelToContent = source.FitLabelToContent; Copy(node.Extensions, step.Metadata);
            });
        }
        if (envelope.Edges.Select(edge => edge.Order).Distinct().Count() != envelope.Edges.Count)
            throw new NotSupportedException("Flow connector order must be unique.");
        foreach (var edge in envelope.Edges.OrderBy(edge => edge.Order)) {
            var source = edge.Flow;
            if (edge.Role != VisualArtifactInterchangeEdgeRole.FlowConnector || source == null || edge.Topology != null || edge.Sequence != null
                || edge.Kind != source.Kind.ToString() || edge.SecondaryLabel != null || edge.TertiaryLabel != null || edge.SourceLabel != null || edge.TargetLabel != null
                || edge.SourcePortId != null || edge.TargetPortId != null || edge.LabelColor != null || edge.Href != null || edge.Tooltip != null || edge.Metrics.Count != 0)
                throw new NotSupportedException("Editable Flow connectors require their native ordered source fields.");
            flow.AddConnector(edge.SourceId, edge.TargetId, edge.Label ?? string.Empty, source.Kind, source.Direction, Status(edge.Status), edge.Color);
            flow.ConfigureConnector(flow.Connectors.Count - 1, connector => { connector.Id = edge.Id; Copy(edge.Extensions, connector.Metadata); });
        }
        var theme = envelope.Presentation?.Theme is {} captured ? RestoreCompleteTopologyTheme(captured) : VisualTheme.Graphite();
        flow.PreparedPresentation = new FlowPreparedPresentation(flow, layout, theme, envelope.Presentation?.Theme?.Mode ?? VisualThemeMode.Light,
            envelope.Presentation?.ContextFont, envelope.Presentation?.Frame, new VisualAccessibility {
                Name = envelope.AccessibleName, Description = envelope.AccessibleDescription,
                Language = envelope.Language, IsDecorative = envelope.IsDecorative });
        flow.ValidateNative();
        return flow;
        static double Bound(double? value, string name) => value ?? throw new NotSupportedException("Native restoration requires " + name + ".");
        static VisualStatus Status(string? value) {
            if (value != null && Enum.TryParse<VisualStatus>(value, out var status) && Enum.IsDefined(typeof(VisualStatus), status) && value == status.ToString()) return status;
            throw new NotSupportedException("Flow status requires a defined native status token.");
        }
    }
}
