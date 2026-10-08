using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualMatrixCompiler {
    private static double CalendarScaleCaptionHeight(Chart chart, VisualSceneBuilder builder, ChartCalendarHeatmapModel model, TextStyle style) {
        var height = builder.MeasureText(chart.Options.Labels.Less + " – " + chart.Options.Labels.More, style).Height;
        if (model.EmptyDays > 0) height = Math.Max(height, builder.MeasureText(chart.Options.Labels.NoData, style).Height);
        if (model.ZeroDays > 0) height = Math.Max(height, builder.MeasureText(ChartNumericFormatter.FormatValue(chart.Options, 0), style).Height);
        return height;
    }

    /// <summary>Keeps measured special-bucket captions and the ordered intensity ramp in one centred key.</summary>
    private static void CalendarScale(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartCalendarHeatmapModel model, ChartRect bounds) {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var style = VisualStateSceneTools.TickStyle(chart, context); var colors = context.Theme.Resolve(context.ThemeMode);
        var gap = context.Theme.Spacing / 2; var captionGap = context.Theme.Spacing / 3;
        var rangeLabel = chart.Options.Labels.Less + " – " + chart.Options.Labels.More;
        var zeroLabel = ChartNumericFormatter.FormatValue(chart.Options, 0);
        var captionHeight = CalendarScaleCaptionHeight(chart, builder, model, style);
        var height = Math.Min(12, Math.Max(0, bounds.Height - captionHeight - captionGap));
        var special = (model.EmptyDays > 0 ? 1 : 0) + (model.ZeroDays > 0 ? 1 : 0);
        var emptyWidth = model.EmptyDays > 0 ? Math.Max(12, builder.MeasureText(chart.Options.Labels.NoData, style).Width + gap) : 0;
        var zeroWidth = model.ZeroDays > 0 ? Math.Max(12, builder.MeasureText(zeroLabel, style).Width + gap) : 0;
        var rampWidth = Math.Max(120, builder.MeasureText(rangeLabel, style).Width);
        var wantedWidth = emptyWidth + zeroWidth + rampWidth + special * gap;
        var ratio = Math.Min(1, bounds.Width / Math.Max(1, wantedWidth));
        emptyWidth *= ratio; zeroWidth *= ratio; rampWidth *= ratio; gap *= ratio;
        var keyWidth = Math.Min(bounds.Width, wantedWidth);
        var left = bounds.Left + (bounds.Width - keyWidth) / 2;
        var textTop = bounds.Top + height + captionGap;
        var textHeight = Math.Max(0, bounds.Bottom - textTop);
        if (height <= 0 || textHeight < captionHeight) builder.AddDiagnostic(new VisualDiagnostic("calendar.scale-overflow",
            "The calendar key does not fit at the configured text size; its complete captions and bucket values remain in descriptive regions."));
        builder.AddRegion(new VisualSemanticRegion("calendar-scale", "calendar-scale", new ChartRect(left, bounds.Top, keyWidth, bounds.Height),
            (model.EmptyDays > 0 ? chart.Options.Labels.NoData + "; " : "") + (model.ZeroDays > 0 ? zeroLabel + "; " : "") + rangeLabel));
        using (builder.PushGroup("calendar-scale", "calendar-scale")) {
            var index = 0;
            if (model.EmptyDays > 0) Special(true, emptyWidth, chart.Options.Labels.NoData, "calendar-scale-empty", "calendar-scale-label");
            if (model.ZeroDays > 0) Special(false, zeroWidth, zeroLabel, "calendar-scale-zero-label", "calendar-scale-zero-label");
            var pitch = rampWidth / 5;
            for (var step = 0; step < 5; step++) {
                var value = model.ScaleValue(step);
                Tile(new ChartRect(left + step * pitch, bounds.Top, Math.Max(0, pitch - Math.Min(3, pitch / 4)), height),
                    value, false, false, ChartNumericFormatter.FormatValue(chart.Options, value));
            }
            VisualStateSceneTools.Text(builder, rangeLabel, new ChartRect(left, textTop, rampWidth, textHeight), style,
                "calendar-scale-label", "calendar-scale-range", TextAlignment.Center);

            void Special(bool empty, double width, string label, string id, string role) {
                var swatch = Math.Min(12, width);
                Tile(new ChartRect(left + (width - swatch) / 2, bounds.Top, swatch, height), 0, empty, !empty, label);
                VisualStateSceneTools.Text(builder, label, new ChartRect(left, textTop, width, textHeight), style, role, id, TextAlignment.Center);
                left += width + gap;
            }
            void Tile(ChartRect box, double value, bool empty, bool zero, string label) {
                var blend = empty ? ChartHeatmapSurface.CalendarEmptyBlend(colors) : zero ? ChartHeatmapSurface.ZeroBlend(colors)
                    : ChartHeatmapSurface.CalendarBlend(colors, model.Series.Color, value, model.RampMin, model.Max, VisualChartPaint.SeriesRole(model.Series));
                var metadata = new Dictionary<string, string> { ["data-cfx-value"] = VisualStateSceneTools.Number(value),
                    ["data-cfx-empty"] = empty ? "true" : "false", ["data-cfx-zero"] = zero ? "true" : "false",
                    ["data-cfx-level"] = model.Level(value).ToString(CultureInfo.InvariantCulture) };
                if (empty && chart.Options.PinStateColorsInForcedColors) metadata["data-cfx-pin-state-colors"] = "true";
                using (VisualStateSceneTools.Mark(builder, "calendar-scale-" + index++, "calendar-scale-step", box, label, metadata))
                    if (height > 0) builder.Rect(box, blend.Color, radius: 1, paint: VisualChartPaint.Fill(blend.Paint));
            }
        }
    }
}
