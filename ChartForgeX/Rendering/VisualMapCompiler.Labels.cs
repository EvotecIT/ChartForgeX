using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static TextStyle MapDataStyle(Chart chart, VisualRenderContext context, int index, ChartColor? color = null) {
        var style = chart.Series[0].DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = color ?? context.Theme.Resolve(context.ThemeMode).Foreground
        }));
        return index >= 0 && index < chart.Series[0].PointDataLabelStyles.Count && chart.Series[0].PointDataLabelStyles[index] != null
            ? chart.Series[0].PointDataLabelStyles[index]!.Resolve(style) : style;
    }
    private static void AddMapLabel(VisualSceneBuilder builder, List<MapLabel> labels, string full, string visible, ChartPoint anchor, TextStyle style,
        string id, string role, ChartRect bounds, double gap, int priority, ChartDataLabelPlacement placement = ChartDataLabelPlacement.Auto) {
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, full));
        var candidates = new List<LabelCandidate>();
        if (placement == ChartDataLabelPlacement.Left) candidates.Add(new LabelCandidate(-gap, 0, 1, .5));
        else if (placement is ChartDataLabelPlacement.Right or ChartDataLabelPlacement.Outside) candidates.Add(new LabelCandidate(gap, 0, 0, .5));
        else if (placement is ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside) candidates.Add(new LabelCandidate(0, 0, .5, .5));
        else if (placement == ChartDataLabelPlacement.Below) candidates.Add(new LabelCandidate(0, gap, .5, 0));
        candidates.Add(new LabelCandidate(0, -gap, .5, 1)); candidates.Add(new LabelCandidate(gap, 0, 0, .5));
        candidates.Add(new LabelCandidate(-gap, 0, 1, .5)); candidates.Add(new LabelCandidate(0, gap, .5, 0));
        labels.Add(new MapLabel(new LabelPlacementRequest(visible, anchor, style, candidates, priority) { MeasuredSize = builder.MeasureText(visible, style),
            AssociatedMarkId = id.EndsWith("-label", System.StringComparison.Ordinal) ? id.Substring(0, id.Length - 6) : null }, id, role, full));
    }
    private static void DrawMapLabels(VisualSceneBuilder builder, ChartRect plot, IReadOnlyList<MapLabel> labels, IReadOnlyList<LabelObstacle> obstacles, double gap) {
        var placed = new LabelPlacementService().Place(labels.Select(label => label.Request).ToArray(), plot, obstacles, gap / 3, builder.MeasureText);
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index]; var label = labels[index];
            using (builder.PushGroup(label.Id + "-source", label.Role + "-source", new Dictionary<string, string> { ["data-cfx-full-label"] = label.Full, ["aria-label"] = label.Full })) {
                if (result.IsDropped || result.IsEllipsized || label.Full != result.Text) builder.AddDiagnostic(new VisualDiagnostic("map.label-overflow", "Map text was shortened or omitted; complete text remains in descriptive regions."));
                if (result.IsDropped) continue;
                var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize; style.TextCase = TextCaseTransform.None;
                style.Baseline = TextBaseline.Normal; style.Alignment = TextAlignment.Left;
                builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(style), style, label.Role, label.Id);
            }
        }
    }
    private sealed class MapLabel {
        internal MapLabel(LabelPlacementRequest request, string id, string role, string full) { Request = request; Id = id; Role = role; Full = full; }
        internal LabelPlacementRequest Request { get; } internal string Id { get; } internal string Role { get; } internal string Full { get; }
    }
}
