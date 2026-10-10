using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualNumericRadialCompiler {
    private static void DataLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartSeries series,
        int point, string id, string text, RadialSeriesMark mark, ChartRect bounds, ChartColor fill, List<RadialSeriesLabel> labels) {
        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var inside = placement is ChartDataLabelPlacement.Center or ChartDataLabelPlacement.Inside;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var ink = inside && mark.Painted ? ChartMarkText.OnPreparedMark(fill, VisualChartPaint.SeriesRole(series, point),
            ChartStateMark.Backdrop(chart.Options, colors, context.Frame)) : ChartColorBlend.Solid(colors.Foreground, SvgColorRole.Text);
        var style = series.DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize,
            Color = ink.Color
        }));
        if (point < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[point] != null) style = series.PointDataLabelStyles[point]!.Resolve(style);
        var gap = context.Theme.Spacing;
        var candidate = placement switch {
            ChartDataLabelPlacement.Above => new LabelCandidate(0, -gap, .5, 1),
            ChartDataLabelPlacement.Below => new LabelCandidate(0, gap, .5, 0),
            ChartDataLabelPlacement.Left => new LabelCandidate(-gap, 0, 1, .5),
            ChartDataLabelPlacement.Right => new LabelCandidate(gap, 0, 0, .5),
            _ => inside ? new LabelCandidate(0, 0, .5, .5) : new LabelCandidate(mark.Offset.X * gap, mark.Offset.Y * gap, .5, .5)
        };
        var request = AddLabel(builder, labels, text, inside && mark.Painted ? mark.Center : mark.End, style, id + "-label", "radial-data-label", plot, candidate, 40,
            VisualMarkLabel.Metadata(text, id));
        // The shared placement engine permits containment in the associated curved mark,
        // rejects partial crossings, and retains the original description if no ink fits.
        if (inside) { request.Bounds = bounds; request.AssociatedMarkId = id; }
        var explicitColor = chart.Options.DataLabelStyle.Color.HasValue || series.DataLabelStyle.Color.HasValue
            || point < series.PointDataLabelStyles.Count && series.PointDataLabelStyles[point]?.Color != null;
        if (!explicitColor) request.Paint = ink.Paint;
    }

    private static void StackTotals(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, RadialSeriesGeometry geometry,
        ChartBarCoordinateKey[] categories, ChartBarCoordinateMap coordinates, ChartStackLayout stacks,
        Dictionary<ChartAxisSide, RadialValueScale> scales, bool bars, List<RadialSeriesLabel> labels) {
        var participants = Enumerable.Range(0, chart.Series.Count).ToArray(); var ordinal = 0;
        foreach (var total in stacks.Totals) {
            if (total.Value == 0) continue;
            var key = coordinates.Resolve(total.SeriesIndex, total.PointIndex);
            var category = Array.FindIndex(categories, item => item.Id == key.Id);
            var mark = Mark(chart, geometry, scales[total.Axis], 0, total.Value, category, categories.Length, stacks.Slot(participants, total.SeriesIndex), bars);
            var id = "stack-total-" + ordinal++;
            var text = ChartNumericFormatter.FormatValue(chart.Options, total.Value);
            var style = chart.Options.DataLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize,
                Color = context.Theme.Resolve(context.ThemeMode).Foreground });
            var facts = new Dictionary<string, string> {
                ["data-cfx-x"] = N(total.Coordinate), ["data-cfx-y"] = N(total.Value), ["data-cfx-source-total"] = N(total.SourceValue),
                ["data-cfx-stack-group"] = total.Group ?? string.Empty, ["data-cfx-axis"] = total.Axis.ToString().ToLowerInvariant()
            };
            AddLabel(builder, labels, text, mark.End, style, id, "stack-total", plot,
                new LabelCandidate(mark.Offset.X * context.Theme.Spacing, mark.Offset.Y * context.Theme.Spacing, .5, .5), 60, facts);
        }
    }

    private static LabelPlacementRequest AddLabel(VisualSceneBuilder builder, List<RadialSeriesLabel> labels, string text, ChartPoint anchor, TextStyle style,
        string id, string role, ChartRect bounds, LabelCandidate candidate, int priority, Dictionary<string, string>? facts = null) {
        builder.AddRegion(new VisualSemanticRegion(id, role, bounds, text));
        var request = new LabelPlacementRequest(text, anchor, style, new[] { candidate }, priority) {
            MeasuredSize = builder.MeasureText(text, style)
        };
        labels.Add(new RadialSeriesLabel(request, id, role, facts));
        return request;
    }

    private static void DrawLabels(VisualSceneBuilder builder, ChartRect plot, IReadOnlyList<RadialSeriesLabel> labels, IReadOnlyList<LabelObstacle>? obstacles, double gap) {
        var placed = new LabelPlacementService().Place(labels.Select(label => label.Request).ToArray(), plot, obstacles, Math.Max(1, gap / 3), builder.MeasureText);
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index]; var label = labels[index];
            var facts = label.Facts ?? new Dictionary<string, string>(); facts["data-cfx-full-label"] = label.Request.Text;
            using (builder.PushGroup(label.Id + "-source", label.Role + "-source", facts)) {
                if (result.IsDropped || result.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("numeric-radial.label-overflow", "Numeric radial text was shortened or omitted to fit its mark and avoid collisions within the viewport; complete source text remains available in descriptive regions."));
                if (result.IsDropped) continue;
                var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
                style.TextCase = TextCaseTransform.None; style.Alignment = TextAlignment.Left;
                builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(style), style, label.Role, label.Id, paint: result.Request.Paint ?? VisualChartPaint.Text(style));
            }
        }
    }

    private sealed class RadialSeriesLabel {
        internal RadialSeriesLabel(LabelPlacementRequest request, string id, string role, Dictionary<string, string>? facts) { Request = request; Id = id; Role = role; Facts = facts; }
        internal LabelPlacementRequest Request { get; } internal string Id { get; } internal string Role { get; }
        internal Dictionary<string, string>? Facts { get; }
    }
}
