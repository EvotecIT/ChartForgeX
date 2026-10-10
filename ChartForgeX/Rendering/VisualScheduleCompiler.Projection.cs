using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualScheduleCompiler {
    private const double GanttLaneOpenEndStrokeWidth = 1.5;

    private static double TodayWidth(Chart chart, VisualRenderContext context) => VisualReferenceStroke.PaintedWidth(
        chart.Options.GanttTodayStyle, context.Theme.Resolve(context.ThemeMode).Status.Critical.Fill, context.Theme.AxisStrokeWidth);

    private static ChartRect GuideProjection(Chart chart, VisualRenderContext context, ChartRect plot, double min, double max,
        double minimumExtent, double maximumExtent, double? now, bool lanes = false) {
        var guides = new List<ChartPoint>();
        void Include(double value, double width) {
            if (width <= 0 || value < min || value > max) return;
            var fraction = lanes ? ChartMath.Normalize(value, min, max) : ChartScaleTransform.Normalize(value, min, max, chart.Options.XAxis);
            guides.Add(new ChartPoint(fraction, width / 2));
        }
        if (now.HasValue) Include(now.Value, TodayWidth(chart, context));
        if (!lanes) foreach (var annotation in chart.Annotations) {
            if (annotation.Kind == ChartAnnotationKind.VerticalLine && !annotation.EndValue.HasValue)
                Include(annotation.Value, VisualReferenceStroke.PaintedWidth(annotation.StrokeStyle, annotation.Color, context.Theme.AxisStrokeWidth));
        }
        var axis = chart.Options.XAxis;
        var initial = MarkProjection(axis, plot, minimumExtent, maximumExtent);
        var left = initial.Left - plot.Left; var right = plot.Right - initial.Right;
        var largest = 0d;
        var fits = true;
        var bounds = new VisualAxisPaintBounds();
        bounds.Include(0, left); bounds.Include(1, right);
        foreach (var guide in guides) {
            largest = Math.Max(largest, guide.Y);
            var coordinate = left + guide.X * initial.Width;
            if (!axis.Minimum.HasValue && coordinate < guide.Y || !axis.Maximum.HasValue && plot.Width - coordinate < guide.Y) fits = false;
            bounds.Include(guide.X, guide.Y);
        }
        if (fits) return initial;
        if (bounds.TryExpand(0, 1, plot.Width, axis.Minimum.HasValue, axis.Maximum.HasValue, out var first, out var last)) {
            var width = plot.Width / (last - first);
            return new ChartRect(plot.Left - first * width, plot.Top, width, plot.Height);
        }
        // An uncontainable stroke still uses the bounded clipping reservation.
        return MarkProjection(axis, plot, Math.Max(minimumExtent, largest), Math.Max(maximumExtent, largest));
    }

    /// <summary>Reserves painted mark extents at automatic axis sides without changing dates or authored clipping windows.</summary>
    private static ChartRect MarkProjection(ChartAxis axis, ChartRect plot, double minimumExtent, double maximumExtent) {
        var left = axis.Minimum.HasValue ? 0 : Math.Min(plot.Width / 2, minimumExtent);
        var right = axis.Maximum.HasValue ? 0 : Math.Min(plot.Width / 2, maximumExtent);
        return new ChartRect(plot.Left + left, plot.Top, Math.Max(0, plot.Width - left - right), plot.Height);
    }
}
