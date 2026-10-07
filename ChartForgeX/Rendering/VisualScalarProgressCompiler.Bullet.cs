using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    private static void Bullets(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        foreach (var series in chart.Series) Range(series, out _, out _);
        var min = chart.Options.XAxis.Minimum ?? chart.Series.Min(series => series.Points[0].X);
        var max = chart.Options.XAxis.Maximum ?? chart.Series.Max(series => series.Points[1].X);
        if (max <= min || !Finite(max - min)) throw new InvalidOperationException("Bullet charts require a finite shared positive range.");
        var labels = chart.Series.Select(series => Value(chart, series, 0, series.Points[0].Y)).ToArray();
        var targets = chart.Series.Select(series => Value(chart, series, 1, series.Points[1].Y)).ToArray();
        var gap = context.Theme.Spacing;
        var labelWidth = 0d; var valueWidth = 0d;
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index]; if (series.ShowDataLabels == false) continue;
            var style = Style(chart, context, series, 0, colors.Foreground, context.Theme.Typography.DataLabelSize, 400);
            labelWidth = Math.Max(labelWidth, builder.MeasureText(series.Name, style).Width + gap);
            valueWidth = Math.Max(valueWidth, Math.Max(builder.MeasureText(labels[index], style).Width,
                builder.MeasureText("target " + targets[index], Style(chart, context, series, 1, colors.MutedForeground, context.Theme.Typography.DataLabelSize, 400)).Width) + gap * 2);
        }
        labelWidth = Math.Min(plot.Width * .3, labelWidth); valueWidth = Math.Min(plot.Width * .3, valueWidth);
        var axisHeight = chart.Options.ShowAxes && chart.Options.XAxis.Visible ? Math.Min(plot.Height * .25, context.Theme.Typography.AxisSize * 1.8 + gap / 2) : 0;
        var rowHeight = Math.Max(0, (plot.Height - axisHeight) / chart.Series.Count);
        var barHeight = Math.Min(context.Theme.Typography.DataLabelSize * 1.8, rowHeight * .32);
        var halfStroke = Math.Max(context.Theme.SeriesStrokeWidth, context.Theme.AxisStrokeWidth) / 2;
        var left = plot.Left + labelWidth + halfStroke; var width = Math.Max(0, plot.Width - labelWidth - valueWidth - halfStroke * 2);
        if (width <= 0 || barHeight <= 0) { builder.AddDiagnostic(new VisualDiagnostic("bullet.insufficient-space", "No space remains for bullet rows.")); return; }
        double X(double value) => left + width * VisualRadialPrimitives.Clamp((VisualRadialPrimitives.Clamp(value, min, max) - min) / (max - min));
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index]; var value = series.Points[0].Y; var target = series.Points[1].Y;
            var y = plot.Top + rowHeight * (index + .5); var bounds = new ChartRect(plot.Left, plot.Top + rowHeight * index, plot.Width, rowHeight);
            var status = value < target ? "below-target" : value > target ? "above-target" : "meets-target";
            var state = series.StateRole == ChartSeriesState.None ? value < target ? ChartSeriesState.Danger : ChartSeriesState.Success : series.StateRole;
            var color = ChartSeriesColours.Point(series, index, 0, colors);
            builder.AddRegion(new VisualSemanticRegion(Id(index), "bullet-row", bounds, series.Name + ": " + labels[index] + ", target " + targets[index]));
            using (builder.PushGroup(Id(index), "bullet-row", new Dictionary<string, string> {
                ["data-cfx-series"] = N(index), ["data-cfx-label"] = series.Name, ["data-cfx-value"] = N(value), ["data-cfx-target"] = N(target),
                ["data-cfx-min"] = N(series.Points[0].X), ["data-cfx-max"] = N(series.Points[1].X), ["data-cfx-scale-min"] = N(min), ["data-cfx-scale-max"] = N(max),
                ["data-cfx-status"] = status, ["data-cfx-state"] = state.ToString(), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                ["data-cfx-source-range-ends"] = string.Join(",", series.Points.Skip(2).Select(point => N(point.X)))
            })) {
                var ends = series.Points.Skip(2).Select(point => point.X).Where(end => end > min && end < max).OrderBy(end => end).Distinct().ToList();
                if (ends.Count == 0) { ends.Add(min + (max - min) * .6); ends.Add(min + (max - min) * .8); }
                ends.Add(max); var previous = min;
                for (var range = 0; range < ends.Count; range++) {
                    var rangeBounds = new ChartRect(X(previous), y - barHeight / 2, Math.Max(0, X(ends[range]) - X(previous)), barHeight);
                    using (builder.PushGroup(null, "bullet-range-source", new Dictionary<string, string> { ["data-cfx-min"] = N(previous), ["data-cfx-max"] = N(ends[range]) }))
                        builder.Rect(rangeBounds, ChartColorMath.WithOpacity(color, ChartMarkSurface.BulletRangeOpacity(range)), role: "bullet-range");
                    previous = ends[range];
                }
                builder.Rect(new ChartRect(left, y - barHeight / 6, X(value) - left, barHeight / 3), color, role: "bullet-value");
                builder.Line(X(target), y - barHeight * .7, X(target), y + barHeight * .7, colors.Foreground, context.Theme.SeriesStrokeWidth, "bullet-target");
                if (series.ShowDataLabels != false) {
                    Text(chart, context, builder, series, 0, series.Name, new ChartRect(plot.Left, bounds.Top, Math.Max(0, labelWidth - gap), rowHeight), "bullet-row-label", Id(index) + "-label", colors.Foreground, context.Theme.Typography.DataLabelSize);
                    var marker = Math.Min(context.Theme.MarkerRadius, Math.Min(rowHeight / 8, gap / 3));
                    builder.Ellipse(left + width + gap / 2, y, marker, marker, ChartSeriesColours.State(state, colors, colors.Accent), role: "bullet-status-marker");
                    var textLeft = left + width + gap;
                    Text(chart, context, builder, series, 0, labels[index], new ChartRect(textLeft, bounds.Top, Math.Max(0, plot.Right - textLeft), rowHeight / 2), "bullet-value-label", Id(index) + "-value", colors.Foreground, context.Theme.Typography.DataLabelSize, 700);
                    Text(chart, context, builder, series, 1, "target " + targets[index], new ChartRect(textLeft, y, Math.Max(0, plot.Right - textLeft), rowHeight / 2), "bullet-target-label", Id(index) + "-target", colors.MutedForeground, context.Theme.Typography.DataLabelSize);
                }
            }
        }
        if (axisHeight > 0) {
            var y = plot.Bottom - axisHeight;
            builder.Line(left, y, left + width, y, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis");
            for (var index = 0; index < 5; index++) {
                var tick = min + (max - min) * index / 4; var x = X(tick);
                builder.Line(x, y, x, y + axisHeight / 6, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis-tick");
                var text = ChartAxisValueFormatter.Format(chart.Options.XAxis, tick, chart.Options.ValueFormatter);
                var boxWidth = width / 4;
                var boxLeft = Math.Max(left, Math.Min(left + width - boxWidth, x - boxWidth / 2));
                Text(chart, context, builder, chart.Series[0], -1, text, new ChartRect(boxLeft, y + axisHeight / 6, boxWidth, axisHeight * 5 / 6),
                    "bullet-axis-label", "bullet-axis-label-" + index, colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
            }
        }
    }
}
