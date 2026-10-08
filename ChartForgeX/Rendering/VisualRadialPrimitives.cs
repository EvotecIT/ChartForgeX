using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Shared numeric arcs and measured factual text for radial value families.</summary>
internal static class VisualRadialPrimitives {
    internal static double Clamp(double value, double min = 0, double max = 1) => Math.Max(min, Math.Min(max, value));

    internal static ChartColor StateColor(VisualThemeColors colors, ChartSeriesState state, ChartColor fallback) {
        return ChartSeriesColours.State(state, colors, fallback);
    }

    internal static ChartColor Color(ChartSeries series, int index, VisualThemeColors colors) =>
        ChartSeriesColours.Point(series, index, index, colors);

    internal static void Arc(VisualSceneBuilder builder, double cx, double cy, double radius, double width,
        double start, double sweep, ChartColor color, string role, string? id = null, bool round = false, SvgPaint? paint = null) {
        if (radius <= 0 || width <= 0 || sweep <= 0) return;
        builder.Slice(cx, cy, radius + width / 2, Math.Max(0, radius - width / 2), start, Math.Min(Math.PI * 2, sweep), color, role: role, id: id,
            paint: paint.HasValue ? VisualChartPaint.Fill(paint.Value) : null);
        if (!round || sweep >= Math.PI * 2 - .000001) return;
        builder.Ellipse(cx + Math.Cos(start) * radius, cy + Math.Sin(start) * radius, width / 2, width / 2, color, role: role + "-cap", paint: paint.HasValue ? VisualChartPaint.Fill(paint.Value) : null);
        builder.Ellipse(cx + Math.Cos(start + sweep) * radius, cy + Math.Sin(start + sweep) * radius, width / 2, width / 2, color, role: role + "-cap", paint: paint.HasValue ? VisualChartPaint.Fill(paint.Value) : null);
    }

    internal static TextStyle Style(Chart chart, VisualRenderContext context, ChartColor color, double size, int weight = 400, int point = -1, bool ticks = false) {
        var fallback = new TextStyle { Font = context.Font, Color = color, FontSize = size };
        fallback.Font.Weight = weight;
        if (ticks) return chart.Options.TickLabelStyle.Resolve(fallback);
        var style = chart.Series[0].DataLabelStyle.Resolve(chart.Options.DataLabelStyle.Resolve(fallback));
        if (point >= 0 && point < chart.Series[0].PointDataLabelStyles.Count && chart.Series[0].PointDataLabelStyles[point] != null)
            style = chart.Series[0].PointDataLabelStyles[point]!.Resolve(style);
        return style;
    }

    internal static void Text(VisualSceneBuilder builder, string full, ChartRect area, TextStyle style, string role, string id, SvgPaint? paint = null,
        TextAlignment alignment = TextAlignment.Center) {
        builder.AddRegion(new VisualSemanticRegion(id, role, area, full));
        using (builder.PushGroup(id + "-source", role + "-source", new Dictionary<string, string> { ["aria-label"] = full, ["data-cfx-label"] = full })) {
            var fitted = ChartTextFitting.TrimEnd(full, style.FontSize, Math.Max(0, area.Width), (text, _) => builder.MeasureText(text, style).Width);
            var metrics = builder.MeasureText(fitted, style);
            // Centering and intersecting a measured row may subtract a floating-point ULP.
            if (fitted.Length == 0 || metrics.Height > area.Height + .000001) {
                builder.AddDiagnostic(new VisualDiagnostic("radial.text-overflow", "Radial text was omitted because it does not fit the fixed viewport; the complete value remains in descriptive regions."));
                return;
            }
            if (!string.Equals(fitted, full, StringComparison.Ordinal))
                builder.AddDiagnostic(new VisualDiagnostic("radial.text-overflow", "Radial text was shortened; the complete value remains in descriptive regions."));
            style = style.Clone(); style.Alignment = alignment;
            var shift = style.Baseline == TextBaseline.Superscript ? -style.FontSize * .35 : style.Baseline == TextBaseline.Subscript ? style.FontSize * .22 : 0;
            var baseline = area.Top + (area.Height - metrics.Height) / 2 + builder.TextAscent(style);
            var anchor = alignment == TextAlignment.Left ? area.Left : alignment == TextAlignment.Right ? area.Right : area.Left + area.Width / 2;
            builder.Text(fitted, anchor, baseline - shift, style, role, id, paint: paint ?? VisualChartPaint.Text(style));
        }
    }

    internal static string Label(Chart chart, ChartPoint point, int index) {
        foreach (var label in chart.Options.XAxisLabels) if (ChartMath.SameCoordinate(label.Value, point.X)) return label.Text;
        return "Ring " + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
