using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    private static void Funnel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var series = chart.Series[0]; var colors = context.Theme.Resolve(context.ThemeMode);
        var show = series.ShowDataLabels ?? chart.Options.ShowDataLabels;
        var maximum = series.Points.Max(point => point.Y);
        var metricsWidth = show && series.Points.Count > 1 ? plot.Width * .27 : 0;
        var gap = Math.Min(context.Theme.Spacing, plot.Height / series.Points.Count * .08);
        var height = Math.Max(0, (plot.Height - gap * (series.Points.Count - 1)) / series.Points.Count);
        var left = plot.Left; var width = Math.Max(0, plot.Width - metricsWidth - (metricsWidth > 0 ? gap : 0));
        var cx = left + width / 2;
        using var group = builder.PushGroup("funnel-chart", "funnel-chart");
        double Width(double value) => maximum <= 0 || value <= 0 ? 0 : width * (.22 + .78 * Math.Min(1, value / maximum));
        for (var index = 0; index < series.Points.Count; index++) {
            var raw = series.Points[index].Y;
            var next = index + 1 < series.Points.Count ? series.Points[index + 1].Y : raw * .82;
            var topWidth = Width(raw); var bottomWidth = Width(next);
            var top = plot.Top + index * (height + gap);
            var path = new ChartPath(new[] {
                ChartPathCommand.MoveTo(cx - topWidth / 2, top), ChartPathCommand.LineTo(cx + topWidth / 2, top),
                ChartPathCommand.LineTo(cx + bottomWidth / 2, top + height), ChartPathCommand.LineTo(cx - bottomWidth / 2, top + height)
            });
            var color = Color(series, index, colors);
            var markWidth = raw > 0 ? Math.Max(topWidth, bottomWidth) : Math.Min(12, width / 10);
            var bounds = new ChartRect(cx - markWidth / 2, top, markWidth, height);
            var previous = index == 0 ? raw : series.Points[index - 1].Y;
            var retention = series.Points[0].Y <= 0 ? (double?)null : raw / series.Points[0].Y;
            var drop = previous <= 0 || index == 0 ? (double?)null : (previous - raw) / previous;
            var metadata = new Dictionary<string, string> { ["data-cfx-zero"] = raw == 0 ? "true" : "false",
                ["data-cfx-retention-defined"] = retention.HasValue ? "true" : "false",
                ["data-cfx-dropoff-defined"] = drop.HasValue ? "true" : "false" };
            if (retention.HasValue) metadata["data-cfx-retention"] = N(retention.Value);
            if (drop.HasValue) metadata["data-cfx-dropoff"] = N(drop.Value);
            var summary = Category(chart, index) + ": " + VisualStateSceneTools.Value(chart, series, index, raw);
            if (retention.HasValue) summary += ", retained " + retention.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture);
            if (drop.HasValue) summary += ", drop-off " + drop.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture);
            using (Point(chart, builder, index, "funnel-stage", bounds, summary, metadata)) {
                if (raw > 0) {
                    builder.Path(path, color, colors.Surface, Math.Min(2, height / 8), "funnel-segment", close: true,
                        paint: new VisualScenePaintBinding(VisualChartPaint.Series(series, color, index), SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                    Pattern(chart, builder, index, path, color);
                } else builder.Line(cx - Math.Min(6, width / 20), top + height / 2, cx + Math.Min(6, width / 20), top + height / 2,
                    colors.Border, role: "funnel-zero", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Surface));
                if (show) {
                    var value = VisualStateSceneTools.Value(chart, series, index, raw);
                    var textColor = raw > 0 ? ChartColorMath.AccessibleTextOnBackground(color) : colors.Foreground;
                    var labelWidth = raw > 0 ? Math.Max(0, (topWidth + bottomWidth) * .4 - gap) : width;
                    VisualStateSceneTools.Text(builder, Category(chart, index) + ": " + value,
                        new ChartRect(cx - labelWidth / 2, top, labelWidth, height), Style(chart, context, index, textColor),
                        "funnel-label", Id(index) + "-label", TextAlignment.Center, shrink: true,
                        paint: VisualChartPaint.ExplicitDataLabelColor(chart, index) || raw == 0 ? SvgPaint.Of(Style(chart, context, index, textColor).Color, SvgColorRole.Text)
                            : SvgPaint.Contrast(color, VisualChartPaint.SeriesRole(series, index)));
                    if (index > 0 && metricsWidth > 0) {
                        var text = (retention.HasValue ? retention.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture) + " retained" : "No initial baseline")
                            + "\n" + (drop.HasValue ? drop.Value.ToString("0.#%", System.Globalization.CultureInfo.InvariantCulture) + " drop-off" : "No previous baseline");
                        VisualStateSceneTools.Text(builder, text, new ChartRect(left + width + gap, top, metricsWidth, height),
                            Style(chart, context, index, colors.MutedForeground), "funnel-ratio", Id(index) + "-ratio", shrink: true,
                            paint: VisualChartPaint.Text(Style(chart, context, index, colors.MutedForeground)));
                    }
                }
            }
        }
    }
}
