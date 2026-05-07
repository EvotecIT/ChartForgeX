using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawSunburst(StringBuilder sb, Chart chart, ChartRect plot) {
        var model = ChartSunburstLayout.Compute(chart, plot);
        if (model.Nodes.Count == 0 || model.Root < 0) return;
        var series = chart.Series.First(item => item.Kind == ChartSeriesKind.Sunburst);
        var showLabels = series.ShowDataLabels != false;
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "sunburst-chart")
            .EndStartElement()
            .Line();
        foreach (var node in model.Nodes.OrderByDescending(node => node.Depth)) DrawSunburstSegment(writer, chart, model, node, showLabels);
        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawSunburstSegment(SvgMarkupWriter writer, Chart chart, ChartSunburstModel model, ChartSunburstNode node, bool showLabels) {
        var t = chart.Options.Theme;
        var color = SunburstNodeColor(chart, node);
        var sweep = node.EndAngle - node.StartAngle;
        if (sweep <= 0 || node.OuterRadius <= node.InnerRadius) return;
        var percent = node.Index == model.Root ? 1 : node.Value / Math.Max(0.000001, model.Nodes[model.Root].Value);
        var summary = node.Label + ": " + FormatValue(chart, node.Value) + ", " + FormatPercent(percent);
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "sunburst-segment")
            .Attribute("data-cfx-node", node.Index)
            .Attribute("data-cfx-parent", node.Parent)
            .Attribute("data-cfx-depth", node.Depth)
            .Attribute("data-cfx-label", node.Label)
            .Attribute("data-cfx-value", node.Value)
            .Attribute("data-cfx-percent", percent)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("d", BuildSlicePath(model.CenterX, model.CenterY, node.OuterRadius, node.InnerRadius, node.StartAngle, node.EndAngle))
            .Attribute("fill", color.ToCss())
            .Attribute("stroke", t.CardBackground.ToCss())
            .Attribute("stroke-width", ChartVisualPrimitives.SliceSeparatorStrokeWidth)
            .Attribute("fill-opacity", 0.96)
            .EndEmptyElement()
            .Line();
        if (!showLabels) return;
        var ringWidth = node.OuterRadius - node.InnerRadius;
        if (ringWidth < 18) return;
        var labelSpace = SunburstSvgLabelSpace(node, sweep, ringWidth);
        if (labelSpace <= 0) return;
        var preferredFontSize = Math.Min(t.TickLabelFontSize, ringWidth * 0.42);
        var fontSize = TextFontSizeForSvgWidth(node.Label, labelSpace, preferredFontSize);
        if (node.Depth > 0 && fontSize < preferredFontSize * 0.92) return;
        var label = node.Depth == 0 ? node.Label : TrimSvgLabelToWidth(node.Label, fontSize, labelSpace);
        if (node.Depth > 0 && label.EndsWith("...", StringComparison.Ordinal)) return;
        if (label.Length == 0) return;
        var angle = node.StartAngle + sweep / 2;
        var radius = node.Depth == 0 ? 0 : node.InnerRadius + ringWidth * 0.64;
        var x = model.CenterX + Math.Cos(angle) * radius;
        var y = model.CenterY + Math.Sin(angle) * radius + fontSize / 3.0;
        var labelColor = HeatmapTextColor(color);
        var halo = TreeLabelHalo(labelColor);
        DrawSvgTextCenteredX(writer, chart, "sunburst-label", label, x, y, labelColor, fontSize, labelSpace, "800", halo, 3);
    }

    private static double SunburstSvgLabelSpace(ChartSunburstNode node, double sweep, double ringWidth) {
        if (node.Depth == 0) return Math.Max(28, ringWidth * 1.58);
        var midRadius = node.InnerRadius + ringWidth * 0.64;
        var arcLength = sweep * midRadius;
        if (sweep < 0.30 || arcLength < 58) return 0;
        if (node.Depth == 1 && (sweep < 0.78 || arcLength < 92)) return 0;
        return Math.Max(36, Math.Min(arcLength * 0.44, ringWidth * 1.48));
    }

    private static ChartColor SunburstNodeColor(Chart chart, ChartSunburstNode node) {
        var t = chart.Options.Theme;
        if (node.Depth == 0) return Blend(t.PlotBackground, t.Palette[0], 0.28);
        return t.Palette[(node.Index + node.Depth - 1) % t.Palette.Length];
    }
}
