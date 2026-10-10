using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    private static void Pyramid(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot) {
        var series = chart.Series[0]; var options = chart.Options.Pyramid; var colors = context.Theme.Resolve(context.ThemeMode);
        var show = series.ShowDataLabels ?? chart.Options.ShowDataLabels;
        var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
        var rail = 0d;
        var sideRail = placement is ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right
            || placement is not (ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below) && options.Orientation == ChartOrientation.Vertical;
        if (show) for (var index = 0; index < series.Points.Count; index++) {
            var measured = builder.MeasureText(PyramidLabel(chart, index), Style(chart, context, index, colors.Foreground));
            rail = Math.Max(rail, (sideRail ? measured.Width : measured.Height) + context.Theme.Spacing);
        }
        if (show && placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Inside or ChartDataLabelPlacement.Center) {
            var candidate = ChartPyramidLayout.Compute(series.Points, plot, options, 0, placement, context.Theme.Spacing);
            var needsRail = false;
            foreach (var stage in candidate.Stages) {
                var measured = builder.MeasureText(PyramidLabel(chart, stage.SourceIndex), Style(chart, context, stage.SourceIndex, colors.Foreground));
                if (stage.InsideLabel.Width < measured.Width || stage.InsideLabel.Height < measured.Height) { needsRail = true; break; }
            }
            if (!needsRail) rail = 0;
        }
        var layout = ChartPyramidLayout.Compute(series.Points, plot, options, rail, placement, context.Theme.Spacing);
        var encoding = options.ValueEncoding == ChartPyramidValueEncoding.Height ? "height" : "area";
        var collapsed = false;
        using var group = builder.PushGroup("pyramid-chart", "pyramid-chart", new Dictionary<string, string> {
            ["data-cfx-value-encoding"] = encoding, ["data-cfx-total"] = N(layout.Total),
            ["data-cfx-orientation"] = options.Orientation == ChartOrientation.Vertical ? "vertical" : "horizontal",
            ["data-cfx-reversed"] = options.Reversed ? "true" : "false",
            ["data-cfx-aspect-ratio"] = options.AspectRatio.HasValue ? N(options.AspectRatio.Value) : "auto"
        });
        foreach (var stage in layout.Stages) {
            var index = stage.SourceIndex; var color = Color(series, index, colors);
            var belowPrecision = series.Points[index].Y > 0 && !stage.HasGeometry;
            collapsed |= belowPrecision;
            var metadata = new Dictionary<string, string> {
                ["data-cfx-zero"] = series.Points[index].Y == 0 ? "true" : "false", ["data-cfx-value-encoding"] = encoding,
                ["data-cfx-geometry-collapsed"] = belowPrecision ? "true" : "false",
                ["data-cfx-value-fraction-defined"] = layout.Total > 0 ? "true" : "false",
                ["data-cfx-value-fraction"] = N(stage.ValueFraction), ["data-cfx-length-fraction"] = N(stage.LengthFraction),
                ["data-cfx-area-fraction"] = N(stage.AreaFraction), ["data-cfx-normalized-start"] = N(stage.Start),
                ["data-cfx-normalized-end"] = N(stage.End)
            };
            var summary = PyramidLabel(chart, index) + ", " + encoding + " encoding, "
                + (layout.Total > 0 ? stage.ValueFraction.ToString("0.#%", CultureInfo.InvariantCulture) + " of total" : "no positive total");
            using (Point(chart, builder, index, "pyramid-stage", stage.Bounds, summary, metadata)) {
                if (stage.HasGeometry) {
                    builder.Path(stage.Segment, color, role: "pyramid-segment", close: true,
                        paint: VisualChartPaint.Fill(VisualChartPaint.Series(series, color, index)));
                    Pattern(chart, builder, index, stage.Segment, color);
                }
                if (show) PyramidLabel(chart, context, builder, stage, layout.LabelSide, placement, color, colors);
            }
        }
        if (collapsed) builder.AddDiagnostic(new VisualDiagnostic("pyramid.precision", "A positive pyramid partition is below floating-point coordinate precision; its complete source value remains represented without an invented minimum size."));
        if (layout.Total == 0) {
            builder.AddDiagnostic(new VisualDiagnostic("pyramid.all-zero", "All pyramid values are zero; source categories remain represented without positive partitions."));
            if (!show) VisualStateSceneTools.Text(builder, chart.Options.Labels.NoData, plot,
                VisualStateSceneTools.TickStyle(chart, context), "pyramid-no-data", "pyramid-no-data", TextAlignment.Center);
        }
    }

    private static string PyramidLabel(Chart chart, int index) => Category(chart, index) + ": "
        + VisualStateSceneTools.Value(chart, chart.Series[0], index, chart.Series[0].Points[index].Y);

    private static void PyramidLabel(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartPyramidStageLayout stage,
        ChartDataLabelPlacement side, ChartDataLabelPlacement placement, ChartColor color, VisualThemeColors colors) {
        var index = stage.SourceIndex; var text = PyramidLabel(chart, index);
        var insideStyle = Style(chart, context, index, ChartColorMath.AccessibleTextOnBackground(color));
        var measured = builder.MeasureText(text, insideStyle);
        var inside = placement is ChartDataLabelPlacement.Auto or ChartDataLabelPlacement.Inside or ChartDataLabelPlacement.Center
            && stage.InsideLabel.Width >= measured.Width && stage.InsideLabel.Height >= measured.Height;
        var bounds = inside ? stage.InsideLabel : stage.OutsideLabel;
        var style = inside ? insideStyle : Style(chart, context, index, colors.Foreground);
        if (!inside && stage.HasGeometry) {
            var end = side switch {
                ChartDataLabelPlacement.Left => new ChartPoint(bounds.Right, bounds.Top + bounds.Height / 2),
                ChartDataLabelPlacement.Right => new ChartPoint(bounds.Left, bounds.Top + bounds.Height / 2),
                ChartDataLabelPlacement.Above => new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Bottom),
                _ => new ChartPoint(bounds.Left + bounds.Width / 2, bounds.Top)
            };
            VisualStateSceneTools.Connector(builder, chart.Options, stage.LabelAnchor, end, colors.MutedForeground);
        }
        StageText(builder, text, bounds, style, "pyramid-label", Id(index) + "-label", TextAlignment.Center,
            VisualChartPaint.ExplicitDataLabelColor(chart, index) || !inside ? VisualChartPaint.Text(style)
                : SvgPaint.Contrast(color, VisualChartPaint.SeriesRole(chart.Series[0], index)), "pyramid");
    }
}
