using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawLayeredRadial(RgbaCanvas c, Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) {
            if (candidate.Kind == ChartSeriesKind.LayeredRadial) {
                series = candidate;
                break;
            }
        }

        if (series == null || series.RadialLayers.Count == 0) return;
        var theme = chart.Options.Theme;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2;
        var outerRadius = Math.Max(24, Math.Min(plot.Width, plot.Height) * 0.42 * chart.Options.RadialBarRadiusScale);

        for (var i = 0; i < series.RadialLayers.Count; i++) {
            var layer = series.RadialLayers[i];
            var ratio = PngLayeredRadialRatio(layer);
            if (ratio <= 0) continue;
            var radius = Math.Max(1, outerRadius * layer.RadiusRatio);
            var stroke = Math.Max(1, outerRadius * layer.StrokeRatio * chart.Options.RadialBarStrokeScale);
            var start = PngDegreesToRadians(layer.StartAngleDegrees);
            var end = start + PngDegreesToRadians(layer.SweepAngleDegrees) * ratio;
            var color = ApplyOpacity(PngLayeredRadialColor(series, theme, layer, i), layer.Opacity);
            if (layer.LineCap == ChartRadialLayerCap.Butt) {
                c.FillRingSlice(cx, cy, radius + stroke / 2, Math.Max(0, radius - stroke / 2), start, end, color);
            } else {
                c.DrawArc(cx, cy, radius, start, end, color, stroke);
            }

            DrawLayeredRadialSeparators(c, chart, layer, cx, cy, radius, stroke, start, end);
        }

        if (series.ShowDataLabels != false) {
            var valueLayer = series.RadialLayers[series.RadialLayers.Count - 1];
            var labelWidth = Math.Max(60, Math.Min(plot.Width - 24, outerRadius * 1.45));
            var valueFontSize = Math.Max(24, theme.TitleFontSize * 1.45);
            var nameFontSize = Math.Max(9, theme.LegendFontSize);
            DrawPngTextEmphasizedCenteredX(c, cx, cy - theme.TitleFontSize * 0.36 - valueFontSize / 2.0, FormatValue(chart, valueLayer.Value), theme.Text, valueFontSize, labelWidth);
            DrawPngTextEmphasizedCenteredX(c, cx, cy + Math.Max(10, theme.LegendFontSize + 12) - nameFontSize + 1, series.Name, theme.MutedText, nameFontSize, labelWidth);
        }
    }

    private static void DrawLayeredRadialSeparators(RgbaCanvas c, Chart chart, ChartRadialLayer layer, double cx, double cy, double radius, double stroke, double start, double end) {
        if (layer.SeparatorCount <= 0) return;
        var separator = layer.SeparatorColor ?? chart.Options.Theme.CardBackground;
        var inset = Math.Min(stroke / 2 - 0.5, Math.Max(0, stroke * layer.SeparatorInsetRatio));
        var inner = Math.Max(0, radius - stroke / 2 + inset);
        var outer = radius + stroke / 2 - inset;
        var sweep = end - start;
        for (var i = 1; i <= layer.SeparatorCount; i++) {
            var angle = start + sweep * i / (layer.SeparatorCount + 1);
            c.DrawLine(cx + Math.Cos(angle) * inner, cy + Math.Sin(angle) * inner, cx + Math.Cos(angle) * outer, cy + Math.Sin(angle) * outer, separator, layer.SeparatorStrokeWidth);
        }
    }

    private static double PngLayeredRadialRatio(ChartRadialLayer layer) => Clamp((layer.Value - layer.Minimum) / (layer.Maximum - layer.Minimum), 0, 1);

    private static ChartColor PngLayeredRadialColor(ChartSeries series, ChartForgeX.Themes.ChartTheme theme, ChartRadialLayer layer, int layerIndex) {
        if (layer.Color.HasValue) return layer.Color.Value;
        if (layerIndex < series.PointColors.Count && series.PointColors[layerIndex].HasValue) return series.PointColors[layerIndex]!.Value;
        return series.Color ?? theme.Palette[layerIndex % theme.Palette.Length];
    }

    private static double PngDegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static bool IsLayeredRadialChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.LayeredRadial);
}
