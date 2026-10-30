using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    private static void Bullets(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) =>
        builder.CaptureValue(new BulletPlan(chart, context, plot, colors));

    private static void PaintBullets(BulletPaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        if (chart.Independent || chart.Orientation == ChartOrientation.Vertical) {
            ConfiguredBullets(chart, context, builder, plot, colors); return;
        }
        var entries = chart.Rows.Select(series => (Series: series, Index: series.Index)).ToArray();
        var min = chart.XAxis.Minimum ?? entries.Min(entry => entry.Series.Reading.Minimum);
        var max = chart.XAxis.Maximum ?? entries.Max(entry => entry.Series.Reading.Maximum);
        if (max <= min || !Finite(max - min)) throw new InvalidOperationException("Bullet charts require a finite shared positive range.");
        var labels = entries.Select(entry => entry.Series.AuthoredLabel ?? chart.Format.Format(entry.Series.Reading.Value)).ToArray();
        var targets = entries.Select(entry => chart.Format.Format(entry.Series.Reading.Target)).ToArray();
        var gap = context.Theme.Spacing;
        var labelWidth = 0d; var valueWidth = 0d;
        for (var index = 0; index < entries.Length; index++) {
            var series = entries[index].Series; if (!series.ShowLabels) continue;
            var style = Style(chart, context, series, 0, colors.Foreground, context.Theme.Typography.DataLabelSize, 400);
            labelWidth = Math.Max(labelWidth, chart.Measure(builder, series.Name, style).Width + gap);
            valueWidth = Math.Max(valueWidth, chart.Measure(builder, labels[index], Style(chart, context, series, 0,
                colors.Foreground, context.Theme.Typography.DataLabelSize, 700)).Width
                + chart.Measure(builder, "/ " + targets[index], Style(chart, context, series, 0, colors.MutedForeground,
                    context.Theme.Typography.DataLabelSize, 400)).Width + gap * 1.5);
        }
        labelWidth = Math.Min(plot.Width * .3, labelWidth); valueWidth = Math.Min(plot.Width * .3, valueWidth);
        var axisHeight = chart.ShowAxes && chart.XAxis.Visible ? Math.Min(plot.Height * .25, context.Theme.Typography.AxisSize * 1.8 + gap / 2) : 0;
        // Design rule 10: rows take a fixed slot from the top; the scale sits directly under the last row.
        var rowHeight = ChartRowDensity.Pitch(plot.Height - axisHeight, entries.Length);
        var rowsBottom = plot.Top + rowHeight * entries.Length;
        var barHeight = Math.Min(context.Theme.Typography.DataLabelSize * 1.8, rowHeight * .55);
        var halfStroke = Math.Max(context.Theme.SeriesStrokeWidth, context.Theme.AxisStrokeWidth) / 2;
        var left = plot.Left + labelWidth + halfStroke; var width = Math.Max(0, plot.Width - labelWidth - valueWidth - halfStroke * 2);
        if (width <= 0 || barHeight <= 0) { builder.AddDiagnostic(new VisualDiagnostic("bullet.insufficient-space", "No space remains for bullet rows.")); return; }
        double X(double value) => left + width * VisualRadialPrimitives.Clamp((VisualRadialPrimitives.Clamp(value, min, max) - min) / (max - min));
        for (var index = 0; index < entries.Length; index++) {
            var series = entries[index].Series; var sourceIndex = entries[index].Index;
            var value = series.Reading.Value; var target = series.Reading.Target;
            var y = plot.Top + rowHeight * (index + .5); var bounds = new ChartRect(plot.Left, plot.Top + rowHeight * index, plot.Width, rowHeight);
            var status = value < target ? "below-target" : value > target ? "above-target" : "meets-target";
            var state = series.StateRole == ChartSeriesState.None ? value < target ? ChartSeriesState.Danger : ChartSeriesState.Success : series.StateRole;
            var color = series.Color;
            var measurePaint = series.Paint;
            builder.AddRegion(new VisualSemanticRegion(Id(sourceIndex), "bullet-row", bounds, series.Name + ": " + labels[index] + ", target " + targets[index]));
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = N(sourceIndex), ["data-cfx-source-point"] = "0", ["data-cfx-source-count"] = "1",
                ["data-cfx-label"] = series.Name, ["data-cfx-value"] = N(value), ["data-cfx-target"] = N(target),
                ["data-cfx-label-target"] = chart.LabelTarget,
                ["data-cfx-min"] = N(series.Reading.Minimum), ["data-cfx-max"] = N(series.Reading.Maximum), ["data-cfx-scale-min"] = N(min), ["data-cfx-scale-max"] = N(max),
                ["data-cfx-status"] = status, ["data-cfx-state"] = state.ToString(), ["data-cfx-series-key"] = series.SeriesKey,
                ["data-cfx-source-range-ends"] = string.Join(",", series.Reading.RangeEnds.Select(N))
            };
            chart.CaptureIdentity(metadata, series);
            using (builder.PushGroup(Id(sourceIndex), "bullet-row", metadata)) {
                var ends = series.Reading.RangeEnds.Where(end => end > min && end < max).OrderBy(end => end).Distinct().ToList();
                if (ends.Count == 0) { ends.Add(min + (max - min) * .6); ends.Add(min + (max - min) * .8); }
                ends.Add(max); var previous = min;
                for (var range = 0; range < ends.Count; range++) {
                    var rangeBounds = new ChartRect(X(previous), y - barHeight / 2, Math.Max(0, X(ends[range]) - X(previous)), barHeight);
                    using (builder.PushGroup(null, "bullet-range-source", new Dictionary<string, string> { ["data-cfx-min"] = N(previous), ["data-cfx-max"] = N(ends[range]) }))
                        BulletBand(builder, rangeBounds, range, ends.Count, colors);
                    previous = ends[range];
                }
                builder.Rect(new ChartRect(left, y - barHeight / 6, X(value) - left, barHeight / 3), color, role: "bullet-value", paint: VisualChartPaint.Fill(measurePaint));
                builder.Line(X(target), y - barHeight / 2, X(target), y + barHeight / 2, colors.Foreground, context.Theme.SeriesStrokeWidth, "bullet-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Text));
                if (series.ShowLabels) {
                    Text(chart, context, builder, series, 0, series.Name, new ChartRect(plot.Left, bounds.Top, Math.Max(0, labelWidth - gap), rowHeight),
                        "bullet-row-label", Id(sourceIndex) + "-label", colors.Foreground, context.Theme.Typography.DataLabelSize, alignment: TextAlignment.Left);
                    var textLeft = left + width + gap;
                    var available = Math.Max(0, plot.Right - textLeft);
                    var valueStyle = Style(chart, context, series, 0, colors.Foreground, context.Theme.Typography.DataLabelSize, 700);
                    var textWidth = Math.Min(chart.Measure(builder, labels[index], valueStyle).Width, available * .6);
                    // The value is a quantity, not a status: below-target stays in data-cfx-status, never in the ink.
                    var valueColor = colors.Foreground;
                    BulletNumber(chart, context, builder, series, labels[index], new ChartRect(textLeft, bounds.Top, textWidth, rowHeight),
                        "bullet-value-label", Id(sourceIndex) + "-value", valueColor, context.Theme.Typography.DataLabelSize, 700, alignment: TextAlignment.Left);
                    var targetLeft = textLeft + textWidth + gap / 2;
                    BulletNumber(chart, context, builder, series, "/ " + targets[index], new ChartRect(targetLeft, bounds.Top, Math.Max(0, plot.Right - targetLeft), rowHeight),
                        "bullet-target-label", Id(sourceIndex) + "-target", colors.MutedForeground, context.Theme.Typography.DataLabelSize, alignment: TextAlignment.Left);
                }
            }
        }
        if (axisHeight > 0) {
            var y = rowsBottom;
            builder.Line(left, y, left + width, y, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var ticks = new double[5];
            for (var index = 0; index < ticks.Length; index++) ticks[index] = min + (max - min) * index / 4;
            var formatTick = ChartAxisValueFormatter.Create(chart.XAxis, ticks, chart.ValueFormatter);
            var style = Style(chart, context, entries[0].Series, -1, colors.MutedForeground, context.Theme.Typography.AxisSize, 400, true);
            var labelArea = new ChartRect(plot.Left, y + axisHeight / 6, plot.Width, axisHeight * 5 / 6);
            var requests = new List<LabelPlacementRequest>();
            for (var index = 0; index < ticks.Length; index++) {
                var tick = ticks[index]; var x = X(tick);
                builder.Line(x, y, x, y + axisHeight / 6, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
                var text = formatTick(tick);
                var measured = chart.Measure(builder, text, style);
                var anchor = Math.Max(labelArea.Left + measured.Width / 2, Math.Min(labelArea.Right - measured.Width / 2, x));
                requests.Add(new LabelPlacementRequest(text, new ChartPoint(anchor, labelArea.Top + labelArea.Height / 2), style,
                    new[] { new LabelCandidate(0, 0, .5, .5) }, priority: index == 0 || index == ticks.Length - 1 ? 1 : 0) {
                    Bounds = labelArea, Fallback = LabelFallbackRule.Drop, MeasuredSize = measured
                });
            }
            var placements = new LabelPlacementService().Place(requests, labelArea, null, gap / 2,
                (text, labelStyle) => chart.Measure(builder, text, labelStyle));
            for (var index = 0; index < placements.Count; index++) {
                var placement = placements[index]; var id = "bullet-axis-label-" + index;
                if (placement.IsDropped) {
                    builder.AddRegion(new VisualSemanticRegion(id, "bullet-axis-label", labelArea, requests[index].Text));
                    builder.AddDiagnostic(new VisualDiagnostic("bullet.label-overflow",
                        "A complete bullet scale caption was omitted because it cannot fit without overlapping; the exact fact remains in descriptive regions."));
                } else BulletNumber(chart, context, builder, entries[0].Series, requests[index].Text, placement.Bounds,
                    "bullet-axis-label", id, colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
            }
        }
    }

    /// <summary>
    /// Qualitative bands run from darkest (poor, at the start of the scale) to lightest (good), so the target mark and
    /// the measure always sit on a light enough band to read. Shades are secondary ink over the surface.
    /// </summary>
    private static void BulletBand(VisualSceneBuilder builder, ChartRect bounds, int band, int count, VisualThemeColors colors) {
        var opacity = BulletBandOpacity(band, count);
        var fill = ChartColorMath.WithOpacity(colors.MutedForeground, opacity);
        builder.Rect(bounds, fill, role: "bullet-range", paint: VisualChartPaint.Fill(SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text).WithOpacity(fill, opacity)));
    }

    /// <summary>Band opacity, darkest first: 0.32 down to 0.10 across the bands.</summary>
    internal static double BulletBandOpacity(int band, int count) => count <= 1 ? .14 : .32 - (.32 - .10) * band / (count - 1);

    /// <summary>Keeps scalar facts complete at their resolved size in ordinary and captured native drawings.</summary>
    private static void BulletNumber(BulletPaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, BulletRow series,
        string text, ChartRect bounds, string role, string id, ChartColor color, double size, int weight = 400, bool ticks = false,
        TextAlignment alignment = TextAlignment.Center) =>
        series.TextPolicy.Text(builder, text, bounds, Style(chart, context, series, ticks ? -1 : 0, color, size, weight, ticks),
            role, id, chart.Captured, alignment: alignment, fallback: LabelFallbackRule.Drop);

    private static TextStyle Style(BulletPaintContext chart, VisualRenderContext context, BulletRow series, int point,
        ChartColor color, double size, int weight, bool ticks = false) => series.TextPolicy.Style(color, size, weight, point, ticks);
    private static void Text(BulletPaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, BulletRow series, int point,
        string text, ChartRect bounds, string role, string id, ChartColor color, double size, int weight = 400, bool ticks = false,
        TextAlignment alignment = TextAlignment.Center) =>
        series.TextPolicy.Text(builder, text, bounds, Style(chart, context, series, point, color, Math.Min(size, Math.Max(.1, bounds.Height / 1.3)), weight, ticks),
            role, id, chart.Captured, alignment: alignment);

    private static bool HasBulletPaint(ChartSeries series) => series.Color.HasValue
        || series.PointColors.Count > 0 && series.PointColors[0].HasValue || series.StateRole != ChartSeriesState.None;
    private static ChartColor BulletColor(ChartSeries series, int index, VisualThemeColors colors) =>
        HasBulletPaint(series) ? ChartSeriesColours.Point(series, index, 0, colors) : colors.Foreground;
    private static SvgPaint BulletPaint(ChartSeries series, ChartColor color) => HasBulletPaint(series)
        ? VisualChartPaint.Series(series, color, 0) : SvgPaint.Of(color, SvgColorRole.Text);
}
