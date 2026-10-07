using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>Compiles every gauge anatomy into the same detached numeric scene.</summary>
internal static class VisualGaugeCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        var data = Read(chart, colors);
        return chart.Series[0].ShowInLegend ? new[] { new VisualLegendEntry(chart.Series[0].Name, data.Color, "series-0", ChartSeriesKind.Gauge,
            stateRole: data.State, seriesKey: chart.Series[0].InteractionIdentityKey, paint: GaugePaint(chart.Series[0], data)) } : Array.Empty<VisualLegendEntry>();
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var data = Read(chart, colors);
        var series = chart.Series[0]; var options = chart.Options.Gauge;
        var value = series.PointLabels.Count > 0 && series.PointLabels[0] != null ? series.PointLabels[0]! : ChartNumericFormatter.FormatValue(chart.Options, data.Raw);
        var caption = options.Caption ?? series.Name;
        var target = options.Target.HasValue ? ChartNumericFormatter.FormatValue(chart.Options, options.Target.Value) : null;
        var min = chart.Options.ShowAxes ? ChartNumericFormatter.FormatValue(chart.Options, data.Min) : null;
        var max = chart.Options.ShowAxes ? ChartNumericFormatter.FormatValue(chart.Options, data.Max) : null;
        builder.AddRegion(new VisualSemanticRegion("series-0", "gauge", plot, series.Name + ": " + value));
        using (builder.PushGroup("series-0", "gauge", new Dictionary<string, string> {
            ["data-cfx-value"] = N(data.Raw), ["data-cfx-min"] = N(data.Min), ["data-cfx-max"] = N(data.Max),
            ["data-cfx-percent"] = N(data.Ratio), ["data-cfx-status"] = data.State.ToString(),
            ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors ? "true" : "false",
            ["data-cfx-target"] = options.Target.HasValue ? N(options.Target.Value) : string.Empty, ["aria-label"] = series.Name + ": " + value
        })) {
            if (plot.Width <= 0 || plot.Height <= 0) { builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "No content viewport remains for the gauge.")); return; }
            using (builder.PushClip(plot)) {
                if (options.Form == ChartGaugeForm.Linear) Linear(chart, context, builder, plot, data, value, caption, target, min, max);
                else Circular(chart, context, builder, plot, data, value, caption, target, min, max);
            }
        }
    }

    private static GaugeData Read(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.Points.Count < 2) throw new InvalidOperationException("A gauge requires its minimum/value and maximum points.");
        var min = series.Points[0].X; var max = series.Points[1].X; var raw = series.Points[0].Y;
        ChartGuards.Finite(min, nameof(chart)); ChartGuards.Finite(max, nameof(chart)); ChartGuards.Finite(raw, nameof(chart));
        if (max <= min || double.IsInfinity(max - min)) throw new InvalidOperationException("A gauge requires a finite positive range.");
        var bands = chart.Options.Gauge.Bands.OrderBy(band => band?.Minimum).ToArray();
        if (bands.Any(band => band == null)) throw new InvalidOperationException("Gauge bands must not contain null.");
        for (var index = 1; index < bands.Length; index++) if (bands[index].Minimum < bands[index - 1].Maximum) throw new InvalidOperationException("Gauge bands must not overlap.");
        var bounded = VisualRadialPrimitives.Clamp(raw, min, max); var ratio = (bounded - min) / (max - min);
        var active = bands.FirstOrDefault(band => bounded >= band.Minimum && (bounded < band.Maximum || bounded == max && bounded == band.Maximum));
        var state = active?.State ?? (ratio < .6 ? ChartSeriesState.Danger : ratio < .8 ? ChartSeriesState.Warning : ChartSeriesState.Success);
        if (series.StateRole != ChartSeriesState.None) state = series.StateRole;
        var color = series.PointColors.Count > 0 && series.PointColors[0].HasValue ? series.PointColors[0]!.Value
            : series.Color ?? VisualRadialPrimitives.StateColor(colors, state, colors.Accent);
        return new GaugeData(min, max, raw, ratio, state, color, bands);
    }

    private static void Circular(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, GaugeData data,
        string value, string caption, string? target, string? min, string? max) {
        var colors = context.Theme.Resolve(context.ThemeMode); var gap = context.Theme.Spacing;
        var labelHeight = context.Theme.Typography.AxisSize * 1.5;
        var bottom = (min != null ? labelHeight : 0) + (target != null ? labelHeight : 0);
        var outer = Math.Max(0, Math.Min(plot.Width / 2 - gap, (plot.Height - bottom - gap) / 1.5));
        if (outer < 2) { builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "The viewport is too small for a gauge arc.")); return; }
        var cx = plot.Left + plot.Width / 2; var cy = plot.Top + outer;
        var radius = outer * .84; var stroke = outer * .12;
        var start = Math.PI * 5 / 6; var sweep = Math.PI * 4 / 3;
        VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, start, sweep, colors.Border, "gauge-track", round: true, paint: SvgPaint.Of(colors.Border, SvgColorRole.Surface));
        for (var index = 0; index < data.Bands.Length; index++) {
            var band = data.Bands[index];
            var lo = VisualRadialPrimitives.Clamp((band.Minimum - data.Min) / (data.Max - data.Min));
            var hi = VisualRadialPrimitives.Clamp((band.Maximum - data.Min) / (data.Max - data.Min));
            using var source = Band(builder, band, index, new ChartRect(cx - outer, cy - outer, outer * 2, outer * 1.5));
            VisualRadialPrimitives.Arc(builder, cx, cy, outer * .97, outer * .035, start + sweep * lo, sweep * (hi - lo),
                VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent), "gauge-band",
                paint: SvgPaint.Of(VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent), band.State == ChartSeriesState.None ? SvgColorRole.Series : SvgColorRole.Status));
        }
        if (chart.Options.Gauge.Form == ChartGaugeForm.Arc)
            VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, start, sweep * data.Ratio, data.Color, "gauge-value", "series-0-value", true, GaugePaint(chart.Series[0], data));
        else {
            var angle = start + sweep * data.Ratio;
            builder.Line(cx, cy, cx + Math.Cos(angle) * (radius - stroke), cy + Math.Sin(angle) * (radius - stroke), data.Color,
                Math.Max(1, context.Theme.SeriesStrokeWidth), "gauge-needle", paint: VisualChartPaint.Stroke(GaugePaint(chart.Series[0], data)));
            builder.Ellipse(cx, cy, stroke / 3, stroke / 3, colors.Foreground, role: "gauge-pivot", paint: VisualChartPaint.Fill(colors.Foreground, SvgColorRole.Axis));
        }
        if (chart.Options.Gauge.Target.HasValue) {
            var angle = start + sweep * VisualRadialPrimitives.Clamp((chart.Options.Gauge.Target.Value - data.Min) / (data.Max - data.Min));
            builder.Line(cx + Math.Cos(angle) * outer * .76, cy + Math.Sin(angle) * outer * .76,
                cx + Math.Cos(angle) * outer, cy + Math.Sin(angle) * outer, colors.Foreground, context.Theme.SeriesStrokeWidth, "gauge-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Axis));
        }
        if (chart.Series[0].ShowDataLabels != false) {
            Label(chart, context, builder, value, new ChartRect(cx - radius * .7, cy - radius * .55, radius * 1.4, radius * .5), "gauge-label", context.Theme.Typography.TitleSize, 700);
            Label(chart, context, builder, caption, new ChartRect(cx - radius * .75, cy + radius * .05, radius * 1.5, radius * .25), "gauge-title", context.Theme.Typography.DataLabelSize);
        }
        Footer(chart, context, builder, plot, data, target, min, max, labelHeight);
    }

    private static void Linear(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, GaugeData data,
        string value, string caption, string? target, string? min, string? max) {
        var colors = context.Theme.Resolve(context.ThemeMode); var gap = context.Theme.Spacing;
        var labelHeight = context.Theme.Typography.AxisSize * 1.5;
        var height = Math.Min(plot.Height / 7, Math.Max(4, context.Theme.Typography.DataLabelSize * 1.4));
        var track = new ChartRect(plot.Left + gap, plot.Top + plot.Height * .45, Math.Max(0, plot.Width - gap * 2), height);
        if (track.Width <= 0 || height <= 0) { builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "The viewport is too small for a linear gauge.")); return; }
        builder.Rect(track, colors.Border, role: "gauge-track", paint: VisualChartPaint.Fill(colors.Border, SvgColorRole.Surface));
        for (var index = 0; index < data.Bands.Length; index++) {
            var band = data.Bands[index];
            var lo = VisualRadialPrimitives.Clamp((band.Minimum - data.Min) / (data.Max - data.Min));
            var hi = VisualRadialPrimitives.Clamp((band.Maximum - data.Min) / (data.Max - data.Min));
            var bounds = new ChartRect(track.Left + track.Width * lo, track.Top, track.Width * (hi - lo), track.Height);
            using var source = Band(builder, band, index, bounds);
            builder.Rect(bounds,
                VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent), role: "gauge-band",
                paint: VisualChartPaint.Fill(VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent), band.State == ChartSeriesState.None ? SvgColorRole.Series : SvgColorRole.Status));
        }
        builder.Rect(new ChartRect(track.Left, track.Top + height / 3, track.Width * data.Ratio, height / 3), data.Color, role: "gauge-value", id: "series-0-value", paint: VisualChartPaint.Fill(GaugePaint(chart.Series[0], data)));
        var x = track.Left + track.Width * data.Ratio;
        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(x - height / 4, track.Top - height / 2),
            ChartPathCommand.LineTo(x + height / 4, track.Top - height / 2), ChartPathCommand.LineTo(x, track.Top) }), data.Color, role: "gauge-value-marker", close: true, paint: VisualChartPaint.Fill(GaugePaint(chart.Series[0], data)));
        if (chart.Options.Gauge.Target.HasValue) {
            var tx = track.Left + track.Width * VisualRadialPrimitives.Clamp((chart.Options.Gauge.Target.Value - data.Min) / (data.Max - data.Min));
            builder.Line(tx, track.Top - height / 4, tx, track.Bottom + height / 4, colors.Foreground, context.Theme.SeriesStrokeWidth, "gauge-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Axis));
        }
        if (chart.Series[0].ShowDataLabels != false) {
            Label(chart, context, builder, caption, new ChartRect(track.Left, plot.Top, track.Width, Math.Max(0, track.Top - height / 2 - plot.Top)), "gauge-title", context.Theme.Typography.DataLabelSize);
            var y = track.Bottom + height / 3;
            var footerTop = plot.Bottom - labelHeight * ((min != null ? 1 : 0) + (target != null ? 1 : 0));
            Label(chart, context, builder, value, new ChartRect(track.Left, y, track.Width, Math.Max(0, Math.Min(labelHeight, footerTop - y - 4))), "gauge-label", context.Theme.Typography.DataLabelSize, 700);
        }
        Footer(chart, context, builder, plot, data, target, min, max, labelHeight);
    }

    private static void Footer(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, GaugeData data, string? target, string? min, string? max, double height) {
        var y = plot.Bottom - height;
        if (target != null) { Label(chart, context, builder, target, new ChartRect(plot.Left + plot.Width / 3, y, plot.Width / 3, height), "gauge-target-label", context.Theme.Typography.AxisSize); y -= height; }
        if (min != null && max != null) {
            if (chart.Options.Gauge.Form == ChartGaugeForm.Linear) {
                var colors = context.Theme.Resolve(context.ThemeMode);
                var left = plot.Left + context.Theme.Spacing; var width = Math.Max(0, plot.Width - context.Theme.Spacing * 2);
                builder.Line(left, y, left + width, y, colors.Border, context.Theme.AxisStrokeWidth, "gauge-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
                for (var tick = 0; tick < 5; tick++) {
                    var x = left + width * tick / 4;
                    builder.Line(x, y, x, y + 3, colors.Border, context.Theme.AxisStrokeWidth, "gauge-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
                    var text = tick == 0 ? min : tick == 4 ? max : ChartNumericFormatter.FormatValue(chart.Options, data.Min + (data.Max - data.Min) * tick / 4);
                    var box = new ChartRect(Math.Max(plot.Left, Math.Min(plot.Right - plot.Width / 5, x - plot.Width / 10)), y + 3, plot.Width / 5, Math.Max(0, height - 3));
                    Label(chart, context, builder, text, box, tick == 0 ? "gauge-min-label" : tick == 4 ? "gauge-max-label" : "gauge-tick-label-" + tick, context.Theme.Typography.AxisSize, ticks: true);
                }
                return;
            }
            Label(chart, context, builder, min, new ChartRect(plot.Left, y, plot.Width / 3, height), "gauge-min-label", context.Theme.Typography.AxisSize, ticks: true);
            Label(chart, context, builder, max, new ChartRect(plot.Left + plot.Width * 2 / 3, y, plot.Width / 3, height), "gauge-max-label", context.Theme.Typography.AxisSize, ticks: true);
        }
    }

    private static IDisposable Band(VisualSceneBuilder builder, ChartGaugeBand band, int index, ChartRect bounds) {
        var id = "series-0-band-" + index;
        builder.AddRegion(new VisualSemanticRegion(id, "gauge-band", bounds, N(band.Minimum) + "–" + N(band.Maximum) + ": " + band.State));
        return builder.PushGroup(id, "gauge-band-source", new Dictionary<string, string> {
            ["data-cfx-min"] = N(band.Minimum), ["data-cfx-max"] = N(band.Maximum), ["data-cfx-status"] = band.State.ToString()
        });
    }

    private static void Label(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string text, ChartRect bounds, string role, double size, int weight = 400, bool ticks = false) =>
        VisualRadialPrimitives.Text(builder, text, bounds, VisualRadialPrimitives.Style(chart, context,
            context.Theme.Resolve(context.ThemeMode).Foreground, Math.Min(size, Math.Max(.1, bounds.Height / 1.3)), weight, point: 0, ticks: ticks), role, "series-0-" + role);

    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static SvgPaint GaugePaint(ChartSeries series, GaugeData data) => SvgPaint.Of(data.Color,
        series.Color.HasValue || series.PointColors.Count > 0 && series.PointColors[0].HasValue ? SvgColorRole.Series : SvgColorRole.Status);
    private sealed class GaugeData {
        internal GaugeData(double min, double max, double raw, double ratio, ChartSeriesState state, ChartColor color, ChartGaugeBand[] bands) {
            Min = min; Max = max; Raw = raw; Ratio = ratio; State = state; Color = color; Bands = bands;
        }
        internal double Min { get; } internal double Max { get; } internal double Raw { get; } internal double Ratio { get; }
        internal ChartSeriesState State { get; } internal ChartColor Color { get; } internal ChartGaugeBand[] Bands { get; }
    }
}
