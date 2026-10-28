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
            var summary = Category(chart, index) + ": " + VisualStateSceneTools.Value(chart, series, index, raw);
            if (retention.HasValue) summary += ", retained " + retention.Value.ToString("0.#%", CultureInfo.InvariantCulture);
            if (drop.HasValue) summary += ", drop-off " + drop.Value.ToString("0.#%", CultureInfo.InvariantCulture);
            using (Point(chart, builder, index, "funnel-stage", stage.Bounds, summary, metadata)) {
                if (form == ChartFunnelForm.Cone) {
                    var stroke = series.HasExplicitStrokeWidth ? series.StrokeWidth : context.Theme.SeriesStrokeWidth;
                    var line = new ChartPath(new[] { ChartPathCommand.MoveTo(stage.LineStart.X, stage.LineStart.Y),
                        ChartPathCommand.LineTo(stage.LineEnd.X, stage.LineEnd.Y) });
                    var outline = StageOutline(series, index, color, chart.Options.Funnel.StageOutline, stroke);
                    if (outline.Color.HasValue) builder.Path(line, stroke: outline.Color, strokeWidth: outline.Width, role: "funnel-stage-line",
                        cap: VisualStrokeCap.Butt, dash: outline.Dash, dashOffset: chart.Options.Funnel.StageOutlineDashOffset,
                        paint: new VisualScenePaintBinding(stroke: outline.Paint));
                    else builder.Path(line, stroke: color, strokeWidth: stroke, role: "funnel-stage-line", cap: VisualStrokeCap.Butt,
                        paint: VisualChartPaint.Stroke(VisualChartPaint.Series(series, color, index)));
                } else if (raw > 0) {
                    if (chart.Options.Funnel.StageOutline != null || index < series.PointStrokeColors.Count && series.PointStrokeColors[index].HasValue)
                        StagePartition(chart, builder, stage.Bar, index, color, "funnel-segment", chart.Options.Funnel.StageOutline, chart.Options.Funnel.StageOutlineDashOffset);
                    else {
                        builder.Path(stage.Bar, color, role: "funnel-segment", close: true,
                            paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, index)));
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
        var fill = FunnelConnectionFill(chart, connection.FromIndex, color, form, style);
        var id = Id(connection.FromIndex) + "-connection";
        var summary = Category(chart, connection.FromIndex) + " to " + Category(chart, connection.ToIndex) + ": "
            + VisualStateSceneTools.Value(chart, series, connection.FromIndex, series.RenderPoints[connection.FromIndex].Y)
            + " to " + VisualStateSceneTools.Value(chart, series, connection.ToIndex, series.RenderPoints[connection.ToIndex].Y);
        using (VisualStateSceneTools.Mark(builder, id, "funnel-connection", connection.Bounds, summary,
            new Dictionary<string, string> {
                ["data-cfx-from-point"] = N(connection.FromIndex), ["data-cfx-to-point"] = N(connection.ToIndex),
                ["data-cfx-from-value"] = N(series.RenderPoints[connection.FromIndex].Y),
                ["data-cfx-to-value"] = N(series.RenderPoints[connection.ToIndex].Y)
            })) {
            if (style != null) StyledFunnelConnection(chart, builder, connection, color, form, style);
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
        var value = VisualStateSceneTools.Value(chart, chart.Series[0], index, raw);
        var text = Category(chart, index) + ": " + value;
        var inside = raw > 0 && form == ChartFunnelForm.StageBars;
        var textColor = inside ? ChartColorMath.AccessibleTextOnBackground(color) : colors.Foreground;
        var style = Style(chart, context, index, textColor);
        var bounds = stage.LabelBounds;
        var metricsBounds = stage.MetricsBounds;
        var metricsStyle = Style(chart, context, index, colors.MutedForeground);
        var metrics = (retention.HasValue ? retention.Value.ToString("0.#%", CultureInfo.InvariantCulture) + " retained" : "No initial baseline")
            + "\n" + (drop.HasValue ? drop.Value.ToString("0.#%", CultureInfo.InvariantCulture) + " drop-off" : "No previous baseline");
        var displayed = text;
        var measured = builder.MeasureText(text, style);
        double Fit(ChartRect box) => Math.Min(box.Width / Math.Max(1, measured.Width), box.Height / Math.Max(1, measured.Height));
        // Try complete outside text before shortening a positive bar's caption. The empty side of a narrow stage
        // can share one rail with its retention rows; neither caption needs to compete for a separate narrow box.
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
        var railCaption = false;
        if (form == ChartFunnelForm.Cone && chart.Options.Funnel.Orientation == ChartOrientation.Vertical && metricsBounds.Width > 0) {
            var readable = style.Clone(); readable.FontSize = Math.Min(10, style.EffectiveFontSize);
            var lineHeight = builder.MeasureText(text, readable).Height;
            // A dense stage gives a useful category and raw value first use of its rail.
            // Ratio descriptors remain available when that space cannot paint both.
            if (lineHeight > bounds.Height && lineHeight <= metricsBounds.Height) {
                var suffix = ": " + value;
                var category = ChartTextFitting.FitEnd(Category(chart, index),
                    prefix => builder.MeasureText(prefix + suffix, readable).Width <= metricsBounds.Width, "…");
                if (category.Length > 0 && category != "…") {
                    displayed = category + suffix; bounds = metricsBounds; style = readable; railCaption = true;
                    if (bounds.Left > stage.LineEnd.X + context.Theme.Spacing)
                        VisualStateSceneTools.Connector(builder, chart.Options, stage.LineEnd,
                            new ChartPoint(bounds.Left - context.Theme.Spacing / 4, stage.LineEnd.Y), colors.MutedForeground);
                }
            }
        }
        StageText(builder, railCaption ? text : displayed, bounds, style, "funnel-label", Id(index) + "-label",
            railCaption ? TextAlignment.Left : TextAlignment.Center,
            VisualChartPaint.ExplicitDataLabelColor(chart, index) || !inside ? VisualChartPaint.Text(style)
                : SvgPaint.Contrast(color, VisualChartPaint.SeriesRole(chart.Series[0], index)), "funnel",
            displayedText: railCaption ? displayed : null);
        if (index > 0 && metricsBounds.Width > 0 && metricsBounds.Height > 0) {
            var ratioBounds = railCaption
                ? new ChartRect(metricsBounds.Left, metricsBounds.Top, metricsBounds.Width, 0)
                : metricsBounds;
            StageText(builder, metrics, ratioBounds, metricsStyle,
                "funnel-ratio", Id(index) + "-ratio", TextAlignment.Left,
                VisualChartPaint.Text(metricsStyle), "funnel");
        }
    }

}
