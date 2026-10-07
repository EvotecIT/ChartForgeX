using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // One formatter result per axis/value serves gutter measurement, placement and detached semantics.
    private sealed class AxisLabelCache {
        private readonly Dictionary<ChartAxis, Dictionary<double, string>> _labels = new();
        internal string Format(ChartAxis axis, double value, Func<double, string>? fallback, IReadOnlyList<double> ticks) {
            if (!_labels.TryGetValue(axis, out var labels)) _labels.Add(axis, labels = new Dictionary<double, string>());
            if (!labels.TryGetValue(value, out var text)) labels.Add(value, text = ChartAxisValueFormatter.Format(axis, value, fallback, ticks));
            return text;
        }
    }

    private static ChartRect MeasurePlot(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect viewport,
        ChartRange range, ChartRange? secondaryRange, VisualThemeColors colors, AxisLabelCache labels) {
        if (!chart.Options.ShowAxes) return viewport;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground });
        var titleStyle = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.Foreground });
        var spacing = context.Theme.Spacing;
        var left = chart.Options.YAxis.Visible ? TickMetrics(builder, chart.Options.YAxis, range.MinY, range.MaxY, style, chart.Options.ValueFormatter, labels).Width + spacing : 0;
        var right = secondaryRange != null && chart.Options.SecondaryYAxis.Visible
            ? TickMetrics(builder, chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY, style, chart.Options.ValueFormatter, labels).Width + spacing : 0;
        var bottom = chart.Options.XAxis.Visible ? TickMetrics(builder, chart.Options.XAxis, range.MinX, range.MaxX, style, null, labels).Height + spacing : 0;
        var xTitle = XAxisTitle(chart);
        if (chart.Options.XAxis.Visible && xTitle.Length > 0) bottom += builder.MeasureText(xTitle, titleStyle).Height + spacing;
        var top = 0d;
        if (chart.Options.YAxis.Visible && chart.YAxisTitle.Length > 0) top = builder.MeasureText(chart.YAxisTitle, titleStyle).Height + spacing;
        if (secondaryRange != null && chart.Options.SecondaryYAxis.Visible && chart.SecondaryYAxisTitle.Length > 0)
            top = Math.Max(top, builder.MeasureText(chart.SecondaryYAxisTitle, titleStyle).Height + spacing);
        // Tick text is shortened in its reserved strip; it cannot consume the entire chart width.
        left = Math.Min(left, viewport.Width * .35);
        right = Math.Min(right, viewport.Width * .35);
        return new ChartRect(viewport.Left + left, viewport.Top + top, Math.Max(0, viewport.Width - left - right), Math.Max(0, viewport.Height - top - bottom));
    }

    private static TextMetrics TickMetrics(VisualSceneBuilder builder, ChartAxis axis, double minimum, double maximum, TextStyle style, Func<double, string>? fallback, AxisLabelCache labels) {
        var ticks = AxisTicks(axis, minimum, maximum);
        var width = 0d; var height = 0d;
        foreach (var tick in ticks) {
            var metrics = builder.MeasureText(labels.Format(axis, tick, fallback, ticks), style);
            width = Math.Max(width, metrics.Width); height = Math.Max(height, metrics.Height);
        }
        return new TextMetrics(width, height, height);
    }

    private static string XAxisTitle(Chart chart) {
        var title = chart.XAxisTitle;
        if (chart.Options.XAxis.Scale == ChartScaleKind.Time && chart.Options.XAxis.ShowTimeZone) {
            var zone = chart.Options.XAxis.TimeZoneLabel ?? chart.Options.XAxis.TimeZone?.Id ?? "UTC";
            title = title.Length == 0 ? zone : title + " (" + zone + ")";
        }
        return title;
    }

    private static void DrawAxes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, ChartRange range,
        ChartMapper map, ChartRange? secondaryRange, ChartMapper? secondaryMap, VisualThemeColors colors, ChartRect viewport, AxisLabelCache labels) {
        var xTicks = AxisTicks(chart.Options.XAxis, range.MinX, range.MaxX);
        var yTicks = AxisTicks(chart.Options.YAxis, range.MinY, range.MaxY);
        if (chart.Options.ShowGrid) {
            var grid = chart.Options.ResolvePreparedGridLineStyle();
            var gridWidth = chart.Options.HasPreparedGridStrokeWidth ? grid.StrokeWidth : context.Theme.GridStrokeWidth;
            var dash = grid.Dash > 0 && grid.Gap > 0 ? new[] { grid.Dash, grid.Gap } : null;
            foreach (var tick in yTicks) {
                var y = map.Y(tick);
                if (grid.ShowHorizontalLines) builder.Line(plot.Left, y, plot.Right, y, colors.Border.WithOpacity(grid.HorizontalOpacity), gridWidth, role: "grid-y", dash: dash);
            }
            foreach (var tick in xTicks) {
                var x = map.X(tick);
                if (grid.ShowVerticalLines) builder.Line(x, plot.Top, x, plot.Bottom, colors.Border.WithOpacity(grid.VerticalOpacity), gridWidth, role: "grid-x", dash: dash);
            }
        }
        if (!chart.Options.ShowAxes) return;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle {
            Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground
        });
        var spacing = context.Theme.Spacing;
        if (chart.Options.XAxis.Visible) {
            if (chart.Options.XAxis.ShowLine) builder.Line(plot.Left, plot.Bottom, plot.Right, plot.Bottom, colors.Border, context.Theme.AxisStrokeWidth, role: "axis-x");
            var tickHeight = TickMetrics(builder, chart.Options.XAxis, range.MinX, range.MaxX, style, null, labels).Height;
            var bounds = new ChartRect(plot.Left, plot.Bottom + spacing, plot.Width,
                Math.Max(0, Math.Min(tickHeight, viewport.Bottom - plot.Bottom - spacing)));
            DrawAxisLabels(builder, chart.Options.XAxis, xTicks, map.X, true, false, bounds, style, spacing, null, labels);
            var title = XAxisTitle(chart);
            if (!string.IsNullOrEmpty(title)) DrawAxisTitle(chart, context, builder, title,
                new ChartRect(plot.Left, bounds.Bottom + spacing, plot.Width, Math.Max(0, viewport.Bottom - bounds.Bottom - spacing)),
                colors, TextAlignment.Center, "axis-x-title");
        }
        if (chart.Options.YAxis.Visible) {
            if (chart.Options.YAxis.ShowLine) builder.Line(plot.Left, plot.Top, plot.Left, plot.Bottom, colors.Border, context.Theme.AxisStrokeWidth, role: "axis-y");
            var bounds = new ChartRect(viewport.Left, plot.Top, Math.Max(0, plot.Left - viewport.Left - spacing), plot.Height);
            DrawAxisLabels(builder, chart.Options.YAxis, yTicks, map.Y, false, false, bounds, style, spacing, chart.Options.ValueFormatter, labels);
            if (!string.IsNullOrEmpty(chart.YAxisTitle)) DrawAxisTitle(chart, context, builder, chart.YAxisTitle,
                new ChartRect(plot.Left, viewport.Top, Math.Max(0, secondaryMap != null && chart.Options.SecondaryYAxis.Visible && chart.SecondaryYAxisTitle.Length > 0 ? plot.Width / 2 - spacing / 2 : plot.Width), Math.Max(0, plot.Top - viewport.Top - spacing)),
                colors, TextAlignment.Left, "axis-y-title");
        }
        if (secondaryMap != null && secondaryRange != null && chart.Options.SecondaryYAxis.Visible) {
            var ticks = AxisTicks(chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY);
            if (chart.Options.SecondaryYAxis.ShowLine) builder.Line(plot.Right, plot.Top, plot.Right, plot.Bottom, colors.Border, context.Theme.AxisStrokeWidth, role: "axis-secondary-y");
            var bounds = new ChartRect(plot.Right + spacing, plot.Top, Math.Max(0, viewport.Right - plot.Right - spacing), plot.Height);
            DrawAxisLabels(builder, chart.Options.SecondaryYAxis, ticks, secondaryMap.Y, false, true, bounds, style, spacing, chart.Options.ValueFormatter, labels);
            if (!string.IsNullOrEmpty(chart.SecondaryYAxisTitle)) {
                var width = chart.YAxisTitle.Length > 0 && chart.Options.YAxis.Visible ? plot.Width / 2 - spacing / 2 : plot.Width;
                DrawAxisTitle(chart, context, builder, chart.SecondaryYAxisTitle,
                    new ChartRect(plot.Right - width, viewport.Top, Math.Max(0, width), Math.Max(0, plot.Top - viewport.Top - spacing)),
                    colors, TextAlignment.Right, "axis-secondary-y-title");
            }
        }
    }

    private static IReadOnlyList<double> AxisTicks(ChartAxis axis, double minimum, double maximum) =>
        axis.Labels.Count > 0 ? axis.Labels.Select(label => label.Value).Where(value => value >= minimum && value <= maximum).Distinct().OrderBy(value => value).ToArray()
            : ChartTicks.GenerateInside(axis, minimum, maximum);

    private static void DrawAxisLabels(VisualSceneBuilder builder, ChartAxis axis, IReadOnlyList<double> ticks, Func<double, double> coordinate,
        bool horizontal, bool secondary, ChartRect bounds, TextStyle style, double spacing, Func<double, string>? fallback, AxisLabelCache labels) {
        var requests = new List<LabelPlacementRequest>();
        var role = horizontal ? "axis-x-label" : secondary ? "axis-secondary-y-label" : "axis-y-label";
        var index = 0;
        foreach (var tick in ticks) {
            var text = labels.Format(axis, tick, fallback, ticks);
            var value = coordinate(tick);
            var anchor = horizontal ? new ChartPoint(value, bounds.Top) : new ChartPoint(secondary ? bounds.Left : bounds.Right, value);
            // Regions describe source content, including text with no visible placement. They are not hit-test targets.
            var regionBounds = horizontal ? new ChartRect(value, bounds.Top, 0, bounds.Height) : new ChartRect(bounds.Left, value, bounds.Width, 0);
            builder.AddRegion(new VisualSemanticRegion(role + "-" + Number(index++), role, regionBounds,
                TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture) + " (" + Number(tick) + ")"));
            if (text.Length == 0) continue;
            requests.Add(new LabelPlacementRequest(text, anchor, style, horizontal
                ? new[] { new LabelCandidate(0, 0, .5, 0), new LabelCandidate(0, 0, 0, 0), new LabelCandidate(0, 0, 1, 0) }
                : new[] { new LabelCandidate(0, 0, secondary ? 0 : 1, .5), new LabelCandidate(0, 0, secondary ? 0 : 1, 0), new LabelCandidate(0, 0, secondary ? 0 : 1, 1) }));
        }
        // End ticks use the same bounds policy as intermediate ticks: labels cannot escape the measured frame.
        foreach (var request in requests) request.MeasuredSize = builder.MeasureText(request.Text, request.Style);
        var gap = axis.LabelDensity == ChartLabelDensity.Dense ? 0 : axis.LabelDensity == ChartLabelDensity.Relaxed ? spacing * 2 : spacing / 2;
        var placed = new LabelPlacementService().Place(requests, bounds, null, gap, builder.MeasureText);
        if (placed.Any(label => label.IsDropped || label.IsEllipsized))
            builder.AddDiagnostic(new VisualDiagnostic("cartesian.axis-label-overflow", "Axis labels were shortened or omitted to remain within the available frame."));
        foreach (var label in placed) {
            if (label.IsDropped) continue;
            var displayedStyle = DisplayedStyle(label.Request.Style);
            builder.Text(label.Text, label.Bounds.Left, label.Bounds.Top + builder.TextAscent(displayedStyle), displayedStyle,
                role: role);
        }
    }

    private static void DrawAxisTitle(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string text, ChartRect bounds,
        VisualThemeColors colors, TextAlignment alignment, string role) {
        var style = chart.Options.AxisTitleStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.Foreground, Alignment = alignment });
        builder.AddRegion(new VisualSemanticRegion(role, role, bounds, TextCaseTransformer.Apply(text, style.TextCase, CultureInfo.InvariantCulture)));
        var fraction = alignment == TextAlignment.Right ? 1 : alignment == TextAlignment.Center ? .5 : 0;
        var request = new LabelPlacementRequest(text, new ChartPoint(bounds.Left + bounds.Width * fraction, bounds.Top), style,
            new[] { new LabelCandidate(0, 0, fraction, 0) }) { MeasuredSize = builder.MeasureText(text, style) };
        var label = new LabelPlacementService().Place(new[] { request }, bounds, null, 2, builder.MeasureText)[0];
        if (label.IsDropped || label.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("cartesian.axis-title-overflow", "An axis title was shortened or omitted within its measured frame."));
        if (label.IsDropped) return;
        var displayed = DisplayedStyle(style); displayed.Alignment = TextAlignment.Left;
        builder.Text(label.Text, label.Bounds.Left, label.Bounds.Top + builder.TextAscent(displayed), displayed, role: role);
    }
}
