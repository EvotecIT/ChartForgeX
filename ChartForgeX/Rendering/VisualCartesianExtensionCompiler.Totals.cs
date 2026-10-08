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
        internal HorizontalStackTotal(double category, double value, string text, TextStyle style, TextMetrics metrics) {
            Category = category; Value = value; Text = text; Style = style; Metrics = metrics;
            Id = "stack-total-horizontal-" + Number(category) + (value > 0 ? "-positive" : "-negative");
        }
        internal double Category { get; }
        internal double Value { get; }
        internal string Text { get; }
        internal TextStyle Style { get; }
        internal TextMetrics Metrics { get; }
        internal string Id { get; }
    }

    private static IReadOnlyList<HorizontalStackTotal> ResolveHorizontalTotals(Chart chart, VisualRenderContext context,
        VisualSceneBuilder builder, VisualThemeColors colors) {
        var totals = new List<HorizontalStackTotal>();
        foreach (var category in chart.Series.SelectMany(series => series.Points.Select(point => point.X)).Distinct().OrderBy(value => value)) {
            foreach (var positive in new[] { true, false }) {
                var total = chart.Series.SelectMany(series => series.Points)
                    .Where(point => ChartMath.SameCoordinate(point.X, category) && (point.Y >= 0) == positive).Sum(point => point.Y);
                if (total == 0) continue;
                var text = Value(chart, total);
                var style = chart.Options.DataLabelStyle.Resolve(new TextStyle {
                    Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = colors.Foreground
                });
                totals.Add(new HorizontalStackTotal(category, total, text, style, builder.MeasureText(text, style)));
            }
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

    private static void AddHorizontalTotals(IReadOnlyList<HorizontalStackTotal> totals, VisualRenderContext context,
        VisualSceneBuilder builder, ChartMapper map, List<LabelPlacementRequest> labels) {
        foreach (var total in totals) {
            var positive = total.Value > 0;
            var point = new ChartPoint(map.X(total.Value), map.Y(total.Category));
            builder.AddRegion(new VisualSemanticRegion(total.Id, "stack-total", new ChartRect(point.X, point.Y, 0, 0),
                total.Text + " category=" + Number(total.Category) + " value=" + Number(total.Value)));
            labels.Add(new LabelPlacementRequest(total.Text, point, total.Style, new[] {
                new LabelCandidate(positive ? Math.Max(2, context.Theme.Spacing) : -Math.Max(2, context.Theme.Spacing), 0, positive ? 0 : 1, .5)
            }, priority: 1) { AssociatedMarkId = total.Id, MeasuredSize = total.Metrics });
        }
    }
}
