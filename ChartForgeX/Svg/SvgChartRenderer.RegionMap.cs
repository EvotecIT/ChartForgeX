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
        var rightLegend = chart.Options.ShowMapScaleLegend && chart.Options.MapScaleLegendPosition == ChartMapScaleLegendPosition.Right;
        var bottomReserve = chart.Options.ShowMapScaleLegend && !rightLegend ? (ChartHeatmapSurface.MapMidpointLabel(chart) == null ? 46 : 62) : 12;
        var rightReserve = rightLegend ? 172 : 0;
        var plot = new ChartRect(basePlot.Left + 10, basePlot.Top + 8, Math.Max(1, basePlot.Width - 20 - rightReserve), Math.Max(1, basePlot.Height - bottomReserve));
        var sourceBounds = RegionMapSourceBounds(definition, chart);
        var map = FitRegionMap(sourceBounds, plot);
        var min = data.Count == 0 ? 0 : data.Values.Min(item => item.Value);
        var max = data.Count == 0 ? 1 : data.Values.Max(item => item.Value);
        var sourceMin = min;
        var sourceMax = max;
        if (Math.Abs(max - min) < 0.000001) max = min + 1;
        var regionStroke = chart.Options.MapRegionStrokeColor ?? t.CardBackground;
        var regionStrokeWidth = chart.Options.MapRegionStrokeWidth;
        var hasMissing = definition.Regions.Any(region => !data.ContainsKey(region.Code));
        var missingCount = definition.Regions.Count(region => !data.ContainsKey(region.Code));
        var containerSummary = series.Name + " " + summaryLabel + " with " + data.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " filled regions and " + missingCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + " missing regions";

        sb.AppendLine($"<g data-cfx-role=\"{rolePrefix}\" data-cfx-map-kind=\"{Escape(mapKind)}\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-region-count=\"{definition.Regions.Count}\" data-cfx-filled-region-count=\"{data.Count}\" data-cfx-missing-region-count=\"{missingCount}\" data-cfx-min-value=\"{F(sourceMin)}\" data-cfx-max-value=\"{F(sourceMax)}\" data-cfx-map-id=\"{Escape(definition.Id)}\" data-cfx-map-color-scale=\"{(chart.Options.MapColorScale == null ? "default" : "custom")}\" data-cfx-source-left=\"{F(sourceBounds.Left)}\" data-cfx-source-top=\"{F(sourceBounds.Top)}\" data-cfx-source-width=\"{F(sourceBounds.Width)}\" data-cfx-source-height=\"{F(sourceBounds.Height)}\" role=\"group\" aria-label=\"{Escape(containerSummary)}\">");
        if (chart.Options.ShowMapSurface) DrawRegionMapSvgSurface(sb, chart, map, rolePrefix);
        foreach (var layer in chart.Options.MapBaseLayers) DrawRegionMapSvgLayer(sb, layer, sourceBounds, map);
        foreach (var region in definition.Regions) {
            var hasValue = data.TryGetValue(region.Code, out var entry);
            var value = hasValue ? entry.Value : 0;
            var ratio = hasValue ? ChartHeatmapSurface.MapRatio(chart, value, min, max) : 0;
            var status = hasValue ? ChartHeatmapSurface.Status(ratio) : "empty";
            var color = hasValue ? ChartHeatmapSurface.MapColor(chart, entry.Color, series.Color ?? t.Palette[0], value, min, max) : ChartHeatmapSurface.MapNoDataColor(chart);
            var path = ScaleMapPath(region.Path, sourceBounds, map, out var regionBounds);
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
                    .Attribute("stroke", regionStroke.ToCss())
                    .Attribute("stroke-width", regionStrokeWidth);
                writer.EndStartElement();
                writer.StartElement("title").Text(summary).EndElement();
                writer.EndElement().Line();
            });
            if (chart.Options.ShowMapLabels) {
                var label = region.HasLabel ? ProjectMapPoint(region.Label, sourceBounds, map) : new ChartPoint(regionBounds.Left + regionBounds.Width / 2, regionBounds.Top + regionBounds.Height / 2);
                var fontSize = Math.Min(t.TickLabelFontSize, Math.Max(7, map.Height * 0.032));
                if (ShouldDrawRegionMapLabel(region.Code, regionBounds, fontSize)) DrawSvgTextCenteredX(sb, chart, rolePrefix + "-label", region.Code, label.X, label.Y + 3, ChartColorMath.TextOnBackground(color), fontSize, 34, "800");
            }
        }

        foreach (var layer in chart.Options.MapOverlayLayers) DrawRegionMapSvgLayer(sb, layer, sourceBounds, map);
        if (chart.Options.ShowMapScaleLegend) {
            if (rightLegend) DrawRegionMapSvgRightScale(sb, chart, series, min, max, hasMissing, Math.Min(basePlot.Right - 124, plot.Right + 52), map.Top + Math.Max(48, map.Height * 0.28), map, rolePrefix);
            else DrawRegionMapSvgScale(sb, chart, series, min, max, hasMissing, RegionMapScaleX(map, plot), plot.Bottom - 14, plot, rolePrefix);
        }
        sb.AppendLine("</g>");
    }

    private static void DrawRegionMapSvgLayer(StringBuilder sb, ChartMapLayer layer, ChartRect sourceBounds, ChartRect map) {
        sb.AppendLine($"<g data-cfx-role=\"{Escape(layer.Role)}\" data-cfx-map-id=\"{Escape(layer.Definition.Id)}\" data-cfx-region-count=\"{layer.Definition.Regions.Count}\">");
        foreach (var region in layer.Definition.Regions) {
            var path = ScaleMapPath(region.Path, sourceBounds, map, out _);
            AppendSvg(sb, 768, writer => {
                writer.StartElement("path")
                    .Attribute("data-cfx-role", layer.Role + "-region")
                    .Attribute("data-cfx-region", region.Code)
                    .Attribute("data-cfx-region-name", region.Name)
                    .Attribute("d", path)
                    .Attribute("fill-rule", "evenodd")
                    .Attribute("fill", layer.FillColor.HasValue ? layer.FillColor.Value.ToCss() : "none")
                    .Attribute("stroke", layer.StrokeColor.HasValue ? layer.StrokeColor.Value.ToCss() : "none")
                    .Attribute("stroke-width", layer.StrokeWidth);
                writer.EndEmptyElement().Line();
            });
        }

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
            .Attribute("fill", ChartColorMath.Blend(t.PlotBackground, t.Grid, 0.20).ToCss())
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
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-label").Attribute("x", x - 8).Attribute("y", y + size / 2).Attribute("text-anchor", "end").Attribute("dominant-baseline", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text(ChartHeatmapSurface.MapLowLabel(chart)).EndElement().Line());
        for (var i = 0; i < 5; i++) {
            var value = ChartHeatmapSurface.MapScaleValue(chart, min, max, i / 4.0);
            var ratio = ChartHeatmapSurface.MapRatio(chart, value, min, max);
            var color = ChartHeatmapSurface.MapColor(chart, null, series.Color ?? t.Palette[0], value, min, max);
            AppendSvg(sb, 256, writer => writer.StartElement("rect").Attribute("data-cfx-role", rolePrefix + "-scale-step").Attribute("data-cfx-value", value).Attribute("data-cfx-status", ChartHeatmapSurface.Status(ratio)).Attribute("x", x + i * (size + gap)).Attribute("y", y).Attribute("width", size).Attribute("height", size).Attribute("rx", "2").Attribute("fill", color.ToCss()).EndEmptyElement().Line());
        }
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-label").Attribute("x", x + 5 * size + 4 * gap + 8).Attribute("y", y + size / 2).Attribute("text-anchor", "start").Attribute("dominant-baseline", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text(ChartHeatmapSurface.MapHighLabel(chart)).EndElement().Line());
        var midpointLabel = ChartHeatmapSurface.MapMidpointLabel(chart);
        if (midpointLabel != null) {
            AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-midpoint-label").Attribute("data-cfx-value", ChartHeatmapSurface.MapScaleMidpoint(chart, min, max)).Attribute("x", x + 2 * (size + gap) + size / 2).Attribute("y", y + size + t.TickLabelFontSize + 2).Attribute("text-anchor", "middle").Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text(midpointLabel).EndElement().Line());
        }
    }

    private static void DrawRegionMapSvgRightScale(StringBuilder sb, Chart chart, ChartSeries series, double min, double max, bool hasMissing, double x, double y, ChartRect map, string rolePrefix) {
        var t = chart.Options.Theme;
        var width = 22.0;
        var height = Math.Min(240, Math.Max(150, map.Height * 0.34));
        var steps = 32;
        var stepHeight = height / steps;
        var titleLines = MapRightScaleTitleLines(series.Name);
        for (var i = 0; i < titleLines.Length; i++) {
            AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-title").Attribute("x", x).Attribute("y", y - 34 + i * 17).Attribute("fill", t.Text.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize + 1).Attribute("font-weight", "750").Text(titleLines[i]).EndElement().Line());
        }
        for (var i = 0; i < steps; i++) {
            var ratio = 1 - i / (double)(steps - 1);
            var value = ChartHeatmapSurface.MapScaleValue(chart, min, max, ratio);
            var color = ChartHeatmapSurface.MapColor(chart, null, series.Color ?? t.Palette[0], value, min, max);
            AppendSvg(sb, 256, writer => writer.StartElement("rect").Attribute("data-cfx-role", rolePrefix + "-scale-step").Attribute("data-cfx-value", value).Attribute("data-cfx-status", ChartHeatmapSurface.Status(ratio)).Attribute("x", x).Attribute("y", y + i * stepHeight).Attribute("width", width).Attribute("height", Math.Ceiling(stepHeight * 1000) / 1000 + 0.2).Attribute("fill", color.ToCss()).EndEmptyElement().Line());
        }

        AppendSvg(sb, 256, writer => writer.StartElement("rect").Attribute("data-cfx-role", rolePrefix + "-scale-border").Attribute("x", x).Attribute("y", y).Attribute("width", width).Attribute("height", height).Attribute("fill", "none").Attribute("stroke", t.PlotBorder.ToCss()).Attribute("stroke-width", "1").EndEmptyElement().Line());
        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-label").Attribute("x", x + width + 10).Attribute("y", y + 9).Attribute("fill", t.Text.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text(ChartHeatmapSurface.MapHighLabel(chart)).EndElement().Line());
        var midpointLabel = ChartHeatmapSurface.MapMidpointLabel(chart);
        if (midpointLabel != null) {
            var midpoint = ChartHeatmapSurface.MapScaleMidpoint(chart, min, max);
            var midpointRatio = ChartHeatmapSurface.MapRatio(chart, midpoint, min, max);
            AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-midpoint-label").Attribute("data-cfx-value", midpoint).Attribute("x", x + width + 10).Attribute("y", y + height * (1 - midpointRatio) + 4).Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text(midpointLabel).EndElement().Line());
        }

        AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-label").Attribute("x", x + width + 10).Attribute("y", y + height).Attribute("fill", t.Text.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text(ChartHeatmapSurface.MapLowLabel(chart)).EndElement().Line());
        if (hasMissing) {
            var missingY = y + height + 24;
            AppendSvg(sb, 256, writer => writer.StartElement("rect").Attribute("data-cfx-role", rolePrefix + "-scale-no-data").Attribute("data-cfx-status", "empty").Attribute("x", x).Attribute("y", missingY - 9).Attribute("width", 11).Attribute("height", 11).Attribute("rx", "2").Attribute("fill", ChartHeatmapSurface.MapNoDataColor(chart).ToCss()).EndEmptyElement().Line());
            AppendSvg(sb, 256, writer => writer.StartElement("text").Attribute("data-cfx-role", rolePrefix + "-scale-no-data-label").Attribute("x", x + 16).Attribute("y", missingY).Attribute("fill", t.MutedText.ToCss()).Attribute("font-family", SvgFontFamily(t.FontFamily)).Attribute("font-size", t.TickLabelFontSize).Text("No data").EndElement().Line());
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

    private static string ScaleMapPath(string path, ChartRect source, ChartRect target, out ChartRect bounds) {
        var sb = new StringBuilder();
        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;
        foreach (var ring in ChartMapPathParser.ParseRings(path)) {
            if (ring.Count == 0) continue;
            for (var i = 0; i < ring.Count; i++) {
                var point = ProjectMapPoint(ring[i], source, target);
                if (point.X < minX) minX = point.X;
                if (point.X > maxX) maxX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.Y > maxY) maxY = point.Y;
                sb.Append(i == 0 ? 'M' : 'L').Append(F(point.X)).Append(' ').Append(F(point.Y));
            }

            sb.Append('Z');
        }

        bounds = double.IsInfinity(minX) ? new ChartRect(0, 0, 0, 0) : new ChartRect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));
        return sb.ToString();
    }

    private static bool ShouldDrawRegionMapLabel(string code, ChartRect bounds, double fontSize) {
        return bounds.Width >= EstimateTextWidth(code, fontSize) + 8 && bounds.Height >= fontSize + 5;
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
