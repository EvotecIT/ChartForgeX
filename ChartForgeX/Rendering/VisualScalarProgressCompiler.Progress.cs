using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    private static void Progress(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.Points.Count == 0) { builder.AddDiagnostic(new VisualDiagnostic("progress.no-data", "The progress chart has no rows.")); return; }
        if (series.Points.Any(point => point.Y < 0)) throw new InvalidOperationException("Progress rows require non-negative values.");
        var labels = series.Points.Select(point => Category(chart, point.X)).ToArray();
        var values = series.Points.Select((point, index) => Value(chart, series, index, point.Y)).ToArray();
        var gap = context.Theme.Spacing; var rowHeight = plot.Height / series.Points.Count;
        var tickStyle = Style(chart, context, series, -1, colors.MutedForeground, context.Theme.Typography.AxisSize, 400, true);
        var labelWidth = Math.Min(plot.Width * .3, labels.Max(label => builder.MeasureText(label, tickStyle).Width) + gap);
        var valueWidth = chart.Options.ShowProgressValues ? Math.Min(plot.Width * .25, values.Select((value, index) => builder.MeasureText(value,
            Style(chart, context, series, index, colors.Foreground, context.Theme.Typography.DataLabelSize, 700)).Width).Max() + gap) : 0;
        var thickness = Math.Min(context.Theme.Typography.DataLabelSize * 2, rowHeight * chart.Options.ProgressBarThicknessRatio);
        var handleStroke = Math.Min(Math.Max(1, thickness * .18), rowHeight * .2);
        var handleRadius = chart.Options.ShowProgressHandles ? Math.Max(0, Math.Min(thickness * .62, rowHeight / 2 - handleStroke / 2)) : 0;
        // Handle paint, including its outline, fits the same fixed content viewport at both zero and full progress.
        var handleClearance = handleRadius + (chart.Options.ShowProgressHandles ? handleStroke / 2 : 0);
        var left = plot.Left + labelWidth + handleClearance; var width = Math.Max(0, plot.Width - labelWidth - valueWidth - handleClearance * 2);
        if (width <= 0 || thickness <= 0) { builder.AddDiagnostic(new VisualDiagnostic("progress.insufficient-space", "No space remains for progress tracks and handles.")); return; }
        using (builder.PushGroup(Id(0), "progress-bar-chart", new Dictionary<string, string> {
            ["data-cfx-maximum"] = N(chart.Options.ProgressMaximum), ["data-cfx-show-values"] = chart.Options.ShowProgressValues ? "true" : "false",
            ["data-cfx-show-handles"] = chart.Options.ShowProgressHandles ? "true" : "false", ["data-cfx-bar-thickness-ratio"] = N(chart.Options.ProgressBarThicknessRatio),
            ["data-cfx-track-opacity"] = N(chart.Options.ProgressTrackOpacity), ["data-cfx-series-key"] = series.InteractionIdentityKey
        })) for (var index = 0; index < series.Points.Count; index++) {
            var raw = series.Points[index].Y; var ratio = VisualRadialPrimitives.Clamp(raw / chart.Options.ProgressMaximum);
            var y = plot.Top + rowHeight * (index + .5); var color = ChartSeriesColours.Point(series, index, index, colors);
            var bounds = new ChartRect(plot.Left, plot.Top + rowHeight * index, plot.Width, rowHeight);
            builder.AddRegion(new VisualSemanticRegion(Id(0, index), "progress-row", bounds, labels[index] + ": " + values[index]));
            using (builder.PushGroup(Id(0, index), "progress-row", new Dictionary<string, string> {
                ["data-cfx-point"] = N(index), ["data-cfx-label"] = labels[index], ["data-cfx-value"] = N(raw), ["data-cfx-ratio"] = N(ratio), ["data-cfx-full-label"] = values[index]
            })) {
                Text(chart, context, builder, series, -1, labels[index], new ChartRect(plot.Left, bounds.Top, Math.Max(0, labelWidth - gap / 2), rowHeight), "progress-label", Id(0, index) + "-label", colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
                builder.Rect(new ChartRect(left, y - thickness / 2, width, thickness), ChartColorMath.WithOpacity(colors.Border, chart.Options.ProgressTrackOpacity), radius: thickness / 2, role: "progress-track");
                builder.Rect(new ChartRect(left, y - thickness / 2, width * ratio, thickness), color, radius: Math.Min(thickness / 2, width * ratio / 2), role: "progress-fill");
                if (chart.Options.ShowProgressHandles) builder.Ellipse(left + width * ratio, y, handleRadius, handleRadius, colors.Surface, color, handleStroke, "progress-handle");
                if (chart.Options.ShowProgressValues) Text(chart, context, builder, series, index, values[index],
                    new ChartRect(left + width + handleClearance, bounds.Top, valueWidth, rowHeight), "progress-value", Id(0, index) + "-value", colors.Foreground, context.Theme.Typography.DataLabelSize, 700);
            }
        }
    }
}
