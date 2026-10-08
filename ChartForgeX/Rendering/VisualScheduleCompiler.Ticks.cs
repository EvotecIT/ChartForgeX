using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Diagnostics;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    /// <summary>Selects schedule ticks from their final clamped, styled boxes, keeping endpoints ahead of interior labels.</summary>
    private static void TickLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ScheduleLayout layout, IReadOnlyList<double> ticks, Func<double, double> project, Func<double, string> format, TextStyle style) {
        var plot = layout.Plot; var gap = context.Theme.Spacing;
        var density = chart.Options.XAxis.LabelDensity;
        var rotation = Math.Max(-80, Math.Min(80, chart.Options.XAxis.LabelAngle));
        var top = Math.Min(viewport.Bottom, plot.Bottom + gap / 2);
        var height = Math.Max(0, Math.Min(layout.AxisReserve - gap / 2, viewport.Bottom - top));
        var labels = new List<(string Text, double X, ChartRect Bounds, TextStyle Style)>();
        var requests = new List<LabelPlacementRequest>();
        for (var index = 0; index < ticks.Count; index++) {
            var text = format(ticks[index]); var x = project(ticks[index]); var tickStyle = style;
            if (chart.Options.TryGetXAxisLabelHighlight(ticks[index], out var highlight)) { tickStyle = style.Clone(); tickStyle.Color = highlight; }
            var width = Math.Min(builder.MeasureText(text, tickStyle).Width, plot.Width);
            var left = Math.Max(plot.Left, Math.Min(plot.Right - width, x - width / 2));
            var bounds = new ChartRect(left, top, width, height);
            labels.Add((text, x, bounds, tickStyle));
            var footprint = TickFootprint(builder, text, tickStyle, bounds, x, rotation);
            requests.Add(new LabelPlacementRequest(text, new ChartPoint(footprint.Left, footprint.Top), tickStyle,
                new[] { new LabelCandidate(0, 0) }, priority: index == 0 || index == ticks.Count - 1 ? 1 : 0) {
                MeasuredSize = new TextMetrics(footprint.Width, footprint.Height, footprint.Height), Fallback = LabelFallbackRule.Drop
            });
        }
        // Explicit All accepts overlaps. Automatic policies select only after edge clamping and rotation,
        // so a preserved final endpoint cannot silently collide with the preceding selected label.
        var separation = density == ChartLabelDensity.Dense ? 0 : density == ChartLabelDensity.Relaxed ? gap * 2 : gap / 2;
        var placed = density == ChartLabelDensity.All ? null
            : new LabelPlacementService().Place(requests, viewport, null, separation, builder.MeasureText);
        var omitted = false;
        for (var index = 0; index < labels.Count; index++) {
            var label = labels[index]; var id = "schedule-tick-" + index;
            if (placed != null && placed[index].IsDropped) {
                builder.AddRegion(new VisualSemanticRegion(id, "schedule-tick-label", new ChartRect(label.X, top, 0, 0), label.Text));
                omitted = true;
                continue;
            }
            using (builder.PushClip(viewport))
            using (builder.PushRotation(rotation, label.X, label.Bounds.Top))
                VisualStateSceneTools.Text(builder, label.Text, label.Bounds, label.Style, "schedule-tick-label", id, TextAlignment.Center);
        }
        if (omitted) builder.AddDiagnostic(new VisualDiagnostic("schedule.axis-label-overflow",
            "Some axis labels were omitted to preserve measured spacing; their complete values remain in descriptive regions."));
    }

    private static ChartRect TickFootprint(VisualSceneBuilder builder, string text, TextStyle style, ChartRect bounds, double x, double rotation) {
        var displayed = ChartTextFitting.TrimEnd(text, style.EffectiveFontSize, bounds.Width, (value, _) => builder.MeasureText(value, style).Width);
        var metrics = builder.MeasureText(displayed, style);
        var left = bounds.Left + (bounds.Width - metrics.Width) / 2;
        var top = bounds.Top + (bounds.Height - metrics.Height) / 2;
        var transform = VisualSceneTransform.Identity.Rotate(new VisualRotation(rotation, x, bounds.Top));
        var points = new[] {
            transform.Apply(new ChartPoint(left, top)), transform.Apply(new ChartPoint(left + metrics.Width, top)),
            transform.Apply(new ChartPoint(left, top + metrics.Height)), transform.Apply(new ChartPoint(left + metrics.Width, top + metrics.Height))
        };
        var minimumX = points.Min(point => point.X); var minimumY = points.Min(point => point.Y);
        return new ChartRect(minimumX, minimumY, points.Max(point => point.X) - minimumX, points.Max(point => point.Y) - minimumY);
    }
}
