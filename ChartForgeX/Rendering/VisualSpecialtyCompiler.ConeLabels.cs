using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSpecialtyCompiler {
    /// <summary>Anchors combined cone captions to the boundary they describe, in both painters.</summary>
    private static void ConeLabels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartFunnelStageLayout stage, string text, string metrics, TextStyle style, TextStyle metricsStyle) {
        var gap = Math.Max(4, context.Theme.Spacing / 2);
        var caption = builder.MeasureText(text, style);
        var ratio = builder.MeasureText(metrics, metricsStyle);
        var boundary = stage.LineEnd.Y;
        var center = (stage.LineStart.X + stage.LineEnd.X) / 2;
        var plotLeft = stage.SlotBounds.Left;
        var availableLeft = stage.LineStart.X - plotLeft - gap * 2;
        var displayed = text;
        ChartRect bounds;
        if (stage.Bounds.Height < caption.Height + gap + ratio.Height / 2) {
            // A dense process slot cannot stack a caption above a two-line ratio block.
            // Keep the complete value on the cone side of the boundary's ratio rail.
            var width = Math.Max(0, stage.LineEnd.X - gap - plotLeft);
            var suffix = ": " + FunnelValue(chart, stage.SourceIndex);
            var category = ChartTextFitting.FitEnd(Category(chart, stage.SourceIndex),
                prefix => builder.MeasureText(prefix + suffix, style).Width <= width, "…");
            displayed = category + suffix;
            var height = Math.Min(caption.Height, stage.Bounds.Height);
            bounds = new ChartRect(plotLeft, boundary - height / 2, width, height);
        } else if (caption.Width > stage.Extent && caption.Width <= availableLeft) {
            bounds = new ChartRect(stage.LineStart.X - gap * 2 - caption.Width,
                boundary - caption.Height / 2, caption.Width, caption.Height);
            VisualStateSceneTools.Connector(builder, chart.Options, stage.LineStart,
                new ChartPoint(bounds.Right + gap / 2, boundary), metricsStyle.Color, ChartDataLabelConnectorStyle.Straight);
        } else {
            var width = Math.Max(stage.LabelBounds.Width, caption.Width);
            width = Math.Min(width, Math.Max(0, stage.MetricsBounds.Right - plotLeft));
            bounds = new ChartRect(Math.Max(plotLeft, center - width / 2),
                boundary - gap - caption.Height, width, caption.Height);
        }
        StageText(builder, text, bounds, style, "funnel-label", Id(stage.SourceIndex) + "-label",
            TextAlignment.Center, VisualChartPaint.Text(style), "funnel", displayedText: displayed);
        if (stage.SourceIndex == 0) return;
        var x = stage.LineEnd.X + gap;
        var ratioHeight = Math.Min(ratio.Height, stage.Bounds.Height);
        var ratioBounds = new ChartRect(x, boundary - ratioHeight / 2,
            Math.Max(0, stage.MetricsBounds.Right - x), ratioHeight);
        VisualStateSceneTools.Connector(builder, chart.Options, stage.LineEnd,
            new ChartPoint(x - gap / 2, boundary), metricsStyle.Color, ChartDataLabelConnectorStyle.Straight);
        StageText(builder, metrics, ratioBounds, metricsStyle, "funnel-ratio", Id(stage.SourceIndex) + "-ratio",
            TextAlignment.Left, VisualChartPaint.Text(metricsStyle), "funnel");
    }
}
