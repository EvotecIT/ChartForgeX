using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using SankeyModel = ChartForgeX.Rendering.ChartSankeyModel;
using SankeyNode = ChartForgeX.Rendering.ChartSankeyNode;
using SankeyLink = ChartForgeX.Rendering.ChartSankeyLayoutLink;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawSankey(RgbaCanvas c, Chart chart, ChartRect plot) {
        var model = BuildSankeyModel(chart, plot);
        if (model.Nodes.Count == 0 || model.Links.Count == 0) return;
        var series = chart.Series.First(item => item.Kind == ChartSeriesKind.Sankey);
        var showDataLabels = series.ShowDataLabels ?? chart.Options.ShowDataLabels;
        foreach (var link in model.Links) DrawSankeyLink(c, chart, model, link);
        foreach (var node in model.Nodes) DrawSankeyNode(c, chart, series, plot, model, node, showDataLabels);
    }

    private static bool IsSankeyChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.Sankey);

    private static void DrawSankeyNode(RgbaCanvas c, Chart chart, ChartSeries series, ChartRect plot, SankeyModel model, SankeyNode node, bool showDataLabels) {
        var theme = chart.Options.Theme;
        var color = theme.Palette[node.Index % theme.Palette.Length];
        var radius = Math.Min(ChartVisualPrimitives.SankeyNodeCornerRadiusMax, model.NodeWidth / 2);
        c.FillRoundedRectVerticalGradient(node.X, node.Y, model.NodeWidth, node.Height, radius, SankeyNodeGradientTop(color), SankeyNodeGradientBottom(color));
        c.StrokeRoundedRect(node.X, node.Y, model.NodeWidth, node.Height, radius, ApplyOpacity(theme.CardBackground, ChartVisualPrimitives.SankeyNodeBorderOpacity), ChartVisualPrimitives.SankeyNodeBorderStrokeWidth);
        if (!showDataLabels || c.SuppressText) return;
        var dataStyle = DataLabelStyle(chart, series);
        var preferredFontSize = PngStyleFontSize(dataStyle, theme.TickLabelFontSize);
        var labelMaxWidth = Math.Max(64, plot.Width / Math.Max(2, model.MaxLayer + 1) * 0.62);
        var fontSize = TextFontSizeForEmphasizedWidth(node.Label, labelMaxWidth, preferredFontSize, dataStyle);
        var label = TrimReadablePngLabelToWidth(node.Label, fontSize, labelMaxWidth, dataStyle);
        if (label.Length == 0) return;
        var labelWidth = EstimatePngStyledTextWidth(label, fontSize, dataStyle, emphasized: true);
        var labelHeight = EstimatePngStyledTextHeight(fontSize, dataStyle);
        var y = node.Y + node.Height / 2 - labelHeight / 2;
        var labelBounds = new ChartRect(chart.Options.Padding.Left, chart.Options.Padding.Top, chart.Options.Size.Width - chart.Options.Padding.Left - chart.Options.Padding.Right, chart.Options.Size.Height - chart.Options.Padding.Top - chart.Options.Padding.Bottom);
        var padX = ChartVisualPrimitives.SankeyLabelBackdropPaddingX;
        var padY = ChartVisualPrimitives.SankeyLabelBackdropPaddingY;
        var labelX = node.Layer == model.MaxLayer ? node.X - labelWidth - 10 : node.X + model.NodeWidth + 10;
        var backdropHeight = labelHeight + padY * 2;
        var labelRadius = Math.Min(6, backdropHeight / 2);
        c.FillRoundedRect(labelX - padX, y - padY, labelWidth + padX * 2, backdropHeight, labelRadius, ApplyOpacity(theme.CardBackground, ChartVisualPrimitives.SankeyLabelBackdropOpacity));
        c.StrokeRoundedRect(labelX - padX, y - padY, labelWidth + padX * 2, backdropHeight, labelRadius, ApplyOpacity(theme.PlotBorder, ChartVisualPrimitives.SankeyLabelBackdropBorderOpacity));
        DrawReadablePngLabel(c, labelBounds, labelX, y, label, theme.MutedText, theme.CardBackground, fontSize, dataStyle);
    }

    private static void DrawSankeyLink(RgbaCanvas c, Chart chart, SankeyModel model, SankeyLink link) {
        var source = model.Nodes[link.Source];
        var target = model.Nodes[link.Target];
        var color = chart.Options.Theme.Palette[source.Index % chart.Options.Theme.Palette.Length];
        var x0 = source.X + model.NodeWidth;
        var x1 = target.X;
        var midX = x0 + (x1 - x0) * 0.55;
        var half = link.Width / 2;
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
        c.StrokeClosedPolyline(points, stroke, ChartVisualPrimitives.SankeyLinkStrokeWidth, RasterLineJoin.Miter);
    }

    private static SankeyModel BuildSankeyModel(Chart chart, ChartRect plot) =>
        ChartSankeyLayout.Compute(chart, plot, chart.Options.Theme.UseGraphiteLayout ? 10 : (double?)null, chart.Options.Theme.UseGraphiteLayout);
    private static double Cubic(double p0, double p1, double p2, double p3, double t) {
        var u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    }

    private static ChartColor SankeyNodeGradientTop(ChartColor color) => ChartMarkSurface.SankeyNodeGradientTop(color);

    private static ChartColor SankeyNodeGradientBottom(ChartColor color) => ChartMarkSurface.SankeyNodeGradientBottom(color);

}
