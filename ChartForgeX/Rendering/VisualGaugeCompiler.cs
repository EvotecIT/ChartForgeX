using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Compiles every gauge anatomy into the same detached numeric scene.</summary>
internal static partial class VisualGaugeCompiler {
    internal static IReadOnlyList<VisualLegendEntry> LegendEntries(Chart chart, VisualThemeColors colors) {
        if (!chart.Series[0].HasSourceData) return Array.Empty<VisualLegendEntry>();
        var data = Read(chart, colors);
        return chart.Series[0].ShowInLegend ? new[] { new VisualLegendEntry(chart.Series[0].Name, data.Color, "series-0", ChartSeriesKind.Gauge,
            stateRole: data.State, seriesKey: chart.Series[0].InteractionIdentityKey, paint: GaugePaint(chart.Series[0], data)) } : Array.Empty<VisualLegendEntry>();
    }

    internal static void Build(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        if (!chart.Series[0].HasSourceData) { builder.AddDiagnostic(new VisualDiagnostic("gauge.no-data", "Gauge charts need one measured value.")); return; }
        var colors = context.Theme.Resolve(context.ThemeMode);
        var data = Read(chart, colors);
        var series = chart.Series[0]; var options = chart.Options.Gauge;
        var authoredValue = series.PointLabels.Count > 0 ? series.PointLabels[0] : null;
        var showValue = series.ResolveDataLabels(chart.Options, defaultVisible: true);
        var caption = options.Caption ?? series.Name;
        var scaleValues = ScaleValues(chart, data);
        var retainedValues = new List<double>(data.Targets.Select(target => target.Value));
        if (authoredValue == null && showValue) retainedValues.Add(data.Raw);
        var requestedValues = scaleValues.Where(number => ExplicitScaleLabel(options, number) == null).Concat(retainedValues);
        // One caption per requested numeric fact keeps a shared value/target/tick consistent and resolves callbacks once.
        var distinctValues = requestedValues.Distinct().ToArray();
        var format = chart.Options.ValueFormat;
        var formatted = distinctValues.Length == 0 ? Array.Empty<string>() : ChartNumericFormatter.FormatScaleValues(format, distinctValues);
        var captions = new Dictionary<double, string>();
        for (var index = 0; index < distinctValues.Length; index++) captions[distinctValues[index]] = formatted[index];
        var scaleCaptions = scaleValues.Select(number => ExplicitScaleLabel(options, number) ?? captions[number]).ToArray();
        FitGeneratedScale(chart, context, builder, plot, data, format, retainedValues, captions, ref scaleValues, ref scaleCaptions);
        var value = authoredValue ?? (showValue ? captions[data.Raw] : N(data.Raw));
        var targetCaptions = data.Targets.Select(target => target.Label ?? captions[target.Value]).ToArray();
        var target = targetCaptions.Length > 0 ? targetCaptions[0] : null;
        var min = scaleCaptions.Length > 0 ? scaleCaptions[0] : null;
        var max = scaleCaptions.Length > 0 ? scaleCaptions[scaleCaptions.Length - 1] : null;
        builder.AddRegion(new VisualSemanticRegion("series-0", "gauge", plot, series.Name + ": " + value));
        var metadata = new Dictionary<string, string> {
            ["data-cfx-series"] = "0", ["data-cfx-point"] = "0", ["data-cfx-source-point"] = "0", ["data-cfx-source-count"] = "1",
            ["data-cfx-value"] = N(data.Raw), ["data-cfx-min"] = N(data.Min), ["data-cfx-max"] = N(data.Max),
            ["data-cfx-percent"] = N(data.Ratio), ["data-cfx-status"] = data.State.ToString(),
            ["data-cfx-label-target"] = chart.Options.Labels.Target,
            ["data-cfx-pin-state-colors"] = chart.Options.PinStateColorsInForcedColors ? "true" : "false",
            ["aria-label"] = series.Name + ": " + value
        };
        CaptureTargets(metadata, data, captions);
        using (builder.PushGroup("series-0", "gauge", metadata)) {
            for (var index = 0; index < data.Targets.Count; index++) ReferenceFact(builder, data, index, plot, captions[data.Targets[index].Value]);
            if (plot.Width <= 0 || plot.Height <= 0) { builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "No content viewport remains for the gauge.")); return; }
            using (builder.PushClip(plot)) {
                if (options.Form == ChartGaugeForm.Linear && options.Orientation == ChartOrientation.Vertical)
                    Vertical(chart, context, builder, plot, data, value, caption, scaleValues, scaleCaptions, targetCaptions);
                else if (options.Form == ChartGaugeForm.Linear)
                    Linear(chart, context, builder, plot, data, value, caption, target, scaleCaptions, scaleValues, targetCaptions);
                else if (ConfiguredCircular(options) || data.Targets.Count > 1)
                    CircularConfigured(chart, context, builder, plot, data, value, caption, scaleValues, scaleCaptions, targetCaptions);
                else Circular(chart, context, builder, plot, data, value, caption, target, min, max);
            }
        }
    }

    private static GaugeData Read(Chart chart, VisualThemeColors colors) {
        var series = chart.Series[0];
        if (series.GaugeValues.Count != 1) throw new InvalidOperationException("A gauge requires one measured value and range.");
        var reading = series.GaugeValues[0];
        var min = reading.Minimum; var max = reading.Maximum; var raw = reading.Value;
        ChartGuards.Finite(min, nameof(chart)); ChartGuards.Finite(max, nameof(chart)); ChartGuards.Finite(raw, nameof(chart));
        if (max <= min || double.IsInfinity(max - min)) throw new InvalidOperationException("A gauge requires a finite positive range.");
        var bands = chart.Options.Gauge.Bands.OrderBy(band => band?.Minimum).ToArray();
        if (bands.Any(band => band == null)) throw new InvalidOperationException("Gauge bands must not contain null.");
        for (var index = 1; index < bands.Length; index++) if (bands[index].Minimum < bands[index - 1].Maximum) throw new InvalidOperationException("Gauge bands must not overlap.");
        var bounded = VisualRadialPrimitives.Clamp(raw, min, max); var ratio = (bounded - min) / (max - min);
        var active = bands.FirstOrDefault(band => bounded >= band.Minimum && (bounded < band.Maximum || bounded == max && bounded == band.Maximum));
        // A numeric value has no implied severity until the caller declares bands or a state.
        var state = active?.State ?? ChartSeriesState.None;
        if (series.StateRole != ChartSeriesState.None) state = series.StateRole;
        var color = series.PointColors.Count > 0 && series.PointColors[0].HasValue ? series.PointColors[0]!.Value
            : series.Color ?? (series.StateRole != ChartSeriesState.None || state is ChartSeriesState.Warning or ChartSeriesState.Danger
                ? VisualRadialPrimitives.StateColor(colors, state, colors.Palette[0]) : colors.Palette[0]);
        return new GaugeData(min, max, raw, ratio, state, color, bands, reading.Targets);
    }

    private static void Circular(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, GaugeData data,
        string value, string caption, string? target, string? min, string? max) {
        var colors = context.Theme.Resolve(context.ThemeMode); var gap = context.Theme.Spacing;
        var labelHeight = context.Theme.Typography.AxisSize * 1.5;
        var bottom = min != null ? labelHeight : 0;
        var top = target != null ? labelHeight : 0;
        var outer = Math.Max(0, Math.Min(plot.Width / 2 - gap, (plot.Height - top - bottom - gap) / 1.5));
        if (outer < 2) { builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "The viewport is too small for a gauge arc.")); return; }
        var cx = plot.Left + plot.Width / 2; var cy = plot.Top + top + outer;
        var stroke = Math.Min(context.Theme.GaugeStrokeWidth, outer * .3);
        var bandWidth = Math.Min(context.Theme.GaugeBandWidth, outer * .1);
        var radius = Math.Max(stroke / 2, outer - stroke / 2 - (data.Bands.Length > 0 ? bandWidth + gap / 2 : 0));
        var start = Start(chart.Options.Gauge); var sweep = Sweep(chart.Options.Gauge);
        VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, start, sweep, colors.Neutral2, "gauge-track", paint: SvgPaint.Of(colors.Neutral2, SvgColorRole.Surface));
        for (var index = 0; index < data.Bands.Length; index++) {
            var band = data.Bands[index];
            var lo = VisualRadialPrimitives.Clamp((band.Minimum - data.Min) / (data.Max - data.Min));
            var hi = VisualRadialPrimitives.Clamp((band.Maximum - data.Min) / (data.Max - data.Min));
            using var source = Band(builder, band, index, new ChartRect(cx - outer, cy - outer, outer * 2, outer * 1.5));
            VisualRadialPrimitives.Arc(builder, cx, cy, outer - bandWidth / 2, bandWidth, start + sweep * lo, sweep * (hi - lo),
                VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent), "gauge-band",
                paint: SvgPaint.Of(VisualRadialPrimitives.StateColor(colors, band.State, colors.Accent), band.State == ChartSeriesState.None ? SvgColorRole.Series : SvgColorRole.Status));
        }
        if (chart.Options.Gauge.Form == ChartGaugeForm.Arc)
            VisualRadialPrimitives.Arc(builder, cx, cy, radius, stroke, start, sweep * data.Ratio, data.Color, "gauge-value", "series-0-value", paint: GaugePaint(chart.Series[0], data));
        else {
            var angle = start + sweep * data.Ratio;
            builder.Line(cx, cy, cx + Math.Cos(angle) * (radius - stroke), cy + Math.Sin(angle) * (radius - stroke), data.Color,
                Math.Max(1, context.Theme.SeriesStrokeWidth), "gauge-needle", paint: VisualChartPaint.Stroke(GaugePaint(chart.Series[0], data)));
            builder.Ellipse(cx, cy, stroke / 3, stroke / 3, colors.Foreground, role: "gauge-pivot", paint: VisualChartPaint.Fill(colors.Foreground, SvgColorRole.Axis));
        }
        foreach (var reference in data.Targets) {
            var angle = start + sweep * Ratio(data, reference.Value);
            builder.Line(cx + Math.Cos(angle) * (radius - stroke / 2 - bandWidth), cy + Math.Sin(angle) * (radius - stroke / 2 - bandWidth),
                cx + Math.Cos(angle) * (radius + stroke / 2 + bandWidth), cy + Math.Sin(angle) * (radius + stroke / 2 + bandWidth),
                colors.Foreground, context.Theme.SeriesStrokeWidth, "gauge-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Axis));
        }
        if (chart.Series[0].ResolveDataLabels(chart.Options, defaultVisible: true)) {
            var needle = chart.Options.Gauge.Form == ChartGaugeForm.Needle;
            var captionBottom = Math.Min(plot.Bottom, cy + radius * .5 + stroke / 2);
            // Below-pivot summaries can use the gap before the scale row, including taller resolved faces.
            if (needle) captionBottom = min != null ? CircularScaleLabelTop(plot, cy, radius, stroke, gap, labelHeight) : plot.Bottom;
            var valueBudget = radius * (needle ? .34 : .6);
            var valueTop = needle ? cy + stroke / 3 + gap / 2 : 0;
            if (needle) {
                var desiredCaptionStyle = VisualRadialPrimitives.Style(chart, context, colors.MutedForeground,
                    context.Theme.Typography.DataLabelSize, point: 0);
                var captionHeight = string.IsNullOrEmpty(caption) ? 0 : builder.MeasureText(caption, desiredCaptionStyle).Height;
                // The needle summary sits below its pivot; reserve its real caption row before fitting the scalar.
                valueBudget = Math.Min(valueBudget, Math.Max(0, captionBottom - valueTop - gap / 3 - captionHeight));
            }
            var valueStyle = LabelStyle(chart, context, builder, value, valueBudget,
                context.Theme.Typography.ScalarValueSize, 700, minimumDefaultSize: needle ? context.Theme.Typography.DataLabelSize : .1);
            var captionTop = 0d;
            if (needle) valueStyle = FitNeedleSummary(chart, context, builder, value, caption, start + sweep * data.Ratio,
                cy, radius, stroke, captionBottom, valueBudget, out valueTop, out valueBudget, out captionTop);
            var valueHeight = Math.Min(valueBudget, builder.MeasureText(value, valueStyle).Height);
            if (!needle) { valueTop = cy - valueHeight / 2; captionTop = valueTop + valueHeight + gap / 3; }
            VisualRadialPrimitives.Text(builder, value, new ChartRect(cx - radius * .7, valueTop, radius * 1.4, valueHeight),
                valueStyle, "gauge-label", "series-0-gauge-label");
            var captionStyle = LabelStyle(chart, context, builder, caption, Math.Max(0, captionBottom - captionTop),
                context.Theme.Typography.DataLabelSize, muted: true, minimumDefaultSize: context.Theme.Typography.DataLabelSize);
            VisualRadialPrimitives.Text(builder, caption, new ChartRect(cx - radius * .75, captionTop, radius * 1.5,
                Math.Max(0, Math.Min(builder.MeasureText(caption, captionStyle).Height, captionBottom - captionTop))),
                captionStyle, "gauge-title", "series-0-gauge-title");
        }
        CircularLabels(chart, context, builder, plot, data, target, min, max, cx, cy, radius, stroke, start, sweep, labelHeight);
    }

    private static TextStyle FitNeedleSummary(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        string value, string caption, double angle, double cy, double radius, double stroke,
        double bottom, double maximumHeight, out double top, out double permittedHeight, out double captionTop) {
        var gap = context.Theme.Spacing;
        var captionStyle = LabelStyle(chart, context, builder, caption, bottom - cy,
            context.Theme.Typography.DataLabelSize, muted: true, minimumDefaultSize: context.Theme.Typography.DataLabelSize);
        var captionHeight = string.IsNullOrEmpty(caption) ? 0 : builder.MeasureText(caption, captionStyle).Height;
        TextStyle Style(double height) => LabelStyle(chart, context, builder, value, height,
            context.Theme.Typography.ScalarValueSize, 700, minimumDefaultSize: context.Theme.Typography.DataLabelSize);
        double ClearTop(string text, TextStyle style, double width) {
            var fitted = ChartTextFitting.TrimEnd(text, style.FontSize, width, (part, _) => builder.MeasureText(part, style).Width);
            var measured = builder.MeasureText(fitted, style).Width;
            var portable = new TextMeasurementContext(style.Font);
            var displayed = TextCaseTransformer.Apply(fitted, style.TextCase, CultureInfo.InvariantCulture);
            var envelope = measured;
            foreach (var line in TextLineScanner.Enumerate(displayed))
                envelope = Math.Max(envelope, portable.Measure(line.Read(displayed), style.EffectiveFontSize, style.Font.Weight >= 600));
            // SVG keeps the native row's left edge, while the host may select a wider fallback face.
            // Reserve that portable extent on the descending needle's side without changing the authored font.
            var half = Math.Cos(angle) > 0 ? envelope - measured / 2 : measured / 2;
            var down = Math.Sin(angle); var sideways = Math.Abs(Math.Cos(angle));
            var depth = down > 0 && sideways > .000001
                ? Math.Min((radius - stroke) * down, half * down / sideways) : 0;
            // Browser SVG can use the face's Windows ascent even when the native painter uses
            // its shorter horizontal-header ascent. Move the shared row by that measured extent.
            var ascenderOverflow = Math.Max(0, builder.TextAscentEnvelope(style) - builder.TextAscent(style));
            return cy + Math.Max(stroke / 3 + gap / 2,
                depth + Math.Max(1, context.Theme.SeriesStrokeWidth) / 2 + gap / 2) + ascenderOverflow;
        }
        bool Fits(TextStyle style) {
            var start = ClearTop(value, style, radius * 1.4);
            var height = builder.MeasureText(value, style).Height;
            var captionStart = Math.Max(start + height + gap / 3, ClearTop(caption, captionStyle, radius * 1.5));
            return height <= maximumHeight + .000001 && captionStart + captionHeight <= bottom + .000001;
        }
        var result = Style(maximumHeight);
        if (!Fits(result)) {
            var low = 0d; var high = maximumHeight;
            // Both the row height and its needle clearance depend on the resolved font width.
            // Find the largest fitting default; authored font sizes retain the usual overflow policy.
            for (var attempt = 0; attempt < 12; attempt++) {
                var middle = (low + high) / 2; var candidate = Style(middle);
                if (Fits(candidate)) { low = middle; result = candidate; }
                else high = middle;
            }
        }
        var fits = Fits(result);
        top = fits ? ClearTop(value, result, radius * 1.4) : bottom;
        permittedHeight = fits ? maximumHeight : 0;
        captionTop = fits ? Math.Max(top + builder.MeasureText(value, result).Height + gap / 3,
            ClearTop(caption, captionStyle, radius * 1.5)) : bottom;
        return result;
    }

    private static void CircularLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        GaugeData data, string? target, string? min, string? max, double cx, double cy, double radius, double stroke,
        double start, double sweep, double height) {
        var gap = context.Theme.Spacing;
        var labelY = CircularScaleLabelTop(plot, cy, radius, stroke, gap, height);
        var endpointWidth = Math.Min(plot.Width / 3, Math.Max(gap * 2, radius * .55));
        ChartRect Endpoint(double angle) => new(
            Math.Max(plot.Left, Math.Min(plot.Right - endpointWidth, cx + Math.Cos(angle) * radius - endpointWidth / 2)),
            labelY, endpointWidth, height);
        var minimum = Endpoint(start); var maximum = Endpoint(start + sweep);
        if (min != null && max != null) {
            Label(chart, context, builder, min, minimum, "gauge-min-label", context.Theme.Typography.AxisSize, ticks: true);
            Label(chart, context, builder, max, maximum, "gauge-max-label", context.Theme.Typography.AxisSize, ticks: true);
        }
        if (target == null) return;
        var angle = start + sweep * Ratio(data, data.Targets[0].Value);
        var style = VisualRadialPrimitives.Style(chart, context, context.Theme.Resolve(context.ThemeMode).MutedForeground,
            context.Theme.Typography.AxisSize, ticks: true);
        var width = Math.Min(plot.Width / 3, builder.MeasureText(target, style).Width + gap);
        var outside = radius + stroke / 2 + gap + height / 2;
        var left = Math.Max(plot.Left, Math.Min(plot.Right - width, cx + Math.Cos(angle) * outside - width / 2));
        var labelTop = Math.Max(plot.Top, Math.Min(plot.Bottom - height, cy + Math.Sin(angle) * outside - height / 2));
        // Near either arc end the scale label already occupies the lower exterior row.
        if (min != null && labelTop + height > minimum.Top && labelTop < minimum.Bottom
            && (left + width > minimum.Left && left < minimum.Right || left + width > maximum.Left && left < maximum.Right))
            labelTop = Math.Max(plot.Top, minimum.Top - height - gap / 2);
        Label(chart, context, builder, target, new ChartRect(left, labelTop, width, height),
            "gauge-target-label", context.Theme.Typography.AxisSize, ticks: true);
    }

    /// <summary>Locates the lower scale row, also bounding the needle's central summary above it.</summary>
    private static double CircularScaleLabelTop(ChartRect plot, double cy, double radius, double stroke, double gap, double height) =>
        Math.Min(plot.Bottom - height, cy + radius * .5 + stroke / 2 + gap / 2);

    private static void Linear(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, GaugeData data,
        string value, string caption, string? target, IReadOnlyList<string> scaleCaptions, IReadOnlyList<double> scaleValues, IReadOnlyList<string> targetCaptions) {
        var colors = context.Theme.Resolve(context.ThemeMode); var gap = context.Theme.Spacing;
        var labelHeight = context.Theme.Typography.AxisSize * 1.5;
        if (scaleCaptions.Count > 0) {
            var style = VisualRadialPrimitives.Style(chart, context, colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
            labelHeight = Math.Max(labelHeight, scaleCaptions.Max(text => builder.MeasureText(text, style).Height) + 3);
        }
        if (targetCaptions.Count > 0) {
            var multiple = targetCaptions.Count > 1;
            var style = VisualRadialPrimitives.Style(chart, context, multiple ? colors.MutedForeground : colors.Foreground,
                context.Theme.Typography.AxisSize, point: multiple ? -1 : 0, ticks: multiple);
            labelHeight = Math.Max(labelHeight, targetCaptions.Max(text => builder.MeasureText(text, style).Height));
        }
        var height = Math.Min(plot.Height / 7, Math.Max(4, context.Theme.Typography.DataLabelSize * 1.4));
        var footerTop = plot.Bottom - labelHeight * ((scaleCaptions.Count > 0 ? 1 : 0) + (target != null ? 1 : 0));
        var maximumTrackTop = footerTop - height - (target != null ? height / 4 : 0) - gap / 3;
        if (maximumTrackTop < plot.Top + height / 2) {
            builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "The viewport cannot fit the linear gauge and its declared scale rows."));
            return;
        }
        var summaryBudget = Math.Max(0, maximumTrackTop - height / 2 - gap / 3 - plot.Top);
        var valueStyle = LabelStyle(chart, context, builder, value, summaryBudget, context.Theme.Typography.ScalarValueSize, 700,
            minimumDefaultSize: context.Theme.Typography.DataLabelSize);
        var captionStyle = LabelStyle(chart, context, builder, caption, summaryBudget, context.Theme.Typography.DataLabelSize, muted: true,
            minimumDefaultSize: context.Theme.Typography.DataLabelSize);
        var summaryHeight = !chart.Series[0].ResolveDataLabels(chart.Options, defaultVisible: true) ? 0
            : Math.Min(summaryBudget, Math.Max(builder.MeasureText(value, valueStyle).Height, builder.MeasureText(caption, captionStyle).Height));
        var trackTop = Math.Min(maximumTrackTop, Math.Max(plot.Top + plot.Height * .45, plot.Top + summaryHeight + gap / 3 + height / 2));
        var track = new ChartRect(plot.Left + gap, trackTop, Math.Max(0, plot.Width - gap * 2), height);
        if (track.Width <= 0 || height <= 0) { builder.AddDiagnostic(new VisualDiagnostic("gauge.insufficient-space", "The viewport is too small for a linear gauge.")); return; }
        builder.Rect(track, colors.Neutral2, role: "gauge-track", paint: VisualChartPaint.Fill(colors.Neutral2, SvgColorRole.Surface));
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
        foreach (var reference in data.Targets) {
            var tx = track.Left + track.Width * Ratio(data, reference.Value);
            builder.Line(tx, track.Top - height / 4, tx, track.Bottom + height / 4, colors.Foreground, context.Theme.SeriesStrokeWidth, "gauge-target", paint: VisualChartPaint.Stroke(colors.Foreground, SvgColorRole.Axis));
        }
        if (chart.Series[0].ResolveDataLabels(chart.Options, defaultVisible: true)) {
            var valueWidth = Math.Min(track.Width * .6, builder.MeasureText(value, valueStyle).Width);
            var captionWidth = Math.Min(Math.Max(0, track.Width - valueWidth - gap), builder.MeasureText(caption, captionStyle).Width);
            var summaryLeft = track.Left + (track.Width - valueWidth - gap - captionWidth) / 2;
            VisualRadialPrimitives.Text(builder, value, new ChartRect(summaryLeft, plot.Top, valueWidth, summaryHeight),
                valueStyle, "gauge-label", "series-0-gauge-label");
            VisualRadialPrimitives.Text(builder, caption, new ChartRect(summaryLeft + valueWidth + gap, plot.Top, captionWidth, summaryHeight),
                captionStyle, "gauge-title", "series-0-gauge-title");
        }
        LinearFooter(chart, context, builder, plot, target, scaleCaptions, labelHeight, data, scaleValues, targetCaptions);
    }

    private static void LinearFooter(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, string? target, IReadOnlyList<string> scaleCaptions, double height,
        GaugeData data, IReadOnlyList<double> scaleValues, IReadOnlyList<string> targetCaptions) {
        var y = plot.Bottom - height;
        if (target != null) {
            if (targetCaptions.Count == 1) Label(chart, context, builder, target, new ChartRect(plot.Left + plot.Width / 3, y, plot.Width / 3, height), "gauge-target-label", context.Theme.Typography.AxisSize);
            else LinearTargetLabels(chart, context, builder, plot, data, targetCaptions, y, height);
            y -= height;
        }
        if (scaleCaptions.Count == 0) return;
        var colors = context.Theme.Resolve(context.ThemeMode);
        var left = plot.Left + context.Theme.Spacing; var width = Math.Max(0, plot.Width - context.Theme.Spacing * 2);
        var occupied = new List<ChartRect>();
        var style = VisualRadialPrimitives.Style(chart, context, colors.MutedForeground, context.Theme.Typography.AxisSize, ticks: true);
        var labelBounds = new ChartRect[scaleCaptions.Count];
        for (var tick = 0; tick < scaleCaptions.Count; tick++) {
            var x = left + width * Ratio(data, scaleValues[tick]);
            var captionWidth = builder.MeasureText(scaleCaptions[tick], style).Width;
            labelBounds[tick] = new ChartRect(Math.Max(plot.Left, Math.Min(plot.Right - captionWidth, x - captionWidth / 2)),
                y + 3, captionWidth, Math.Max(0, height - 3));
        }
        var last = scaleCaptions.Count - 1;
        var reserveEndpoints = chart.Options.Gauge.TickLabels.Count == 0 && last > 0
            && FitsCaption(plot, labelBounds[0], occupied)
            && FitsCaption(plot, labelBounds[last], new List<ChartRect> { labelBounds[0] })
            && builder.MeasureText(scaleCaptions[0], style).Height <= labelBounds[0].Height + .000001
            && builder.MeasureText(scaleCaptions[last], style).Height <= labelBounds[last].Height + .000001;
        // Reserve the complete generated endpoints before interior facts, retaining authored paint order.
        if (reserveEndpoints) { occupied.Add(labelBounds[0]); occupied.Add(labelBounds[last]); }
        builder.Line(left, y, left + width, y, colors.Border, context.Theme.AxisStrokeWidth, "gauge-axis", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
        for (var tick = 0; tick < scaleCaptions.Count; tick++) {
            var x = left + width * Ratio(data, scaleValues[tick]);
            builder.Line(x, y, x, y + 3, colors.Border, context.Theme.AxisStrokeWidth, "gauge-tick", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Axis));
            var bounds = labelBounds[tick];
            if (reserveEndpoints && (tick == 0 || tick == last)) occupied.Remove(bounds);
            FactualLabel(chart, context, builder, plot, scaleCaptions[tick], bounds, ScaleRole(data, scaleValues[tick], tick),
                "series-0-gauge-scale-" + tick, occupied);
        }
    }

    private static IDisposable Band(VisualSceneBuilder builder, ChartGaugeBand band, int index, ChartRect bounds) {
        var id = "series-0-band-" + index;
        builder.AddRegion(new VisualSemanticRegion(id, "gauge-band", bounds, N(band.Minimum) + "–" + N(band.Maximum) + ": " + band.State));
        return builder.PushGroup(id, "gauge-band-source", new Dictionary<string, string> {
            ["data-cfx-min"] = N(band.Minimum), ["data-cfx-max"] = N(band.Maximum), ["data-cfx-status"] = band.State.ToString()
        });
    }

    private static void Label(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string text, ChartRect bounds, string role, double size, int weight = 400, bool ticks = false, bool muted = false) =>
        VisualRadialPrimitives.Text(builder, text, bounds,
            LabelStyle(chart, context, builder, text, bounds.Height, size, weight, ticks, muted), role, "series-0-" + role);

    private static TextStyle LabelStyle(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, string text, double maximumHeight,
        double size, int weight = 400, bool ticks = false, bool muted = false, double minimumDefaultSize = .1) {
        var colors = context.Theme.Resolve(context.ThemeMode);
        var color = muted || ticks ? colors.MutedForeground : colors.Foreground;
        var style = VisualRadialPrimitives.Style(chart, context, color, size, weight, point: 0, ticks: ticks);
        var measured = builder.MeasureText(text, style).Height;
        // Font rows depend on the resolved face, line height and authored formatting, not a font-size multiplier.
        // Re-resolving a bounded default keeps explicit chart, series and point font sizes authoritative.
        return measured <= maximumHeight ? style : VisualRadialPrimitives.Style(chart, context, color,
            Math.Max(minimumDefaultSize, size * Math.Max(0, maximumHeight) / measured), weight, point: 0, ticks: ticks);
    }

    private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static SvgPaint GaugePaint(ChartSeries series, GaugeData data) => SvgPaint.Of(data.Color,
        series.Color.HasValue || series.PointColors.Count > 0 && series.PointColors[0].HasValue ? SvgColorRole.Series
            : series.StateRole != ChartSeriesState.None || data.State is ChartSeriesState.Warning or ChartSeriesState.Danger ? SvgColorRole.Status : SvgColorRole.Series);
    private sealed class GaugeData {
        internal GaugeData(double min, double max, double raw, double ratio, ChartSeriesState state, ChartColor color, ChartGaugeBand[] bands, IReadOnlyList<ChartGaugeTarget> targets) {
            Min = min; Max = max; Raw = raw; Ratio = ratio; State = state; Color = color; Bands = bands; Targets = targets;
        }
        internal double Min { get; } internal double Max { get; } internal double Raw { get; } internal double Ratio { get; }
        internal ChartSeriesState State { get; } internal ChartColor Color { get; } internal ChartGaugeBand[] Bands { get; }
        internal IReadOnlyList<ChartGaugeTarget> Targets { get; }
    }
}
