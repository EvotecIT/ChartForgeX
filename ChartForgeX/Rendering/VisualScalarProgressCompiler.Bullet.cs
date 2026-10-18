using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    private static void Bullets(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var entries = chart.Series.Select((series, index) => (Series: series, Index: index)).Where(entry => entry.Series.HasSourceData).ToArray();
        foreach (var entry in entries) Range(entry.Series, out _, out _);
        var min = chart.Options.XAxis.Minimum ?? entries.Min(entry => entry.Series.RenderPoints[0].X);
        var max = chart.Options.XAxis.Maximum ?? entries.Max(entry => entry.Series.RenderPoints[1].X);
        if (max <= min || !Finite(max - min)) throw new InvalidOperationException("Bullet charts require a finite shared positive range.");
        var labels = entries.Select(entry => Value(chart, entry.Series, 0, entry.Series.RenderPoints[0].Y)).ToArray();
        var targets = entries.Select(entry => ChartNumericFormatter.FormatValue(chart.Options, entry.Series.BulletValues[0].Target)).ToArray();
        var gap = context.Theme.Spacing;
        var labelWidth = 0d; var valueWidth = 0d;
        for (var index = 0; index < entries.Length; index++) {
            var series = entries[index].Series; if (!series.ResolveDataLabels(chart.Options, defaultVisible: true)) continue;
            var style = Style(chart, context, series, 0, colors.Foreground, context.Theme.Typography.DataLabelSize, 400);
            labelWidth = Math.Max(labelWidth, builder.MeasureText(series.Name, style).Width + gap);
            valueWidth = Math.Max(valueWidth, builder.MeasureText(labels[index], Style(chart, context, series, 0,
                colors.Foreground, context.Theme.Typography.DataLabelSize, 700)).Width
                + builder.MeasureText("/ " + targets[index], Style(chart, context, series, 0, colors.MutedForeground,
                    context.Theme.Typography.DataLabelSize, 400)).Width + gap * 1.5);
        }
        labelWidth = Math.Min(plot.Width * .3, labelWidth); valueWidth = Math.Min(plot.Width * .3, valueWidth);
        var axisHeight = chart.Options.ShowAxes && chart.Options.XAxis.Visible ? Math.Min(plot.Height * .25, context.Theme.Typography.AxisSize * 1.8 + gap / 2) : 0;
        var rowHeight = Math.Max(0, (plot.Height - axisHeight) / entries.Length);
        var barHeight = Math.Min(context.Theme.Typography.DataLabelSize * 1.8, rowHeight * .32);
        var halfStroke = Math.Max(context.Theme.SeriesStrokeWidth, context.Theme.AxisStrokeWidth) / 2;
        var left = plot.Left + labelWidth + halfStroke; var width = Math.Max(0, plot.Width - labelWidth - valueWidth - halfStroke * 2);
        if (width <= 0 || barHeight <= 0) { builder.AddDiagnostic(new VisualDiagnostic("bullet.insufficient-space", "No space remains for bullet rows.")); return; }
        double X(double value) => left + width * VisualRadialPrimitives.Clamp((VisualRadialPrimitives.Clamp(value, min, max) - min) / (max - min));
        for (var index = 0; index < entries.Length; index++) {
            var series = entries[index].Series; var sourceIndex = entries[index].Index;
            var value = series.RenderPoints[0].Y; var target = series.RenderPoints[1].Y;
            var y = plot.Top + rowHeight * (index + .5); var bounds = new ChartRect(plot.Left, plot.Top + rowHeight * index, plot.Width, rowHeight);
            var status = value < target ? "below-target" : value > target ? "above-target" : "meets-target";
            var state = series.StateRole == ChartSeriesState.None ? value < target ? ChartSeriesState.Danger : ChartSeriesState.Success : series.StateRole;
            var color = BulletColor(series, sourceIndex, colors);
            var measurePaint = BulletPaint(series, color);
            builder.AddRegion(new VisualSemanticRegion(Id(sourceIndex), "bullet-row", bounds, series.Name + ": " + labels[index] + ", target " + targets[index]));
            using (builder.PushGroup(Id(sourceIndex), "bullet-row", new Dictionary<string, string> {
                ["data-cfx-series"] = N(sourceIndex), ["data-cfx-source-point"] = "0", ["data-cfx-source-count"] = "1",
                ["data-cfx-label"] = series.Name, ["data-cfx-value"] = N(value), ["data-cfx-target"] = N(target),
                ["data-cfx-label-target"] = chart.Options.Labels.Target,
                ["data-cfx-min"] = N(series.RenderPoints[0].X), ["data-cfx-max"] = N(series.RenderPoints[1].X), ["data-cfx-scale-min"] = N(min), ["data-cfx-scale-max"] = N(max),
                ["data-cfx-status"] = status, ["data-cfx-state"] = state.ToString(), ["data-cfx-series-key"] = series.InteractionIdentityKey,
                ["data-cfx-source-range-ends"] = string.Join(",", series.RenderPoints.Skip(2).Select(point => N(point.X)))
            })) {
                var ends = series.RenderPoints.Skip(2).Select(point => point.X).Where(end => end > min && end < max).OrderBy(end => end).Distinct().ToList();
                if (ends.Count == 0) { ends.Add(min + (max - min) * .6); ends.Add(min + (max - min) * .8); }
                ends.Add(max); var previous = min;
                for (var range = 0; range < ends.Count; range++) {
                    var rangeBounds = new ChartRect(X(previous), y - barHeight / 2, Math.Max(0, X(ends[range]) - X(previous)), barHeight);
                    var neutral = range == 0 ? colors.Neutral : range == 1 ? colors.Neutral2 : colors.Neutral3;
                    using (builder.PushGroup(null, "bullet-range-source", new Dictionary<string, string> { ["data-cfx-min"] = N(previous), ["data-cfx-max"] = N(ends[range]) }))
                        builder.Rect(rangeBounds, neutral, role: "bullet-range", paint: VisualChartPaint.Fill(neutral, SvgColorRole.Surface));
                    previous = ends[range];
                }
                builder.Rect(new ChartRect(left, y - barHeight / 6, X(value) - left, barHeight / 3), color, role: "bullet-value", paint: VisualChartPaint.Fill(measurePaint));
                builder.Line(X(target), y - barHeight / 2, X(target), y + barHeight / 2, colors.Foreground, context.Theme.SeriesStrokeWidth, "bullet-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Text));
                if (series.ResolveDataLabels(chart.Options, defaultVisible: true)) {
                    Text(chart, context, builder, series, 0, series.Name, new ChartRect(plot.Left, bounds.Top, Math.Max(0, labelWidth - gap), rowHeight),
                        "bullet-row-label", Id(sourceIndex) + "-label", colors.Foreground, context.Theme.Typography.DataLabelSize, alignment: TextAlignment.Left);
                    var textLeft = left + width + gap;
                    var available = Math.Max(0, plot.Right - textLeft);
                    var valueStyle = Style(chart, context, series, 0, colors.Foreground, context.Theme.Typography.DataLabelSize, 700);
                    var textWidth = Math.Min(builder.MeasureText(labels[index], valueStyle).Width, available * .6);
                    var valueColor = value < target ? colors.Status.Critical.Ink : colors.Foreground;
                    Text(chart, context, builder, series, 0, labels[index], new ChartRect(textLeft, bounds.Top, textWidth, rowHeight),
                        "bullet-value-label", Id(sourceIndex) + "-value", valueColor, context.Theme.Typography.DataLabelSize, 700, alignment: TextAlignment.Left);
                    var targetLeft = textLeft + textWidth + gap / 2;
                    Text(chart, context, builder, series, 0, "/ " + targets[index], new ChartRect(targetLeft, bounds.Top, Math.Max(0, plot.Right - targetLeft), rowHeight),
                        "bullet-target-label", Id(sourceIndex) + "-target", colors.MutedForeground, context.Theme.Typography.DataLabelSize, alignment: TextAlignment.Left);
                }
            }
        }
        if (axisHeight > 0) {
            var y = plot.Bottom - axisHeight;
            builder.Line(left, y, left + width, y, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var ticks = new double[5];
            for (var index = 0; index < ticks.Length; index++) ticks[index] = min + (max - min) * index / 4;
            var formatTick = ChartAxisValueFormatter.Create(chart.Options.XAxis, ticks, chart.Options.ValueFormatter);
            for (var index = 0; index < 5; index++) {
                var tick = ticks[index]; var x = X(tick);
                builder.Line(x, y, x, y + axisHeight / 6, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
                var text = formatTick(tick);
                var boxWidth = width / 4;
                var boxLeft = Math.Max(left, Math.Min(left + width - boxWidth, x - boxWidth / 2));
                Text(chart, context, builder, entries[0].Series, -1, text, new ChartRect(boxLeft, y + axisHeight / 6, boxWidth, axisHeight * 5 / 6),
                    "bullet-axis-label", "bullet-axis-label-" + index, colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
            }
        }
    }

    private static bool HasBulletPaint(ChartSeries series) => series.Color.HasValue
        || series.PointColors.Count > 0 && series.PointColors[0].HasValue || series.StateRole != ChartSeriesState.None;
    private static ChartColor BulletColor(ChartSeries series, int index, VisualThemeColors colors) =>
        HasBulletPaint(series) ? ChartSeriesColours.Point(series, index, 0, colors) : colors.Foreground;
    private static SvgPaint BulletPaint(ChartSeries series, ChartColor color) => HasBulletPaint(series)
        ? VisualChartPaint.Series(series, color, 0) : SvgPaint.Of(color, SvgColorRole.Text);
}
