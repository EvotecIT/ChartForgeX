using System;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawRegionMap(StringBuilder sb, Chart chart, ChartRect basePlot) {
        var definition = chart.Options.RegionMapDefinition ?? throw new InvalidOperationException("Region maps require a map definition.");
        DrawRegionMap(sb, chart, basePlot, ChartSeriesKind.RegionMap, definition, "region-map", definition.Id, "region map");
    }

    private static void DrawRegionMap(StringBuilder sb, Chart chart, ChartRect basePlot, ChartSeriesKind kind, ChartMapDefinition definition, string rolePrefix, string mapKind, string summaryLabel) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == kind);
        if (series == null || series.Points.Count == 0) return;
        var data = MapValues(chart, series);
        var t = chart.Options.Theme;
        var bottomReserve = chart.Options.ShowMapScaleLegend ? 46 : 12;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 8, Math.Max(1, basePlot.Width - 20), Math.Max(1, basePlot.Height - bottomReserve));
        var map = FitRegionMap(definition, plot);
        var min = data.Count == 0 ? 0 : data.Values.Min(item => item.Value);
        var max = data.Count == 0 ? 1 : data.Values.Max(item => item.Value);
        var sourceMin = min;
        var sourceMax = max;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var hasMissing = definition.Regions.Any(region => !data.ContainsKey(region.Code));
        var missingCount = definition.Regions.Count(region => !data.ContainsKey(region.Code));
        var containerSummary = series.Name + " " + summaryLabel + " with " + data.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " filled regions and " + missingCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " missing regions";

        sb.AppendLine($"<g data-cfx-role=\"{rolePrefix}\" data-cfx-map-kind=\"{Escape(mapKind)}\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-region-count=\"{definition.Regions.Count}\" data-cfx-filled-region-count=\"{data.Count}\" data-cfx-missing-region-count=\"{missingCount}\" data-cfx-min-value=\"{F(sourceMin)}\" data-cfx-max-value=\"{F(sourceMax)}\" data-cfx-map-id=\"{Escape(definition.Id)}\" role=\"group\" aria-label=\"{Escape(containerSummary)}\">");
        DrawRegionMapSvgSurface(sb, chart, map, rolePrefix);
        foreach (var region in definition.Regions) {
            var hasValue = data.TryGetValue(region.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var ratio = hasValue ? HeatmapRatio(value, min, max) : 0;
            var status = hasValue ? HeatmapStatus(ratio) : "empty";
            var color = hasValue ? HeatmapColor(chart, entry.Color ?? series.Color ?? t.Palette[0], value, min, max) : MapNoDataColor(chart);
            var path = ScaleMapPath(region.Path, definition.Bounds, map, out var regionBounds);
            var summary = region.Name + " (" + region.Code + "): " + (hasValue ? FormatValue(chart, value) : "No data");
            AppendSvg(sb, 1024, writer => {
                writer.StartElement("path")
                    .Attribute("class", "cfx-interactive-region")
                    .Attribute("tabindex", "0")
                    .Attribute("focusable", "true")
                    .Attribute("data-cfx-role", rolePrefix + "-region")
                    .Attribute("data-cfx-region", region.Code)
                    .Attribute("data-cfx-region-name", region.Name)
                    .Attribute("data-cfx-value", value)
                    .Attribute("data-cfx-empty", !hasValue)
                    .Attribute("data-cfx-status", status)
                    .Attribute("role", "img")
                    .Attribute("aria-label", summary)
                    .Attribute("d", path)
                    .Attribute("fill-rule", "evenodd")
                    .Attribute("fill", color.ToCss())
                    .Attribute("stroke", t.CardBackground.ToCss())
                    .Attribute("stroke-width", "1.1");
                writer.EndStartElement();
                writer.StartElement("title").Text(summary).EndElement();
                writer.EndElement().Line();
            });
            if (chart.Options.ShowMapLabels) {
                var label = region.HasLabel ? ProjectMapPoint(region.Label, definition.Bounds, map) : new ChartPoint(regionBounds.Left + regionBounds.Width / 2, regionBounds.Top + regionBounds.Height / 2);
                var fontSize = Math.Min(t.TickLabelFontSize, Math.Max(7, map.Height * 0.032));
                if (ShouldDrawRegionMapLabel(region.Code, regionBounds, fontSize)) DrawSvgTextCenteredX(sb, chart, rolePrefix + "-label", region.Code, label.X, label.Y + 3, HeatmapTextColor(color), fontSize, 34, "800");
            }
        }

        if (chart.Options.ShowMapScaleLegend) DrawRegionMapSvgScale(sb, chart, series, min, max, hasMissing, RegionMapScaleX(map, plot), plot.Bottom - 14, plot, rolePrefix);
        sb.AppendLine("</g>");
    }

    private static void DrawRegionMapSvgSurface(StringBuilder sb, Chart chart, ChartRect map, string rolePrefix) {
        var t = chart.Options.Theme;
        var pad = Math.Max(7, map.Height * 0.018);
        AppendSvg(sb, 384, writer => writer.StartElement("rect")
            .Attribute("data-cfx-role", rolePrefix + "-surface")
            .Attribute("x", map.Left - pad)
            .Attribute("y", map.Top - pad)
            .Attribute("width", map.Width + pad * 2)
            .Attribute("height", map.Height + pad * 2)
            .Attribute("rx", Math.Min(20, Math.Max(8, map.Height * 0.045)))
            .Attribute("fill", Blend(t.PlotBackground, t.Grid, 0.20).ToCss())
            .Attribute("fill-opacity", "0.30")
            .Attribute("stroke", t.PlotBorder.ToCss())
            .Attribute("stroke-opacity", "0.24")
            .EndEmptyElement()
            .Line());
    }

    private static void DrawRegionMapSvgScale(StringBuilder sb, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double x, double y, ChartRect plot, string rolePrefix) {
        var t = chart.Options.Theme;
        var size = 11.0;
        var gap = 3.0;
        if (hasMissing) DrawMapSvgNoDataScale(sb, chart, rolePrefix, x, y, size, plot);
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-label").Attribute("x", x - 8).Attribute("y", y + size / 2).Attribute("text-anchor", "end").Attribute("dominant-baseline", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text("Less").EndElement().Line());
        for (var i = 0; i < 5; i++) {
            var value = min + (max - min) * (i / 4.0);
            var ratio = HeatmapRatio(value, min, max);
            var color = HeatmapColor(chart, series.Color ?? t.Palette[0], value, min, max);
            AppendSvg(sb, 256, writer => writer.StartElement("rect").Attribute("data-cfx-role", rolePrefix + "-scale-step").Attribute("data-cfx-value", value).Attribute("data-cfx-status", HeatmapStatus(ratio)).Attribute("x", x + i * (size + gap)).Attribute("y", y).Attribute("width", size).Attribute("height", size).Attribute("rx", "2").Attribute("fill", color.ToCss()).EndEmptyElement().Line());
        }
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-label").Attribute("x", x + 5 * size + 4 * gap + 8).Attribute("y", y + size / 2).Attribute("text-anchor", "start").Attribute("dominant-baseline", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text("More").EndElement().Line());
    }

    private static double RegionMapScaleX(ChartRect map, ChartRect plot) {
        const double scaleWidth = 5 * 11.0 + 4 * 3.0;
        return Clamp(map.Right - scaleWidth - 48, plot.Left + 70, plot.Right - scaleWidth - 44);
    }

    private static string ScaleMapPath(string path, ChartRect source, ChartRect target, out ChartRect bounds) {
        var sb = new StringBuilder();
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
                var point = ProjectMapPoint(new ChartPoint(x, y), source, target);
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
                sb.Append(command).Append(F(point.X)).Append(' ').Append(F(point.Y));
                i--;
            } else if (command == 'Z') {
                sb.Append('Z');
            } else if (!char.IsWhiteSpace(command)) {
                throw new InvalidOperationException("Unsupported map path command.");
            }
        }

        bounds = double.IsInfinity(minX) ? new ChartRect(0, 0, 0, 0) : new ChartRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
        return sb.ToString();
    }

    private static bool ShouldDrawRegionMapLabel(string code, ChartRect bounds, double fontSize) {
        return bounds.Width >= EstimateTextWidth(code, fontSize) + 8 && bounds.Height >= fontSize + 5;
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

    private static bool IsRegionMapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.RegionMap);
}
