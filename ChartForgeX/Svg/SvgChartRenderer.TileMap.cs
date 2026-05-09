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
    private static void DrawTileMap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.TileMap);
        if (series == null || series.Points.Count == 0) return;
        var definition = chart.Options.TileMapDefinition ?? throw new InvalidOperationException("Tile maps require a tile-map definition.");
        var data = MapValues(chart, series);
        var t = chart.Options.Theme;
        var bottomReserve = chart.Options.ShowMapScaleLegend ? 52 : 20;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 10, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - bottomReserve));
        var maxColumn = definition.ColumnCount - 1;
        var maxRow = definition.RowCount - 1;
        var gap = 4.0;
        var tileSize = Math.Max(4, Math.Min((plot.Width - gap * maxColumn) / (maxColumn + 1), (plot.Height - gap * maxRow) / (maxRow + 1)));
        var width = (maxColumn + 1) * tileSize + maxColumn * gap;
        var height = (maxRow + 1) * tileSize + maxRow * gap;
        var x0 = plot.Left + Math.Max(0, (plot.Width - width) / 2);
        var y0 = plot.Top + Math.Max(0, (plot.Height - height) / 2);
        var min = data.Count == 0 ? 0 : data.Values.Min(item => item.Value);
        var max = data.Count == 0 ? 1 : data.Values.Max(item => item.Value);
        var sourceMin = min;
        var sourceMax = max;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var hasMissing = definition.Regions.Any(tile => !data.ContainsKey(tile.Code));
        var missingCount = definition.Regions.Count(tile => !data.ContainsKey(tile.Code));
        var containerSummary = series.Name + " tile map with " + data.Count.ToString(CultureInfo.InvariantCulture) + " filled regions and " + missingCount.ToString(CultureInfo.InvariantCulture) + " missing regions";

        sb.AppendLine($"<g data-cfx-role=\"tile-map\" data-cfx-map-kind=\"{Escape(definition.Id)}\" data-cfx-map-id=\"{Escape(definition.Id)}\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-region-count=\"{definition.Regions.Count}\" data-cfx-filled-region-count=\"{data.Count}\" data-cfx-missing-region-count=\"{missingCount}\" data-cfx-min-value=\"{F(sourceMin)}\" data-cfx-max-value=\"{F(sourceMax)}\" role=\"group\" aria-label=\"{Escape(containerSummary)}\">");
        DrawTileMapSvgSurface(sb, chart, x0, y0, width, height, tileSize);
        foreach (var tile in definition.Regions) {
            var hasValue = data.TryGetValue(tile.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var ratio = hasValue ? ChartHeatmapSurface.Ratio(value, min, max) : 0;
            var status = hasValue ? ChartHeatmapSurface.Status(ratio) : "empty";
            var color = hasValue ? ChartHeatmapSurface.Color(chart, entry.Color ?? series.Color ?? t.Palette[0], value, min, max) : ChartHeatmapSurface.MapNoDataColor(chart);
            var x = x0 + tile.Column * (tileSize + gap);
            var y = y0 + tile.Row * (tileSize + gap);
            var points = HexTilePoints(x, y, tileSize);
            var regionName = tile.Name;
            var summary = regionName + " (" + tile.Code + "): " + (hasValue ? FormatValue(chart, value) : "No data");
            AppendSvg(sb, 768, writer => {
                writer.StartElement("polygon")
                    .Attribute("class", "cfx-interactive-region")
                    .Attribute("tabindex", "0")
                    .Attribute("focusable", "true")
                    .Attribute("data-cfx-role", "tile-map-region")
                    .Attribute("data-cfx-region", tile.Code)
                    .Attribute("data-cfx-region-name", regionName)
                    .Attribute("data-cfx-value", value)
                    .Attribute("data-cfx-empty", !hasValue)
                    .Attribute("data-cfx-status", status)
                    .Attribute("role", "img")
                    .Attribute("aria-label", summary)
                    .Attribute("points", points)
                    .Attribute("fill", color.ToCss())
                    .Attribute("stroke", t.CardBackground.ToCss())
                    .Attribute("stroke-width", Math.Max(1, tileSize * 0.035));
                writer.EndStartElement();
                writer.StartElement("title").Text(summary).EndElement();
                writer.EndElement().Line();
            });
            if (chart.Options.ShowMapLabels) DrawSvgTextCenteredX(sb, chart, "tile-map-label", tile.Code, x + tileSize / 2, y + tileSize / 2, ChartColorMath.TextOnBackground(color), Math.Min(t.TickLabelFontSize, tileSize * 0.32), tileSize - 6, "800");
        }

        if (chart.Options.ShowMapScaleLegend) {
            var scaleSize = Math.Max(8, Math.Min(13, tileSize * 0.32));
            var scaleY = Clamp(y0 + height + 22, basePlot.Top, basePlot.Bottom - scaleSize - 6);
            DrawTileMapSvgScale(sb, chart, series, min, max, hasMissing, x0 + width, scaleY, tileSize, plot);
        }
        sb.AppendLine("</g>");
    }

    private static void DrawTileMapSvgSurface(StringBuilder sb, Chart chart, double x, double y, double width, double height, double tileSize) {
        var t = chart.Options.Theme;
        var pad = Math.Max(6, tileSize * 0.16);
        AppendSvg(sb, 384, writer => writer.StartElement("rect")
            .Attribute("data-cfx-role", "tile-map-surface")
            .Attribute("x", x - pad)
            .Attribute("y", y - pad)
            .Attribute("width", width + pad * 2)
            .Attribute("height", height + pad * 2)
            .Attribute("rx", Math.Min(18, Math.Max(6, tileSize * 0.42)))
            .Attribute("fill", ChartColorMath.Blend(t.PlotBackground, t.Grid, 0.22).ToCss())
            .Attribute("fill-opacity", "0.32")
            .Attribute("stroke", t.PlotBorder.ToCss())
            .Attribute("stroke-opacity", "0.28")
            .EndEmptyElement()
            .Line());
    }

    private static void DrawTileMapSvgScale(StringBuilder sb, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double right, double y, double tileSize, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = Math.Max(8, Math.Min(13, tileSize * 0.32));
        var gap = Math.Max(2, size * 0.3);
        var width = 5 * size + 4 * gap;
        var x = right - width;
        if (hasMissing) DrawMapSvgNoDataScale(sb, chart, "tile-map", x, y, size, plot);
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", "tile-map-scale-label").Attribute("x", x - 8).Attribute("y", y + size / 2).Attribute("text-anchor", "end").Attribute("dominant-baseline", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text("Less").EndElement().Line());
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var ratio = ChartHeatmapSurface.Ratio(value, min, max);
            var color = ChartHeatmapSurface.Color(chart, series.Color ?? t.Palette[0], value, min, max);
            AppendSvg(sb, 256, writer => writer.StartElement("rect").Attribute("data-cfx-role", "tile-map-scale-step").Attribute("data-cfx-value", value).Attribute("data-cfx-status", ChartHeatmapSurface.Status(ratio)).Attribute("x", x + i * (size + gap)).Attribute("y", y).Attribute("width", size).Attribute("height", size).Attribute("rx", Math.Min(3, size * 0.22)).Attribute("fill", color.ToCss()).EndEmptyElement().Line());
        }
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", "tile-map-scale-label").Attribute("x", x + width + 8).Attribute("y", y + size / 2).Attribute("text-anchor", "start").Attribute("dominant-baseline", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text("More").EndElement().Line());
    }

    private static void DrawMapSvgNoDataScale(StringBuilder sb, Chart chart, string rolePrefix, double valueScaleX, double y, double size, ChartRect plot) {
        var t = chart.Options.Theme;
        var noData = ChartHeatmapSurface.MapNoDataColor(chart);
        const string label = "No data";
        var labelWidth = EstimateTextWidth(label, t.TickLabelFontSize);
        var width = size + 5 + labelWidth;
        var x = valueScaleX - width - 18;
        if (x < plot.Left) {
            x = plot.Left;
            y = Math.Max(plot.Top, y - size - 9);
        }

        AppendSvg(sb, 256, writer => writer.StartElement("rect").Attribute("data-cfx-role", rolePrefix + "-scale-no-data").Attribute("data-cfx-status", "empty").Attribute("x", x).Attribute("y", y).Attribute("width", size).Attribute("height", size).Attribute("rx", Math.Min(3, size * 0.22)).Attribute("fill", noData.ToCss()).EndEmptyElement().Line());
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-no-data-label").Attribute("x", x + size + 5).Attribute("y", y + size / 2).Attribute("dominant-baseline", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text(label).EndElement().Line());
    }

    private static string HexTilePoints(double x, double y, double size) {
        var inset = size * 0.22;
        return F(x + inset) + "," + F(y) + " " + F(x + size - inset) + "," + F(y) + " " + F(x + size) + "," + F(y + size / 2) + " " + F(x + size - inset) + "," + F(y + size) + " " + F(x + inset) + "," + F(y + size) + " " + F(x) + "," + F(y + size / 2);
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
