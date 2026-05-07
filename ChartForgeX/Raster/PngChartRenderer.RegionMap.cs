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
        var bottomReserve = chart.Options.ShowMapScaleLegend ? 46 : 12;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 8, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - bottomReserve));
        var map = FitRegionMap(definition, plot);
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;
        foreach (var entry in data.Values) { if (entry.Value < min) min = entry.Value; if (entry.Value > max) max = entry.Value; }
        if (double.IsInfinity(min)) { min = 0; max = 1; }
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var hasMissing = false;
        foreach (var region in definition.Regions) {
            if (!data.ContainsKey(region.Code)) { hasMissing = true; break; }
        }

        DrawRegionMapPngSurface(c, chart, map);
        foreach (var region in definition.Regions) {
            var hasValue = data.TryGetValue(region.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var color = hasValue ? HeatmapColor(chart, entry.Color ?? series.Color ?? t.Palette[0], value, min, max) : MapNoDataColor(chart);
            var rings = ProjectMapRings(region.Path, definition.Bounds, map, out var regionBounds);
            c.FillCompoundPolygon(rings, color);
            foreach (var points in rings) {
                for (var i = 0; i < points.Count; i++) {
                    var next = points[(i + 1) % points.Count];
                    c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, t.CardBackground, 1.1);
                }
            }
            if (chart.Options.ShowMapLabels) {
                var label = region.HasLabel ? ProjectMapPoint(region.Label, definition.Bounds, map) : new ChartPoint(regionBounds.Left + regionBounds.Width / 2, regionBounds.Top + regionBounds.Height / 2);
                var fontSize = Math.Min(PngTickFontSize(chart), Math.Max(7, map.Height * 0.032));
                if (ShouldDrawRegionMapLabel(region.Code, regionBounds, fontSize)) c.DrawTextEmphasized(label.X - EstimatePngEmphasizedTextWidth(region.Code, fontSize) / 2, label.Y - fontSize / 2, region.Code, HeatmapTextColor(color), fontSize);
            }
        }

        if (chart.Options.ShowMapScaleLegend) DrawRegionMapPngScale(c, chart, series, min, max, hasMissing, RegionMapScaleX(map, plot), plot.Bottom - 14, plot);
    }

    private static void DrawRegionMapPngSurface(RgbaCanvas c, Chart chart, ChartRect map) {
        var t = chart.Options.Theme;
        var pad = Math.Max(7, map.Height * 0.018);
        var radius = Math.Min(20, Math.Max(8, map.Height * 0.045));
        c.FillRoundedRect(map.Left - pad, map.Top - pad, map.Width + pad * 2, map.Height + pad * 2, radius, ApplyOpacity(Blend(t.PlotBackground, t.Grid, 0.20), 0.30));
        c.StrokeRoundedRect(map.Left - pad, map.Top - pad, map.Width + pad * 2, map.Height + pad * 2, radius, ApplyOpacity(t.PlotBorder, 0.24), 1);
    }

    private static void DrawRegionMapPngScale(RgbaCanvas c, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double x, double y, ChartRect plot) {
        var t = chart.Options.Theme;
        var size = 11.0;
        var gap = 3.0;
        var fontSize = PngTickFontSize(chart);
        if (hasMissing) DrawMapPngNoDataScale(c, chart, x, y, size, fontSize, plot);
        c.DrawText(x - EstimatePngTextWidth("Less", fontSize) - 8, y + size / 2 - fontSize / 2, "Less", t.MutedText, fontSize);
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var color = HeatmapColor(chart, series.Color ?? t.Palette[0], value, min, max);
            c.FillRoundedRect(x + i * (size + gap), y, size, size, 2, color);
        }
        c.DrawText(x + 5 * size + 4 * gap + 8, y + size / 2 - fontSize / 2, "More", t.MutedText, fontSize);
    }

    private static double RegionMapScaleX(ChartRect map, ChartRect plot) {
        const double scaleWidth = 5 * 11.0 + 4 * 3.0;
        return Clamp(map.Right - scaleWidth - 48, plot.Left + 70, plot.Right - scaleWidth - 44);
    }

    private static List<List<ChartPoint>> ProjectMapRings(string path, ChartRect source, ChartRect target, out ChartRect bounds) {
        var rings = new List<List<ChartPoint>>();
        List<ChartPoint>? current = null;
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        for (var i = 0; i < path.Length; i++) {
            var command = path[i];
            if (command == 'M' || command == 'L') {
                i++;
                var x = ReadMapPathNumber(path, ref i);
                var y = ReadMapPathNumber(path, ref i);
                if (command == 'M') {
                    current = new List<ChartPoint>();
                    rings.Add(current);
                }

                var point = ProjectMapPoint(new ChartPoint(x, y), source, target);
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
                current?.Add(point);
                i--;
            } else if (command == 'Z') {
                current = null;
            } else if (!char.IsWhiteSpace(command)) {
                throw new InvalidOperationException("Unsupported map path command.");
            }
        }

        for (var i = rings.Count - 1; i >= 0; i--) if (rings[i].Count < 3) rings.RemoveAt(i);
        bounds = double.IsInfinity(minX) ? new ChartRect(0, 0, 0, 0) : new ChartRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
        return rings;
    }

    private static bool ShouldDrawRegionMapLabel(string code, ChartRect bounds, double fontSize) {
        return bounds.Width >= EstimatePngEmphasizedTextWidth(code, fontSize) + 8 && bounds.Height >= fontSize + 5;
    }

    private static double ReadMapPathNumber(string path, ref int index) {
        while (index < path.Length && (char.IsWhiteSpace(path[index]) || path[index] == ',')) index++;
        var start = index;
        if (index < path.Length && (path[index] == '-' || path[index] == '+')) index++;
        var hasDigit = false;
        while (index < path.Length && char.IsDigit(path[index])) { index++; hasDigit = true; }
        if (index < path.Length && path[index] == '.') {
            index++;
            while (index < path.Length && char.IsDigit(path[index])) { index++; hasDigit = true; }
        }

        if (!hasDigit) throw new InvalidOperationException("Invalid map path number.");
        if (index < path.Length && (path[index] == 'e' || path[index] == 'E')) {
            var exponent = index;
            index++;
            if (index < path.Length && (path[index] == '-' || path[index] == '+')) index++;
            var hasExponentDigit = false;
            while (index < path.Length && char.IsDigit(path[index])) { index++; hasExponentDigit = true; }
            if (!hasExponentDigit) index = exponent;
        }

        return double.Parse(path.Substring(start, index - start), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static ChartPoint ProjectMapPoint(ChartPoint point, ChartRect source, ChartRect target) {
        var x = target.Left + (point.X - source.Left) / source.Width * target.Width;
        var y = target.Top + (point.Y - source.Top) / source.Height * target.Height;
        return new ChartPoint(x, y);
    }

    private static ChartRect FitRegionMap(ChartMapDefinition definition, ChartRect plot) {
        var aspect = definition.Bounds.Width / Math.Max(1, definition.Bounds.Height);
        var width = Math.Min(plot.Width, plot.Height * aspect);
        var height = width / aspect;
        if (height > plot.Height) {
            height = plot.Height;
            width = height * aspect;
        }

        return new ChartRect(plot.Left + Math.Max(0, (plot.Width - width) / 2), plot.Top + Math.Max(0, (plot.Height - height) / 2), width, height);
    }

    private static bool IsRegionMapChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.RegionMap) return true;
        return false;
    }
}
