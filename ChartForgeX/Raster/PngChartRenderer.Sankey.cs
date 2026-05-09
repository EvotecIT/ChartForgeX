using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawSankey(RgbaCanvas c, Chart chart, ChartRect plot) {
        var model = BuildSankeyModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var showDataLabels = chart.Series.First(series => series.Kind == ChartSeriesKind.Sankey).ShowDataLabels ?? chart.Options.ShowDataLabels;
        foreach (var link in model.Links) DrawSankeyLink(c, chart, model, link);
        foreach (var node in model.Nodes) DrawSankeyNode(c, chart, plot, model, node, showDataLabels);
    }

    private static bool IsSankeyChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Sankey);

    private static void DrawSankeyNode(RgbaCanvas c, Chart chart, ChartRect plot, SankeyModel model, SankeyNode node, bool showDataLabels) {
        var theme = chart.Options.Theme;
        var color = theme.Palette[node.Index % theme.Palette.Length];
        var radius = Math.Min(ChartVisualPrimitives.SankeyNodeCornerRadiusMax, model.NodeWidth / 2);
        c.FillRoundedRectVerticalGradient(node.X, node.Y, model.NodeWidth, node.Height, radius, SankeyNodeGradientTop(color), SankeyNodeGradientBottom(color));
        c.StrokeRoundedRect(node.X, node.Y, model.NodeWidth, node.Height, radius, ApplyOpacity(theme.CardBackground, ChartVisualPrimitives.SankeyNodeBorderOpacity), ChartVisualPrimitives.SankeyNodeBorderStrokeWidth);
        if (!showDataLabels) return;
        var preferredFontSize = PngTickFontSize(chart);
        var labelMaxWidth = Math.Max(64, plot.Width / Math.Max(2, model.MaxLayer + 1) * 0.62);
        var fontSize = TextFontSizeForEmphasizedWidth(node.Label, labelMaxWidth, preferredFontSize);
        var label = TrimReadablePngLabelToWidth(node.Label, fontSize, labelMaxWidth);
        if (label.Length == 0) return;
        var labelWidth = EstimatePngEmphasizedTextWidth(label, fontSize);
        var y = node.Y + node.Height / 2 - fontSize / 2;
        var labelBounds = new ChartRect(chart.Options.Padding.Left, chart.Options.Padding.Top, chart.Options.Size.Width - chart.Options.Padding.Left - chart.Options.Padding.Right, chart.Options.Size.Height - chart.Options.Padding.Top - chart.Options.Padding.Bottom);
        var padX = ChartVisualPrimitives.SankeyLabelBackdropPaddingX;
        var padY = ChartVisualPrimitives.SankeyLabelBackdropPaddingY;
        var labelX = node.Layer == model.MaxLayer ? node.X - labelWidth - 10 : node.X + model.NodeWidth + 10;
        var labelRadius = Math.Min(6, (fontSize + padY * 2) / 2);
        c.FillRoundedRect(labelX - padX, y - padY, labelWidth + padX * 2, fontSize + padY * 2, labelRadius, ApplyOpacity(theme.CardBackground, ChartVisualPrimitives.SankeyLabelBackdropOpacity));
        c.StrokeRoundedRect(labelX - padX, y - padY, labelWidth + padX * 2, fontSize + padY * 2, labelRadius, ApplyOpacity(theme.PlotBorder, ChartVisualPrimitives.SankeyLabelBackdropBorderOpacity));
        if (node.Layer == model.MaxLayer) {
            DrawReadablePngLabel(c, labelBounds, labelX, y, label, theme.MutedText, theme.CardBackground, fontSize);
        } else {
            DrawReadablePngLabel(c, labelBounds, labelX, y, label, theme.MutedText, theme.CardBackground, fontSize);
        }
    }

    private static void DrawSankeyLink(RgbaCanvas c, Chart chart, SankeyModel model, SankeyLink link) {
        var source = model.Nodes[link.Source];
        var target = model.Nodes[link.Target];
        var color = chart.Options.Theme.Palette[source.Index % chart.Options.Theme.Palette.Length];
        var x0 = source.X + model.NodeWidth;
        var x1 = target.X;
        var midX = x0 + (x1 - x0) * 0.55;
        var half = Math.Max(1, link.Width / 2);
        var points = new List<ChartPoint>((ChartVisualPrimitives.SankeyLinkCurveSegments + 1) * 2);
        for (var step = 0; step <= ChartVisualPrimitives.SankeyLinkCurveSegments; step++) {
            var t = step / (double)ChartVisualPrimitives.SankeyLinkCurveSegments;
            points.Add(new ChartPoint(Cubic(x0, midX, midX, x1, t), Cubic(link.SourceY - half, link.SourceY - half, link.TargetY - half, link.TargetY - half, t)));
        }

        for (var step = ChartVisualPrimitives.SankeyLinkCurveSegments; step >= 0; step--) {
            var t = step / (double)ChartVisualPrimitives.SankeyLinkCurveSegments;
            points.Add(new ChartPoint(Cubic(x0, midX, midX, x1, t), Cubic(link.SourceY + half, link.SourceY + half, link.TargetY + half, link.TargetY + half, t)));
        }

        c.FillPolygon(points, ApplyOpacity(color, ChartVisualPrimitives.SankeyLinkFillOpacity));
        var stroke = ApplyOpacity(color, ChartVisualPrimitives.SankeyLinkStrokeOpacity);
        for (var i = 1; i < points.Count; i++) c.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, stroke, ChartVisualPrimitives.SankeyLinkStrokeWidth);
    }

    private static SankeyModel BuildSankeyModel(Chart chart, ChartRect plot) {
        ChartSeries? series = null;
        foreach (var candidate in chart.Series) if (candidate.Kind == ChartSeriesKind.Sankey) { series = candidate; break; }
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
        LayoutSankeyNodes(nodes, plot, out var nodeWidth, out var scale, out var maxLayer);
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

    private static void LayoutSankeyNodes(List<SankeyNode> nodes, ChartRect plot, out double nodeWidth, out double scale, out int maxLayer) {
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

    private static double Cubic(double p0, double p1, double p2, double p3, double t) {
        var u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    }

    private static string SankeyNodeLabel(Chart chart, int index) =>
        index >= 0 && index < chart.Options.SankeyNodeLabels.Count ? chart.Options.SankeyNodeLabels[index] : "Node " + (index + 1).ToString(CultureInfo.InvariantCulture);

    private static ChartColor SankeyNodeGradientTop(ChartColor color) => ChartMarkSurface.SankeyNodeGradientTop(color);

    private static ChartColor SankeyNodeGradientBottom(ChartColor color) => ChartMarkSurface.SankeyNodeGradientBottom(color);

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
