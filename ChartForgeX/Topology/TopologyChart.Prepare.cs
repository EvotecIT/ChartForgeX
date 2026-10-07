using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Topology;

public sealed partial class TopologyChart : IVisualRenderable {
    /// <summary>Prepares a bounded topology card/link diagram directly into the shared static scene.</summary>
    /// <remarks>The initial prepared subset supports manual and layered ungrouped generic cards, straight or orthogonal links, and explicit multiline labels. Unsupported presentation options and content exceeding the fixed common viewport throw explicitly. The existing topology exporter covers the remaining options.</remarks>
    public PreparedVisual Prepare(VisualRenderContext context) {
        if (context == null) throw new ArgumentNullException(nameof(context));
        ValidatePreparedSubset();
        var validation = new TopologyChartValidator().Validate(this);
        if (!validation.IsValid) throw new TopologyValidationException(validation);
        context = VisualDiagramPrimitives.WithFrame(context, Title, Subtitle);
        var builder = new VisualSceneBuilder(context.Layout.Size, context.Font);
        var plot = VisualFrameLayout.Build(builder, context, Array.Empty<VisualLegendEntry>());
        var input = TopologyLayoutEngine.Clone(this);
        input.Title = null; input.Subtitle = null;
        input.Viewport = new TopologyViewport { Width = plot.Width, Height = plot.Height, Padding = context.Theme.Spacing };
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false };
        var prepared = TopologyLayoutEngine.Prepare(input, options: options, measurement: new TextMeasurementContext(context.Font));
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var colors = context.Theme.Resolve(context.ThemeMode);
        foreach (var edge in prepared.Edges) BuildPreparedEdge(builder, prepared, edge, nodes, context, colors, plot);
        foreach (var node in prepared.Nodes) {
            var bounds = new ChartRect(plot.X + node.X, plot.Y + node.Y, node.Width, node.Height);
            var halfStroke = context.Theme.AxisStrokeWidth / 2;
            VisualDiagramPrimitives.RequireInside(new ChartRect(bounds.X - halfStroke, bounds.Y - halfStroke, bounds.Width + halfStroke * 2, bounds.Height + halfStroke * 2), plot, node.Id);
            var status = PreparedStatus(node.Status, colors);
            var stroke = string.IsNullOrWhiteSpace(node.Color) ? status : ChartColor.Parse(node.Color!);
            var fill = string.IsNullOrWhiteSpace(node.BackgroundColor) ? colors.Surface : ChartColor.Parse(node.BackgroundColor!);
            using (builder.PushGroup(node.Id, "topology-node", new Dictionary<string, string> { ["data-status"] = node.Status.ToString(), ["data-kind"] = node.Kind.ToString() })) {
                builder.Rect(bounds, fill, stroke, context.Theme.AxisStrokeWidth, node.Shape == TopologyNodeShape.Rectangle ? 0 : context.Theme.BarRadius);
                if (node.ShowStatusBadge) {
                    VisualDiagramPrimitives.RequireInside(new ChartRect(bounds.Right - 13, bounds.Y + 7, 6, 6), plot, node.Id + "-status");
                    builder.Ellipse(bounds.Right - 10, bounds.Y + 10, 3, 3, status, role: "topology-status");
                }
                var label = node.Label + (string.IsNullOrEmpty(node.Subtitle) ? string.Empty : "\n" + node.Subtitle);
                VisualDiagramPrimitives.Text(builder, label, new ChartRect(bounds.X + 10, bounds.Y + 8, Math.Max(0, bounds.Width - 30), Math.Max(0, bounds.Height - 16)),
                    context.Theme.Typography.DataLabelSize, colors.Foreground, 400, "topology-node-label", node.Id + "-label");
            }
            builder.AddRegion(new VisualSemanticRegion(node.Id, "topology-node", bounds, node.Label));
        }
        var accessibility = Accessibility.Clone();
        accessibility.Name ??= string.IsNullOrWhiteSpace(context.Frame.Title) ? Title ?? Id : context.Frame.Title;
        accessibility.Description ??= string.IsNullOrWhiteSpace(context.Frame.Subtitle) ? Subtitle : context.Frame.Subtitle;
        return new PreparedVisual(builder.Build(), accessibility);
    }

    private static void BuildPreparedEdge(VisualSceneBuilder builder, TopologyChart chart, TopologyEdge edge,
        IReadOnlyDictionary<string, TopologyNode> nodes, VisualRenderContext context, VisualThemeColors colors, ChartRect plot) {
        var route = TopologyRenderPrimitives.EdgePoints(chart, edge, nodes);
        var points = route.Select(point => new ChartPoint(plot.X + point.X, plot.Y + point.Y)).ToList();
        if (points.Count < 2) throw new InvalidOperationException("A prepared topology edge requires at least two route points: " + edge.Id);
        var left = points.Min(point => point.X); var top = points.Min(point => point.Y);
        var bounds = new ChartRect(left, top, points.Max(point => point.X) - left, points.Max(point => point.Y) - top);
        var width = edge.StrokeWidth ?? context.Theme.SeriesStrokeWidth;
        var margin = Math.Max(8, width / 2);
        var regionBounds = new ChartRect(bounds.X - margin, bounds.Y - margin, bounds.Width + margin * 2, bounds.Height + margin * 2);
        VisualDiagramPrimitives.RequireInside(regionBounds, plot, edge.Id);
        var color = string.IsNullOrWhiteSpace(edge.Color) ? edge.IsMuted ? colors.Border : PreparedStatus(edge.Status, colors) : ChartColor.Parse(edge.Color!);
        if (edge.Opacity.HasValue) color = ChartColorMath.WithOpacity(color, edge.Opacity.Value);
        var lineStyle = TopologyRenderPrimitives.EdgePngDash(edge);
        var dash = lineStyle.Dashed ? new[] { lineStyle.Dash, lineStyle.Gap } : null;
        using (builder.PushGroup(edge.Id, "topology-edge", new Dictionary<string, string> { ["data-source"] = edge.SourceNodeId, ["data-target"] = edge.TargetNodeId, ["data-direction"] = edge.Direction.ToString() })) {
            for (var i = 1; i < points.Count; i++) builder.Line(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, color, width, dash: dash);
            if (edge.Direction is VisualLinkDirection.Forward or VisualLinkDirection.Bidirectional)
                VisualDiagramPrimitives.Arrow(builder, points[points.Count - 2], points[points.Count - 1], color, "topology-arrow");
            if (edge.Direction is VisualLinkDirection.Backward or VisualLinkDirection.Bidirectional)
                VisualDiagramPrimitives.Arrow(builder, points[1], points[0], color, "topology-arrow");
            if (!string.IsNullOrEmpty(edge.Label)) {
                var position = TopologyRenderPrimitives.EdgeLabelPoint(points);
                var metrics = builder.MeasureText(edge.Label!, context.Theme.Typography.DataLabelSize);
                var labelBounds = new ChartRect(position.X - metrics.Width / 2 - 6 + edge.LabelOffsetX, position.Y - metrics.Height - 12 + edge.LabelOffsetY, metrics.Width + 12, metrics.Height + 8);
                VisualDiagramPrimitives.RequireInside(labelBounds, plot, edge.Id + "-label");
                var regionLeft = Math.Min(regionBounds.Left, labelBounds.Left); var regionTop = Math.Min(regionBounds.Top, labelBounds.Top);
                regionBounds = new ChartRect(regionLeft, regionTop, Math.Max(regionBounds.Right, labelBounds.Right) - regionLeft, Math.Max(regionBounds.Bottom, labelBounds.Bottom) - regionTop);
                builder.Rect(labelBounds, colors.Surface, radius: context.Theme.BarRadius, role: "topology-edge-label-surface");
                VisualDiagramPrimitives.Text(builder, edge.Label!, labelBounds, context.Theme.Typography.DataLabelSize, colors.Foreground, 400, "topology-edge-label");
            }
        }
        builder.AddRegion(new VisualSemanticRegion(edge.Id, "topology-edge", regionBounds, edge.Label));
    }

    private static ChartColor PreparedStatus(TopologyHealthStatus status, VisualThemeColors colors) => status switch {
        TopologyHealthStatus.Healthy => colors.Status.Pass.Fill,
        TopologyHealthStatus.Warning => colors.Status.Medium.Fill,
        TopologyHealthStatus.Critical => colors.Status.Critical.Fill,
        TopologyHealthStatus.Disabled => colors.Status.Maintenance.Fill,
        _ => colors.Status.Neutral.Fill
    };

    private void ValidatePreparedSubset() {
        if (LayoutMode is not (TopologyLayoutMode.Manual or TopologyLayoutMode.Layered) || Groups.Count != 0 || Scenarios.Count != 0 || Legend != null || DefaultRenderOptions != null)
            throw new NotSupportedException("Prepared topology currently supports manual/layered ungrouped diagrams without scenario, legacy legend, or legacy render options.");
        foreach (var node in Nodes) {
            if (node.Kind != TopologyNodeKind.Generic || node.DisplayMode is not (null or TopologyNodeDisplayMode.Card) ||
                node.Shape is not (null or TopologyNodeShape.Rectangle or TopologyNodeShape.Rounded) || node.Artwork != null ||
                node.Symbol != null || node.IconId != null || node.Badge != null || node.GroupId != null || node.Ports.Count != 0 || node.Details.Count != 0 ||
                node.MaximumLabelCharacters.HasValue || node.Href != null || node.Tooltip != null || node.CssClass != null)
                throw new NotSupportedException("Prepared topology supports generic cards without icon, badge, detail, port, host-link, or character-limit presentation: " + node.Id);
        }
        foreach (var edge in Edges) {
            if (edge.Routing is not (TopologyEdgeRouting.Straight or TopologyEdgeRouting.Orthogonal) || edge.SourceNodeId == edge.TargetNodeId ||
                edge.SourceMarker.HasValue || edge.TargetMarker.HasValue || edge.SourcePortId != null || edge.TargetPortId != null ||
                edge.Emphasis != TopologyEdgeEmphasis.Normal || edge.DashPattern.Count != 0 || edge.SecondaryLabel != null || edge.TertiaryLabel != null ||
                edge.SourceLabel != null || edge.TargetLabel != null || edge.HasLabelAnchorOverride || edge.LabelAnchorNodeId != null ||
                edge.Href != null || edge.Tooltip != null || edge.CssClass != null)
                throw new NotSupportedException("Prepared topology supports non-self straight/orthogonal links without custom markers, named ports, extra labels, host links, or emphasis: " + edge.Id);
        }
    }
}
