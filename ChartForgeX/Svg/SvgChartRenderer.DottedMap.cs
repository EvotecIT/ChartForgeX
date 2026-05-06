using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Svg;

public sealed partial class SvgChartRenderer {
    private static void DrawDottedMap(StringBuilder sb, Chart chart, ChartRect basePlot, string id) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.DottedMap);
        if (series == null || series.Points.Count == 0) return;
        var t = chart.Options.Theme;
        var viewport = chart.Options.MapViewport;
        var plot = new ChartRect(basePlot.Left + 8, basePlot.Top + 8, Math.Max(1, basePlot.Width - 16), Math.Max(1, basePlot.Height - 16));
        var map = FitDottedMap(plot, viewport);
        var dot = DottedMapDotSize(map, viewport);
        var landColor = DottedMapLandDotColor(t.PlotBackground, t.MutedText);
        var landOpacity = DottedMapLandDotOpacity(t.PlotBackground);
        var reservedLabels = new List<ChartLabelBounds>();
        var visiblePoints = series.Points.Count(point => IsVisibleMapCoordinate(viewport, point.X, point.Y));
        var visibleConnectors = chart.Options.MapConnectors.Count(item => IsVisibleMapCoordinate(viewport, item.FromLongitude, item.FromLatitude) && IsVisibleMapCoordinate(viewport, item.ToLongitude, item.ToLatitude));
        var valuedPointCount = DottedMapValuedPointCount(series);
        var valueRange = GetDottedMapValueRange(series);
        var minLongitude = series.Points.Min(point => point.X);
        var maxLongitude = series.Points.Max(point => point.X);
        var minLatitude = series.Points.Min(point => point.Y);
        var maxLatitude = series.Points.Max(point => point.Y);
        var containerSummary = series.Name + " dotted map with " + series.Points.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " highlighted " + (series.Points.Count == 1 ? "point" : "points");

        var valueAttributes = valueRange.HasValue ? $" data-cfx-valued-point-count=\"{valuedPointCount}\" data-cfx-min-value=\"{F(valueRange.Value.Min)}\" data-cfx-max-value=\"{F(valueRange.Value.Max)}\"" : string.Empty;
        sb.AppendLine($"<g data-cfx-role=\"dotted-map\" data-cfx-map-kind=\"world-dotted\" data-cfx-label=\"{Escape(series.Name)}\" data-cfx-projection=\"equirectangular\" data-cfx-point-count=\"{series.Points.Count}\" data-cfx-visible-point-count=\"{visiblePoints}\" data-cfx-connector-count=\"{visibleConnectors}\"{valueAttributes} data-cfx-viewport=\"{Escape(viewport.Name)}\" data-cfx-viewport-min-longitude=\"{F(viewport.MinimumLongitude)}\" data-cfx-viewport-max-longitude=\"{F(viewport.MaximumLongitude)}\" data-cfx-viewport-min-latitude=\"{F(viewport.MinimumLatitude)}\" data-cfx-viewport-max-latitude=\"{F(viewport.MaximumLatitude)}\" data-cfx-min-longitude=\"{F(minLongitude)}\" data-cfx-max-longitude=\"{F(maxLongitude)}\" data-cfx-min-latitude=\"{F(minLatitude)}\" data-cfx-max-latitude=\"{F(maxLatitude)}\" role=\"group\" aria-label=\"{Escape(containerSummary)}\">");
        sb.AppendLine($"<g data-cfx-role=\"dotted-map-base-layer\" clip-path=\"url(#{id}-plotClip)\">");
        DrawDottedMapGraticule(sb, chart, viewport, map);
        DrawDottedMapViewportShape(sb, chart, viewport, map, dot);
        DrawDottedMapLandAreas(sb, chart, viewport, map);
        var landDots = DottedMapLandDots(viewport);
        var landOffsets = DottedMapLandOffsets(viewport);
        foreach (var land in landDots) {
            foreach (var offset in landOffsets) {
                var longitude = land.X + offset.X;
                var latitude = land.Y + offset.Y;
                if (!IsInsideMapViewport(viewport, longitude, latitude)) continue;
                if (!IsInsideDottedMapViewportShape(viewport, longitude, latitude)) continue;
                var x = ProjectMapX(map, viewport, longitude);
                var y = ProjectMapY(map, viewport, latitude);
                sb.AppendLine($"<circle data-cfx-role=\"dotted-map-land-dot\" cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(DottedMapLandDotRadius(dot, viewport))}\" fill=\"{landColor.ToCss()}\" opacity=\"{F(landOpacity)}\"/>");
            }
        }

        DrawDottedMapBoundaries(sb, chart, viewport, map, dot);
        DrawDottedMapConnectors(sb, chart, series, viewport, map, dot);
        sb.AppendLine("</g>");

        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            if (!IsVisibleMapCoordinate(viewport, point.X, point.Y)) continue;
            var color = i < series.PointColors.Count && series.PointColors[i].HasValue ? series.PointColors[i]!.Value : series.Color ?? t.Positive;
            var x = ProjectMapX(map, viewport, point.X);
            var y = ProjectMapY(map, viewport, point.Y);
            var label = DottedMapLabel(chart, i);
            var value = DottedMapPointValue(series, i);
            var pointRadius = DottedMapPointRadius(dot, value, valueRange);
            var haloRadius = Math.Max(dot * 2.45, pointRadius * 1.85);
            var latitude = DottedMapCoordinateText(point.Y, "N", "S");
            var longitude = DottedMapCoordinateText(point.X, "E", "W");
            var summary = value.HasValue ? label + ": " + FormatValue(chart, value.Value) + "; " + latitude + ", " + longitude : label + ": " + latitude + ", " + longitude;
            var valueAttribute = value.HasValue ? $" data-cfx-value=\"{F(value.Value)}\" data-cfx-formatted-value=\"{Escape(FormatValue(chart, value.Value))}\"" : string.Empty;
            sb.AppendLine($"<circle data-cfx-role=\"dotted-map-point-halo\" data-cfx-point=\"{i}\" cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(haloRadius)}\" fill=\"{color.ToCss()}\" opacity=\"0.18\"/>");
            sb.AppendLine($"<circle class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"dotted-map-point\" data-cfx-point=\"{i}\" data-cfx-label=\"{Escape(label)}\"{valueAttribute} data-cfx-longitude=\"{F(point.X)}\" data-cfx-latitude=\"{F(point.Y)}\" data-cfx-longitude-label=\"{Escape(longitude)}\" data-cfx-latitude-label=\"{Escape(latitude)}\" role=\"img\" aria-label=\"{Escape(summary)}\" cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"{F(pointRadius)}\" fill=\"{color.ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"{F(Math.Max(1, dot * 0.55))}\"><title>{Escape(summary)}</title></circle>");
            if (ShouldDrawDataLabels(chart, series)) DrawDottedMapDataLabel(sb, chart, series, DottedMapDisplayLabel(chart, series, i), i, x, y, Math.Max(dot, pointRadius), map, reservedLabels);
        }

        sb.AppendLine("</g>");
    }

    private static void DrawDottedMapGraticule(StringBuilder sb, Chart chart, ChartMapViewport viewport, ChartRect map) {
        var t = chart.Options.Theme;
        for (var i = 1; i < 4; i++) {
            var longitude = viewport.MinimumLongitude + (viewport.MaximumLongitude - viewport.MinimumLongitude) * i / 4.0;
            var x = ProjectMapX(map, viewport, longitude);
            sb.AppendLine($"<line data-cfx-role=\"dotted-map-graticule\" data-cfx-axis=\"longitude\" data-cfx-longitude=\"{F(longitude)}\" x1=\"{F(x)}\" y1=\"{F(map.Top)}\" x2=\"{F(x)}\" y2=\"{F(map.Bottom)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"0.8\" stroke-opacity=\"0.22\"/>");
        }

        for (var i = 1; i < 3; i++) {
            var latitude = viewport.MinimumLatitude + (viewport.MaximumLatitude - viewport.MinimumLatitude) * i / 3.0;
            var y = ProjectMapY(map, viewport, latitude);
            sb.AppendLine($"<line data-cfx-role=\"dotted-map-graticule\" data-cfx-axis=\"latitude\" data-cfx-latitude=\"{F(latitude)}\" x1=\"{F(map.Left)}\" y1=\"{F(y)}\" x2=\"{F(map.Right)}\" y2=\"{F(y)}\" stroke=\"{t.Grid.ToCss()}\" stroke-width=\"0.8\" stroke-opacity=\"0.18\"/>");
        }
    }

    private static void DrawDottedMapViewportShape(StringBuilder sb, Chart chart, ChartMapViewport viewport, ChartRect map, double dot) {
        var outlines = DottedMapViewportOutlines(viewport);
        if (outlines.Length == 0) return;
        var t = chart.Options.Theme;
        var color = DottedMapLandDotColor(t.PlotBackground, t.MutedText);
        for (var outlineIndex = 0; outlineIndex < outlines.Length; outlineIndex++) {
            var outline = outlines[outlineIndex];
            var path = new StringBuilder();
            for (var i = 0; i < outline.Length; i++) {
                var point = outline[i];
                var x = ProjectMapX(map, viewport, point.X);
                var y = ProjectMapY(map, viewport, point.Y);
                path.Append(i == 0 ? "M " : " L ");
                path.Append(F(x)).Append(' ').Append(F(y));
            }

            path.Append(" Z");
            sb.AppendLine($"<path data-cfx-role=\"dotted-map-viewport-shape\" data-cfx-viewport=\"{Escape(viewport.Name)}\" data-cfx-shape=\"{outlineIndex}\" d=\"{path}\" fill=\"{color.ToCss()}\" fill-opacity=\"0.10\" stroke=\"{color.ToCss()}\" stroke-opacity=\"0.46\" stroke-width=\"{F(Math.Max(1.2, dot * 0.42))}\"/>");
        }
    }

    private static void DrawDottedMapConnectors(StringBuilder sb, Chart chart, ChartSeries series, ChartMapViewport viewport, ChartRect map, double dot) {
        var t = chart.Options.Theme;
        var valueRange = GetDottedMapValueRange(series);
        for (var i = 0; i < chart.Options.MapConnectors.Count; i++) {
            var connector = chart.Options.MapConnectors[i];
            if (!IsVisibleMapCoordinate(viewport, connector.FromLongitude, connector.FromLatitude) || !IsVisibleMapCoordinate(viewport, connector.ToLongitude, connector.ToLatitude)) continue;
            var x1 = ProjectMapX(map, viewport, connector.FromLongitude);
            var y1 = ProjectMapY(map, viewport, connector.FromLatitude);
            var x2 = ProjectMapX(map, viewport, connector.ToLongitude);
            var y2 = ProjectMapY(map, viewport, connector.ToLatitude);
            var color = connector.Color ?? series.Color ?? t.Warning;
            var control = DottedMapConnectorControlPoint(x1, y1, x2, y2, map, dot);
            var fromTrim = DottedMapEndpointTrim(series, viewport, connector.FromLongitude, connector.FromLatitude, dot, valueRange);
            var toTrim = DottedMapEndpointTrim(series, viewport, connector.ToLongitude, connector.ToLatitude, dot, valueRange);
            var renderedFrom = MoveDottedMapConnectorEndpoint(x1, y1, control.X, control.Y, fromTrim);
            var renderedTo = MoveDottedMapConnectorEndpoint(x2, y2, control.X, control.Y, toTrim);
            var renderedD = $"M {F(renderedFrom.X)} {F(renderedFrom.Y)} Q {F(control.X)} {F(control.Y)} {F(renderedTo.X)} {F(renderedTo.Y)}";
            var summary = connector.Label + ": " + DottedMapCoordinateText(connector.FromLatitude, "N", "S") + ", " + DottedMapCoordinateText(connector.FromLongitude, "E", "W") + " to " + DottedMapCoordinateText(connector.ToLatitude, "N", "S") + ", " + DottedMapCoordinateText(connector.ToLongitude, "E", "W");
            var strokeWidth = Math.Max(1.2, dot * 0.78);
            var focusStrokeWidth = strokeWidth + Math.Max(1.4, dot * 0.5);
            sb.AppendLine($"<path data-cfx-role=\"dotted-map-connector-halo\" data-cfx-connector=\"{i}\" d=\"{renderedD}\" fill=\"none\" stroke=\"{t.PlotBackground.ToCss()}\" stroke-width=\"{F(strokeWidth + 3.2)}\" stroke-opacity=\"0.72\" stroke-linecap=\"round\"/>");
            sb.AppendLine($"<path class=\"cfx-interactive-region\" tabindex=\"0\" focusable=\"true\" data-cfx-role=\"dotted-map-connector\" data-cfx-connector=\"{i}\" data-cfx-label=\"{Escape(connector.Label)}\" data-cfx-from-longitude=\"{F(connector.FromLongitude)}\" data-cfx-from-latitude=\"{F(connector.FromLatitude)}\" data-cfx-to-longitude=\"{F(connector.ToLongitude)}\" data-cfx-to-latitude=\"{F(connector.ToLatitude)}\" data-cfx-rendered-from-x=\"{F(renderedFrom.X)}\" data-cfx-rendered-from-y=\"{F(renderedFrom.Y)}\" data-cfx-rendered-to-x=\"{F(renderedTo.X)}\" data-cfx-rendered-to-y=\"{F(renderedTo.Y)}\" data-cfx-control-x=\"{F(control.X)}\" data-cfx-control-y=\"{F(control.Y)}\" style=\"--cfx-interactive-focus-stroke-width:{F(focusStrokeWidth)}\" role=\"img\" aria-label=\"{Escape(summary)}\" d=\"{renderedD}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(strokeWidth)}\" stroke-opacity=\"0.72\" stroke-linecap=\"round\"><title>{Escape(summary)}</title></path>");
            sb.AppendLine($"<path data-cfx-role=\"dotted-map-connector-arrow\" data-cfx-connector=\"{i}\" data-cfx-label=\"{Escape(connector.Label)}\" d=\"{BuildDottedMapConnectorArrowPath(renderedFrom.X, renderedFrom.Y, control.X, control.Y, renderedTo.X, renderedTo.Y, dot)}\" fill=\"{color.ToCss()}\" fill-opacity=\"0.78\" stroke=\"{t.PlotBackground.ToCss()}\" stroke-opacity=\"0.78\" stroke-width=\"{F(Math.Max(0.8, strokeWidth * 0.48))}\" stroke-linejoin=\"round\"><title>{Escape(summary)}</title></path>");
            if (ShouldDrawDataLabels(chart, series)) {
                var labelPoint = DottedMapConnectorPoint(renderedFrom.X, renderedFrom.Y, control.X, control.Y, renderedTo.X, renderedTo.Y, 0.5);
                var label = CompactDottedMapConnectorLabel(connector.Label);
                sb.AppendLine($"<text data-cfx-role=\"dotted-map-connector-label\" data-cfx-connector=\"{i}\" data-cfx-label=\"{Escape(connector.Label)}\" x=\"{F(labelPoint.X)}\" y=\"{F(labelPoint.Y - Math.Max(7, dot * 2.2))}\" text-anchor=\"middle\" fill=\"{color.ToCss()}\" stroke=\"{t.PlotBackground.ToCss()}\" stroke-width=\"3\" paint-order=\"stroke\" font-size=\"12\" font-weight=\"700\">{Escape(label)}</text>");
            }
        }
    }

    private static ChartPoint DottedMapConnectorPoint(double x1, double y1, double controlX, double controlY, double x2, double y2, double t) {
        var oneMinus = 1 - t;
        return new ChartPoint(
            oneMinus * oneMinus * x1 + 2 * oneMinus * t * controlX + t * t * x2,
            oneMinus * oneMinus * y1 + 2 * oneMinus * t * controlY + t * t * y2);
    }

    private static string CompactDottedMapConnectorLabel(string label) {
        var ms = label.LastIndexOf(" ms", StringComparison.OrdinalIgnoreCase);
        if (ms > 0) {
            var start = label.LastIndexOf(' ', ms - 1);
            if (start >= 0 && ms + 3 <= label.Length) return label.Substring(start + 1, ms - start + 2);
        }

        return label.Length <= 24 ? label : label.Substring(0, 21) + "...";
    }

    private static ChartPoint DottedMapConnectorControlPoint(double x1, double y1, double x2, double y2, ChartRect map, double dot) {
        var dx = x2 - x1;
        var dy = y2 - y1;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.000001) return new ChartPoint((x1 + x2) / 2, (y1 + y2) / 2);

        var midX = (x1 + x2) / 2;
        var midY = (y1 + y2) / 2;
        var perpendicularX = dy / length;
        var perpendicularY = -dx / length;
        if (Math.Abs(dx) >= Math.Abs(dy)) {
            if (perpendicularY > 0) {
                perpendicularX = -perpendicularX;
                perpendicularY = -perpendicularY;
            }
        } else {
            var mapCenterX = map.Left + map.Width / 2;
            if ((midX < mapCenterX && perpendicularX < 0) || (midX >= mapCenterX && perpendicularX > 0)) {
                perpendicularX = -perpendicularX;
                perpendicularY = -perpendicularY;
            }
        }

        var maximumCurve = Math.Min(68, Math.Max(18, Math.Min(map.Width, map.Height) * 0.18));
        var curve = Clamp(length * 0.16, Math.Max(10, dot * 5.5), maximumCurve);
        return new ChartPoint(
            Clamp(midX + perpendicularX * curve, map.Left + 4, map.Right - 4),
            Clamp(midY + perpendicularY * curve, map.Top + 4, map.Bottom - 4));
    }

    private static double DottedMapEndpointTrim(ChartSeries series, ChartMapViewport viewport, double longitude, double latitude, double dot, DottedMapValueRange? valueRange) {
        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            if (!IsVisibleMapCoordinate(viewport, point.X, point.Y)) continue;
            if (Math.Abs(point.X - longitude) > 0.000001 || Math.Abs(point.Y - latitude) > 0.000001) continue;
            var radius = DottedMapPointRadius(dot, DottedMapPointValue(series, i), valueRange);
            return radius + Math.Max(1, dot * 0.55) + Math.Max(1.5, dot * 0.45);
        }

        return 0;
    }

    private static ChartPoint MoveDottedMapConnectorEndpoint(double x, double y, double targetX, double targetY, double distance) {
        if (distance <= 0) return new ChartPoint(x, y);
        var dx = targetX - x;
        var dy = targetY - y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.000001) return new ChartPoint(x, y);
        var shift = Math.Min(distance, length * 0.42);
        return new ChartPoint(x + dx / length * shift, y + dy / length * shift);
    }

    private static string BuildDottedMapConnectorArrowPath(double x1, double y1, double controlX, double controlY, double x2, double y2, double dot) {
        var points = DottedMapConnectorArrowPoints(x1, y1, controlX, controlY, x2, y2, dot);
        return "M " + F(points[0].X) + " " + F(points[0].Y) + " L " + F(points[1].X) + " " + F(points[1].Y) + " L " + F(points[2].X) + " " + F(points[2].Y) + " Z";
    }

    private static ChartPoint[] DottedMapConnectorArrowPoints(double x1, double y1, double controlX, double controlY, double x2, double y2, double dot) {
        const double t = 0.63;
        var oneMinus = 1 - t;
        var tipX = oneMinus * oneMinus * x1 + 2 * oneMinus * t * controlX + t * t * x2;
        var tipY = oneMinus * oneMinus * y1 + 2 * oneMinus * t * controlY + t * t * y2;
        var dx = 2 * oneMinus * (controlX - x1) + 2 * t * (x2 - controlX);
        var dy = 2 * oneMinus * (controlY - y1) + 2 * t * (y2 - controlY);
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.000001) {
            dx = x2 - x1;
            dy = y2 - y1;
            length = Math.Max(0.000001, Math.Sqrt(dx * dx + dy * dy));
        }

        var unitX = dx / length;
        var unitY = dy / length;
        var arrowLength = Math.Max(10, dot * 4.8);
        var arrowWidth = Math.Max(6, dot * 2.4);
        var baseX = tipX - unitX * arrowLength;
        var baseY = tipY - unitY * arrowLength;
        var perpX = -unitY;
        var perpY = unitX;
        return new[] {
            new ChartPoint(tipX, tipY),
            new ChartPoint(baseX + perpX * arrowWidth / 2, baseY + perpY * arrowWidth / 2),
            new ChartPoint(baseX - perpX * arrowWidth / 2, baseY - perpY * arrowWidth / 2)
        };
    }

    private static void DrawDottedMapBoundaries(StringBuilder sb, Chart chart, ChartMapViewport viewport, ChartRect map, double dot) {
        var boundaries = DottedMapBoundaryLines(viewport);
        if (boundaries.Length == 0) return;
        var t = chart.Options.Theme;
        var color = DottedMapBoundaryColor(t.PlotBackground, t.MutedText);
        var opacity = DottedMapBoundaryOpacity(t.PlotBackground);
        var strokeWidth = Math.Max(0.65, dot * 0.26);
        for (var lineIndex = 0; lineIndex < boundaries.Length; lineIndex++) {
            var line = boundaries[lineIndex];
            if (line.Length < 2) continue;
            var path = new StringBuilder();
            for (var i = 0; i < line.Length; i++) {
                var point = line[i];
                path.Append(i == 0 ? "M " : " L ");
                path.Append(F(ProjectMapX(map, viewport, point.X))).Append(' ').Append(F(ProjectMapY(map, viewport, point.Y)));
            }

            sb.AppendLine($"<path data-cfx-role=\"dotted-map-boundary\" data-cfx-viewport=\"{Escape(viewport.Name)}\" data-cfx-boundary=\"{lineIndex}\" d=\"{path}\" fill=\"none\" stroke=\"{color.ToCss()}\" stroke-width=\"{F(strokeWidth)}\" stroke-opacity=\"{F(opacity)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
        }
    }

    private static void DrawDottedMapLandAreas(StringBuilder sb, Chart chart, ChartMapViewport viewport, ChartRect map) {
        var boundaries = DottedMapBoundaryLines(viewport);
        if (boundaries.Length == 0) return;
        var t = chart.Options.Theme;
        var color = DottedMapLandAreaColor(t.PlotBackground, t.MutedText);
        var opacity = DottedMapLandAreaOpacity(t.PlotBackground);
        for (var lineIndex = 0; lineIndex < boundaries.Length; lineIndex++) {
            var line = boundaries[lineIndex];
            if (!CanFillDottedMapBoundary(line)) continue;
            var path = new StringBuilder();
            for (var i = 0; i < line.Length; i++) {
                var point = line[i];
                path.Append(i == 0 ? "M " : " L ");
                path.Append(F(ProjectMapX(map, viewport, point.X))).Append(' ').Append(F(ProjectMapY(map, viewport, point.Y)));
            }

            path.Append(" Z");
            sb.AppendLine($"<path data-cfx-role=\"dotted-map-land-area\" data-cfx-viewport=\"{Escape(viewport.Name)}\" data-cfx-boundary=\"{lineIndex}\" d=\"{path}\" fill=\"{color.ToCss()}\" fill-opacity=\"{F(opacity)}\"/>");
        }
    }

    private static string DottedMapLabel(Chart chart, int index) {
        return index < chart.Options.XAxisLabels.Count ? chart.Options.XAxisLabels[index].Text : (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string DottedMapDisplayLabel(Chart chart, ChartSeries series, int index) {
        var label = DottedMapLabel(chart, index);
        var value = DottedMapPointValue(series, index);
        return value.HasValue ? label + " " + FormatValue(chart, value.Value) : label;
    }

    private static double? DottedMapPointValue(ChartSeries series, int index) {
        return index < series.PointValues.Count ? series.PointValues[index] : null;
    }

    private static int DottedMapValuedPointCount(ChartSeries series) {
        var count = 0;
        for (var i = 0; i < series.PointValues.Count; i++) if (series.PointValues[i].HasValue) count++;
        return count;
    }

    private static DottedMapValueRange? GetDottedMapValueRange(ChartSeries series) {
        double? min = null;
        double? max = null;
        for (var i = 0; i < series.PointValues.Count; i++) {
            var value = series.PointValues[i];
            if (!value.HasValue) continue;
            min = !min.HasValue || value.Value < min.Value ? value.Value : min;
            max = !max.HasValue || value.Value > max.Value ? value.Value : max;
        }

        return min.HasValue && max.HasValue ? new DottedMapValueRange(min.Value, max.Value) : null;
    }

    private static ChartColor DottedMapLandDotColor(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightDottedMapSurface(plotBackground) ? 0.62 : 0.58;
        return Blend(plotBackground, mutedText, weight);
    }

    private static double DottedMapLandDotOpacity(ChartColor plotBackground) =>
        IsLightDottedMapSurface(plotBackground) ? 0.28 : 0.58;

    private static double DottedMapLandDotRadius(double dot, ChartMapViewport viewport) =>
        DottedMapBoundaryLines(viewport).Length == 0 ? dot / 2 : Math.Max(0.8, dot * 0.34);

    private static ChartColor DottedMapLandAreaColor(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightDottedMapSurface(plotBackground) ? 0.24 : 0.28;
        return Blend(plotBackground, mutedText, weight);
    }

    private static double DottedMapLandAreaOpacity(ChartColor plotBackground) =>
        IsLightDottedMapSurface(plotBackground) ? 0.92 : 0.30;

    private static ChartColor DottedMapBoundaryColor(ChartColor plotBackground, ChartColor mutedText) {
        var weight = IsLightDottedMapSurface(plotBackground) ? 0.68 : 0.70;
        return Blend(plotBackground, mutedText, weight);
    }

    private static double DottedMapBoundaryOpacity(ChartColor plotBackground) =>
        IsLightDottedMapSurface(plotBackground) ? 0.16 : 0.22;

    private static bool IsLightDottedMapSurface(ChartColor color) =>
        (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0 > 0.70;

    private static double DottedMapPointRadius(double dot, double? value, DottedMapValueRange? range) {
        const double minimumReadablePointRadius = 3;
        if (!value.HasValue || !range.HasValue) return Math.Max(minimumReadablePointRadius, dot * 1.35);
        var min = range.Value.Min;
        var max = Math.Abs(range.Value.Max - min) < 0.000001 ? min + 1 : range.Value.Max;
        var ratio = Clamp((value.Value - min) / (max - min), 0, 1);
        return Math.Max(minimumReadablePointRadius, dot * (1.15 + ratio * 1.05));
    }

    private static string DottedMapCoordinateText(double value, string positiveSuffix, string negativeSuffix) {
        var suffix = value < 0 ? negativeSuffix : positiveSuffix;
        return Math.Abs(value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " " + suffix;
    }

    private static void DrawDottedMapDataLabel(StringBuilder sb, Chart chart, ChartSeries series, string label, int pointIndex, double x, double y, double dot, ChartRect map, List<ChartLabelBounds> reservedLabels) {
        var t = chart.Options.Theme;
        var style = DataLabelStyle(chart, series, pointIndex);
        var fontSize = StyleFontSize(style, t.DataLabelFontSize);
        label = TrimSvgLabelToWidth(label, fontSize, Math.Min(132, PlotLabelMaxWidth(map)));
        if (label.Length == 0) return;

        var offset = Math.Max(16, dot * 5.6);
        var candidates = new[] {
            new DottedMapLabelCandidate("top", x, y - offset, "middle"),
            new DottedMapLabelCandidate("right", x + offset, y, "start"),
            new DottedMapLabelCandidate("bottom", x, y + offset, "middle"),
            new DottedMapLabelCandidate("left", x - offset, y, "end"),
            new DottedMapLabelCandidate("top-right", x + offset * 0.82, y - offset * 0.82, "start"),
            new DottedMapLabelCandidate("bottom-right", x + offset * 0.82, y + offset * 0.82, "start"),
            new DottedMapLabelCandidate("bottom-left", x - offset * 0.82, y + offset * 0.82, "end"),
            new DottedMapLabelCandidate("top-left", x - offset * 0.82, y - offset * 0.82, "end"),
            new DottedMapLabelCandidate("far-top-right", x + offset * 1.32, y - offset * 2.0, "start"),
            new DottedMapLabelCandidate("far-bottom-right", x + offset * 1.32, y + offset * 2.0, "start"),
            new DottedMapLabelCandidate("far-bottom-left", x - offset * 1.32, y + offset * 2.0, "end"),
            new DottedMapLabelCandidate("far-top-left", x - offset * 1.32, y - offset * 2.0, "end")
        };

        var width = EstimateTextWidth(label, fontSize) + 8;
        var height = fontSize + 6;
        DottedMapLabelPlacement? fallback = null;
        foreach (var candidate in candidates) {
            var placement = PlaceDottedMapLabel(label, candidate, map, fontSize, width, height);
            fallback ??= placement;
            if (DottedMapLabelTouchesPoint(placement.Bounds, x, y, dot)) continue;
            var collides = false;
            foreach (var item in reservedLabels) {
                if (placement.Bounds.Intersects(item)) {
                    collides = true;
                    break;
                }
            }

            if (collides) continue;
            reservedLabels.Add(placement.Bounds);
            DrawDottedMapDataLabel(sb, chart, style, label, placement, x, y, dot);
            return;
        }

        if (fallback.HasValue) {
            DrawDottedMapDataLabel(sb, chart, style, label, fallback.Value, x, y, dot);
        }
    }

    private static bool DottedMapLabelTouchesPoint(ChartLabelBounds bounds, double x, double y, double dot) {
        var guard = Math.Max(5, dot * 1.75);
        return bounds.Intersects(new ChartLabelBounds(x - guard, y - guard, guard * 2, guard * 2));
    }

    private static DottedMapLabelPlacement PlaceDottedMapLabel(string label, DottedMapLabelCandidate candidate, ChartRect map, double fontSize, double width, double height) {
        var inset = ChartVisualPrimitives.DataLabelPlotInset;
        var anchor = candidate.Anchor;
        var safeX = candidate.X;
        if (anchor == "middle") {
            safeX = EdgeAwareTextX(label, candidate.X, map, fontSize);
            anchor = EdgeAwareAnchor(label, candidate.X, map, fontSize);
        } else if (anchor == "start") {
            safeX = Clamp(candidate.X, map.Left + inset, map.Right - width - inset);
            if (safeX < map.Left + inset) safeX = map.Left + inset;
            if (safeX > map.Right - inset) {
                safeX = map.Right - inset;
                anchor = "end";
            }
        } else {
            safeX = Clamp(candidate.X, map.Left + width + inset, map.Right - inset);
            if (safeX > map.Right - inset) safeX = map.Right - inset;
            if (safeX < map.Left + inset) {
                safeX = map.Left + inset;
                anchor = "start";
            }
        }

        var safeY = Clamp(candidate.Y, map.Top + inset + height / 2, map.Bottom - inset - height / 2);
        var left = anchor == "end" ? safeX - width : anchor == "start" ? safeX : safeX - width / 2;
        return new DottedMapLabelPlacement(candidate.Placement, safeX, safeY, anchor, new ChartLabelBounds(left, safeY - height / 2, width, height));
    }

    private static void DrawDottedMapDataLabel(StringBuilder sb, Chart chart, ChartTextStyle style, string label, DottedMapLabelPlacement placement, double pointX, double pointY, double dot) {
        var t = chart.Options.Theme;
        var radius = Math.Min(6, placement.Bounds.Height / 2);
        DrawDottedMapLabelLeader(sb, chart, label, placement, pointX, pointY, dot);
        sb.AppendLine($"<rect data-cfx-role=\"dotted-map-label-backdrop\" data-cfx-label=\"{Escape(label)}\" data-cfx-placement=\"{placement.Placement}\" x=\"{F(placement.Bounds.X)}\" y=\"{F(placement.Bounds.Y)}\" width=\"{F(placement.Bounds.Width)}\" height=\"{F(placement.Bounds.Height)}\" rx=\"{F(radius)}\" fill=\"{t.CardBackground.ToCss()}\" fill-opacity=\"0.86\" stroke=\"{t.PlotBorder.ToCss()}\" stroke-opacity=\"0.46\"/>");
        sb.AppendLine($"<text data-cfx-role=\"dotted-map-label\" data-cfx-label=\"{Escape(label)}\" data-cfx-placement=\"{placement.Placement}\" x=\"{F(placement.X)}\" y=\"{F(placement.Y)}\" text-anchor=\"{placement.Anchor}\" dominant-baseline=\"middle\" fill=\"{StyleColor(style, t.Text).ToCss()}\" stroke=\"{t.CardBackground.ToCss()}\" stroke-width=\"1.2\" paint-order=\"stroke fill\" stroke-linejoin=\"round\" font-family=\"{SvgFontFamily(StyleFontFamily(chart, style))}\" font-size=\"{F(StyleFontSize(style, t.DataLabelFontSize))}\" font-weight=\"{StyleWeight(style, "700")}\"{SvgTextStyleAttributes(style)}>{Escape(label)}</text>");
    }

    private static void DrawDottedMapLabelLeader(StringBuilder sb, Chart chart, string label, DottedMapLabelPlacement placement, double pointX, double pointY, double dot) {
        var end = DottedMapLabelLeaderEndpoint(placement.Bounds, pointX, pointY);
        var dx = end.X - pointX;
        var dy = end.Y - pointY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < Math.Max(8, dot * 2.1)) return;

        var startGap = Math.Min(length * 0.42, Math.Max(dot * 1.32, 4));
        var startX = pointX + dx / length * startGap;
        var startY = pointY + dy / length * startGap;
        var t = chart.Options.Theme;
        sb.AppendLine($"<line data-cfx-role=\"dotted-map-label-leader\" data-cfx-label=\"{Escape(label)}\" data-cfx-placement=\"{placement.Placement}\" x1=\"{F(startX)}\" y1=\"{F(startY)}\" x2=\"{F(end.X)}\" y2=\"{F(end.Y)}\" stroke=\"{DottedMapBoundaryColor(t.PlotBackground, t.MutedText).ToCss()}\" stroke-opacity=\"0.52\" stroke-width=\"{F(Math.Max(0.85, dot * 0.22))}\" stroke-linecap=\"round\"/>");
    }

    private static ChartPoint DottedMapLabelLeaderEndpoint(ChartLabelBounds bounds, double pointX, double pointY) {
        var centerX = bounds.X + bounds.Width / 2;
        var centerY = bounds.Y + bounds.Height / 2;
        var dx = centerX - pointX;
        var dy = centerY - pointY;
        if (Math.Abs(dx) < 0.000001 && Math.Abs(dy) < 0.000001) return new ChartPoint(centerX, centerY);

        var t = double.PositiveInfinity;
        if (Math.Abs(dx) >= 0.000001) {
            var edgeX = dx > 0 ? bounds.X : bounds.X + bounds.Width;
            var candidate = (edgeX - pointX) / dx;
            if (candidate > 0) t = Math.Min(t, candidate);
        }

        if (Math.Abs(dy) >= 0.000001) {
            var edgeY = dy > 0 ? bounds.Y : bounds.Y + bounds.Height;
            var candidate = (edgeY - pointY) / dy;
            if (candidate > 0) t = Math.Min(t, candidate);
        }

        if (double.IsInfinity(t)) return new ChartPoint(centerX, centerY);
        return new ChartPoint(pointX + dx * t, pointY + dy * t);
    }

    private static double ProjectMapX(ChartRect plot, ChartMapViewport viewport, double longitude) {
        var clamped = Clamp(longitude, viewport.MinimumLongitude, viewport.MaximumLongitude);
        return plot.Left + (clamped - viewport.MinimumLongitude) / (viewport.MaximumLongitude - viewport.MinimumLongitude) * plot.Width;
    }

    private static double ProjectMapY(ChartRect plot, ChartMapViewport viewport, double latitude) {
        var clamped = Clamp(latitude, viewport.MinimumLatitude, viewport.MaximumLatitude);
        return plot.Top + (viewport.MaximumLatitude - clamped) / (viewport.MaximumLatitude - viewport.MinimumLatitude) * plot.Height;
    }

    private static ChartRect FitDottedMap(ChartRect plot, ChartMapViewport viewport) {
        var aspect = (viewport.MaximumLongitude - viewport.MinimumLongitude) / (viewport.MaximumLatitude - viewport.MinimumLatitude);
        var width = Math.Min(plot.Width, plot.Height * aspect);
        var height = width / aspect;
        if (height > plot.Height) {
            height = plot.Height;
            width = height * aspect;
        }

        return new ChartRect(plot.Left + Math.Max(0, (plot.Width - width) / 2), plot.Top + Math.Max(0, (plot.Height - height) / 2), width, height);
    }

    private static double DottedMapDotSize(ChartRect map, ChartMapViewport viewport) {
        const double sourceDotStep = 3.5;
        var longitudeSlots = Math.Max(1, (viewport.MaximumLongitude - viewport.MinimumLongitude) / sourceDotStep);
        var latitudeSlots = Math.Max(1, (viewport.MaximumLatitude - viewport.MinimumLatitude) / sourceDotStep);
        var projectedStep = Math.Min(map.Width / longitudeSlots, map.Height / latitudeSlots);
        var maximum = IsWorldMapViewport(viewport) || IsEuropeDottedMapViewport(viewport) || IsPolandDottedMapViewport(viewport) ? 3.6 : 5.2;
        return Clamp(projectedStep * 0.42, 1.4, maximum);
    }

    private static ChartPoint[] DottedMapLandOffsets(ChartMapViewport viewport) {
        if (IsWorldMapViewport(viewport)) return DottedMapWorldLandOffsets;
        if (IsEuropeDottedMapViewport(viewport)) return DottedMapWorldLandOffsets;
        if (IsPolandDottedMapViewport(viewport)) return DottedMapWorldLandOffsets;
        var longitudeSpan = viewport.MaximumLongitude - viewport.MinimumLongitude;
        var latitudeSpan = viewport.MaximumLatitude - viewport.MinimumLatitude;
        if (longitudeSpan >= 45 || latitudeSpan >= 30) return DottedMapWorldLandOffsets;
        return longitudeSpan <= 30 || latitudeSpan <= 18 ? DottedMapCountryLandOffsets : DottedMapRegionalLandOffsets;
    }

    private static ChartPoint[] DottedMapLandDots(ChartMapViewport viewport) =>
        IsPolandDottedMapViewport(viewport) ? WorldMapDots.PolandLand :
        IsNorthAmericaDottedMapViewport(viewport) ? WorldMapDots.NorthAmericaLand :
        IsSouthAmericaDottedMapViewport(viewport) ? WorldMapDots.SouthAmericaLand :
        IsAfricaDottedMapViewport(viewport) ? WorldMapDots.AfricaLand :
        IsAsiaDottedMapViewport(viewport) ? WorldMapDots.AsiaLand :
        IsOceaniaDottedMapViewport(viewport) ? WorldMapDots.OceaniaLand :
        IsEuropeDottedMapViewport(viewport) ? WorldMapDots.EuropeLand : WorldMapDots.Land;

    private static ChartPoint[][] DottedMapViewportOutlines(ChartMapViewport viewport) {
        if (IsPolandDottedMapViewport(viewport)) return new[] { WorldMapDots.PolandOutline };
        return Array.Empty<ChartPoint[]>();
    }

    private static ChartPoint[][] DottedMapBoundaryLines(ChartMapViewport viewport) =>
        IsNorthAmericaDottedMapViewport(viewport) ? WorldMapDots.NorthAmericaBoundaries :
        IsSouthAmericaDottedMapViewport(viewport) ? WorldMapDots.SouthAmericaBoundaries :
        IsAfricaDottedMapViewport(viewport) ? WorldMapDots.AfricaBoundaries :
        IsAsiaDottedMapViewport(viewport) ? WorldMapDots.AsiaBoundaries :
        IsOceaniaDottedMapViewport(viewport) ? WorldMapDots.OceaniaBoundaries :
        IsEuropeDottedMapViewport(viewport) ? WorldMapDots.EuropeBoundaries : Array.Empty<ChartPoint[]>();

    private static bool CanFillDottedMapBoundary(ChartPoint[] line) {
        if (line.Length < 4) return false;
        var first = line[0];
        var last = line[line.Length - 1];
        return Math.Abs(first.X - last.X) <= 0.9 && Math.Abs(first.Y - last.Y) <= 0.9;
    }

    private static bool IsInsideDottedMapViewportShape(ChartMapViewport viewport, double longitude, double latitude) {
        var outlines = DottedMapViewportOutlines(viewport);
        if (outlines.Length == 0) return true;
        foreach (var outline in outlines) if (IsInsideDottedMapPolygon(outline, longitude, latitude)) return true;
        return false;
    }

    private static bool IsInsideDottedMapPolygon(ChartPoint[] polygon, double longitude, double latitude) {
        var inside = false;
        var j = polygon.Length - 1;
        for (var i = 0; i < polygon.Length; i++) {
            var a = polygon[i];
            var b = polygon[j];
            if ((a.Y > latitude) != (b.Y > latitude) && longitude < (b.X - a.X) * (latitude - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            j = i;
        }

        return inside;
    }

    private static bool IsInsideMapViewport(ChartMapViewport viewport, double longitude, double latitude) {
        return longitude >= viewport.MinimumLongitude && longitude <= viewport.MaximumLongitude && latitude >= viewport.MinimumLatitude && latitude <= viewport.MaximumLatitude;
    }

    private static bool IsVisibleMapCoordinate(ChartMapViewport viewport, double longitude, double latitude) {
        if (IsWorldMapViewport(viewport)) return longitude >= viewport.MinimumLongitude && longitude <= viewport.MaximumLongitude && latitude >= -90 && latitude <= 90;
        return IsInsideMapViewport(viewport, longitude, latitude);
    }

    private static bool IsWorldMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - WorldMapDots.MinimumLongitude) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - WorldMapDots.MaximumLongitude) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - WorldMapDots.MinimumLatitude) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - WorldMapDots.MaximumLatitude) < 0.000001;
    }

    private static bool IsEuropeDottedMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - (-11)) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - 35) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - 36) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - 66.5) < 0.000001;
    }

    private static bool IsNorthAmericaDottedMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - (-170)) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - (-50)) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - 5) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - 72) < 0.000001;
    }

    private static bool IsSouthAmericaDottedMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - (-86)) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - (-32)) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - (-56)) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - 14) < 0.000001;
    }

    private static bool IsAfricaDottedMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - (-20)) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - 55) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - (-36)) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - 38) < 0.000001;
    }

    private static bool IsAsiaDottedMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - 44) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - 150) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - (-10)) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - 72) < 0.000001;
    }

    private static bool IsOceaniaDottedMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - 108) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - 180) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - (-48)) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - 2) < 0.000001;
    }

    private static bool IsPolandDottedMapViewport(ChartMapViewport viewport) {
        return Math.Abs(viewport.MinimumLongitude - 13.5) < 0.000001 &&
            Math.Abs(viewport.MaximumLongitude - 24.5) < 0.000001 &&
            Math.Abs(viewport.MinimumLatitude - 48.5) < 0.000001 &&
            Math.Abs(viewport.MaximumLatitude - 55.2) < 0.000001;
    }

    private static bool IsDottedMapChart(Chart chart) => chart.Series.Any(series => series.Kind == ChartSeriesKind.DottedMap);

    private readonly struct DottedMapLabelCandidate {
        public readonly string Placement;
        public readonly double X;
        public readonly double Y;
        public readonly string Anchor;

        public DottedMapLabelCandidate(string placement, double x, double y, string anchor) {
            Placement = placement;
            X = x;
            Y = y;
            Anchor = anchor;
        }
    }

    private readonly struct DottedMapLabelPlacement {
        public readonly string Placement;
        public readonly double X;
        public readonly double Y;
        public readonly string Anchor;
        public readonly ChartLabelBounds Bounds;

        public DottedMapLabelPlacement(string placement, double x, double y, string anchor, ChartLabelBounds bounds) {
            Placement = placement;
            X = x;
            Y = y;
            Anchor = anchor;
            Bounds = bounds;
        }
    }

    private readonly struct DottedMapValueRange {
        public readonly double Min;
        public readonly double Max;

        public DottedMapValueRange(double min, double max) {
            Min = min;
            Max = max;
        }
    }

    private static readonly ChartPoint[] DottedMapWorldLandOffsets = {
        new(0, 0)
    };

    private static readonly ChartPoint[] DottedMapRegionalLandOffsets = {
        new(0, 0),
        new(-0.9, -0.9),
        new(0.9, -0.9),
        new(-0.9, 0.9),
        new(0.9, 0.9)
    };

    private static readonly ChartPoint[] DottedMapCountryLandOffsets = {
        new(0, 0),
        new(-1.05, -1.05),
        new(0, -1.05),
        new(1.05, -1.05),
        new(-1.05, 0),
        new(1.05, 0),
        new(-1.05, 1.05),
        new(0, 1.05),
        new(1.05, 1.05)
    };
}
