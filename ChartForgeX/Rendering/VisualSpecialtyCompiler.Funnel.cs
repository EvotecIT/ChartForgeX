using System;
using System.Collections.Generic;
using System.Globalization;
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
        var form = chart.Options.Funnel.Form;
        var layout = ChartFunnelLayout.Compute(series.Points, plot, chart.Options.Funnel, context.Theme.Spacing, show);
        using var group = builder.PushGroup("funnel-chart", "funnel-chart", new Dictionary<string, string> {
            ["data-cfx-form"] = form == ChartFunnelForm.StageBars ? "stage-bars" : "cone",
            ["data-cfx-orientation"] = chart.Options.Funnel.Orientation == ChartOrientation.Vertical ? "vertical" : "horizontal"
        });
        foreach (var connection in layout.Connections) FunnelConnection(chart, builder, connection, colors, form);
        foreach (var stage in layout.Stages) {
            var index = stage.SourceIndex; var raw = series.Points[index].Y;
            var color = Color(series, index, colors);
            var previous = index == 0 ? raw : series.Points[index - 1].Y;
            var retention = series.Points[0].Y <= 0 ? (double?)null : raw / series.Points[0].Y;
            var drop = previous <= 0 || index == 0 ? (double?)null : (previous - raw) / previous;
            var metadata = new Dictionary<string, string> { ["data-cfx-zero"] = raw == 0 ? "true" : "false",
                ["data-cfx-retention-defined"] = retention.HasValue ? "true" : "false",
                ["data-cfx-dropoff-defined"] = drop.HasValue ? "true" : "false" };
            if (retention.HasValue) metadata["data-cfx-retention"] = N(retention.Value);
            if (drop.HasValue) metadata["data-cfx-dropoff"] = N(drop.Value);
            var summary = Category(chart, index) + ": " + VisualStateSceneTools.Value(chart, series, index, raw);
            if (retention.HasValue) summary += ", retained " + retention.Value.ToString("0.#%", CultureInfo.InvariantCulture);
            if (drop.HasValue) summary += ", drop-off " + drop.Value.ToString("0.#%", CultureInfo.InvariantCulture);
            using (Point(chart, builder, index, "funnel-stage", stage.Bounds, summary, metadata)) {
                if (form == ChartFunnelForm.Cone) {
                    var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;
                    var line = new ChartPath(new[] { ChartPathCommand.MoveTo(stage.LineStart.X, stage.LineStart.Y),
                        ChartPathCommand.LineTo(stage.LineEnd.X, stage.LineEnd.Y) });
                    builder.Path(line, stroke: color, strokeWidth: stroke, role: "funnel-stage-line", cap: VisualStrokeCap.Butt,
                        paint: VisualChartPaint.Stroke(VisualChartPaint.Series(series, color, index)));
                } else if (raw > 0) {
                    builder.Path(stage.Bar, color, role: "funnel-segment", close: true,
                        paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, index)));
                    Pattern(chart, builder, index, stage.Bar, color);
                }
                if (raw == 0) builder.Line(stage.ZeroStart.X, stage.ZeroStart.Y, stage.ZeroEnd.X, stage.ZeroEnd.Y,
                    colors.Border, role: "funnel-zero", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Surface));
                if (show) FunnelLabels(chart, context, builder, stage, color, colors, raw, retention, drop, form);
            }
        }
        if (series.Points.All(point => point.Y == 0))
            builder.AddDiagnostic(new VisualDiagnostic("funnel.all-zero", "All funnel stage values are zero; their category slots remain visible."));
    }

    private static void FunnelConnection(Chart chart, VisualSceneBuilder builder, ChartFunnelConnectionLayout connection,
        VisualThemeColors colors, ChartFunnelForm form) {
        var series = chart.Series[0]; var color = Color(series, connection.FromIndex, colors);
        var opacity = form == ChartFunnelForm.Cone ? .3 : .18;
        var fill = ChartColorMath.WithOpacity(color, opacity);
        var id = Id(connection.FromIndex) + "-connection";
        var summary = Category(chart, connection.FromIndex) + " to " + Category(chart, connection.ToIndex) + ": "
            + VisualStateSceneTools.Value(chart, series, connection.FromIndex, series.Points[connection.FromIndex].Y)
            + " to " + VisualStateSceneTools.Value(chart, series, connection.ToIndex, series.Points[connection.ToIndex].Y);
        using (VisualStateSceneTools.Mark(builder, id, "funnel-connection", connection.Bounds, summary,
            new Dictionary<string, string> {
                ["data-cfx-from-point"] = N(connection.FromIndex), ["data-cfx-to-point"] = N(connection.ToIndex),
                ["data-cfx-from-value"] = N(series.Points[connection.FromIndex].Y),
                ["data-cfx-to-value"] = N(series.Points[connection.ToIndex].Y)
            })) {
            builder.Path(connection.Path, fill, role: form == ChartFunnelForm.Cone ? "funnel-connection-area" : "funnel-dropoff", close: true,
                paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, connection.FromIndex).WithOpacity(fill, opacity)));
            if (form == ChartFunnelForm.Cone) Pattern(chart, builder, connection.FromIndex, connection.Path, color);
        }
    }

    private static void FunnelLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartFunnelStageLayout stage,
        ChartColor color, VisualThemeColors colors, double raw, double? retention, double? drop, ChartFunnelForm form) {
        var index = stage.SourceIndex;
        var text = Category(chart, index) + ": " + VisualStateSceneTools.Value(chart, chart.Series[0], index, raw);
        var inside = raw > 0 && form == ChartFunnelForm.StageBars;
        var textColor = inside ? ChartColorMath.AccessibleTextOnBackground(color) : colors.Foreground;
        var style = Style(chart, context, index, textColor);
        var bounds = stage.LabelBounds;
        var measured = builder.MeasureText(text, style);
        double Fit(ChartRect box) => Math.Min(box.Width / Math.Max(1, measured.Width), box.Height / Math.Max(1, measured.Height));
        // Keep very small positive marks truthful; a fitted label can use the adjacent empty space instead.
        if (Fit(bounds) < .65 && Fit(stage.OutsideLabelBounds) > Fit(bounds)) {
            bounds = stage.OutsideLabelBounds;
            inside = false;
            style = Style(chart, context, index, colors.Foreground);
        }
        FunnelText(builder, text, bounds, style, "funnel-label", Id(index) + "-label", TextAlignment.Center,
            VisualChartPaint.ExplicitDataLabelColor(chart, index) || !inside ? VisualChartPaint.Text(style)
                : SvgPaint.Contrast(color, VisualChartPaint.SeriesRole(chart.Series[0], index)));
        if (index > 0 && stage.MetricsBounds.Width > 0 && stage.MetricsBounds.Height > 0) {
            var metrics = (retention.HasValue ? retention.Value.ToString("0.#%", CultureInfo.InvariantCulture) + " retained" : "No initial baseline")
                + "\n" + (drop.HasValue ? drop.Value.ToString("0.#%", CultureInfo.InvariantCulture) + " drop-off" : "No previous baseline");
            FunnelText(builder, metrics, stage.MetricsBounds, Style(chart, context, index, colors.MutedForeground),
                "funnel-ratio", Id(index) + "-ratio", TextAlignment.Left,
                VisualChartPaint.Text(Style(chart, context, index, colors.MutedForeground)));
        }
    }

    private static void FunnelText(VisualSceneBuilder builder, string text, ChartRect bounds, TextStyle style,
        string role, string id, TextAlignment alignment, SvgPaint paint) {
        var measured = builder.MeasureText(text, style);
        var ratio = Math.Min(1, Math.Min(bounds.Width / Math.Max(1, measured.Width), bounds.Height / Math.Max(1, measured.Height)));
        style = style.Clone();
        // Preserve deliberately authored small type; automatic fitting never turns an ordinary label into tiny glyphs.
        style.FontSize = Math.Max(Math.Min(10, style.EffectiveFontSize), style.EffectiveFontSize * ratio);
        style.Baseline = TextBaseline.Normal;
        if (bounds.Width <= 0 || bounds.Height < builder.MeasureText(text, style).Height)
            builder.AddDiagnostic(new VisualDiagnostic("funnel.label-hidden", "A funnel label did not fit at a readable size; complete text remains in descriptive regions."));
        VisualStateSceneTools.Text(builder, text, bounds, style, role, id, alignment, paint: paint);
    }
}
