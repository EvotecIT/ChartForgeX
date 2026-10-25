using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Projects configured gauge anatomy and its captured reference facts into the shared scene.</summary>
internal static partial class VisualGaugeCompiler {
    private static double Start(ChartGaugeOptions options) => options.StartAngleDegrees % 360 * (Math.PI / 180);
    private static double Sweep(ChartGaugeOptions options) => options.SweepAngleDegrees * (Math.PI / 180);
    private static double Ratio(GaugeData data, double value) => VisualRadialPrimitives.Clamp((value - data.Min) / (data.Max - data.Min));
    private static bool ConfiguredCircular(ChartGaugeOptions options) {
        var start = options.StartAngleDegrees % 360;
        var defaultStart = start == 150 || start == 150 - 360;
        return !defaultStart || options.SweepAngleDegrees != 240 || (options.TickCount ?? 2) != 2 || options.TickLabels.Count > 0;
    }

    private static double[] ScaleValues(GaugePaintContext chart, GaugeData data) {
        if (!chart.ShowAxes) return Array.Empty<double>();
        var options = chart.Options;
        if (options.TickLabels.Count > 0) return options.TickLabels.Select(label => label.Value)
            .Where(value => value >= data.Min && value <= data.Max).Distinct().OrderBy(value => value).ToArray();
        var count = options.TickCount ?? (options.Form == ChartGaugeForm.Linear ? 5 : 2);
        var fullTurn = options.Form != ChartGaugeForm.Linear && options.SweepAngleDegrees == 360;
        return GeneratedScaleValues(data, count, fullTurn);
    }

    private static double[] GeneratedScaleValues(GaugeData data, int count, bool fullTurn) =>
        Enumerable.Range(0, count).Select(index => ChartHeatmapSurface.InterpolateObservedRange(data.Min, data.Max,
            index / (double)(fullTurn ? count : count - 1))).ToArray();

    private static string? ExplicitScaleLabel(ChartGaugeOptions options, double value) => ChartAxisValueFormatter.FindExplicitLabel(options.TickLabels, value);
    private static string ScaleRole(GaugeData data, double value, int index) => value == data.Min ? "gauge-min-label" : value == data.Max ? "gauge-max-label" : "gauge-tick-label-" + index;

    private static void FitGeneratedScale(GaugePaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        GaugeData data, ChartValueFormat format, IReadOnlyList<double> retainedValues, Dictionary<double, string> captured,
        ref double[] values, ref string[] captions) {
        var options = chart.Options;
        if (options.TickLabels.Count > 0 || values.Length < 3) return;
        var style = chart.Style(context.Theme.Resolve(context.ThemeMode).MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
        var vertical = options.Form == ChartGaugeForm.Linear && options.Orientation == ChartOrientation.Vertical;
        var length = options.Form == ChartGaugeForm.Linear ? vertical ? plot.Height * .55 : plot.Width - context.Theme.Spacing * 2
            : Math.Min(plot.Width, plot.Height) * .3 * Sweep(options);
        int Maximum(IReadOnlyList<string> labels) {
            var extent = vertical ? labels.Max(text => chart.Measure(builder, text, style).Height) : labels.Max(text => chart.Measure(builder, text, style).Width);
            return Math.Max(2, (int)Math.Floor(length / Math.Max(1, extent + context.Theme.Spacing)) + 1);
        }
        var maximum = Maximum(captions);
        var fullTurn = options.Form != ChartGaugeForm.Linear && options.SweepAngleDegrees == 360;
        while (values.Length > maximum) {
            // A divisor preserves existing positions and both partial-scale endpoints when one fits the budget.
            var intervals = fullTurn ? values.Length : values.Length - 1;
            var stride = 1;
            while (stride < intervals && (intervals % stride != 0 || (fullTurn ? intervals / stride : intervals / stride + 1) > maximum)) stride++;
            if (fullTurn && intervals / stride < 2) {
                values = GeneratedScaleValues(data, maximum, fullTurn: true);
                var missing = values.Where(value => !captured.ContainsKey(value)).ToArray();
                var formatted = ChartNumericFormatter.FormatScaleValues(format, missing);
                for (var index = 0; index < missing.Length; index++) captured.Add(missing[index], formatted[index]);
            } else {
                values = values.Where((_, index) => index % stride == 0).ToArray();
            }
            // Newly generated ticks must remain distinct from every retained reading/target fact.
            // Refine captured defaults together; authored callbacks keep their captions and run once.
            if (ReferenceEquals(format, ChartValueFormat.ExistingValue)) {
                var facts = values.Concat(retainedValues).Distinct().ToArray();
                var labels = facts.Select(value => captured[value]).ToArray();
                ChartNumericFormatter.RefineDefaults(facts, labels, ChartValueFormat.ExistingValue.Format(0));
                for (var index = 0; index < facts.Length; index++) captured[facts[index]] = labels[index];
            }
            captions = values.Select(value => captured[value]).ToArray();
            maximum = Maximum(captions);
        }
    }

    private static void CaptureTargets(Dictionary<string, string> metadata, GaugeData data, IReadOnlyDictionary<double, string> captions) {
        metadata["data-cfx-gauge-target-count"] = data.Targets.Count.ToString(CultureInfo.InvariantCulture);
        for (var index = 0; index < data.Targets.Count; index++) {
            var target = data.Targets[index]; var key = "data-cfx-gauge-target-" + index.ToString(CultureInfo.InvariantCulture);
            metadata[key + "-value"] = N(target.Value);
            metadata[key + "-formatted-value"] = captions[target.Value];
            if (target.Label != null) metadata[key + "-label"] = target.Label;
        }
    }

    private static void ReferenceFact(VisualSceneBuilder builder, GaugeData data, int index, ChartRect bounds, string text) {
        builder.AddRegion(new VisualSemanticRegion("series-0-gauge-reference-" + index, "gauge-reference", bounds,
            (string.IsNullOrEmpty(data.Targets[index].Label) ? text : data.Targets[index].Label + ": " + text)));
    }

    private static bool Overlaps(ChartRect left, ChartRect right) => left.Left < right.Right && left.Right > right.Left
        && left.Top < right.Bottom && left.Bottom > right.Top;

    private static bool FitsCaption(ChartRect plot, ChartRect bounds, List<ChartRect> occupied) =>
        bounds.Left >= plot.Left - .000001 && bounds.Right <= plot.Right + .000001
        && bounds.Top >= plot.Top - .000001 && bounds.Bottom <= plot.Bottom + .000001
        && !occupied.Any(other => Overlaps(other, bounds));

    private static void FactualLabel(GaugePaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        string text, ChartRect bounds, string role, string id, List<ChartRect> occupied, bool ticks = true) {
        var style = chart.Style(context.Theme.Resolve(context.ThemeMode).MutedForeground,
            context.Theme.Typography.AxisSize, ticks: ticks);
        var metrics = chart.Measure(builder, text, style);
        var fits = FitsCaption(plot, bounds, occupied) && metrics.Width <= bounds.Width + .000001 && metrics.Height <= bounds.Height + .000001;
        if (text.Length == 0 || !fits) {
            builder.AddRegion(new VisualSemanticRegion(id, role, bounds, text));
            if (text.Length != 0) builder.AddDiagnostic(new VisualDiagnostic("gauge.label-overflow",
                "A gauge caption was omitted because it cannot fit without moving from its value; the complete fact remains in semantic regions."));
            return;
        }
        chart.Text(builder, text, bounds, style, role, id);
        occupied.Add(bounds);
    }

    private static void LinearTargetLabels(GaugePaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, GaugeData data,
        IReadOnlyList<string> captions, double top, double height) {
        var occupied = new List<ChartRect>();
        var style = chart.Style(context.Theme.Resolve(context.ThemeMode).MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
        var gap = context.Theme.Spacing;
        for (var index = 0; index < captions.Count; index++) {
            var width = chart.Measure(builder, captions[index], style).Width + gap;
            var x = plot.Left + gap + (plot.Width - gap * 2) * Ratio(data, data.Targets[index].Value);
            var bounds = new ChartRect(Math.Max(plot.Left, Math.Min(plot.Right - width, x - width / 2)), top, width, height);
            FactualLabel(chart, context, builder, plot, captions[index], bounds, "gauge-target-label", "series-0-gauge-target-label-" + index, occupied);
        }
    }

    private static void CircularConfigured(GaugePaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        GaugeData data, string value, string caption, IReadOnlyList<double> scaleValues, IReadOnlyList<string> scaleCaptions, IReadOnlyList<string> targetCaptions) {
        var colors = context.Theme.Resolve(context.ThemeMode); var gap = context.Theme.Spacing;
        var summary = Summary(chart, context, builder, plot, value, caption, out var summaryHeight);
        var area = new ChartRect(plot.Left, plot.Top, plot.Width, Math.Max(0, plot.Height - summaryHeight - gap));
        var labelStyle = chart.Style(colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
        var captions = scaleCaptions.Concat(targetCaptions).ToArray();
        var labelWidth = captions.Length == 0 ? 0 : captions.Max(text => chart.Measure(builder, text, labelStyle).Width);
        var labelHeight = captions.Length == 0 ? 0 : captions.Max(text => chart.Measure(builder, text, labelStyle).Height);
        var start = Start(chart.Options); var sweep = Sweep(chart.Options);
        var envelope = ArcEnvelope(start, sweep);
        var reserveX = labelWidth / 2 + gap * 1.5 + labelHeight / 2; var reserveY = labelHeight + gap * 1.5;
        var outer = Math.Max(0, Math.Min((area.Width - reserveX * 2) / envelope.Width, (area.Height - reserveY * 2) / envelope.Height));
        if (outer < 2) { builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "The viewport cannot fit the configured gauge anatomy and readable captions.")); return; }
        var cx = area.Left + (area.Width - envelope.Width * outer) / 2 - envelope.Left * outer;
        var cy = area.Top + (area.Height - envelope.Height * outer) / 2 - envelope.Top * outer;
        var stroke = Math.Min(context.Theme.GaugeStrokeWidth, outer * .3);
        var bandWidth = Math.Min(context.Theme.GaugeBandWidth, outer * .1);
        var radius = Math.Max(stroke / 2, outer - stroke / 2 - (data.Bands.Length > 0 ? bandWidth + gap / 2 : 0));
        VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, start, sweep, colors.Neutral2, "gauge-track", paint: SvgPaint.Of(colors.Neutral2, SvgColorRole.Surface));
        for (var index = 0; index < data.Bands.Length; index++) {
            var band = data.Bands[index];
            BandRatios(chart, data, band, outer - bandWidth / 2, true, out var lo, out var hi);
            using var source = Band(builder, band, index, area);
            VisualRadialPrimitives.Arc(builder, cx, cy, outer - bandWidth / 2, bandWidth, start + sweep * lo, sweep * (hi - lo),
                BandColor(colors, band), "gauge-band", paint: BandPaint(colors, band));
        }
        if (chart.Options.Form is ChartGaugeForm.Arc or ChartGaugeForm.ArcWithNeedle)
            VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, start, sweep * data.Ratio, data.Color, "gauge-value", "series-0-value", paint: chart.ValuePaint(data));
        if (chart.Options.Form is ChartGaugeForm.Needle or ChartGaugeForm.ArcWithNeedle) {
            var angle = start + sweep * data.Ratio;
            builder.Line(cx, cy, cx + Math.Cos(angle) * (radius - stroke), cy + Math.Sin(angle) * (radius - stroke), chart.Options.NeedleColor ?? data.Color,
                Math.Max(1, context.Theme.SeriesStrokeWidth), "gauge-needle", paint: VisualChartPaint.Stroke(NeedlePaint(chart, data)));
            builder.Ellipse(cx, cy, stroke / 3, stroke / 3, colors.Foreground, role: "gauge-pivot", paint: VisualChartPaint.Fill(colors.Foreground, SvgColorRole.Axis));
        }
        var occupied = new List<ChartRect>(summary);
        var exterior = outer + gap / 2;
        for (var index = 0; index < scaleValues.Count; index++) {
            var angle = start + sweep * Ratio(data, scaleValues[index]); var cos = Math.Cos(angle); var sin = Math.Sin(angle);
            var duplicateSeam = chart.Options.SweepAngleDegrees == 360 && scaleValues[index] == data.Max
                && scaleValues.Contains(data.Min);
            if (!duplicateSeam) builder.Line(cx + cos * exterior, cy + sin * exterior, cx + cos * (exterior + 3), cy + sin * (exterior + 3),
                colors.Border, context.Theme.AxisStrokeWidth, "gauge-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var width = chart.Measure(builder, scaleCaptions[index], labelStyle).Width;
            var box = new ChartRect(cx + cos * (exterior + gap + labelHeight / 2) - width / 2,
                cy + sin * (exterior + gap + labelHeight / 2) - labelHeight / 2, width, labelHeight);
            FactualLabel(chart, context, builder, plot, scaleCaptions[index], box, ScaleRole(data, scaleValues[index], index), "series-0-gauge-scale-" + index, occupied);
        }
        for (var index = 0; index < data.Targets.Count; index++) {
            var angle = start + sweep * Ratio(data, data.Targets[index].Value); var cos = Math.Cos(angle); var sin = Math.Sin(angle);
            builder.Line(cx + cos * (radius - stroke / 2 - bandWidth), cy + sin * (radius - stroke / 2 - bandWidth),
                cx + cos * (radius + stroke / 2 + bandWidth), cy + sin * (radius + stroke / 2 + bandWidth),
                colors.Foreground, context.Theme.SeriesStrokeWidth, "gauge-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Axis));
            var width = chart.Measure(builder, targetCaptions[index], labelStyle).Width;
            var box = new ChartRect(cx + cos * (exterior + gap + labelHeight / 2) - width / 2,
                cy + sin * (exterior + gap + labelHeight / 2) - labelHeight / 2, width, labelHeight);
            if (!FitsCaption(plot, box, occupied)) {
                var farther = exterior + gap + labelHeight * 1.75;
                var alternative = new ChartRect(cx + cos * farther - width / 2, cy + sin * farther - labelHeight / 2, width, labelHeight);
                if (FitsCaption(plot, alternative, occupied)) box = alternative;
            }
            FactualLabel(chart, context, builder, plot, targetCaptions[index], box, "gauge-target-label", "series-0-gauge-target-label-" + index, occupied);
        }
    }

    /// <summary>Includes crossed cardinal points and the pivot, so any authored sweep has one bounded projection.</summary>
    private static ChartRect ArcEnvelope(double start, double sweep) {
        var xs = new List<double> { 0, Math.Cos(start), Math.Cos(start + sweep) };
        var ys = new List<double> { 0, Math.Sin(start), Math.Sin(start + sweep) };
        for (var step = -4; step <= 8; step++) {
            var angle = step * Math.PI / 2;
            if (angle < start || angle > start + sweep) continue;
            xs.Add(Math.Cos(angle)); ys.Add(Math.Sin(angle));
        }
        return new ChartRect(xs.Min(), ys.Min(), Math.Max(.000001, xs.Max() - xs.Min()), Math.Max(.000001, ys.Max() - ys.Min()));
    }

    private static List<ChartRect> Summary(GaugePaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        string value, string caption, out double height, bool atTop = false) {
        height = 0; var result = new List<ChartRect>();
        if (!chart.ShowValue) return result;
        var gap = context.Theme.Spacing;
        var valueStyle = LabelStyle(chart, context, builder, value, plot.Height / 5, context.Theme.Typography.ScalarValueSize, 700,
            minimumDefaultSize: context.Theme.Typography.DataLabelSize);
        var captionStyle = LabelStyle(chart, context, builder, caption, plot.Height / 8, context.Theme.Typography.DataLabelSize, muted: true,
            minimumDefaultSize: context.Theme.Typography.DataLabelSize);
        var valueHeight = chart.Measure(builder, value, valueStyle).Height; var captionHeight = chart.Measure(builder, caption, captionStyle).Height;
        height = valueHeight + captionHeight + gap / 3;
        var top = atTop ? plot.Top : plot.Bottom - height;
        var valueBox = new ChartRect(plot.Left, top, plot.Width, valueHeight);
        var captionBox = new ChartRect(plot.Left, top + valueHeight + gap / 3, plot.Width, captionHeight);
        chart.Text(builder, value, valueBox, valueStyle, "gauge-label", "series-0-gauge-label");
        chart.Text(builder, caption, captionBox, captionStyle, "gauge-title", "series-0-gauge-title");
        result.Add(valueBox); result.Add(captionBox); return result;
    }

    private static void Vertical(GaugePaintContext chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, GaugeData data,
        string value, string caption, IReadOnlyList<double> scaleValues, IReadOnlyList<string> scaleCaptions, IReadOnlyList<string> targetCaptions) {
        var colors = context.Theme.Resolve(context.ThemeMode); var gap = context.Theme.Spacing;
        var occupied = Summary(chart, context, builder, plot, value, caption, out var summaryHeight, atTop: true);
        var style = chart.Style(colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
        var labelHeight = chart.Measure(builder, "0", style).Height;
        var scaleWidth = scaleCaptions.Count == 0 ? 0 : scaleCaptions.Max(text => chart.Measure(builder, text, style).Width);
        var targetWidth = targetCaptions.Count == 0 ? 0 : targetCaptions.Max(text => chart.Measure(builder, text, style).Width);
        var thickness = Math.Min(plot.Width / 7, Math.Max(4, context.Theme.Typography.DataLabelSize * 1.4));
        var left = plot.Left + (plot.Width - (scaleWidth + targetWidth + thickness + gap * 4)) / 2 + scaleWidth + gap * 2;
        var top = plot.Top + summaryHeight + gap + labelHeight / 2;
        var track = new ChartRect(left, top, thickness, plot.Bottom - labelHeight / 2 - top);
        if (track.Height <= 0 || left - scaleWidth - gap < plot.Left || track.Right + targetWidth + gap > plot.Right) {
            builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "The viewport cannot fit the vertical gauge and its readable scale/reference captions.")); return;
        }
        double Y(double raw) => track.Bottom - track.Height * Ratio(data, raw);
        builder.Rect(track, colors.Neutral2, role: "gauge-track", paint: VisualChartPaint.Fill(colors.Neutral2, SvgColorRole.Surface));
        for (var index = 0; index < data.Bands.Length; index++) {
            var band = data.Bands[index];
            BandRatios(chart, data, band, track.Height, false, out var lo, out var hi);
            var bounds = chart.Options.BandGap == 0
                ? new ChartRect(track.Left, Y(band.Maximum), track.Width, Math.Max(0, Y(band.Minimum) - Y(band.Maximum)))
                : new ChartRect(track.Left, track.Bottom - track.Height * hi, track.Width, Math.Max(0, track.Height * (hi - lo)));
            using var source = Band(builder, band, index, bounds);
            builder.Rect(bounds, BandColor(colors, band), role: "gauge-band", paint: VisualChartPaint.Fill(BandPaint(colors, band)));
        }
        builder.Rect(new ChartRect(track.Left + thickness / 3, Y(data.Raw), thickness / 3, track.Bottom - Y(data.Raw)), data.Color,
            role: "gauge-value", id: "series-0-value", paint: VisualChartPaint.Fill(chart.ValuePaint(data)));
        var y = Y(data.Raw);
        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(track.Right + thickness / 2, y - thickness / 4),
            ChartPathCommand.LineTo(track.Right + thickness / 2, y + thickness / 4), ChartPathCommand.LineTo(track.Right, y) }),
            data.Color, role: "gauge-value-marker", close: true, paint: VisualChartPaint.Fill(chart.ValuePaint(data)));
        if (scaleValues.Count > 0) builder.Line(track.Left - gap / 2, track.Top, track.Left - gap / 2, track.Bottom,
            colors.Border, context.Theme.AxisStrokeWidth, "gauge-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
        for (var index = 0; index < scaleValues.Count; index++) {
            y = Y(scaleValues[index]);
            builder.Line(track.Left - gap / 2 - 3, y, track.Left - gap / 2, y, colors.Border, context.Theme.AxisStrokeWidth, "gauge-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var width = chart.Measure(builder, scaleCaptions[index], style).Width;
            var box = new ChartRect(track.Left - gap - width, y - labelHeight / 2, width, labelHeight);
            FactualLabel(chart, context, builder, plot, scaleCaptions[index], box, ScaleRole(data, scaleValues[index], index), "series-0-gauge-scale-" + index, occupied);
        }
        for (var index = 0; index < data.Targets.Count; index++) {
            y = Y(data.Targets[index].Value);
            builder.Line(track.Left - thickness / 4, y, track.Right + thickness / 4, y, colors.Foreground, context.Theme.SeriesStrokeWidth,
                "gauge-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Axis));
            var width = chart.Measure(builder, targetCaptions[index], style).Width;
            var box = new ChartRect(track.Right + gap, y - labelHeight / 2, width, labelHeight);
            FactualLabel(chart, context, builder, plot, targetCaptions[index], box, "gauge-target-label", "series-0-gauge-target-label-" + index, occupied);
        }
    }
}
