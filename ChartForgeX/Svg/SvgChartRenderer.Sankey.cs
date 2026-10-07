using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using SankeyModel = ChartForgeX.Rendering.ChartSankeyModel;
using SankeyLink = ChartForgeX.Rendering.ChartSankeyLayoutLink;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawSankey(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        if (chart.Options.Theme.UseGraphiteLayout) { DrawGraphiteSankey(sb, chart, plot); return; }
        var model = BuildSankeyModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var series = chart.Series.First(item => item.Kind == ChartSeriesKind.Sankey);
        var showDataLabels = series.ShowDataLabels ?? chart.Options.ShowDataLabels;
        var dataStyle = DataLabelStyle(chart, series);
        var t = chart.Options.Theme;
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "sankey-chart")
            .EndStartElement()
            .Line();
        DrawSankeyNodeGradients(writer, chart, id);
        foreach (var link in model.Links) DrawSankeyLink(writer, chart, model, link);
        foreach (var node in model.Nodes) {
            var labelMaxWidth = Math.Max(64, plot.Width / Math.Max(2, model.MaxLayer + 1) * 0.62);
            var anchor = node.Layer == model.MaxLayer ? "end" : "start";
            var labelX = node.Layer == model.MaxLayer ? node.X - 10 : node.X + model.NodeWidth + 10;
            var styledLabel = StyleText(dataStyle, node.Label);
            var labelFontSize = TextFontSizeForSvgWidth(styledLabel, labelMaxWidth, StyleFontSize(dataStyle, t.TickLabelFontSize));
            var label = TrimSvgLabelToWidth(styledLabel, labelFontSize, labelMaxWidth);
            var summary = node.Label + ": " + FormatValue(chart, node.Value);
            var borderStroke = ChartVisualPrimitives.SankeyNodeBorderStrokeWidth;
            var borderInset = borderStroke / 2.0;
            var radius = Math.Min(ChartVisualPrimitives.SankeyNodeCornerRadiusMax, model.NodeWidth / 2);
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "sankey-node")
                .Attribute("data-cfx-node", node.Index)
                .Attribute("data-cfx-layer", node.Layer)
                .Attribute("data-cfx-label", node.Label)
                .Attribute("data-cfx-value", node.Value)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("x", node.X)
                .Attribute("y", node.Y)
                .Attribute("width", model.NodeWidth)
                .Attribute("height", node.Height)
                .Attribute("rx", radius)
                .Attribute("fill", $"url(#{id}-sankeyFill{node.Index % t.Palette.Length})")
                .EndEmptyElement()
                .Line();
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "sankey-node-border")
                .Attribute("x", node.X + borderInset)
                .Attribute("y", node.Y + borderInset)
                .Attribute("width", Math.Max(0, model.NodeWidth - borderStroke))
                .Attribute("height", Math.Max(0, node.Height - borderStroke))
                .Attribute("rx", Math.Max(0, radius - borderInset))
                .Attribute("fill", "none")
                .Attribute("stroke", t.CardBackground.ToCss())
                .Attribute("stroke-opacity", ChartVisualPrimitives.SankeyNodeBorderOpacity)
                .Attribute("stroke-width", borderStroke)
                .EndEmptyElement()
                .Line();
            if (showDataLabels && label.Length > 0) {
                var labelY = node.Y + node.Height / 2;
                var labelWidth = EstimateTextWidth(label, labelFontSize);
                var padX = ChartVisualPrimitives.SankeyLabelBackdropPaddingX;
                var padY = ChartVisualPrimitives.SankeyLabelBackdropPaddingY;
                var backdropX = anchor == "end" ? labelX - labelWidth - padX : labelX - padX;
                var backdropY = labelY - labelFontSize / 2 - padY;
                writer
                    .StartElement("rect")
                    .Attribute("data-cfx-role", "sankey-node-label-backdrop")
                    .Attribute("data-cfx-node", node.Index)
                    .Attribute("x", backdropX)
                    .Attribute("y", backdropY)
                    .Attribute("width", labelWidth + padX * 2)
                    .Attribute("height", labelFontSize + padY * 2)
                    .Attribute("rx", Math.Min(6, (labelFontSize + padY * 2) / 2))
                    .Attribute("fill", t.CardBackground.ToCss())
                    .Attribute("fill-opacity", ChartVisualPrimitives.SankeyLabelBackdropOpacity)
                    .Attribute("stroke", t.PlotBorder.ToCss())
                    .Attribute("stroke-opacity", ChartVisualPrimitives.SankeyLabelBackdropBorderOpacity)
                    .EndEmptyElement()
                    .Line();
                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "sankey-node-label")
                    .Attribute("data-cfx-node", node.Index)
                    .Attribute("x", labelX)
                    .Attribute("y", labelY)
                    .Attribute("text-anchor", anchor)
                    .Attribute("dominant-baseline", "middle")
                    .Attribute("fill", StyleColor(dataStyle, t.MutedText).ToCss())
                    .Attribute("stroke", t.CardBackground.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.SankeyLabelStrokeWidth)
                    .Attribute("paint-order", "stroke fill")
                    .Attribute("stroke-linejoin", "round")
                    .Attribute("font-family", SvgFontFamilyAttributeValue(StyleFontFamily(chart, dataStyle)))
                    .Attribute("font-size", labelFontSize)
                    .Attribute("font-weight", StyleWeight(dataStyle, "700"));
                WriteSvgTextStyleAttributes(writer, dataStyle);
                WriteSvgStyledTextContent(writer, dataStyle, label)
                    .EndElement()
                    .Line();
            }
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawSankeyNodeGradients(SvgMarkupWriter writer, Chart chart, string id) {
        writer.StartElement("defs").EndStartElement().Line();
        for (var i = 0; i < chart.Options.Theme.Palette.Length; i++) {
            var color = chart.Options.Theme.Palette[i];
            writer
                .StartElement("linearGradient")
                .Attribute("id", $"{id}-sankeyFill{i}")
                .Attribute("x1", 0)
                .Attribute("x2", 0)
                .Attribute("y1", 0)
                .Attribute("y2", 1)
                .EndStartElement()
                .StartElement("stop")
                .Attribute("offset", "0%")
                .Attribute("stop-color", SankeyNodeGradientTop(color).ToHex())
                .EndEmptyElement()
                .StartElement("stop")
                .Attribute("offset", "100%")
                .Attribute("stop-color", SankeyNodeGradientBottom(color).ToHex())
                .EndEmptyElement()
                .EndElement()
                .Line();
        }
        writer.EndElement().Line();
    }

    private static void DrawSankeyLink(SvgMarkupWriter writer, Chart chart, SankeyModel model, SankeyLink link) {
        var source = model.Nodes[link.Source];
        var target = model.Nodes[link.Target];
        var color = chart.Options.Theme.UseGraphiteLayout ? SankeyColour(chart, source.Index) : chart.Options.Theme.Palette[source.Index % chart.Options.Theme.Palette.Length];
        var x0 = source.X + model.NodeWidth;
        var x1 = target.X;
        var midX = x0 + (x1 - x0) * 0.55;
        var half = link.Width / 2;
        var path = "M " + F(x0) + " " + F(link.SourceY - half) +
            " C " + F(midX) + " " + F(link.SourceY - half) + " " + F(midX) + " " + F(link.TargetY - half) + " " + F(x1) + " " + F(link.TargetY - half) +
            " L " + F(x1) + " " + F(link.TargetY + half) +
            " C " + F(midX) + " " + F(link.TargetY + half) + " " + F(midX) + " " + F(link.SourceY + half) + " " + F(x0) + " " + F(link.SourceY + half) + " Z";
        var summary = source.Label + " to " + target.Label + ": " + FormatValue(chart, link.Value);
        writer
            .StartElement("path")
            .Attribute("data-cfx-role", "sankey-link")
            .Attribute("data-cfx-source", link.Source)
            .Attribute("data-cfx-target", link.Target)
            .Attribute("data-cfx-value", link.Value)
            .Attribute("data-cfx-source-label", source.Label)
            .Attribute("data-cfx-target-label", target.Label)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("d", path)
            .Attribute("fill", color.ToCss())
            .Attribute("fill-opacity", chart.Options.Theme.FlatMarks ? .35 : ChartVisualPrimitives.SankeyLinkFillOpacity)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-opacity", chart.Options.Theme.FlatMarks ? 0 : ChartVisualPrimitives.SankeyLinkStrokeOpacity)
            .Attribute("stroke-width", ChartVisualPrimitives.SankeyLinkStrokeWidth)
            .EndEmptyElement()
            .Line();
    }

    private static SankeyModel BuildSankeyModel(Chart chart, ChartRect plot) =>
        ChartSankeyLayout.Compute(chart, plot, chart.Options.Theme.UseGraphiteLayout ? 10 : (double?)null, chart.Options.Theme.UseGraphiteLayout);
    private static ChartColor SankeyNodeGradientTop(ChartColor color) => ChartMarkSurface.SankeyNodeGradientTop(color);

    private static ChartColor SankeyNodeGradientBottom(ChartColor color) => ChartMarkSurface.SankeyNodeGradientBottom(color);

}
