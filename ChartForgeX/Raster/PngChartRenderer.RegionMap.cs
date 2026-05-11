using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawRegionMap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        var definition = chart.Options.RegionMapDefinition ?? throw new InvalidOperationException("Region maps require a map definition.");
        DrawRegionMap(c, chart, basePlot, ChartSeriesKind.RegionMap, definition, "region-map");
    }

    private static void DrawRegionMap(RgbaCanvas c, Chart chart, ChartRect basePlot, ChartSeriesKind kind, ChartMapDefinition definition, string rolePrefix) {
        ChartSeries? series = null;
        foreach (var item in chart.Series) if (item.Kind == kind) { series = item; break; }
        if (series == null || series.Points.Count == 0) return;
        var data = MapValues(chart, series);
        var t = chart.Options.Theme;
        var rightLegend = chart.Options.ShowMapScaleLegend && chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right;
        var bottomReserve = chart.Options.ShowMapScaleLegend && !rightLegend ? (ChartHeatmapSurface.MapMidpointLabel(chart) == null ? 46 : 62) : 12;
        var rightReserve = rightLegend ? 172 : 0;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 8, Math.Max(1, basePlot.Width - 20 - rightReserve), Math.Max(1, basePlot.Height - bottomReserve));
        var sourceBounds = RegionMapSourceBounds(definition, chart);
        var map = FitRegionMap(sourceBounds, plot);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var entry in data.Values) { if (entry.Value < min) min = entry.Value; if (entry.Value > max) max = entry.Value; }
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var regionStroke = chart.Options.MapRegionStrokeColor ?? t.CardBackground;
        var regionStrokeWidth = chart.Options.MapRegionStrokeWidth;
        var hasMissing = false;
        foreach (var region in definition.Regions) {
            if (!data.ContainsKey(region.Code)) { hasMissing = true; break; }
        }

        if (chart.Options.ShowMapSurface) DrawRegionMapPngSurface(c, chart, map);
        foreach (var layer in chart.Options.MapBaseLayers) DrawRegionMapPngLayer(c, layer, sourceBounds, map);
        foreach (var region in definition.Regions) {
            var hasValue = data.TryGetValue(region.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var color = hasValue ? ChartHeatmapSurface.MapColor(chart, entry.Color, series.Color ?? t.Palette[0], value, min, max) : ChartHeatmapSurface.MapNoDataColor(chart);
            var rings = ProjectMapRings(region.Path, sourceBounds, map, out var regionBounds);
            c.FillCompoundPolygon(rings, color);
            if (regionStrokeWidth > 0) {
                foreach (var points in rings) {
                    for (var i = 0; i < points.Count; i++) {
                        var next = points[(i + 1) % points.Count];
                        c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, regionStroke, regionStrokeWidth);
                    }
                }
            }
            if (chart.Options.ShowMapLabels) {
                var label = region.HasLabel ? ProjectMapPoint(region.Label, sourceBounds, map) : new ChartPoint(regionBounds.Left + regionBounds.Width / 2, regionBounds.Top + regionBounds.Height / 2);
                var fontSize = Math.Min(PngTickFontSize(chart), Math.Max(7, map.Height * 0.032));
                if (ShouldDrawRegionMapLabel(region.Code, regionBounds, fontSize)) c.DrawTextEmphasized(label.X - EstimatePngEmphasizedTextWidth(region.Code, fontSize) / 2, label.Y - fontSize / 2, region.Code, ChartColorMath.TextOnBackground(color), fontSize);
            }
        }

        foreach (var layer in chart.Options.MapOverlayLayers) DrawRegionMapPngLayer(c, layer, sourceBounds, map);
        if (chart.Options.ShowMapScaleLegend) {
            if (rightLegend) DrawRegionMapPngRightScale(c, chart, series, min, max, hasMissing, Math.Min(basePlot.Right - 124, plot.Right + 52), map.Top + Math.Max(48, map.Height * 0.28), map);
            else DrawRegionMapPngScale(c, chart, series, min, max, hasMissing, RegionMapScaleX(map, plot), plot.Bottom - 14, plot);
        }
    }

    private static void DrawRegionMapPngLayer(RgbaCanvas c, ChartMapLayer layer, ChartRect sourceBounds, ChartRect map) {
        foreach (var region in layer.Definition.Regions) {
            var rings = ProjectMapRings(region.Path, sourceBounds, map, out _);
            if (layer.FillColor.HasValue) c.FillCompoundPolygon(rings, layer.FillColor.Value);
            if (!layer.StrokeColor.HasValue || layer.StrokeWidth <= 0) continue;
            foreach (var points in rings) {
                for (var i = 0; i < points.Count; i++) {
                    var next = points[(i + 1) % points.Count];
                    c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, layer.StrokeColor.Value, layer.StrokeWidth);
                }
            }
        }
    }

    private static void DrawRegionMapPngSurface(RgbaCanvas c, Chart chart, ChartRect map) {
        var t = chart.Options.Theme;
        var pad = Math.Max(7, map.Height * 0.018);
        var radius = Math.Min(20, Math.Max(8, map.Height * 0.045));
        c.FillRoundedRect(map.Left - pad, map.Top - pad, map.Width + pad * 2, map.Height + pad * 2, radius, ApplyOpacity(ChartColorMath.Blend(t.PlotBackground, t.Grid, 0.20), 0.30));
        c.StrokeRoundedRect(map.Left - pad, map.Top - pad, map.Width + pad * 2, map.Height + pad * 2, radius, ApplyOpacity(t.PlotBorder, 0.24), 1);
    }

    private static void DrawRegionMapPngScale(RgbaCanvas c, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double x, double y, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = 11.0;
        var gap = 3.0;
        var fontSize = PngTickFontSize(chart);
        if (hasMissing) DrawMapPngNoDataScale(c, chart, x, y, size, fontSize, plot);
        var lowLabel = ChartHeatmapSurface.MapLowLabel(chart);
        c.DrawText(x - EstimatePngTextWidth(lowLabel, fontSize) - 8, y + size / 2 - fontSize / 2, lowLabel, t.MutedText, fontSize);
        for (var i = 0; i < 5; i++) {
            var value = ChartHeatmapSurface.MapScaleValue(chart, min, max, i / 4.0);
            var color = ChartHeatmapSurface.MapColor(chart, null, series.Color ?? t.Palette[0], value, min, max);
            c.FillRoundedRect(x + i * (size + gap), y, size, size, 2, color);
        }
        var highLabel = ChartHeatmapSurface.MapHighLabel(chart);
        c.DrawText(x + 5 * size + 4 * gap + 8, y + size / 2 - fontSize / 2, highLabel, t.MutedText, fontSize);
        var midpointLabel = ChartHeatmapSurface.MapMidpointLabel(chart);
        if (midpointLabel != null) {
            c.DrawText(x + 2 * (size + gap) + size / 2 - EstimatePngTextWidth(midpointLabel, fontSize) / 2, y + size + 2, midpointLabel, t.MutedText, fontSize);
        }
    }

    private static void DrawRegionMapPngRightScale(RgbaCanvas c, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double x, double y, ChartRect map) {
        var t = chart.Options.Theme;
        var width = 22.0;
        var height = Math.Min(240, Math.Max(150, map.Height * 0.34));
        var steps = 32;
        var stepHeight = height / steps;
        var fontSize = PngTickFontSize(chart);
        var titleLines = MapRightScaleTitleLines(series.Name);
        for (var i = 0; i < titleLines.Length; i++) c.DrawText(x, y - 34 + i * 17, titleLines[i], t.Text, fontSize + 1);
        for (var i = 0; i < steps; i++) {
            var ratio = 1 - i / (double)(steps - 1);
            var value = ChartHeatmapSurface.MapScaleValue(chart, min, max, ratio);
            var color = ChartHeatmapSurface.MapColor(chart, null, series.Color ?? t.Palette[0], value, min, max);
            c.FillRect(x, y + i * stepHeight, width, Math.Ceiling(stepHeight * 1000) / 1000 + 0.2, color);
        }

        c.StrokeRect(x, y, width, height, t.PlotBorder, 1);
        c.DrawText(x + width + 10, y + 2, ChartHeatmapSurface.MapHighLabel(chart), t.Text, fontSize);
        var midpointLabel = ChartHeatmapSurface.MapMidpointLabel(chart);
        if (midpointLabel != null) {
            var midpoint = ChartHeatmapSurface.MapScaleMidpoint(chart, min, max);
            var midpointRatio = ChartHeatmapSurface.MapRatio(chart, midpoint, min, max);
            c.DrawText(x + width + 10, y + height * (1 - midpointRatio) - fontSize / 2, midpointLabel, t.MutedText, fontSize);
        }

        c.DrawText(x + width + 10, y + height - fontSize, ChartHeatmapSurface.MapLowLabel(chart), t.Text, fontSize);
        if (hasMissing) {
            var missingY = y + height + 24;
            c.FillRoundedRect(x, missingY - 9, 11, 11, 2, ChartHeatmapSurface.MapNoDataColor(chart));
            c.DrawText(x + 16, missingY - fontSize / 2, "No data", t.MutedText, fontSize);
        }
    }

    private static string[] MapRightScaleTitleLines(string title) {
        if (string.IsNullOrWhiteSpace(title)) return new[] { "Value", "scale" };
        title = title.Trim();
        var per = title.IndexOf(" per ", StringComparison.OrdinalIgnoreCase);
        if (per > 0 && per + 5 < title.Length) return new[] { title.Substring(0, per + 4), title.Substring(per + 5) };
        if (title.Length <= 18) return new[] { title };
        var split = title.LastIndexOf(' ', Math.Min(title.Length - 1, 18));
        if (split > 0) return new[] { title.Substring(0, split), title.Substring(split + 1) };
        return new[] { title };
    }

    private static double RegionMapScaleX(ChartRect map, ChartRect plot) {
        const double scaleWidth = 5 * 11.0 + 4 * 3.0;
        return Clamp(map.Right - scaleWidth - 48, plot.Left + 70, plot.Right - scaleWidth - 44);
    }

    private static List<List<ChartPoint>> ProjectMapRings(string path, ChartRect source, ChartRect target, out ChartRect bounds) {
        var rings = ChartMapPathParser.ParseRings(path);
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        for (var ringIndex = 0; ringIndex < rings.Count; ringIndex++) {
            var ring = rings[ringIndex];
            for (var i = 0; i < ring.Count; i++) {
                var point = ProjectMapPoint(ring[i], source, target);
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
                ring[i] = point;
            }
        }

        for (var i = rings.Count - 1; i >= 0; i--) if (rings[i].Count < 3) rings.RemoveAt(i);
        bounds = double.IsInfinity(minX) ? new ChartRect(0, 0, 0, 0) : new ChartRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
        return rings;
    }

    private static bool ShouldDrawRegionMapLabel(string code, ChartRect bounds, double fontSize) {
        return bounds.Width >= EstimatePngEmphasizedTextWidth(code, fontSize) + 8 && bounds.Height >= fontSize + 5;
    }

    private static ChartPoint ProjectMapPoint(ChartPoint point, ChartRect source, ChartRect target) {
        var x = target.Left + (point.X - source.Left) / source.Width * target.Width;
        var y = target.Top + (point.Y - source.Top) / source.Height * target.Height;
        return new ChartPoint(x, y);
    }

    private static ChartRect RegionMapSourceBounds(ChartMapDefinition definition, Chart chart) {
        return chart.Options.RegionMapBounds ?? definition.Bounds;
    }

    private static ChartRect FitRegionMap(ChartRect sourceBounds, ChartRect plot) {
        var aspect = sourceBounds.Width / sourceBounds.Height;
        var width = Math.Min(plot.Width, plot.Height * aspect);
        var height = width / aspect;
        if (height > plot.Height) {
            height = plot.Height;
            width = height * aspect;
        }

        return new ChartRect(plot.Left + Math.Max(0, (plot.Width - width) / 2), plot.Top + Math.Max(0, (plot.Height - height) / 2), width, height);
    }

    private static bool IsRegionMapChart(Chart chart) => ChartSeriesKindTraits.ContainsKind(chart, ChartSeriesKind.RegionMap);
}
