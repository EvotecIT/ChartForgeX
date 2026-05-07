using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawSankey(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var model = BuildSankeyModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var showDataLabels = chart.Series.First(series => series.Kind == ChartSeriesKind.Sankey).ShowDataLabels ?? chart.Options.ShowDataLabels;
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
            var labelFontSize = TextFontSizeForSvgWidth(node.Label, labelMaxWidth, t.TickLabelFontSize);
            var label = TrimSvgLabelToWidth(node.Label, labelFontSize, labelMaxWidth);
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
                    .Attribute("x", labelX)
                    .Attribute("y", labelY)
                    .Attribute("text-anchor", anchor)
                    .Attribute("dominant-baseline", "middle")
                    .Attribute("fill", t.MutedText.ToCss())
                    .Attribute("stroke", t.CardBackground.ToCss())
                    .Attribute("stroke-width", ChartVisualPrimitives.SankeyLabelStrokeWidth)
                    .Attribute("paint-order", "stroke fill")
                    .Attribute("stroke-linejoin", "round")
                    .Attribute("font-family", SvgFontFamily(t.FontFamily))
                    .Attribute("font-size", labelFontSize)
                    .Attribute("font-weight", "700")
                    .Text(label)
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
        var color = chart.Options.Theme.Palette[source.Index % chart.Options.Theme.Palette.Length];
        var x0 = source.X + model.NodeWidth;
        var x1 = target.X;
        var midX = x0 + (x1 - x0) * 0.55;
        var half = Math.Max(1, link.Width / 2);
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
            .Attribute("fill-opacity", ChartVisualPrimitives.SankeyLinkFillOpacity)
            .Attribute("stroke", color.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.SankeyLinkStrokeOpacity)
            .Attribute("stroke-width", ChartVisualPrimitives.SankeyLinkStrokeWidth)
            .EndEmptyElement()
            .Line();
    }

    private static SankeyModel BuildSankeyModel(Chart chart, ChartRect plot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.Sankey);
        if (series == null || series.Points.Count < 2) return SankeyModel.Empty;
        var links = new List<SankeyLink>();
        var nodeCount = chart.Options.SankeyNodeLabels.Count;
        for (var i = 0; i + 1 < series.Points.Count; i += 2) {
            var endpoints = series.Points[i];
            var valuePoint = series.Points[i + 1];
            var source = Math.Max(0, (int)Math.Round(endpoints.X));
            var target = Math.Max(0, (int)Math.Round(endpoints.Y));
            var value = Math.Max(0, valuePoint.Y);
            if (value <= 0) continue;
            nodeCount = Math.Max(nodeCount, Math.Max(source, target) + 1);
            links.Add(new SankeyLink(source, target, value));
        }

        if (nodeCount == 0 || links.Count == 0) return SankeyModel.Empty;
        var nodes = new List<SankeyNode>();
        for (var i = 0; i < nodeCount; i++) nodes.Add(new SankeyNode(i, SankeyNodeLabel(chart, i)));
        foreach (var link in links) {
            nodes[link.Source].Outgoing += link.Value;
            nodes[link.Target].Incoming += link.Value;
        }

        ApplySankeyLayers(nodes, links);
        LayoutSankeyNodes(nodes, links, plot, out var nodeWidth, out var scale, out var maxLayer);
        LayoutSankeyLinks(nodes, links, scale);
        return new SankeyModel(nodes, links, nodeWidth, maxLayer);
    }

    private static void ApplySankeyLayers(List<SankeyNode> nodes, List<SankeyLink> links) {
        for (var pass = 0; pass < nodes.Count; pass++) {
            foreach (var link in links) nodes[link.Target].Layer = Math.Max(nodes[link.Target].Layer, nodes[link.Source].Layer + 1);
        }

        var maxLayer = Math.Max(1, nodes.Max(node => node.Layer));
        foreach (var node in nodes) if (node.Outgoing <= 0 && node.Incoming > 0) node.Layer = maxLayer;
    }

    private static void LayoutSankeyNodes(List<SankeyNode> nodes, List<SankeyLink> links, ChartRect plot, out double nodeWidth, out double scale, out int maxLayer) {
        maxLayer = Math.Max(1, nodes.Max(node => node.Layer));
        nodeWidth = Math.Max(ChartVisualPrimitives.SankeyNodeMinWidth, Math.Min(ChartVisualPrimitives.SankeyNodeMaxWidth, plot.Width / (maxLayer + 1) * ChartVisualPrimitives.SankeyNodeWidthFactor));
        scale = double.PositiveInfinity;
        for (var layer = 0; layer <= maxLayer; layer++) {
            var layerNodes = nodes.Where(node => node.Layer == layer).ToArray();
            if (layerNodes.Length == 0) continue;
            var sum = layerNodes.Sum(node => node.Value);
            scale = Math.Min(scale, (plot.Height - Math.Max(0, layerNodes.Length - 1) * ChartVisualPrimitives.SankeyNodeGap) / Math.Max(0.000001, sum));
        }

        if (double.IsInfinity(scale) || scale <= 0) scale = 1;
        var effectiveScale = scale;
        for (var layer = 0; layer <= maxLayer; layer++) {
            var layerNodes = nodes.Where(node => node.Layer == layer).OrderBy(node => node.Index).ToArray();
            var totalHeight = layerNodes.Sum(node => Math.Max(ChartVisualPrimitives.SankeyNodeMinHeight, node.Value * effectiveScale)) + Math.Max(0, layerNodes.Length - 1) * ChartVisualPrimitives.SankeyNodeGap;
            var y = plot.Top + Math.Max(0, (plot.Height - totalHeight) / 2);
            foreach (var node in layerNodes) {
                node.X = maxLayer == 0 ? plot.Left + plot.Width / 2 - nodeWidth / 2 : plot.Left + node.Layer / (double)maxLayer * (plot.Width - nodeWidth);
                node.Height = Math.Max(ChartVisualPrimitives.SankeyNodeMinHeight, node.Value * effectiveScale);
                node.Y = y;
                y += node.Height + ChartVisualPrimitives.SankeyNodeGap;
            }
        }
    }

    private static void LayoutSankeyLinks(List<SankeyNode> nodes, List<SankeyLink> links, double scale) {
        var outgoingOffset = new double[nodes.Count];
        var incomingOffset = new double[nodes.Count];
        foreach (var link in links.OrderBy(link => nodes[link.Source].Layer).ThenBy(link => link.Target)) {
            link.Width = Math.Max(2, link.Value * scale);
            link.SourceY = nodes[link.Source].Y + outgoingOffset[link.Source] + link.Width / 2;
            link.TargetY = nodes[link.Target].Y + incomingOffset[link.Target] + link.Width / 2;
            outgoingOffset[link.Source] += link.Width;
            incomingOffset[link.Target] += link.Width;
        }
    }

    private static string SankeyNodeLabel(Chart chart, int index) =>
        index >= 0 && index < chart.Options.SankeyNodeLabels.Count ? chart.Options.SankeyNodeLabels[index] : "Node " + (index + 1).ToString(CultureInfo.InvariantCulture);

    private static ChartColor SankeyNodeGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.SankeyNodeGradientTopBlend);

    private static ChartColor SankeyNodeGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.SankeyNodeGradientBottomBlend);

    private sealed class SankeyNode {
        public SankeyNode(int index, string label) { Index = index; Label = label; }
        public int Index { get; }
        public string Label { get; }
        public double Incoming { get; set; }
        public double Outgoing { get; set; }
        public double Value => Math.Max(Incoming, Outgoing);
        public int Layer { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Height { get; set; }
    }

    private sealed class SankeyLink {
        public SankeyLink(int source, int target, double value) { Source = source; Target = target; Value = value; }
        public int Source { get; }
        public int Target { get; }
        public double Value { get; }
        public double Width { get; set; }
        public double SourceY { get; set; }
        public double TargetY { get; set; }
    }

    private readonly struct SankeyModel {
        public SankeyModel(List<SankeyNode> nodes, List<SankeyLink> links, double nodeWidth, int maxLayer) { Nodes = nodes; Links = links; NodeWidth = nodeWidth; MaxLayer = maxLayer; }
        public static SankeyModel Empty { get; } = new(new List<SankeyNode>(), new List<SankeyLink>(), 0, 0);
        public List<SankeyNode> Nodes { get; }
        public List<SankeyLink> Links { get; }
        public double NodeWidth { get; }
        public int MaxLayer { get; }
    }
}
