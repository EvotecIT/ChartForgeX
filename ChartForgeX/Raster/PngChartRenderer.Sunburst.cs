using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawSunburst(RgbaCanvas c, Chart chart, ChartRect plot) {
        var model = ChartSunburstLayout.Compute(chart, plot);
        if (model.Nodes.Count == 0 || model.Root < 0) return;
        var series = chart.Series.First(item => item.Kind == ChartSeriesKind.Sunburst);
        var showLabels = series.ShowDataLabels != false;
        foreach (var node in model.Nodes.OrderByDescending(node => node.Depth)) DrawSunburstSegment(c, chart, model, node, showLabels);
    }

    private static bool IsSunburstChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Sunburst);

    private static void DrawSunburstSegment(RgbaCanvas c, Chart chart, ChartSunburstModel model, ChartSunburstNode node, bool showLabels) {
        var color = PngSunburstNodeColor(chart, node);
        var sweep = node.EndAngle - node.StartAngle;
        if (sweep <= 0 || node.OuterRadius <= node.InnerRadius) return;
        c.FillRingSlice(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, node.EndAngle, color);
        DrawSunburstSeparator(c, chart, model, node);
        if (!showLabels) return;
        var ringWidth = node.OuterRadius - node.InnerRadius;
        if (ringWidth < 18) return;
        var fontSize = Math.Min(chart.Options.Theme.TickLabelFontSize, ringWidth * 0.42);
        var maxWidth = SunburstPngLabelSpace(node, sweep, ringWidth);
        if (maxWidth <= 0) return;
        var labelFontSize = TextFontSizeForEmphasizedWidth(node.Label, maxWidth, fontSize);
        var label = node.Depth == 0 ? node.Label : TrimReadablePngLabelToWidth(node.Label, labelFontSize, maxWidth);
        if (node.Depth > 0 && label.EndsWith("...", StringComparison.Ordinal)) return;
        if (label.Length == 0) return;
        var angle = node.StartAngle + sweep / 2;
        var radius = node.Depth == 0 ? 0 : node.InnerRadius + ringWidth * 0.64;
        var x = model.CenterX + Math.Cos(angle) * radius - EstimatePngEmphasizedTextWidth(label, labelFontSize) / 2.0;
        var y = model.CenterY + Math.Sin(angle) * radius - labelFontSize / 2.0;
        var labelColor = ChartColorMath.TextOnBackground(color);
        DrawReadablePngLabel(c, x, y, label, labelColor, ChartColorMath.TextOnBackground(labelColor, 0.70), labelFontSize);
    }

    private static double SunburstPngLabelSpace(ChartSunburstNode node, double sweep, double ringWidth) {
        if (node.Depth == 0) return Math.Max(28, ringWidth * 1.58);
        var midRadius = node.InnerRadius + ringWidth * 0.64;
        var arcLength = sweep * midRadius;
        if (sweep < 0.30 || arcLength < 58) return 0;
        if (node.Depth == 1 && (sweep < 0.78 || arcLength < 92)) return 0;
        return Math.Max(36, Math.Min(arcLength * 0.44, ringWidth * 1.48));
    }

    private static void DrawSunburstSeparator(RgbaCanvas c, Chart chart, ChartSunburstModel model, ChartSunburstNode node) {
        var separator = chart.Options.Theme.CardBackground;
        c.DrawArc(model.CenterX, model.CenterY, node.OuterRadius, node.StartAngle, node.EndAngle, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);
        if (node.InnerRadius > 0) c.DrawArc(model.CenterX, model.CenterY, node.InnerRadius, node.StartAngle, node.EndAngle, separator, ChartVisualPrimitives.SliceSeparatorStrokeWidth);
        if (node.EndAngle - node.StartAngle < Math.PI * 2 - 0.000001) {
            DrawSliceSeparator(c, model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, separator);
            DrawSliceSeparator(c, model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.EndAngle, separator);
        }
    }

    private static ChartColor PngSunburstNodeColor(Chart chart, ChartSunburstNode node) {
        var t = chart.Options.Theme;
        if (node.Depth == 0) return ChartColorMath.Blend(t.PlotBackground, t.Palette[0], 0.28);
        return t.Palette[(node.Index + node.Depth - 1) % t.Palette.Length];
    }
}
