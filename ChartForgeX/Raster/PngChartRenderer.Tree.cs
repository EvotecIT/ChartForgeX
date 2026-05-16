using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawTree(RgbaCanvas c, Chart chart, ChartRect plot) {
        var model = ChartTreeLayout.Build(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var showLabels = chart.Series.First(series => series.Kind == ChartSeriesKind.Tree).ShowDataLabels != false;
        foreach (var link in model.Links) DrawTreeLink(c, chart, model, link);
        foreach (var node in model.Nodes) DrawTreeNode(c, chart, model, node, showLabels);
    }

    private static bool IsTreeChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Tree);

    private static void DrawTreeNode(RgbaCanvas c, Chart chart, ChartTreeModel model, ChartTreeNode node, bool showLabels) {
        var theme = chart.Options.Theme;
        var color = theme.Palette[node.Depth % theme.Palette.Length];
        var labelColor = ChartColorMath.TextOnBackground(color);
        var radius = Math.Min(ChartVisualPrimitives.TreeNodeCornerRadiusMax, model.NodeHeight / 2);
        c.FillRoundedRectVerticalGradient(node.X, node.Y, model.NodeWidth, model.NodeHeight, radius, TreeNodeGradientTop(color), TreeNodeGradientBottom(color));
        c.StrokeRoundedRect(node.X, node.Y, model.NodeWidth, model.NodeHeight, radius, ApplyOpacity(ChartColorMath.TextOnBackground(labelColor, 0.70), ChartVisualPrimitives.TreeNodeBorderOpacity), ChartVisualPrimitives.TreeNodeBorderStrokeWidth);
        if (showLabels) DrawTreeNodeLabel(c, chart, model, node, color, labelColor);
    }

    private static void DrawTreeLink(RgbaCanvas c, Chart chart, ChartTreeModel model, ChartTreeLayoutLink link) {
        var parent = model.Nodes[link.Parent];
        var child = model.Nodes[link.Child];
        var color = chart.Options.Theme.Palette[parent.Depth % chart.Options.Theme.Palette.Length];
        var x0 = parent.X + model.NodeWidth;
        var y0 = parent.Y + model.NodeHeight / 2;
        var x1 = child.X;
        var y1 = child.Y + model.NodeHeight / 2;
        var gap = Math.Max(ChartVisualPrimitives.TreeLinkMinGap, (x1 - x0) / 2);
        var width = Math.Max(ChartVisualPrimitives.TreeLinkMinStrokeWidth, Math.Min(ChartVisualPrimitives.TreeLinkMaxStrokeWidth, ChartVisualPrimitives.TreeLinkMinStrokeWidth + link.Value / Math.Max(0.000001, model.MaxLinkValue) * ChartVisualPrimitives.TreeLinkStrokeWidthRange));
        var stroke = ChartColor.FromRgba(color.R, color.G, color.B, (byte)Math.Round(255 * ChartVisualPrimitives.TreeLinkStrokeOpacity));
        var points = ChartRouteVisualStyles.SampleCubic(x0, y0, x0 + gap, y0, x1 - gap, y1, x1, y1, ChartVisualPrimitives.TreeLinkCurveSegments);
        foreach (var layer in ChartLineVisualLayers.Build(stroke, width, ChartRouteVisualStyles.TreeLink())) {
            if (layer.IsVisible) c.DrawPolyline(points, layer.ColorWithOpacity(), layer.StrokeWidth);
        }
    }

    private static double TreeNodeLabelFontSize(double baseSize) => Math.Max(ChartVisualPrimitives.TreeNodeLabelMinFontSize, baseSize);

    private static void DrawTreeNodeLabel(RgbaCanvas c, Chart chart, ChartTreeModel model, ChartTreeNode node, ChartColor nodeColor, ChartColor labelColor) {
        var fontSize = TreeNodeLabelFontSize(chart.Options.Theme.TickLabelFontSize);
        var maxWidth = model.NodeWidth - ChartVisualPrimitives.TreeNodeLabelHorizontalPadding * 2;
        var lines = ChartLabelWrapping.BalancedTwoLine(node.Label, fontSize, maxWidth, EstimatePngEmphasizedTextWidth);
        var lineHeight = fontSize * ChartVisualPrimitives.TreeNodeLabelLineHeightFactor;
        var textHeight = lines.Length * lineHeight;
        var top = node.Y + (model.NodeHeight - textHeight) / 2.0;
        for (var i = 0; i < lines.Length; i++) {
            var y = top + i * lineHeight;
            var x = node.X + (model.NodeWidth - EstimatePngEmphasizedTextWidth(lines[i], fontSize)) / 2.0;
            DrawReadablePngLabel(c, x, y, lines[i], labelColor, nodeColor, fontSize);
        }
    }

    private static ChartColor TreeNodeGradientTop(ChartColor color) => ChartMarkSurface.TreeNodeGradientTop(color);

    private static ChartColor TreeNodeGradientBottom(ChartColor color) => ChartMarkSurface.TreeNodeGradientBottom(color);
}
