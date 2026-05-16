using System;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawTree(StringBuilder sb, Chart chart, ChartRect plot, string id) {
        var model = ChartTreeLayout.Build(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var t = chart.Options.Theme;
        var showLabels = chart.Series.First(series => series.Kind == ChartSeriesKind.Tree).ShowDataLabels != false;
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "tree-chart")
            .EndStartElement()
            .Line();
        DrawTreeNodeGradients(writer, chart, id);
        foreach (var link in model.Links) DrawTreeLink(writer, chart, model, link);
        foreach (var node in model.Nodes) {
            var fillIndex = node.Depth % Math.Max(1, t.Palette.Length);
            var summary = node.Label + ": level " + node.Depth.ToString(CultureInfo.InvariantCulture);
            var radius = Math.Min(ChartVisualPrimitives.TreeNodeCornerRadiusMax, model.NodeHeight / 2);
            var labelColor = ChartColorMath.TextOnBackground(t.Palette[fillIndex]);
            var borderStroke = ChartVisualPrimitives.TreeNodeBorderStrokeWidth;
            var borderInset = borderStroke / 2.0;
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "tree-node")
                .Attribute("data-cfx-node", node.Index)
                .Attribute("data-cfx-depth", node.Depth)
                .Attribute("data-cfx-label", node.Label)
                .Attribute("role", "img")
                .Attribute("aria-label", summary)
                .Attribute("x", node.X)
                .Attribute("y", node.Y)
                .Attribute("width", model.NodeWidth)
                .Attribute("height", model.NodeHeight)
                .Attribute("rx", radius)
                .Attribute("fill", $"url(#{id}-treeFill{fillIndex})")
                .EndEmptyElement()
                .Line();
            writer
                .StartElement("rect")
                .Attribute("data-cfx-role", "tree-node-border")
                .Attribute("x", node.X + borderInset)
                .Attribute("y", node.Y + borderInset)
                .Attribute("width", Math.Max(0, model.NodeWidth - borderStroke))
                .Attribute("height", Math.Max(0, model.NodeHeight - borderStroke))
                .Attribute("rx", Math.Max(0, radius - borderInset))
                .Attribute("fill", "none")
                .Attribute("stroke", ChartColorMath.TextOnBackground(labelColor, 0.70).ToCss())
                .Attribute("stroke-opacity", ChartVisualPrimitives.TreeNodeBorderOpacity)
                .Attribute("stroke-width", borderStroke)
                .Attribute("vector-effect", "non-scaling-stroke")
                .EndEmptyElement()
                .Line();
            if (showLabels) DrawTreeNodeLabel(writer, chart, node, model, t.Palette[fillIndex], labelColor);
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawTreeNodeGradients(SvgMarkupWriter writer, Chart chart, string id) {
        writer.StartElement("defs").EndStartElement().Line();
        for (var i = 0; i < chart.Options.Theme.Palette.Length; i++) {
            var color = chart.Options.Theme.Palette[i];
            writer
                .StartElement("linearGradient")
                .Attribute("id", $"{id}-treeFill{i}")
                .Attribute("x1", 0)
                .Attribute("x2", 0)
                .Attribute("y1", 0)
                .Attribute("y2", 1)
                .EndStartElement()
                .StartElement("stop")
                .Attribute("offset", "0%")
                .Attribute("stop-color", TreeNodeGradientTop(color).ToHex())
                .EndEmptyElement()
                .StartElement("stop")
                .Attribute("offset", "100%")
                .Attribute("stop-color", TreeNodeGradientBottom(color).ToHex())
                .EndEmptyElement()
                .EndElement()
                .Line();
        }
        writer.EndElement().Line();
    }

    private static void DrawTreeLink(SvgMarkupWriter writer, Chart chart, ChartTreeModel model, ChartTreeLayoutLink link) {
        var parent = model.Nodes[link.Parent];
        var child = model.Nodes[link.Child];
        var color = chart.Options.Theme.Palette[parent.Depth % chart.Options.Theme.Palette.Length];
        var x0 = parent.X + model.NodeWidth;
        var y0 = parent.Y + model.NodeHeight / 2;
        var x1 = child.X;
        var y1 = child.Y + model.NodeHeight / 2;
        var gap = Math.Max(ChartVisualPrimitives.TreeLinkMinGap, (x1 - x0) / 2);
        var width = Math.Max(ChartVisualPrimitives.TreeLinkMinStrokeWidth, Math.Min(ChartVisualPrimitives.TreeLinkMaxStrokeWidth, ChartVisualPrimitives.TreeLinkMinStrokeWidth + link.Value / Math.Max(0.000001, model.MaxLinkValue) * ChartVisualPrimitives.TreeLinkStrokeWidthRange));
        var path = "M " + F(x0) + " " + F(y0) + " C " + F(x0 + gap) + " " + F(y0) + " " + F(x1 - gap) + " " + F(y1) + " " + F(x1) + " " + F(y1);
        var stroke = ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(255 * ChartVisualPrimitives.TreeLinkStrokeOpacity));
        foreach (var layer in ChartLineVisualLayers.Build(stroke, width, ChartRouteVisualStyles.TreeLink())) {
            if (!layer.IsVisible) continue;
            writer
                .StartElement("path")
                .Attribute("data-cfx-role", "tree-link" + layer.RoleSuffix)
                .Attribute("data-cfx-parent", link.Parent)
                .Attribute("data-cfx-child", link.Child)
                .Attribute("data-cfx-value", link.Value)
                .Attribute("data-cfx-parent-label", parent.Label)
                .Attribute("data-cfx-child-label", child.Label)
                .Attribute("d", path)
                .Attribute("class", ChartVisualPrimitives.SvgPremiumStrokeClass)
                .Attribute("fill", "none")
                .Attribute("stroke", layer.Color.ToCss())
                .Attribute("stroke-width", layer.StrokeWidth)
                .Attribute("stroke-linecap", "round")
                .Attribute("stroke-linejoin", "round");
            if (layer.Opacity < 1) writer.Attribute("opacity", layer.Opacity);
            writer.EndEmptyElement().Line();
        }
    }

    private static double TreeNodeLabelFontSize(double baseSize) => Math.Max(ChartVisualPrimitives.TreeNodeLabelMinFontSize, baseSize);

    private static void DrawTreeNodeLabel(SvgMarkupWriter writer, Chart chart, ChartTreeNode node, ChartTreeModel model, ChartColor nodeColor, ChartColor labelColor) {
        var fontSize = TreeNodeLabelFontSize(chart.Options.Theme.TickLabelFontSize);
        var maxWidth = model.NodeWidth - ChartVisualPrimitives.TreeNodeLabelHorizontalPadding * 2;
        var lines = ChartLabelWrapping.BalancedTwoLine(node.Label, fontSize, maxWidth, EstimateTextWidth);
        var lineHeight = fontSize * ChartVisualPrimitives.TreeNodeLabelLineHeightFactor;
        var firstY = node.Y + model.NodeHeight / 2 - (lines.Length - 1) * lineHeight / 2;
        for (var i = 0; i < lines.Length; i++) {
            DrawTreeNodeLabelLine(writer, chart, lines[i], node.X + model.NodeWidth / 2, firstY + i * lineHeight, nodeColor, labelColor, fontSize, maxWidth);
        }
    }

    private static void DrawTreeNodeLabelLine(SvgMarkupWriter writer, Chart chart, string text, double centerX, double y, ChartColor nodeColor, ChartColor labelColor, double fontSize, double maxWidth) {
        var fittedFontSize = TextFontSizeForSvgWidth(text, Math.Max(8, maxWidth), fontSize);
        var fittedText = TrimSvgLabelToWidth(text, fittedFontSize, Math.Max(8, maxWidth));
        if (fittedText.Length == 0) return;

        writer
            .StartElement("text")
            .Attribute("data-cfx-role", "tree-node-label")
            .Attribute("x", centerX)
            .Attribute("y", y)
            .Attribute("text-anchor", "middle")
            .Attribute("dominant-baseline", "middle")
            .Attribute("fill", labelColor.ToCss())
            .Attribute("stroke", nodeColor.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.TreeLabelHaloOpacity)
            .Attribute("stroke-width", ChartVisualPrimitives.TreeLabelHaloStrokeWidth)
            .Attribute("paint-order", "stroke fill")
            .Attribute("stroke-linejoin", "round")
            .Attribute("font-family", SvgFontFamily(chart.Options.Theme.FontFamily))
            .Attribute("font-size", fittedFontSize)
            .Attribute("font-weight", "800")
            .Text(fittedText)
            .EndElement()
            .Line();
    }

    private static ChartColor TreeNodeGradientTop(ChartColor color) => ChartMarkSurface.TreeNodeGradientTop(color);

    private static ChartColor TreeNodeGradientBottom(ChartColor color) => ChartMarkSurface.TreeNodeGradientBottom(color);
}
