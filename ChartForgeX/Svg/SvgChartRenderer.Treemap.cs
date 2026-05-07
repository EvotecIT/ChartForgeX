using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawTreemap(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var seriesIndex = -1;
        ChartSeries? series = null;
        for (var i = 0; i < chart.Series.Count; i++) {
            if (chart.Series[i].Kind != ChartSeriesKind.Treemap) continue;
            seriesIndex = i;
            series = chart.Series[i];
            break;
        }

        if (series == null || seriesIndex < 0) return;
        var tiles = ChartTreemapLayout.Compute(series, TreemapPlot(chart, basePlot));
        if (tiles.Count == 0) return;

        var showLabels = series.ShowDataLabels != false;
        var writer = new SvgMarkupWriter(4096);
        writer
            .StartElement("g")
            .Attribute("data-cfx-role", "treemap")
            .EndStartElement()
            .Line();
        DrawTreemapTileGradients(writer, chart, id, series, seriesIndex);
        for (var i = 0; i < tiles.Count; i++) {
            var tile = tiles[i];
            var rect = tile.Rect;
            if (rect.Width <= 0 || rect.Height <= 0) continue;
            var color = TreemapTileColor(chart, series, tile.PointIndex);
            var label = FormatX(chart, tile.Point.X);
            var value = FormatValue(chart, tile.Point.Y);
            var summary = label + ": " + value;
            var radius = Math.Min(ChartVisualPrimitives.TreemapTileCornerRadiusMax, Math.Min(rect.Width, rect.Height) * ChartVisualPrimitives.TreemapTileCornerRadiusFactor);
            DrawTreemapTile(writer, chart, id, series, seriesIndex, tile.PointIndex, rect, radius, summary, label, tile.Point.Y);
            if (showLabels) DrawTreemapTileLabels(writer, chart, rect, label, value, color);
        }

        writer.EndElement().Line();
        sb.Append(writer.Build());
    }

    private static void DrawTreemapTileGradients(SvgMarkupWriter writer, Chart chart, string id, ChartSeries series, int seriesIndex) {
        writer.StartElement("defs").EndStartElement().Line();
        if (series.Color.HasValue) {
            var color = series.Color.Value;
            WriteTreemapTileGradient(writer, id + "-treemapFillSeries" + seriesIndex, color);
        }

        for (var i = 0; i < series.PointColors.Count; i++) {
            if (!series.PointColors[i].HasValue) continue;
            var color = series.PointColors[i]!.Value;
            WriteTreemapTileGradient(writer, id + "-treemapFillSeries" + seriesIndex + "Point" + i, color);
        }

        for (var i = 0; i < chart.Options.Theme.Palette.Length; i++) {
            var color = chart.Options.Theme.Palette[i];
            WriteTreemapTileGradient(writer, id + "-treemapFill" + i, color);
        }
        writer.EndElement().Line();
    }

    private static void WriteTreemapTileGradient(SvgMarkupWriter writer, string gradientId, ChartColor color) {
        writer
            .StartElement("linearGradient")
            .Attribute("id", gradientId)
            .Attribute("x1", "0")
            .Attribute("x2", "0")
            .Attribute("y1", "0")
            .Attribute("y2", "1")
            .EndStartElement()
            .StartElement("stop")
            .Attribute("offset", "0%")
            .Attribute("stop-color", TreemapTileGradientTop(color).ToHex())
            .EndEmptyElement()
            .StartElement("stop")
            .Attribute("offset", "100%")
            .Attribute("stop-color", TreemapTileGradientBottom(color).ToHex())
            .EndEmptyElement()
            .EndElement()
            .Line();
    }

    private static void DrawTreemapTile(SvgMarkupWriter writer, Chart chart, string id, ChartSeries series, int seriesIndex, int pointIndex, ChartRect rect, double radius, string summary, string label, double value) {
        var fill = TreemapTileFill(chart, series, seriesIndex, pointIndex, id);
        var highlightInset = Math.Min(radius, rect.Width / 4);
        var highlightEnd = rect.X + rect.Width - highlightInset;
        writer
            .StartElement("rect")
            .Attribute("data-cfx-role", "treemap-tile")
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("data-cfx-label", label)
            .Attribute("data-cfx-value", value)
            .Attribute("role", "img")
            .Attribute("aria-label", summary)
            .Attribute("x", rect.X)
            .Attribute("y", rect.Y)
            .Attribute("width", rect.Width)
            .Attribute("height", rect.Height)
            .Attribute("rx", radius)
            .Attribute("fill", fill)
            .EndEmptyElement()
            .Line()
            .StartElement("rect")
            .Attribute("data-cfx-role", "treemap-tile-border")
            .Attribute("data-cfx-point", pointIndex)
            .Attribute("x", rect.X + 0.5)
            .Attribute("y", rect.Y + 0.5)
            .Attribute("width", Math.Max(0, rect.Width - 1))
            .Attribute("height", Math.Max(0, rect.Height - 1))
            .Attribute("rx", Math.Max(0, radius - 0.5))
            .Attribute("fill", "none")
            .Attribute("stroke", chart.Options.Theme.CardBackground.ToCss())
            .Attribute("stroke-opacity", ChartVisualPrimitives.TreemapTileBorderOpacity)
            .Attribute("stroke-width", ChartVisualPrimitives.TreemapTileBorderStrokeWidth)
            .EndEmptyElement()
            .Line();
        if (highlightEnd > rect.X + highlightInset && rect.Height > 12) {
            writer
                .StartElement("line")
                .Attribute("data-cfx-role", "treemap-tile-highlight")
                .Attribute("data-cfx-point", pointIndex)
                .Attribute("x1", rect.X + highlightInset)
                .Attribute("y1", rect.Y + 1.35)
                .Attribute("x2", highlightEnd)
                .Attribute("y2", rect.Y + 1.35)
                .Attribute("stroke", "#fff")
                .Attribute("stroke-opacity", ChartVisualPrimitives.TreemapTileHighlightOpacity)
                .Attribute("stroke-width", ChartVisualPrimitives.TreemapTileHighlightStrokeWidth)
                .Attribute("stroke-linecap", "round")
                .EndEmptyElement()
                .Line();
        }
    }

    private static ChartRect TreemapPlot(Chart chart, ChartRect basePlot) {
        return new ChartRect(basePlot.X + 10, basePlot.Y + 12, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - 24));
    }

    private static void DrawTreemapTileLabels(SvgMarkupWriter writer, Chart chart, ChartRect rect, string label, string value, ChartColor color) {
        if (rect.Width < 48 || rect.Height < 30) return;
        var t = chart.Options.Theme;
        var textColor = HeatmapTextColor(color);
        var insetX = Math.Min(ChartVisualPrimitives.TreemapTileLabelInsetX, Math.Max(6, rect.Width * 0.12));
        var insetY = Math.Min(ChartVisualPrimitives.TreemapTileLabelInsetY, Math.Max(7, rect.Height * 0.14));
        var maxWidth = Math.Max(8, rect.Width - insetX * 2);
        var labelFontSize = TextFontSizeForSvgWidth(label, maxWidth, Math.Min(t.LegendFontSize, Math.Max(8, rect.Height * 0.20)));
        var fittedLabel = TrimSvgLabelToWidth(label, labelFontSize, maxWidth);
        if (fittedLabel.Length > 0) {
            writer
                .StartElement("text")
                .Attribute("data-cfx-role", "treemap-label")
                .Attribute("x", rect.X + insetX)
                .Attribute("y", rect.Y + insetY + labelFontSize)
                .Attribute("fill", textColor.ToCss())
                .Attribute("font-family", TreemapSvgFontFamily(t.FontFamily))
                .Attribute("font-size", labelFontSize)
                .Attribute("font-weight", "800")
                .Text(fittedLabel)
                .EndElement()
                .Line();
        }

        if (rect.Height < 52) return;
        var valueFontSize = TextFontSizeForSvgWidth(value, maxWidth, Math.Min(t.DataLabelFontSize, Math.Max(8, rect.Height * 0.18)));
        var fittedValue = TrimSvgLabelToWidth(value, valueFontSize, maxWidth);
        if (fittedValue.Length > 0) {
            var valueY = rect.Y + insetY + labelFontSize + ChartVisualPrimitives.TreemapTileValueGap + valueFontSize;
            if (valueY <= rect.Bottom - insetY * 0.45) {
                writer
                    .StartElement("text")
                    .Attribute("data-cfx-role", "treemap-value")
                    .Attribute("x", rect.X + insetX)
                    .Attribute("y", valueY)
                    .Attribute("fill", textColor.ToCss())
                    .Attribute("fill-opacity", ChartVisualPrimitives.TreemapValueOpacity)
                    .Attribute("font-family", TreemapSvgFontFamily(t.FontFamily))
                    .Attribute("font-size", valueFontSize)
                    .Attribute("font-weight", "700")
                    .Text(fittedValue)
                    .EndElement()
                    .Line();
            }
        }
    }

    private static string TreemapSvgFontFamily(string value) => string.IsNullOrWhiteSpace(value) ? "system-ui, sans-serif" : value;

    private static bool IsTreemapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.Treemap);

    private static ChartColor TreemapTileColor(Chart chart, ChartSeries series, int pointIndex) =>
        pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue
            ? series.PointColors[pointIndex]!.Value
            : series.Color ?? chart.Options.Theme.Palette[pointIndex % chart.Options.Theme.Palette.Length];

    private static string TreemapTileFill(Chart chart, ChartSeries series, int seriesIndex, int pointIndex, string id) {
        if (pointIndex < series.PointColors.Count && series.PointColors[pointIndex].HasValue) return $"url(#{id}-treemapFillSeries{seriesIndex}Point{pointIndex})";
        if (series.Color.HasValue) return $"url(#{id}-treemapFillSeries{seriesIndex})";
        return $"url(#{id}-treemapFill{pointIndex % chart.Options.Theme.Palette.Length})";
    }

    private static ChartColor TreemapTileGradientTop(ChartColor color) => Blend(ChartColor.White, color, ChartVisualPrimitives.TreemapTileGradientTopBlend);

    private static ChartColor TreemapTileGradientBottom(ChartColor color) => Blend(ChartColor.Black, color, ChartVisualPrimitives.TreemapTileGradientBottomBlend);
}
