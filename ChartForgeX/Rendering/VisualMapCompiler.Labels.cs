using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static TextStyle MapDataStyle(Chart chart, VisualRenderContext context, int index, ChartColor? color = null, ChartSeries? series = null) {
        series ??= chart.Series[0];
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = color ?? context.Theme.Resolve(context.ThemeMode).Foreground
        }));
        return index >= 0 && index < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[index] != null
            ? series.PointDataLabelStyles[index]!.Resolve(style) : style;
    }
    private static void AddMapLabel(VisualSceneBuilder builder, List<MapLabel> labels, string full, string visible, ChartPoint anchor, TextStyle style,
        string id, string role, ChartRect bounds, double gap, int priority, ChartDataLabelPlacement placement = ChartDataLabelPlacement.Auto, SvgPaint? paint = null,
        ChartColor? leaderColor = null, SvgPaint? leaderPaint = null, double leaderRadius = 0, ChartMapCallouts? callouts = null) {
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, full));
        var candidates = new List<LabelCandidate>();
        if (placement == ChartDataLabelPlacement.Left) candidates.Add(new LabelCandidate(-gap, 0, 1, .5));
        else if (placement is ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside) candidates.Add(new LabelCandidate(gap, 0, 0, .5));
        else if (placement is ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside) candidates.Add(new LabelCandidate(0, 0, .5, .5));
        else if (placement == ChartDataLabelPlacement.Below) candidates.Add(new LabelCandidate(0, gap, .5, 0));
        candidates.Add(new LabelCandidate(0, -gap, .5, 1)); candidates.Add(new LabelCandidate(gap, 0, 0, .5));
        candidates.Add(new LabelCandidate(-gap, 0, 1, .5)); candidates.Add(new LabelCandidate(0, gap, .5, 0));
        if (role == "dotted-map-connector-label") for (var band = 2; band <= 4; band++) {
            candidates.Add(new LabelCandidate(gap * band, gap * band, 0, .5));
            candidates.Add(new LabelCandidate(-gap * band, gap * band, 1, .5));
            candidates.Add(new LabelCandidate(gap * band, -gap * band, 0, .5));
            candidates.Add(new LabelCandidate(-gap * band, -gap * band, 1, .5));
        }
        if (callouts != null) candidates = MapCalloutCandidates(placement, gap, callouts.MaximumDistance);
        labels.Add(new MapLabel(new LabelPlacementRequest(visible, anchor, style, candidates, priority) { MeasuredSize = builder.MeasureText(visible, style),
            AssociatedMarkId = id.EndsWith("-label", System.StringComparison.Ordinal) ? id.Substring(0, id.Length - 6) : null,
            HasLeaderLine = callouts != null && leaderColor.HasValue, LeaderOnFirstCandidate = callouts != null }, id, role, full, paint ?? VisualChartPaint.Text(style),
            leaderColor, leaderPaint, leaderRadius));
    }
    private static void DrawMapLabels(VisualSceneBuilder builder, ChartRect plot, IReadOnlyList<MapLabel> labels, IReadOnlyList<LabelObstacle> obstacles, double gap, bool outline = false) {
        var placed = new LabelPlacementService().Place(labels.Select(label => label.Request).ToArray(), plot, obstacles, gap / 3, builder.MeasureText,
            avoidLeaderLabels: labels.Any(label => label.Request.HasLeaderLine));
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index]; var label = labels[index];
            var metadata = new Dictionary<string, string> { ["data-cfx-full-label"] = label.Full, ["aria-label"] = label.Full };
            if (label.Request.HasLeaderLine) {
                metadata["data-cfx-callout"] = "true"; metadata["data-cfx-label-dropped"] = result.IsDropped ? "true" : "false";
                metadata["data-cfx-anchor-x"] = N(label.Request.Anchor.X); metadata["data-cfx-anchor-y"] = N(label.Request.Anchor.Y);
                if (!result.IsDropped) {
                    metadata["data-cfx-leader-x"] = N(result.LeaderEnd.X); metadata["data-cfx-leader-y"] = N(result.LeaderEnd.Y);
                    metadata["data-cfx-label-left"] = N(result.Bounds.Left); metadata["data-cfx-label-top"] = N(result.Bounds.Top);
                    metadata["data-cfx-label-width"] = N(result.Bounds.Width); metadata["data-cfx-label-height"] = N(result.Bounds.Height);
                }
            }
            using (builder.PushGroup(label.Id + "-source", label.Role + "-source", metadata)) {
                if (result.IsDropped || result.IsEllipsized || label.Full != result.Text) builder.AddDiagnostic(new VisualDiagnostic("map.label-overflow", "Map text was shortened or omitted; complete text remains in descriptive regions."));
                if (result.IsDropped) continue;
                DrawMapLabelLeader(builder, label, result);
                var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize; style.TextCase = TextCaseTransform.None;
                style.Baseline = TextBaseline.Normal; style.Alignment = TextAlignment.Left;
                var halo = outline ? ChartColorMath.AccessibleTextOnBackground(style.Color) : (ChartColor?)null;
                builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(style), style, label.Role, label.Id, paint: label.Paint,
                    stroke: halo, strokeWidth: outline ? ChartTextHalo.SvgStrokeWidth(style.EffectiveFontSize, style.Font.Weight >= 700) : 0,
                    strokePaint: outline ? SvgPaint.Contrast(style.Color, label.Paint) : null);
            }
        }
    }
    private static void DrawMapLabelLeader(VisualSceneBuilder builder, MapLabel label, PlacedLabel result) {
        if (!label.LeaderColor.HasValue) return;
        var anchor = result.Request.Anchor; var end = result.LeaderEnd;
        var dx = end.X - anchor.X; var dy = end.Y - anchor.Y;
        var distance = System.Math.Sqrt(dx * dx + dy * dy);
        // Centered labels need no leader. Keep the line outside the dot and clear of the measured text rectangle.
        if (distance <= label.LeaderRadius + 1) return;
        var ux = dx / distance; var uy = dy / distance;
        var start = new ChartPoint(anchor.X + ux * label.LeaderRadius, anchor.Y + uy * label.LeaderRadius);
        var clearance = System.Math.Min(2, (distance - label.LeaderRadius) / 2);
        end = new ChartPoint(end.X - ux * clearance, end.Y - uy * clearance);
        var color = label.LeaderColor.Value; var paint = label.LeaderPaint ?? SvgPaint.Literal(color);
        foreach (var layer in ChartLineVisualLayers.Build(color, 1, ChartRouteVisualStyles.DottedMapLeader())) {
            if (!layer.IsVisible) continue;
            builder.Line(start.X, start.Y, end.X, end.Y, layer.ColorWithOpacity(), layer.StrokeWidth,
                role: "dotted-map-label-leader" + layer.RoleSuffix,
                paint: VisualChartPaint.Stroke(layer.IsHighlight ? SvgPaint.Literal(layer.ColorWithOpacity()) : paint.WithOpacity(layer.ColorWithOpacity(), layer.Opacity)));
        }
    }
    private sealed class MapLabel {
        internal MapLabel(LabelPlacementRequest request, string id, string role, string full, SvgPaint paint,
            ChartColor? leaderColor, SvgPaint? leaderPaint, double leaderRadius) {
            Request = request; Id = id; Role = role; Full = full; Paint = paint;
            LeaderColor = leaderColor; LeaderPaint = leaderPaint; LeaderRadius = leaderRadius;
        }
        internal LabelPlacementRequest Request { get; } internal string Id { get; } internal string Role { get; } internal string Full { get; } internal SvgPaint Paint { get; }
        internal ChartColor? LeaderColor { get; } internal SvgPaint? LeaderPaint { get; } internal double LeaderRadius { get; }
    }
}
