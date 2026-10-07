using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private sealed class VerticalStackCaption {
        internal VerticalStackCaption(string text, TextStyle style, TextMetrics metrics) { Text = text; Style = style; Metrics = metrics; }
        internal string Text { get; }
        internal TextStyle Style { get; }
        internal TextMetrics Metrics { get; }
    }

    private static Dictionary<(ChartAxisSide Axis, ChartBarCoordinateKey Coordinate, bool Positive), VerticalStackCaption> ResolveVerticalTotals(
        Chart chart, VisualRenderContext context, VisualSceneBuilder builder, VisualThemeColors colors, ChartBarCoordinateMap coordinates) {
        var values = new Dictionary<(ChartAxisSide Axis, ChartBarCoordinateKey Coordinate, bool Positive), double>();
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                var point = series.Points[pointIndex];
                var key = (series.YAxis, coordinates.Resolve(seriesIndex, pointIndex), point.Y >= 0);
                values.TryGetValue(key, out var previous);
                values[key] = previous + point.Y;
            }
        }
        var captions = new Dictionary<(ChartAxisSide Axis, ChartBarCoordinateKey Coordinate, bool Positive), VerticalStackCaption>();
        foreach (var value in values.OrderBy(item => item.Key.Axis).ThenBy(item => item.Key.Coordinate.Value).ThenByDescending(item => item.Key.Positive)) {
            if (Math.Abs(value.Value) < .000001) continue;
            var text = ChartNumericFormatter.FormatValue(chart.Options, value.Value);
            var style = chart.Options.DataLabelStyle.Resolve(new TextStyle {
                Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = colors.Foreground
            });
            captions.Add(value.Key, new VerticalStackCaption(text, style, builder.MeasureText(text, style)));
        }
        return captions;
    }

    private static ChartRect ReserveVerticalTotalGutters(ChartRect bounds,
        Dictionary<(ChartAxisSide Axis, ChartBarCoordinateKey Coordinate, bool Positive), VerticalStackCaption> totals, double spacing) {
        spacing = Math.Max(2, spacing);
        var top = 0d; var bottom = 0d;
        foreach (var total in totals) {
            if (total.Key.Positive) top = Math.Max(top, total.Value.Metrics.Height + spacing);
            else bottom = Math.Max(bottom, total.Value.Metrics.Height + spacing);
        }
        // Retain measured space outside the value mapper rather than relying on its
        // numeric range padding. Tall or multiline fonts must leave a useful subset
        // of totals, with the same bounded overflow policy as horizontal stacks.
        var available = bounds.Height - Math.Min(bounds.Height / 2, Math.Max(1, spacing));
        var scale = top + bottom > available ? available / (top + bottom) : 1;
        top *= scale; bottom *= scale;
        return new ChartRect(bounds.Left, bounds.Top + top, bounds.Width, bounds.Height - top - bottom);
    }

    private static void AddStackTotals(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartBarCoordinateMap coordinates, ChartMapper primary, ChartMapper? secondary, List<LabelPlacementRequest> labels,
        Dictionary<(ChartAxisSide Axis, ChartBarCoordinateKey Coordinate, bool Positive), VerticalStackCaption> captions) {
        var totals = new Dictionary<(ChartAxisSide Axis, ChartBarCoordinateKey Coordinate, bool Positive), (double Value, double Center, double Left, double Right)>();
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            if (series.Kind != ChartSeriesKind.Bar) continue;
            var mapper = series.YAxis == ChartAxisSide.Secondary ? secondary! : primary;
            var layout = ResolveBarLayout(chart, context, plot, mapper, seriesIndex);
            for (var pointIndex = 0; pointIndex < series.Points.Count; pointIndex++) {
                var point = series.Points[pointIndex];
                var key = (series.YAxis, coordinates.Resolve(seriesIndex, pointIndex), point.Y >= 0);
                var center = mapper.X(point.X) + layout.Offset;
                var left = center - layout.Width / 2; var right = center + layout.Width / 2;
                if (ChartHistogramBarSlot.TryResolve(chart, coordinates, seriesIndex, pointIndex, mapper, out var histogramLeft, out var width)) {
                    left = histogramLeft; right = left + width; center = left + width / 2;
                }
                var exists = totals.TryGetValue(key, out var previous);
                totals[key] = (previous.Value + point.Y, center, exists ? Math.Min(previous.Left, left) : left, exists ? Math.Max(previous.Right, right) : right);
            }
        }
        var ordinal = 0;
        foreach (var total in totals.OrderBy(item => item.Key.Axis).ThenBy(item => item.Key.Coordinate.Value).ThenByDescending(item => item.Key.Positive)) {
            var value = total.Value.Value;
            if (Math.Abs(value) < .000001) continue;
            var mapper = total.Key.Axis == ChartAxisSide.Secondary ? secondary! : primary;
            var anchor = new ChartPoint(total.Value.Center, mapper.Y(value));
            var caption = captions[total.Key];
            var text = caption.Text;
            var style = caption.Style;
            var displayed = TextCaseTransformer.Apply(text, style.TextCase, System.Globalization.CultureInfo.InvariantCulture);
            var id = "stack-total-" + total.Key.Axis.ToString().ToLowerInvariant() + "-" + Number(ordinal++);
            builder.AddRegion(new VisualSemanticRegion(id, "stack-total", new ChartRect(anchor.X, anchor.Y, 0, 0), displayed));
            using (builder.PushGroup(id, "stack-total", new Dictionary<string, string> {
                ["data-cfx-x"] = Number(total.Key.Coordinate.Value), ["data-cfx-y"] = Number(value), ["data-cfx-label"] = displayed,
                ["data-cfx-axis"] = total.Key.Axis.ToString().ToLowerInvariant()
            })) { }
            labels.Add(new LabelPlacementRequest(text, anchor, style, new[] {
                new LabelCandidate(0, value >= 0 ? -context.Theme.Spacing : context.Theme.Spacing, .5, value >= 0 ? 1 : 0),
                new LabelCandidate(total.Value.Right - anchor.X + context.Theme.Spacing, 0, 0, value >= 0 ? 0 : 1),
                new LabelCandidate(total.Value.Left - anchor.X - context.Theme.Spacing, 0, 1, value >= 0 ? 0 : 1)
            }, priority: 1) { AssociatedMarkId = id, MeasuredSize = caption.Metrics });
        }
    }
}
