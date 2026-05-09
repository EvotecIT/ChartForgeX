using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawLayeredRadial(StringBuilder sb, Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.LayeredRadial);
        if (series == null || series.RadialLayers.Count == 0) return;

        var t = chart.Options.Theme;
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2;
        var outerRadius = Math.Max(24, Math.Min(plot.Width, plot.Height) * 0.42 * chart.Options.RadialBarRadiusScale);
        var w = new SvgMarkupWriter(4096);
        w.StartElement("g")
            .Attribute("data-cfx-role", "layered-radial-chart")
            .Attribute("data-cfx-label", series.Name)
            .Attribute("data-cfx-radius-scale", chart.Options.RadialBarRadiusScale)
            .EndStartElement().Line();

        for (var i = 0; i < series.RadialLayers.Count; i++) {
            var layer = series.RadialLayers[i];
            var ratio = LayeredRadialRatio(layer);
            if (ratio <= 0) continue;
            var radius = Math.Max(1, outerRadius * layer.RadiusRatio);
            var stroke = Math.Max(1, outerRadius * layer.StrokeRatio * chart.Options.RadialBarStrokeScale);
            var start = DegreesToRadians(layer.StartAngleDegrees);
            var end = start + DegreesToRadians(layer.SweepAngleDegrees) * ratio;
            var color = LayeredRadialColor(series, t, layer, i);
            var summary = layer.Name + ": " + FormatValue(chart, layer.Value);
            var cap = layer.LineCap == ChartRadialLayerCap.Butt ? "butt" : "round";

            w.StartElement("path")
                .Attribute("data-cfx-role", "layered-radial-layer")
                .Attribute("data-cfx-layer", i)
                .Attribute("data-cfx-label", layer.Name)
                .Attribute("data-cfx-value", layer.Value)
                .Attribute("data-cfx-min", layer.Minimum)
                .Attribute("data-cfx-max", layer.Maximum)
                .Attribute("data-cfx-percent", ratio)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("d", BuildRadialBarArc(cx, cy, radius, start, end))
                .Attribute("fill", "none")
                .Attribute("stroke", color.ToCss())
                .Attribute("stroke-width", stroke)
                .Attribute("stroke-linecap", cap);
            if (layer.Opacity < 1) w.Attribute("opacity", layer.Opacity);
            w.EndEmptyElement().Line();

            WriteLayeredRadialSeparators(w, chart, layer, i, cx, cy, radius, stroke, start, end);
        }

        if (series.ShowDataLabels != false) {
            var valueLayer = series.RadialLayers.Last();
            var labelWidth = Math.Max(60, Math.Min(plot.Width - 24, outerRadius * 1.45));
            DrawSvgTextCenteredX(w, chart, "layered-radial-value", FormatValue(chart, valueLayer.Value), cx, cy - t.TitleFontSize * 0.36, t.Text, Math.Max(24, t.TitleFontSize * 1.45), labelWidth, "850", t.CardBackground, 3.2);
            DrawSvgTextCenteredX(w, chart, "layered-radial-title", series.Name, cx, cy + Math.Max(10, t.LegendFontSize + 12), t.MutedText, Math.Max(9, t.LegendFontSize), labelWidth, "700", t.CardBackground, 2.4, middleBaseline: false);
        }

        w.EndElement().Line();
        sb.Append(w.Build());
    }

    private static void WriteLayeredRadialSeparators(SvgMarkupWriter w, Chart chart, ChartRadialLayer layer, int layerIndex, double cx, double cy, double radius, double stroke, double start, double end) {
        if (layer.SeparatorCount <= 0) return;
        var separator = layer.SeparatorColor ?? chart.Options.Theme.CardBackground;
        var inset = Math.Min(stroke / 2 - 0.5, Math.Max(0, stroke * layer.SeparatorInsetRatio));
        var inner = Math.Max(0, radius - stroke / 2 + inset);
        var outer = radius + stroke / 2 - inset;
        var sweep = end - start;
        for (var i = 1; i <= layer.SeparatorCount; i++) {
            var angle = start + sweep * i / (layer.SeparatorCount + 1);
            w.StartElement("line")
                .Attribute("data-cfx-role", "layered-radial-separator")
                .Attribute("data-cfx-layer", layerIndex)
                .Attribute("x1", cx + Math.Cos(angle) * inner)
                .Attribute("y1", cy + Math.Sin(angle) * inner)
                .Attribute("x2", cx + Math.Cos(angle) * outer)
                .Attribute("y2", cy + Math.Sin(angle) * outer)
                .Attribute("stroke", separator.ToCss())
                .Attribute("stroke-width", layer.SeparatorStrokeWidth)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement().Line();
        }
    }

    private static double LayeredRadialRatio(ChartRadialLayer layer) => Clamp((layer.Value - layer.Minimum) / (layer.Maximum - layer.Minimum), 0, 1);

    private static ChartColor LayeredRadialColor(ChartSeries series, ChartForgeX.Themes.ChartTheme theme, ChartRadialLayer layer, int layerIndex) {
        if (layer.Color.HasValue) return layer.Color.Value;
        if (layerIndex < series.PointColors.Count && series.PointColors[layerIndex].HasValue) return series.PointColors[layerIndex]!.Value;
        return series.Color ?? theme.Palette[layerIndex % theme.Palette.Length];
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static bool IsLayeredRadialChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.LayeredRadial);
}
