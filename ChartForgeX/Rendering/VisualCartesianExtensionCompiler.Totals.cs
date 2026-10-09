using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // Resolve once before layout: a caller's formatter can be stateful, and the full
    // styled metrics (including line height) must agree with the later label request.
    private sealed class HorizontalStackTotal {
        internal HorizontalStackTotal(ChartStackTotal total, int ordinal, string text, TextStyle style, TextMetrics metrics) {
            Total = total; Text = text; Style = style; Metrics = metrics;
            Id = "stack-total-horizontal-" + Number(ordinal);
        }
        internal ChartStackTotal Total { get; }
        internal double Value => Total.Value;
        internal string Text { get; }
        internal TextStyle Style { get; }
        internal TextMetrics Metrics { get; }
        internal string Id { get; }
    }

    private static IReadOnlyList<HorizontalStackTotal> ResolveHorizontalTotals(Chart chart, VisualRenderContext context,
        VisualSceneBuilder builder, VisualThemeColors colors, ChartStackLayout stacks) {
        var totals = new List<HorizontalStackTotal>();
        foreach (var total in stacks.Totals.Where(total => total.Kind == ChartSeriesKind.HorizontalBar)) {
            if (total.Value == 0) continue;
            var text = Value(chart, total.Value);
            var style = chart.Options.DataLabelStyle.Resolve(new TextStyle {
                Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = colors.Foreground
            });
            totals.Add(new HorizontalStackTotal(total, totals.Count, text, style, builder.MeasureText(text, style)));
        }
        return totals;
    }

    private static ChartRect ReserveHorizontalTotalGutters(ChartRect bounds, IReadOnlyList<HorizontalStackTotal> totals, double spacing) {
        spacing = Math.Max(2, spacing);
        var left = 0d; var right = 0d;
        foreach (var total in totals) {
            if (total.Value > 0) right = Math.Max(right, total.Metrics.Width + spacing);
            else left = Math.Max(left, total.Metrics.Width + spacing);
        }
        // Keep a positive data viewport even when authored captions exceed available
        // space. The shared placement policy then shortens/drops them with diagnostics.
        var available = bounds.Width - Math.Min(bounds.Width / 2, Math.Max(1, spacing));
        var scale = left + right > available ? available / (left + right) : 1;
        left *= scale; right *= scale;
        return new ChartRect(bounds.Left + left, bounds.Top, bounds.Width - left - right, bounds.Height);
    }

    private static void AddHorizontalTotals(Chart chart, IReadOnlyList<HorizontalStackTotal> totals, VisualRenderContext context,
        VisualSceneBuilder builder, ChartRect plot, ChartMapper map, ChartStackLayout stacks, List<LabelPlacementRequest> labels) {
        foreach (var total in totals) {
            var positive = total.Value > 0;
            var layout = ResolveHorizontalBarLayout(chart, context, plot, map, stacks, total.Total.SeriesIndex);
            var point = new ChartPoint(map.X(total.Value), map.Y(total.Total.Coordinate) + layout.Offset);
            builder.AddRegion(new VisualSemanticRegion(total.Id, "stack-total", new ChartRect(point.X, point.Y, 0, 0),
                total.Text + " category=" + Number(total.Total.Coordinate) + " value=" + Number(total.Value)));
            using (builder.PushGroup(total.Id, "stack-total", new Dictionary<string, string> {
                ["data-cfx-x"] = Number(total.Total.Coordinate), ["data-cfx-y"] = Number(total.Value), ["data-cfx-label"] = total.Text,
                ["data-cfx-source-total"] = Number(total.Total.SourceValue), ["data-cfx-stack-group"] = total.Total.Group ?? string.Empty,
                ["data-cfx-axis"] = "primary"
            })) { }
            labels.Add(new LabelPlacementRequest(total.Text, point, total.Style, new[] {
                new LabelCandidate(positive ? Math.Max(2, context.Theme.Spacing) : -Math.Max(2, context.Theme.Spacing), 0, positive ? 0 : 1, .5)
            }, priority: 1) { AssociatedMarkId = total.Id, MeasuredSize = total.Metrics });
        }
    }
}
