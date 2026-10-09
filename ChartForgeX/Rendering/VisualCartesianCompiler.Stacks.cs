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
        internal VerticalStackCaption(ChartStackTotal total, string text, TextStyle style, TextMetrics metrics) { Total = total; Text = text; Style = style; Metrics = metrics; }
        internal ChartStackTotal Total { get; }
        internal string Text { get; }
        internal TextStyle Style { get; }
        internal TextMetrics Metrics { get; }
    }

    private static IReadOnlyList<VerticalStackCaption> ResolveVerticalTotals(
        Chart chart, VisualRenderContext context, VisualSceneBuilder builder, VisualThemeColors colors, ChartStackLayout stacks) {
        var captions = new List<VerticalStackCaption>();
        foreach (var total in stacks.Totals.Where(total => total.Kind == ChartSeriesKind.Bar)) {
            if (total.Value == 0) continue;
            var text = ChartNumericFormatter.FormatValue(chart.Options, total.Value);
            var style = chart.Options.DataLabelStyle.Resolve(new TextStyle {
                Font = context.Font, FontSize = context.Theme.Typography.DataLabelSize, Color = colors.Foreground
            });
            captions.Add(new VerticalStackCaption(total, text, style, builder.MeasureText(text, style)));
        }
        return captions;
    }

    private static ChartRect ReserveVerticalTotalGutters(ChartRect bounds,
        IReadOnlyList<VerticalStackCaption> totals, double spacing) {
        spacing = Math.Max(2, spacing);
        var top = 0d; var bottom = 0d;
        foreach (var total in totals) {
            if (total.Total.Positive) top = Math.Max(top, total.Metrics.Height + spacing);
            else bottom = Math.Max(bottom, total.Metrics.Height + spacing);
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
        ChartBarCoordinateMap coordinates, ChartStackLayout stacks, ChartMapper primary, ChartMapper? secondary, List<LabelPlacementRequest> labels,
        IReadOnlyList<VerticalStackCaption> captions) {
        var ordinal = 0;
        foreach (var caption in captions) {
            var total = caption.Total;
            var value = total.Value;
            var mapper = total.Axis == ChartAxisSide.Secondary ? secondary! : primary;
            var layout = ResolveBarLayout(chart, context, plot, mapper, stacks, total.SeriesIndex);
            var center = mapper.X(total.Coordinate) + layout.Offset;
            var left = center - layout.Width / 2; var right = center + layout.Width / 2;
            if (ChartHistogramBarSlot.TryResolve(chart, coordinates, stacks, total.SeriesIndex, total.PointIndex, mapper, out var histogramLeft, out var width)) {
                left = histogramLeft; right = left + width; center = left + width / 2;
            }
            var anchor = new ChartPoint(center, mapper.Y(value));
            var text = caption.Text;
            var style = caption.Style;
            var displayed = TextCaseTransformer.Apply(text, style.TextCase, System.Globalization.CultureInfo.InvariantCulture);
            var id = "stack-total-" + total.Axis.ToString().ToLowerInvariant() + "-" + Number(ordinal++);
            builder.AddRegion(new VisualSemanticRegion(id, "stack-total", new ChartRect(anchor.X, anchor.Y, 0, 0), displayed));
            using (builder.PushGroup(id, "stack-total", new Dictionary<string, string> {
                ["data-cfx-x"] = Number(total.Coordinate), ["data-cfx-y"] = Number(value), ["data-cfx-label"] = displayed,
                ["data-cfx-source-total"] = Number(total.SourceValue), ["data-cfx-stack-group"] = total.Group ?? string.Empty,
                ["data-cfx-axis"] = total.Axis.ToString().ToLowerInvariant()
            })) { }
            labels.Add(new LabelPlacementRequest(text, anchor, style, new[] {
                new LabelCandidate(0, value >= 0 ? -context.Theme.Spacing : context.Theme.Spacing, .5, value >= 0 ? 1 : 0),
                new LabelCandidate(right - anchor.X + context.Theme.Spacing, 0, 0, value >= 0 ? 0 : 1),
                new LabelCandidate(left - anchor.X - context.Theme.Spacing, 0, 1, value >= 0 ? 0 : 1)
            }, priority: 1) { AssociatedMarkId = id, MeasuredSize = caption.Metrics });
        }
    }
}
