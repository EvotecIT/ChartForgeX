using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private (ChartColor Color, SvgPaint Paint) NodeContourStroke(TopologyNode node, ChartColor accent, SvgPaint accentPaint) {
        if (node.BorderColor == null) return (accent, accentPaint);
        var active = _highlight.IsNodeHighlighted(node);
        var color = Highlight(Color(node.BorderColor, NodeAccent(node)), active);
        return (color, SourcePaint(node.BorderColor, color, SvgColorRole.Any, HighlightFactor(active), _colors.Border));
    }

    private static bool HasArtworkSurfacePolicy(TopologyNode node) => !string.IsNullOrWhiteSpace(node.BackgroundColor)
        || node.BorderColor != null || node.StrokeWidth.HasValue || node.SurfaceOpacity.HasValue || node.DashPattern.Count > 0 || node.CornerRadius.HasValue;

    private void BuildNodeSurface(TopologyNode node, TopologyNodeDisplayMode mode, ChartRect bounds, ChartColor fill, ChartColor accent,
        double strokeWidth, double[]? surfaceDash, VisualScenePaintBinding surfacePaint, SvgPaint accentPaint) {
        var contour = NodeContourStroke(node, accent, accentPaint);
        surfacePaint = new VisualScenePaintBinding(surfacePaint.Fill, contour.Paint);
        if (mode == TopologyNodeDisplayMode.Dot) {
            var ring = node.Shape == TopologyNodeShape.DoubleCircle ? Math.Min(bounds.Width, bounds.Height) * .25 : 0;
            using (PinnedState()) {
                if (ring > 0) SurfaceEllipse(bounds.X + bounds.Width / 2,bounds.Y + bounds.Height / 2,Math.Max(0,bounds.Width / 2-strokeWidth / 2),Math.Max(0,bounds.Height / 2-strokeWidth / 2),null,contour.Color,strokeWidth,"topology-node-surface",surfaceDash,new VisualScenePaintBinding(stroke:contour.Paint),node.SurfaceOpacity??1);
                _builder.Ellipse(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2, bounds.Width / 2 - ring, bounds.Height / 2 - ring, accent, role: "topology-node-surface", paint: new VisualScenePaintBinding(accentPaint),opacity:node.SurfaceOpacity??1);
            }
            if (!string.IsNullOrWhiteSpace(node.Symbol)) {
                // The canonical compact symbol is 8px on the default 11px typography
                // scale. Preserve that ratio for custom themes and smaller dot budgets;
                // the common measured text fitter still owns line and width containment.
                var symbolRatio = DotNodeSymbolFontSize / 11;
                var symbolSize = Math.Min(TextPolicySize(_context.Theme.Typography.DataLabelSize), node.Height) * symbolRatio;
                Text(node.Symbol!, bounds, symbolSize, _colors.Surface, 700, "topology-node-symbol", centered: true, paint: CardPaint(_colors.Surface), fitSingleLine: true);
            }
        }
        else if (node.Shape.HasValue || node.CornerRadius.HasValue) {
            var parts = TopologyNodeShapeGeometry.SceneParts(node);
            var elements = TopologySurfaceElementAppearance.Resolve(node, mode);
            elements?.ValidateParts(parts);
            using (elements?.GroupOpacity is double groupOpacity ? _builder.PushGroup(null, null, opacity: groupOpacity) : null) {
                foreach (var part in parts) {
                    if (!part.Ink) continue;
                    var accentFill = part.AccentFill || node.Shape is TopologyNodeShape.SynchronizationBar or TopologyNodeShape.ActorSilhouette;
                    var partStroke = part.ContourPaint ? contour.Color : accent;
                    var partStrokePaint = part.ContourPaint ? contour.Paint : accentPaint;
                    var partWidth = part.ContourPaint ? strokeWidth : NodeSurfaceStrokeWidth(node, false) * _scale;
                    ChartColor? partFill = part.Fill || elements?.FillContours == true ? (accentFill ? accent : fill) : null;
                    var partFillPaint = accentFill ? accentPaint : surfacePaint.Fill;
                    var path = Transform(part.Path);
                    var partOpacity = part.ContourPaint ? node.SurfaceOpacity ?? 1 : 1;
                    if (elements?.SeparateFillStroke == true) {
                        // The same native curve owns both paint partitions; no rough path geometry is retained or synthesized.
                        _builder.Path(path, partFill, null, 0, role: "topology-node-surface", close: part.Close,
                            paint: new VisualScenePaintBinding(partFillPaint), opacity: partOpacity);
                        _builder.Path(path, null, partStroke, partWidth, role: "topology-node-surface", close: part.Close,
                            dash: surfaceDash, paint: new VisualScenePaintBinding(stroke: partStrokePaint), opacity: partOpacity);
                    } else _builder.Path(path, partFill, partStroke, partWidth, role: "topology-node-surface", close: part.Close,
                        dash:part.ContourPaint ? surfaceDash : null,paint:new VisualScenePaintBinding(partFillPaint,partStrokePaint),opacity:partOpacity);
                }
            }
        } else SurfaceRectangle(bounds,fill,contour.Color,strokeWidth,mode==TopologyNodeDisplayMode.Pill?bounds.Height/2:_context.Theme.BarRadius*_scale,"topology-node-surface",surfaceDash,surfacePaint,node.SurfaceOpacity??1);
    }
}
