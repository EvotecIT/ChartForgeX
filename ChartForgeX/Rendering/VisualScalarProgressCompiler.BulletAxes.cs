using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualScalarProgressCompiler {
    /// <summary>Uses captured canonical numeric axes for independent rows or vertical tracks; the legacy shared-horizontal path is unchanged.</summary>
    private static void ConfiguredBullets(BulletPaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var entries = chart.Rows.Select(series => (Series: series, Index: series.Index)).ToArray();
        var independent = chart.Independent;
        var vertical = chart.Orientation == ChartOrientation.Vertical;
        var sharedMinimum = chart.XAxis.Minimum ?? entries.Min(entry => entry.Series.Reading.Minimum);
        var sharedMaximum = chart.XAxis.Maximum ?? entries.Max(entry => entry.Series.Reading.Maximum);
        var rows = entries.Select(entry => new BulletAxisRow(chart, entry.Series, entry.Index, independent, sharedMinimum, sharedMaximum)).ToArray();
        foreach (var row in rows) row.CaptureCaptions(chart);
        var gap = context.Theme.Spacing;
        var axisStyle = Style(chart, context, rows[0].Series, -1, colors.MutedForeground, context.Theme.Typography.AxisSize, 400, true);
        var scaleHeight = chart.Measure(builder, "0123456789", axisStyle).Height + gap;
        var labelWidth = Math.Min(plot.Width * .25, rows.Where(row => row.Series.ShowLabels)
            .Select(row => chart.Measure(builder, row.Series.Name, Style(chart, context, row.Series, 0, colors.Foreground, context.Theme.Typography.DataLabelSize, 400)).Width + gap).DefaultIfEmpty(0).Max());
        var valueWidth = Math.Min(plot.Width * .3, rows.Where(row => row.Series.ShowLabels)
            .Select(row => chart.Measure(builder, row.ValueText + " / " + row.TargetText, Style(chart, context, row.Series, 0, colors.Foreground, context.Theme.Typography.DataLabelSize, 700)).Width + gap).DefaultIfEmpty(0).Max());
        var sharedScaleWidth = rows[0].Axis.Visible && chart.ShowAxes
            ? rows[0].TickText.Select(text => chart.Measure(builder, text, axisStyle).Width + gap).DefaultIfEmpty(0).Max() : 0;
        for (var index = 0; index < rows.Length; index++) {
            var row = rows[index]; var series = row.Series; var show = series.ShowLabels;
            var bounds = vertical ? new ChartRect(plot.Left + plot.Width * index / rows.Length, plot.Top, plot.Width / rows.Length, plot.Height)
                : new ChartRect(plot.Left, plot.Top + plot.Height * index / rows.Length, plot.Width, plot.Height / rows.Length);
            var hasAxis = chart.ShowAxes && row.Axis.Visible;
            var rowScaleWidth = hasAxis ? row.TickText.Select(text => chart.Measure(builder, text, axisStyle).Width + gap).DefaultIfEmpty(0).Max() : 0;
            ChartRect track;
            if (vertical) {
                var topReserve = show ? scaleHeight * 1.5 : gap;
                var bottomReserve = show ? scaleHeight * 2 : gap;
                var leftReserve = independent ? Math.Min(bounds.Width * .45, rowScaleWidth) : Math.Min(bounds.Width * .45, sharedScaleWidth);
                var available = Math.Max(0, bounds.Width - leftReserve - gap);
                var width = Math.Min(context.Theme.Typography.DataLabelSize * 1.8, available * .45);
                track = new ChartRect(bounds.Left + leftReserve + (available - width) / 2, bounds.Top + topReserve,
                    width, Math.Max(0, bounds.Height - topReserve - bottomReserve));
            } else {
                var availableHeight = Math.Max(0, bounds.Height - (hasAxis ? scaleHeight : 0) - gap);
                var height = Math.Min(context.Theme.Typography.DataLabelSize * 1.8, availableHeight * .4);
                track = new ChartRect(bounds.Left + labelWidth + gap, bounds.Top + availableHeight / 2 - height / 2,
                    Math.Max(0, bounds.Width - labelWidth - valueWidth - gap * 2), height);
            }
            if (track.Width <= 0 || track.Height <= 0) {
                builder.AddDiagnostic(new VisualDiagnostic("bullet.insufficient-space", "No space remains for the configured bullet row and its numeric axis.")); continue;
            }
            double Position(double value) {
                var ratio = ChartScaleTransform.Normalize(VisualRadialPrimitives.Clamp(value, row.Minimum, row.Maximum), row.Minimum, row.Maximum, row.Axis);
                return vertical ? track.Bottom - ratio * track.Height : track.Left + ratio * track.Width;
            }
            var value = row.Reading;
            var status = value.Value < value.Target ? "below-target" : value.Value > value.Target ? "above-target" : "meets-target";
            var state = series.StateRole == ChartSeriesState.None ? value.Value < value.Target ? ChartSeriesState.Danger : ChartSeriesState.Success : series.StateRole;
            builder.AddRegion(new VisualSemanticRegion(Id(row.Index), "bullet-row", bounds, series.Name + ": " + row.ValueText + ", target " + row.TargetText));
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = N(row.Index), ["data-cfx-source-point"] = "0", ["data-cfx-source-count"] = "1",
                ["data-cfx-label"] = series.Name, ["data-cfx-value"] = N(value.Value), ["data-cfx-target"] = N(value.Target),
                ["data-cfx-min"] = N(value.Minimum), ["data-cfx-max"] = N(value.Maximum),
                ["data-cfx-scale-min"] = N(row.Minimum), ["data-cfx-scale-max"] = N(row.Maximum),
                ["data-cfx-scale"] = row.Axis.Scale.ToString(), ["data-cfx-reversed"] = row.Axis.Reversed ? "true" : "false",
                ["data-cfx-orientation"] = vertical ? "vertical" : "horizontal", ["data-cfx-label-target"] = chart.LabelTarget,
                ["data-cfx-status"] = status, ["data-cfx-state"] = state.ToString(), ["data-cfx-series-key"] = series.SeriesKey,
                ["data-cfx-source-range-ends"] = string.Join(",", value.RangeEnds.Select(N))
            };
            chart.CaptureIdentity(metadata, series);
            using (builder.PushGroup(Id(row.Index), "bullet-row", metadata)) {
                var ends = value.RangeEnds.Where(end => end > row.Minimum && end < row.Maximum).OrderBy(end => end).Distinct().ToList();
                if (ends.Count == 0) { ends.Add(row.Minimum + (row.Maximum - row.Minimum) * .6); ends.Add(row.Minimum + (row.Maximum - row.Minimum) * .8); }
                ends.Add(row.Maximum); var previous = row.Minimum;
                for (var band = 0; band < ends.Count; band++) {
                    var neutral = band == 0 ? colors.Neutral : band == 1 ? colors.Neutral2 : colors.Neutral3;
                    using (builder.PushGroup(null, "bullet-range-source", new Dictionary<string, string> { ["data-cfx-min"] = N(previous), ["data-cfx-max"] = N(ends[band]) }))
                        builder.Rect(BulletSpan(track, Position(previous), Position(ends[band]), vertical, 1), neutral, role: "bullet-range", paint: VisualChartPaint.Fill(neutral, SvgColorRole.Surface));
                    previous = ends[band];
                }
                var color = series.Color;
                builder.Rect(BulletSpan(track, Position(row.Minimum), Position(value.Value), vertical, 1d / 3), color, role: "bullet-value", paint: VisualChartPaint.Fill(series.Paint));
                var target = Position(value.Target);
                if (vertical) builder.Line(track.Left, target, track.Right, target, colors.Foreground, context.Theme.SeriesStrokeWidth, "bullet-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Text));
                else builder.Line(target, track.Top, target, track.Bottom, colors.Foreground, context.Theme.SeriesStrokeWidth, "bullet-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Text));
                if (show) {
                    var nameBounds = vertical ? new ChartRect(bounds.Left, bounds.Top, bounds.Width, scaleHeight * 1.5)
                        : new ChartRect(bounds.Left, bounds.Top, Math.Max(0, labelWidth - gap / 2), Math.Max(0, bounds.Height - (hasAxis ? scaleHeight : 0)));
                    Text(chart, context, builder, series, 0, series.Name, nameBounds, "bullet-row-label", Id(row.Index) + "-label", colors.Foreground, context.Theme.Typography.DataLabelSize);
                    var valueBounds = vertical ? new ChartRect(bounds.Left, track.Bottom + gap / 2, bounds.Width, scaleHeight)
                        : new ChartRect(track.Right + gap / 2, nameBounds.Top, Math.Max(0, bounds.Right - track.Right - gap / 2), nameBounds.Height / 2);
                    var targetBounds = vertical ? new ChartRect(bounds.Left, valueBounds.Bottom, bounds.Width, scaleHeight)
                        : new ChartRect(valueBounds.Left, valueBounds.Bottom, valueBounds.Width, valueBounds.Height);
                    BulletNumber(chart, context, builder, series, row.ValueText, valueBounds, "bullet-value-label", Id(row.Index) + "-value", colors.Foreground, context.Theme.Typography.DataLabelSize, 700);
                    BulletNumber(chart, context, builder, series, "/ " + row.TargetText, targetBounds, "bullet-target-label", Id(row.Index) + "-target", colors.MutedForeground, context.Theme.Typography.DataLabelSize);
                }
                if (hasAxis && (independent || !vertical || index == 0)) {
                    var axisArea = vertical ? new ChartRect(bounds.Left, track.Top, Math.Max(0, track.Left - bounds.Left - gap / 2), track.Height)
                        : new ChartRect(track.Left - gap / 2, track.Bottom + gap / 2, track.Width + gap, scaleHeight);
                    BulletRowAxis(chart, context, builder, row, axisArea, track, Position, vertical, colors, axisStyle);
                }
            }
        }
    }

    private static ChartRect BulletSpan(ChartRect track, double first, double last, bool vertical, double thickness) => vertical
        ? new ChartRect(track.Left + track.Width * (1 - thickness) / 2, Math.Min(first, last), track.Width * thickness, Math.Abs(last - first))
        : new ChartRect(Math.Min(first, last), track.Top + track.Height * (1 - thickness) / 2, Math.Abs(last - first), track.Height * thickness);

    private static void BulletRowAxis(BulletPaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, BulletAxisRow row,
        ChartRect area, ChartRect track, Func<double, double> position, bool vertical, VisualThemeColors colors, TextStyle style) {
        if (row.Axis.ShowLine) {
            if (vertical) builder.Line(track.Left - 3, track.Top, track.Left - 3, track.Bottom, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            else builder.Line(track.Left, area.Top, track.Right, area.Top, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
        }
        var requests = new List<LabelPlacementRequest>();
        for (var index = 0; index < row.Ticks.Count; index++) {
            var text = row.TickText[index]; var measured = chart.Measure(builder, text, style); var coordinate = position(row.Ticks[index]);
            if (vertical) builder.Line(track.Left - 3, coordinate, track.Left - 6, coordinate, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            else builder.Line(coordinate, area.Top, coordinate, area.Top + 3, colors.Border, context.Theme.AxisStrokeWidth, "bullet-axis-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var x = vertical ? area.Right - measured.Width / 2 : Math.Max(area.Left + measured.Width / 2, Math.Min(area.Right - measured.Width / 2, coordinate));
            var y = vertical ? Math.Max(area.Top + measured.Height / 2, Math.Min(area.Bottom - measured.Height / 2, coordinate)) : area.Top + area.Height / 2;
            requests.Add(new LabelPlacementRequest(text, new ChartPoint(x, y), style, new[] { new LabelCandidate(0, 0, .5, .5) }, priority: index == 0 || index == row.Ticks.Count - 1 ? 1 : 0) { Bounds = area, Fallback = LabelFallbackRule.Drop, MeasuredSize = measured });
        }
        var placements = new LabelPlacementService().Place(requests, area, null, context.Theme.Spacing / 2, (text, labelStyle) => chart.Measure(builder, text, labelStyle));
        for (var index = 0; index < placements.Count; index++) {
            var placement = placements[index]; var id = Id(row.Index) + "-bullet-axis-" + index;
            if (placement.IsDropped) {
                builder.AddRegion(new VisualSemanticRegion(id, "bullet-axis-label", area, row.TickText[index]));
                builder.AddDiagnostic(new VisualDiagnostic("bullet.label-overflow", "A complete bullet scale caption was omitted because it cannot fit without overlapping; the exact fact remains in descriptive regions."));
            } else BulletNumber(chart, context, builder, row.Series, row.TickText[index], placement.Bounds, "bullet-axis-label", id, colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
        }
    }

    /// <summary>Captures row axis/options and resolves each distinct numeric fact once before measured layout.</summary>
    private sealed class BulletAxisRow {
        internal BulletAxisRow(BulletPaintContext chart, BulletRow series, int index, bool independent, double sharedMinimum, double sharedMaximum) {
            Series = series; Index = index; Axis = independent ? series.Axis.Clone() : chart.XAxis.Clone();
            Reading = series.Reading; var reading = Reading; Minimum = independent ? Axis.Minimum ?? reading.Minimum : sharedMinimum; Maximum = independent ? Axis.Maximum ?? reading.Maximum : sharedMaximum;
            if (Maximum <= Minimum || !Finite(Maximum - Minimum) || Axis.Scale == ChartScaleKind.Time)
                throw new InvalidOperationException("Bullet axes require a finite positive numeric domain.");
            var lower = ChartScaleTransform.Forward(Minimum, Axis); var upper = ChartScaleTransform.Forward(Maximum, Axis);
            if (!Finite(lower) || !Finite(upper) || upper <= lower) throw new InvalidOperationException("Bullet axis bounds must map to a finite increasing numeric span.");
            Ticks = chart.ShowAxes && Axis.Visible ? ChartTicks.ForAxis(Axis, Minimum, Maximum) : Array.Empty<double>();
        }
        internal void CaptureCaptions(BulletPaintContext chart) {
            var reading = Reading; var series = Series;
            var facts = Ticks.Concat(new[] { reading.Value, reading.Target }).Distinct().ToArray();
            var captions = ChartNumericFormatter.FormatScaleValues(Axis.ValueFormat ?? chart.Format, facts);
            var cache = new Dictionary<double, string>(); for (var item = 0; item < facts.Length; item++) cache.Add(facts[item], captions[item]);
            TickText = Ticks.Select(value => ChartAxisValueFormatter.FindExplicitLabel(Axis.Labels, value) ?? cache[value]).ToArray();
            ValueText = series.AuthoredLabel != null ? series.AuthoredLabel : cache[reading.Value]; TargetText = cache[reading.Target];
        }
        internal BulletRow Series { get; }
        internal int Index { get; }
        internal ChartAxis Axis { get; }
        internal ChartBulletValue Reading { get; }
        internal double Minimum { get; }
        internal double Maximum { get; }
        internal IReadOnlyList<double> Ticks { get; }
        internal string[] TickText { get; private set; } = Array.Empty<string>();
        internal string ValueText { get; private set; } = string.Empty;
        internal string TargetText { get; private set; } = string.Empty;
    }
}
