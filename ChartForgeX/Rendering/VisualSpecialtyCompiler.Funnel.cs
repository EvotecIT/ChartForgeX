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
    private static string FunnelValue(Chart chart, int index) {
        var series = chart.Series[0];
        if (index < series.PointLabels.Count && series.PointLabels[index] != null) return series.PointLabels[index]!;
        var format = ChartNumericFormatter.SeriesCaptionFormat(chart.Options, series.RenderPoints.Select(point => point.Y), sharePrecision: true);
        return format?.Invoke(series.RenderPoints[index].Y) ?? ChartNumericFormatter.FormatValue(chart.Options, series.RenderPoints[index].Y);
    }
    private static ChartValueFormat FunnelPercentageFormat(Chart chart) {
        var points = chart.Series[0].RenderPoints;
        var ratios = new List<double>();
        for (var index = 0; index < points.Count; index++) {
            if (points[0].Y > 0) ratios.Add(points[index].Y / points[0].Y);
            if (index > 0 && points[index - 1].Y > 0) ratios.Add((points[index - 1].Y - points[index].Y) / points[index - 1].Y);
        }
        return ChartValueFormat.Percentages(chart.Options.ValueFormat, ratios);
    }
    private static void Funnel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var series = chart.Series[0]; var colors = context.Theme.Resolve(context.ThemeMode);
        var percentages = FunnelPercentageFormat(chart);
        var show = series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false);
        var form = chart.Options.Funnel.Form;
        var separate = show && chart.Options.Funnel.LabelContent == ChartStageLabelContent.Separate
            ? new StageCaptions(chart, context, builder, plot, true) : null;
        var layout = ChartFunnelLayout.Compute(series.RenderPoints, separate?.Plot ?? plot, chart.Options.Funnel, context.Theme.Spacing, show && separate == null);
        if (separate != null && form == ChartFunnelForm.Cone) separate.CaptureConeConnections(chart, layout, colors);
        if (separate != null) separate.Place(builder, layout.Stages.Select(stage => {
            var vertical = chart.Options.Funnel.Orientation == ChartOrientation.Vertical;
            var zero = series.RenderPoints[stage.SourceIndex].Y == 0;
            var path = zero ? new ChartPath(new[] { ChartPathCommand.MoveTo(stage.ZeroStart.X, stage.ZeroStart.Y),
                ChartPathCommand.LineTo(stage.ZeroEnd.X, stage.ZeroEnd.Y) }) : form == ChartFunnelForm.StageBars ? stage.Bar : new ChartPath(new[] {
                ChartPathCommand.MoveTo(stage.LineStart.X, stage.LineStart.Y), ChartPathCommand.LineTo(stage.LineEnd.X, stage.LineEnd.Y) });
            var outline = StageOutline(series, stage.SourceIndex, Color(series, stage.SourceIndex, colors), chart.Options.Funnel.StageOutline,
                form == ChartFunnelForm.Cone ? context.Theme.SeriesStrokeWidth : 1);
            var width = zero ? 1 : form == ChartFunnelForm.Cone && !outline.Color.HasValue
                ? series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth : outline.Width;
            if (!zero && form == ChartFunnelForm.Cone && !series.HasExplicitStrokeWidth && !outline.Color.HasValue && stage.Extent <= 12)
                width = Math.Max(4, width);
            var bounds = stage.Bounds;
            return new StageCaptionFrame(stage.SourceIndex, bounds, stage.LabelBounds, path, !zero && form == ChartFunnelForm.StageBars, true, width,
                vertical ? stage.LineStart : new ChartPoint(bounds.Left, bounds.Top + bounds.Height / 2),
                vertical ? stage.LineEnd : new ChartPoint(bounds.Right, bounds.Top + bounds.Height / 2),
                vertical ? new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top) : stage.LineStart,
                vertical ? new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Bottom) : stage.LineEnd);
        }).ToArray());
        using var group = builder.PushGroup("funnel-chart", "funnel-chart", new Dictionary<string, string> {
            ["data-cfx-form"] = form == ChartFunnelForm.StageBars ? "stage-bars" : "cone",
            ["data-cfx-orientation"] = chart.Options.Funnel.Orientation == ChartOrientation.Vertical ? "vertical" : "horizontal"
        });
        foreach (var connection in layout.Connections) FunnelConnection(chart, builder, connection, colors, form);
        foreach (var stage in layout.Stages) {
            var index = stage.SourceIndex; var raw = series.RenderPoints[index].Y;
            var color = Color(series, index, colors);
            var previous = index == 0 ? raw : series.RenderPoints[index - 1].Y;
            var retention = series.RenderPoints[0].Y <= 0 ? (double?)null : raw / series.RenderPoints[0].Y;
            var drop = previous <= 0 || index == 0 ? (double?)null : (previous - raw) / previous;
            var metadata = new Dictionary<string, string> { ["data-cfx-zero"] = raw == 0 ? "true" : "false",
                ["data-cfx-retention-defined"] = retention.HasValue ? "true" : "false",
                ["data-cfx-dropoff-defined"] = drop.HasValue ? "true" : "false" };
            if (retention.HasValue) metadata["data-cfx-retention"] = N(retention.Value);
            if (drop.HasValue) metadata["data-cfx-dropoff"] = N(drop.Value);
            var summary = Category(chart, index) + ": " + FunnelValue(chart, index);
            if (retention.HasValue) summary += ", retained " + percentages.FormatFraction(retention.Value);
            if (drop.HasValue) summary += ", drop-off " + percentages.FormatFraction(drop.Value);
            using (Point(chart, builder, index, "funnel-stage", stage.Bounds, summary, metadata)) {
                if (form == ChartFunnelForm.Cone) {
                    var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;
                    if (!series.HasExplicitStrokeWidth && raw > 0 && stage.Extent <= 12) stroke = Math.Max(4, stroke);
                    var line = new ChartPath(new[] { ChartPathCommand.MoveTo(stage.LineStart.X, stage.LineStart.Y),
                        ChartPathCommand.LineTo(stage.LineEnd.X, stage.LineEnd.Y) });
                    var outline = StageOutline(series, index, color, chart.Options.Funnel.StageOutline, stroke);
                    if (outline.Color.HasValue) builder.Path(line, stroke: outline.Color, strokeWidth: outline.Width, role: "funnel-stage-line",
                        cap: VisualStrokeCap.Butt, dash: outline.Dash, dashOffset: chart.Options.Funnel.StageOutlineDashOffset,
                        paint: new VisualScenePaintBinding(stroke: outline.Paint));
                    else builder.Path(line, stroke: color, strokeWidth: stroke, role: "funnel-stage-line", cap: VisualStrokeCap.Butt,
                        paint: VisualChartPaint.Stroke(StagePaint(series, index, color, colors)));
                } else if (raw > 0) {
                    if (chart.Options.Funnel.StageOutline != null || index < series.PointStrokeColors.Count && series.PointStrokeColors[index].HasValue)
                        StagePartition(chart, builder, stage.Bar, index, color, StagePaint(series, index, color, colors), "funnel-segment", chart.Options.Funnel.StageOutline, chart.Options.Funnel.StageOutlineDashOffset);
                    else {
                        builder.Path(stage.Bar, color, role: "funnel-segment", close: true,
                            paint: VisualChartPaint.Fill(StagePaint(series, index, color, colors)));
                        Pattern(chart, builder, index, stage.Bar, color);
                    }
                }
                if (raw == 0) builder.Line(stage.ZeroStart.X, stage.ZeroStart.Y, stage.ZeroEnd.X, stage.ZeroEnd.Y,
                    colors.Border, role: "funnel-zero", paint: VisualChartPaint.Stroke(colors.Border, SvgColorRole.Surface));
                if (separate != null) separate.Draw(chart, builder, index, "funnel", colors);
                else if (show) FunnelLabels(chart, context, builder, stage, color, colors, raw, retention, drop, form);
            }
        }
        if (series.RenderPoints.All(point => point.Y == 0))
            builder.AddDiagnostic(new VisualDiagnostic("funnel.all-zero", "All funnel stage values are zero; their category slots remain visible."));
    }

    private static void FunnelConnection(Chart chart, VisualSceneBuilder builder, ChartFunnelConnectionLayout connection,
        VisualThemeColors colors, ChartFunnelForm form) {
        var series = chart.Series[0]; var color = Color(series, connection.FromIndex, colors);
        var style = FunnelConnectionStyle(chart, connection.FromIndex);
        var fill = FunnelConnectionFill(chart, connection.FromIndex, color, form, style, StagePaint(series, connection.FromIndex, color, colors));
        var id = Id(connection.FromIndex) + "-connection";
        var summary = Category(chart, connection.FromIndex) + " to " + Category(chart, connection.ToIndex) + ": "
            + FunnelValue(chart, connection.FromIndex) + " to " + FunnelValue(chart, connection.ToIndex);
        using (VisualStateSceneTools.Mark(builder, id, "funnel-connection", connection.Bounds, summary,
            new Dictionary<string, string> {
                ["data-cfx-from-point"] = N(connection.FromIndex), ["data-cfx-to-point"] = N(connection.ToIndex),
                ["data-cfx-from-value"] = N(series.RenderPoints[connection.FromIndex].Y),
                ["data-cfx-to-value"] = N(series.RenderPoints[connection.ToIndex].Y)
            })) {
            if (style != null) StyledFunnelConnection(chart, builder, connection, color, StagePaint(series, connection.FromIndex, color, colors), form, style);
            else {
                builder.Path(connection.Path, fill.Color, role: form == ChartFunnelForm.Cone ? "funnel-connection-area" : "funnel-dropoff", close: true,
                    paint: VisualChartPaint.Fill(fill.Paint));
                if (form == ChartFunnelForm.Cone) Pattern(chart, builder, connection.FromIndex, connection.Path, color);
            }
        }
    }

    private static void FunnelLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartFunnelStageLayout stage,
        ChartColor color, VisualThemeColors colors, double raw, double? retention, double? drop, ChartFunnelForm form) {
        var index = stage.SourceIndex;
        var value = FunnelValue(chart, index);
        var text = chart.Series[0].PreparedLabel(index) ?? Category(chart, index) + ": " + value;
        var inside = raw > 0 && form == ChartFunnelForm.StageBars;
        var textColor = inside ? ChartColorMath.AccessibleTextOnBackground(color) : colors.Foreground;
        var style = Style(chart, context, index, textColor);
        var bounds = stage.LabelBounds;
        var metricsBounds = stage.MetricsBounds;
        var metricsStyle = Style(chart, context, index, colors.MutedForeground);
        var percentages = FunnelPercentageFormat(chart);
        var metrics = (retention.HasValue ? percentages.FormatFraction(retention.Value) + " retained" : "No initial baseline")
            + (index == 0 ? "" : "\n" + (drop.HasValue ? percentages.FormatFraction(drop.Value) + " drop-off" : "No previous baseline"));
        if (form == ChartFunnelForm.Cone && chart.Options.Funnel.Orientation == ChartOrientation.Vertical) {
            ConeLabels(chart, context, builder, stage, text, metrics, style, metricsStyle);
            return;
        }
        var displayed = text;
        var measured = builder.MeasureText(text, style);
        double Fit(ChartRect box) => Math.Min(box.Width / Math.Max(1, measured.Width), box.Height / Math.Max(1, measured.Height));
        // Try complete outside text before shortening a positive bar's caption. The empty side of a narrow stage
        // can share one rail with its retention rows; neither caption needs to compete for a separate narrow box.
        var wrappedInside = false;
        // A caption a little wider than its stage stays inside on two balanced lines ("Completed:" over "41")
        // before it moves out beside the stage, where it would compete with the ratio column.
        if (inside && Fit(bounds) < 1) {
            var wrapped = string.Join("\n", ChartLabelWrapping.BalancedTwoLine(text, style.EffectiveFontSize,
                bounds.Width, (value, _) => builder.MeasureText(value, style).Width));
            var wrappedSize = builder.MeasureText(wrapped, style);
            if (wrapped.IndexOf('\n') >= 0 && wrappedSize.Width <= bounds.Width && wrappedSize.Height <= bounds.Height) {
                displayed = wrapped;
                measured = wrappedSize;
                wrappedInside = true;
            }
        }
        if (inside && Fit(bounds) < 1) {
            if (Fit(stage.OutsideLabelBounds) >= 1) {
                bounds = stage.OutsideLabelBounds;
                inside = false;
            } else if (chart.Options.Funnel.Orientation == ChartOrientation.Vertical && index > 0) {
                var outside = new ChartRect(stage.OutsideLabelBounds.Left, bounds.Top,
                    Math.Max(0, metricsBounds.Right - stage.OutsideLabelBounds.Left), bounds.Height);
                var metricSize = builder.MeasureText(metrics, metricsStyle);
                var separation = Math.Min(context.Theme.Spacing / 4, 3);
                var height = measured.Height + separation + metricSize.Height;
                if (measured.Width <= outside.Width && metricSize.Width <= outside.Width && height <= outside.Height) {
                    var top = outside.Top + (outside.Height - height) / 2;
                    bounds = new ChartRect(outside.Left, top, outside.Width, measured.Height);
                    metricsBounds = new ChartRect(outside.Left, bounds.Bottom + separation, outside.Width, metricSize.Height);
                    inside = false;
                } else {
                    // A shallow stage may not have three rows for the complete caption and its two ratios.
                    // Wrap the caption at words and place it beside the complete ratio block at the requested size.
                    var captionWidth = Math.Max(0, outside.Width - metricSize.Width - separation);
                    var wrapped = string.Join("\n", ChartLabelWrapping.BalancedTwoLine(text, style.EffectiveFontSize,
                        captionWidth, (value, _) => builder.MeasureText(value, style).Width));
                    var wrappedSize = builder.MeasureText(wrapped, style);
                    if (wrappedSize.Width <= captionWidth && Math.Max(wrappedSize.Height, metricSize.Height) <= outside.Height) {
                        bounds = new ChartRect(outside.Left, outside.Top, captionWidth, outside.Height);
                        metricsBounds = new ChartRect(bounds.Right + separation, outside.Top, metricSize.Width, outside.Height);
                        displayed = wrapped;
                        inside = false;
                    }
                }
            }
            if (!inside) style = Style(chart, context, index, colors.Foreground);
        }
        // The described text stays complete; a wrapped caption only changes what is painted.
        StageText(builder, wrappedInside ? text : displayed, bounds, style, "funnel-label", Id(index) + "-label", TextAlignment.Center,
            VisualChartPaint.ExplicitDataLabelColor(chart, index) || !inside ? VisualChartPaint.Text(style)
                : SvgPaint.Contrast(color, VisualChartPaint.SeriesRole(chart.Series[0], index)), "funnel",
            displayedText: wrappedInside ? displayed : null);
        if (index > 0 && metricsBounds.Width > 0 && metricsBounds.Height > 0) {
            // The ratio column is a numeric column: right-aligned so its percentages line up.
            StageText(builder, metrics, metricsBounds, metricsStyle,
                "funnel-ratio", Id(index) + "-ratio", TextAlignment.Right,
                VisualChartPaint.Text(metricsStyle), "funnel");
        }
    }

}
