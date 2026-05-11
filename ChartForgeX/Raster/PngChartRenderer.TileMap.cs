using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawTileMap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var item in chart.Series) if (item.Kind == ChartSeriesKind.TileMap) { series = item; break; }
        if (series == null || series.Points.Count == 0) return;
        var definition = chart.Options.TileMapDefinition ?? throw new InvalidOperationException("Tile maps require a tile-map definition.");
        var data = MapValues(chart, series);
        var t = chart.Options.Theme;
        var rightLegend = chart.Options.ShowMapScaleLegend && chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right;
        var bottomReserve = chart.Options.ShowMapScaleLegend && !rightLegend ? (ChartHeatmapSurface.MapMidpointLabel(chart) == null ? 52 : 68) : 20;
        var rightReserve = rightLegend ? 172 : 0;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 10, Math.Max(1, basePlot.Width - 20 - rightReserve), Math.Max(1, basePlot.Height - bottomReserve));
        var maxColumn = 0;
        var maxRow = 0;
        maxColumn = definition.ColumnCount - 1;
        maxRow = definition.RowCount - 1;
        var gap = 4.0;
        var tileSize = Math.Max(4, Math.Min((plot.Width - gap * maxColumn) / (maxColumn + 1), (plot.Height - gap * maxRow) / (maxRow + 1)));
        var width = (maxColumn + 1) * tileSize + maxColumn * gap;
        var height = (maxRow + 1) * tileSize + maxRow * gap;
        var x0 = plot.Left + Math.Max(0, (plot.Width - width) / 2);
        var y0 = plot.Top + Math.Max(0, (plot.Height - height) / 2);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var entry in data.Values) { if (entry.Value < min) min = entry.Value; if (entry.Value > max) max = entry.Value; }
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var regionStroke = chart.Options.MapRegionStrokeColor ?? t.CardBackground;
        var regionStrokeWidth = Math.Max(0, chart.Options.MapRegionStrokeWidth);
        var hasMissing = false;
        foreach (var tile in definition.Regions) {
            if (!data.ContainsKey(tile.Code)) { hasMissing = true; break; }
        }

        if (chart.Options.ShowMapSurface) DrawTileMapPngSurface(c, chart, x0, y0, width, height, tileSize);
        foreach (var tile in definition.Regions) {
            var hasValue = data.TryGetValue(tile.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var color = hasValue ? ChartHeatmapSurface.MapColor(chart, entry.Color, series.Color ?? t.Palette[0], value, min, max) : ChartHeatmapSurface.MapNoDataColor(chart);
            var x = x0 + tile.Column * (tileSize + gap);
            var y = y0 + tile.Row * (tileSize + gap);
            var points = HexTilePoints(x, y, tileSize);
            c.FillPolygon(points, color);
            if (regionStrokeWidth > 0) {
                for (var i = 0; i < points.Count; i++) {
                    var next = points[(i + 1) % points.Count];
                    c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, regionStroke, regionStrokeWidth);
                }
            }
            if (chart.Options.ShowMapLabels) {
                var fontSize = Math.Min(PngTickFontSize(chart), tileSize * 0.32);
                c.DrawTextEmphasized(x + tileSize / 2 - EstimatePngEmphasizedTextWidth(tile.Code, fontSize) / 2, y + tileSize / 2 - fontSize / 2, tile.Code, ChartColorMath.TextOnBackground(color), fontSize);
            }
        }

        if (chart.Options.ShowMapScaleLegend && !rightLegend) {
            var scaleSize = Math.Max(8, Math.Min(13, tileSize * 0.32));
            var scaleY = Clamp(y0 + height + 22, basePlot.Top, basePlot.Bottom - scaleSize - 6);
            DrawTileMapPngScale(c, chart, series, min, max, hasMissing, x0 + width, scaleY, tileSize, plot);
        } else if (rightLegend) {
            DrawRegionMapPngRightScale(c, chart, series, min, max, hasMissing, Math.Min(basePlot.Right - 124, plot.Right + 52), y0 + Math.Max(28, height * 0.18), new ChartRect(x0, y0, width, height));
        }
    }

    private static void DrawTileMapPngSurface(RgbaCanvas c, Chart chart, double x, double y, double width, double height, double tileSize) {
        var t = chart.Options.Theme;
        var pad = Math.Max(6, tileSize * 0.16);
        var radius = Math.Min(18, Math.Max(6, tileSize * 0.42));
        c.FillRoundedRect(x - pad, y - pad, width + pad * 2, height + pad * 2, radius, ApplyOpacity(ChartColorMath.Blend(t.PlotBackground, t.Grid, 0.22), 0.32));
        c.StrokeRoundedRect(x - pad, y - pad, width + pad * 2, height + pad * 2, radius, ApplyOpacity(t.PlotBorder, 0.28), 1);
    }

    private static void DrawTileMapPngScale(RgbaCanvas c, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double right, double y, double tileSize, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = Math.Max(8, Math.Min(13, tileSize * 0.32));
        var gap = Math.Max(2, size * 0.3);
        var width = 5 * size + 4 * gap;
        var x = right - width;
        var fontSize = PngTickFontSize(chart);
        if (hasMissing) DrawMapPngNoDataScale(c, chart, x, y, size, fontSize, plot);
        var lowLabel = ChartHeatmapSurface.MapLowLabel(chart);
        c.DrawText(x - EstimatePngTextWidth(lowLabel, fontSize) - 8, y + size / 2 - fontSize / 2, lowLabel, t.MutedText, fontSize);
        for (var i = 0; i < 5; i++) {
            var value = ChartHeatmapSurface.MapScaleValue(chart, min, max, i / 4.0);
            var color = ChartHeatmapSurface.MapColor(chart, null, series.Color ?? t.Palette[0], value, min, max);
            c.FillRoundedRect(x + i * (size + gap), y, size, size, Math.Min(3, size * 0.22), color);
        }
        var highLabel = ChartHeatmapSurface.MapHighLabel(chart);
        c.DrawText(x + width + 8, y + size / 2 - fontSize / 2, highLabel, t.MutedText, fontSize);
        var midpointLabel = ChartHeatmapSurface.MapMidpointLabel(chart);
        if (midpointLabel != null) {
            c.DrawText(x + 2 * (size + gap) + size / 2 - EstimatePngTextWidth(midpointLabel, fontSize) / 2, y + size + 2, midpointLabel, t.MutedText, fontSize);
        }
    }

    private static void DrawMapPngNoDataScale(RgbaCanvas c, Chart chart, double valueScaleX, double y, double size, double fontSize, ChartRect plot) {
        const string label = "No data";
        var width = size + 5 + EstimatePngTextWidth(label, fontSize);
        var x = valueScaleX - width - 18;
        if (x < plot.Left) {
            x = plot.Left;
            y = Math.Max(plot.Top, y - size - 9);
        }

        c.FillRoundedRect(x, y, size, size, Math.Min(3, size * 0.22), ChartHeatmapSurface.MapNoDataColor(chart));
        c.DrawText(x + size + 5, y + size / 2 - fontSize / 2, label, chart.Options.Theme.MutedText, fontSize);
    }

    private static List<ChartPoint> HexTilePoints(double x, double y, double size) {
        var inset = size * 0.22;
        return new List<ChartPoint> {
            new(x + inset, y),
            new(x + size - inset, y),
            new(x + size, y + size / 2),
            new(x + size - inset, y + size),
            new(x + inset, y + size),
            new(x, y + size / 2)
        };
    }

    private static Dictionary<string, RegionMapValue> MapValues(Chart chart, ChartSeries series) {
        var values = new Dictionary<string, RegionMapValue>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < series.Points.Count; i++) {
            var region = i < chart.Options.XAxisLabels.Count ? chart.Options.XAxisLabels[i].Text : string.Empty;
            if (region.Length == 0) continue;
            var color = i < series.PointColors.Count ? series.PointColors[i] : null;
            values[region] = new RegionMapValue(series.Points[i].Y, color);
        }

        return values;
    }

    private static bool IsTileMapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.TileMap);

    private readonly struct RegionMapValue {
        public readonly double Value;
        public readonly ChartColor? Color;

        public RegionMapValue(double value, ChartColor? color) {
            Value = value;
            Color = color;
        }
    }

}
