using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Raster;

public sealed partial class PngChartRenderer {
    private static void DrawDottedMap(RgbaCanvas c, Chart chart, ChartRect basePlot) {
        ChartSeries? series = null;
        foreach (var item in chart.Series) if (item.Kind == ChartSeriesKind.DottedMap) { series = item; break; }
        if (series == null || series.Points.Count == 0) return;
        var t = chart.Options.Theme;
        var viewport = chart.Options.MapViewport;
        var plot = new ChartRect(basePlot.Left + 8, basePlot.Top + 8, Math.Max(1, basePlot.Width - 16), Math.Max(1, basePlot.Height - 16));
        var map = FitDottedMap(plot, viewport);
        var dot = DottedMapDotSize(map, viewport);
        var landColor = DottedMapLandDotColor(t.PlotBackground, t.MutedText);
        var landOpacity = DottedMapLandDotOpacity(t.PlotBackground);
        var reservedLabels = new List<ChartLabelBounds>();
        var valueRange = GetDottedMapValueRange(series);

        DrawDottedMapGraticule(c, chart, viewport, map);
        DrawDottedMapViewportShape(c, chart, viewport, map, dot);
        DrawDottedMapLandAreas(c, chart, viewport, map);
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
                var dotRadius = DottedMapLandDotRadius(dot, viewport);
                c.FillRoundedRect(x - dotRadius, y - dotRadius, dotRadius * 2, dotRadius * 2, dotRadius, ApplyOpacity(landColor, landOpacity));
            }
        }

        DrawDottedMapBoundaries(c, chart, viewport, map, dot);
        DrawDottedMapConnectors(c, chart, series, viewport, map, dot);

        for (var i = 0; i < series.Points.Count; i++) {
            var point = series.Points[i];
            if (!IsVisibleMapCoordinate(viewport, point.X, point.Y)) continue;
            var color = i < series.PointColors.Count && series.PointColors[i].HasValue ? series.PointColors[i]!.Value : series.Color ?? t.Positive;
            var x = ProjectMapX(map, viewport, point.X);
            var y = ProjectMapY(map, viewport, point.Y);
            var value = DottedMapPointValue(series, i);
            var radius = DottedMapPointRadius(dot, value, valueRange);
            var haloRadius = Math.Max(dot * 2.45, radius * 1.85);
            c.DrawCircle(x, y, haloRadius, ApplyOpacity(color, 0.18));
            c.DrawCircle(x, y, radius, color);
            c.DrawCircleOutline(x, y, radius, t.CardBackground, Math.Max(1, dot * 0.55));
            if (ShouldDrawDataLabels(chart, series)) {
                var label = DottedMapDisplayLabel(chart, series, i);
                DrawDottedMapPngDataLabel(c, chart, series, label, i, x, y, Math.Max(dot, radius), map, reservedLabels);
            }
        }
    }

    private static void DrawDottedMapGraticule(RgbaCanvas c, Chart chart, ChartMapViewport viewport, ChartRect map) {
        var t = chart.Options.Theme;
        var verticalColor = ApplyOpacity(t.Grid, 0.22);
        var horizontalColor = ApplyOpacity(t.Grid, 0.18);
        for (var i = 1; i < 4; i++) {
            var longitude = viewport.MinimumLongitude + (viewport.MaximumLongitude - viewport.MinimumLongitude) * i / 4.0;
            var x = ProjectMapX(map, viewport, longitude);
            c.DrawLine(x, map.Top, x, map.Bottom, verticalColor, 0.8);
        }

        for (var i = 1; i < 3; i++) {
            var latitude = viewport.MinimumLatitude + (viewport.MaximumLatitude - viewport.MinimumLatitude) * i / 3.0;
            var y = ProjectMapY(map, viewport, latitude);
            c.DrawLine(map.Left, y, map.Right, y, horizontalColor, 0.8);
        }
    }

    private static void DrawDottedMapViewportShape(RgbaCanvas c, Chart chart, ChartMapViewport viewport, ChartRect map, double dot) {
        var outlines = DottedMapViewportOutlines(viewport);
        if (outlines.Length == 0) return;
        var t = chart.Options.Theme;
        var color = DottedMapLandDotColor(t.PlotBackground, t.MutedText);
        foreach (var outline in outlines) {
            var points = new List<ChartPoint>(outline.Length);
            foreach (var point in outline) points.Add(new ChartPoint(ProjectMapX(map, viewport, point.X), ProjectMapY(map, viewport, point.Y)));
            c.FillPolygon(points, ApplyOpacity(color, 0.10));
            for (var i = 0; i < points.Count; i++) {
                var next = points[(i + 1) % points.Count];
                c.DrawLine(points[i].X, points[i].Y, next.X, next.Y, ApplyOpacity(color, 0.46), Math.Max(1.2, dot * 0.42));
            }
        }
    }

    private static void DrawDottedMapConnectors(RgbaCanvas c, Chart chart, ChartSeries series, ChartMapViewport viewport, ChartRect map, double dot) {
        var t = chart.Options.Theme;
        var valueRange = GetDottedMapValueRange(series);
        foreach (var connector in chart.Options.MapConnectors) {
            if (!IsVisibleMapCoordinate(viewport, connector.FromLongitude, connector.FromLatitude) || !IsVisibleMapCoordinate(viewport, connector.ToLongitude, connector.ToLatitude)) continue;
            var x1 = ProjectMapX(map, viewport, connector.FromLongitude);
            var y1 = ProjectMapY(map, viewport, connector.FromLatitude);
            var x2 = ProjectMapX(map, viewport, connector.ToLongitude);
            var y2 = ProjectMapY(map, viewport, connector.ToLatitude);
            var color = ApplyOpacity(connector.Color ?? series.Color ?? t.Warning, 0.62);
            var control = DottedMapConnectorControlPoint(x1, y1, x2, y2, map, dot);
            var fromTrim = DottedMapEndpointTrim(series, viewport, connector.FromLongitude, connector.FromLatitude, dot, valueRange);
            var toTrim = DottedMapEndpointTrim(series, viewport, connector.ToLongitude, connector.ToLatitude, dot, valueRange);
            var renderedFrom = MoveDottedMapConnectorEndpoint(x1, y1, control.X, control.Y, fromTrim);
            var renderedTo = MoveDottedMapConnectorEndpoint(x2, y2, control.X, control.Y, toTrim);
            var strokeWidth = Math.Max(1.2, dot * 0.78);
            DrawDottedMapPngConnectorCurve(c, renderedFrom.X, renderedFrom.Y, renderedTo.X, renderedTo.Y, control.X, control.Y, ApplyOpacity(t.PlotBackground, 0.72), strokeWidth + 3.2);
            DrawDottedMapPngConnectorCurve(c, renderedFrom.X, renderedFrom.Y, renderedTo.X, renderedTo.Y, control.X, control.Y, color, strokeWidth);
            c.FillPolygon(DottedMapConnectorArrowPoints(renderedFrom.X, renderedFrom.Y, control.X, control.Y, renderedTo.X, renderedTo.Y, dot), ApplyOpacity(connector.Color ?? series.Color ?? t.Warning, 0.78));
            if (ShouldDrawDataLabels(chart, series)) {
                var labelPoint = DottedMapConnectorPoint(renderedFrom.X, renderedFrom.Y, control.X, control.Y, renderedTo.X, renderedTo.Y, 0.5);
                var label = CompactDottedMapConnectorLabel(connector.Label);
                var textWidth = RgbaCanvas.MeasureTextEmphasizedWidth(label, 12, null);
                c.FillRoundedRect(labelPoint.X - textWidth / 2 - 5, labelPoint.Y - Math.Max(25, dot * 4), textWidth + 10, 20, 9, ApplyOpacity(t.PlotBackground, 0.78));
                c.DrawTextEmphasized(labelPoint.X - textWidth / 2, labelPoint.Y - Math.Max(21, dot * 3.4), label, connector.Color ?? series.Color ?? t.Warning, 12);
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

    private static void DrawDottedMapPngConnectorCurve(RgbaCanvas c, double x1, double y1, double x2, double y2, double controlX, double controlY, ChartColor color, double strokeWidth) {
        var previousX = x1;
        var previousY = y1;
        for (var step = 1; step <= 22; step++) {
            var ratio = step / 22.0;
            var oneMinus = 1 - ratio;
            var x = oneMinus * oneMinus * x1 + 2 * oneMinus * ratio * controlX + ratio * ratio * x2;
            var y = oneMinus * oneMinus * y1 + 2 * oneMinus * ratio * controlY + ratio * ratio * y2;
            c.DrawLine(previousX, previousY, x, y, color, strokeWidth);
            previousX = x;
            previousY = y;
        }
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

    private static void DrawDottedMapBoundaries(RgbaCanvas c, Chart chart, ChartMapViewport viewport, ChartRect map, double dot) {
        var boundaries = DottedMapBoundaryLines(viewport);
        if (boundaries.Length == 0) return;
        var t = chart.Options.Theme;
        var color = ApplyOpacity(DottedMapBoundaryColor(t.PlotBackground, t.MutedText), DottedMapBoundaryOpacity(t.PlotBackground));
        var strokeWidth = Math.Max(0.65, dot * 0.26);
        foreach (var line in boundaries) {
            if (line.Length < 2) continue;
            var previous = line[0];
            var previousX = ProjectMapX(map, viewport, previous.X);
            var previousY = ProjectMapY(map, viewport, previous.Y);
            for (var i = 1; i < line.Length; i++) {
                var point = line[i];
                var x = ProjectMapX(map, viewport, point.X);
                var y = ProjectMapY(map, viewport, point.Y);
                c.DrawLine(previousX, previousY, x, y, color, strokeWidth);
                previousX = x;
                previousY = y;
            }
        }
    }

    private static void DrawDottedMapLandAreas(RgbaCanvas c, Chart chart, ChartMapViewport viewport, ChartRect map) {
        var boundaries = DottedMapBoundaryLines(viewport);
        if (boundaries.Length == 0) return;
        var t = chart.Options.Theme;
        var color = ApplyOpacity(DottedMapLandAreaColor(t.PlotBackground, t.MutedText), DottedMapLandAreaOpacity(t.PlotBackground));
        foreach (var line in boundaries) {
            if (!CanFillDottedMapBoundary(line)) continue;
            var points = new List<ChartPoint>(line.Length);
            foreach (var point in line) points.Add(new ChartPoint(ProjectMapX(map, viewport, point.X), ProjectMapY(map, viewport, point.Y)));
            c.FillPolygon(points, color);
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

    private static void DrawDottedMapPngDataLabel(RgbaCanvas c, Chart chart, ChartSeries series, string label, int pointIndex, double x, double y, double dot, ChartRect map, List<ChartLabelBounds> reservedLabels) {
        var style = DataLabelStyle(chart, series, pointIndex);
        var fontSize = PngDataLabelFontSize(chart, series, pointIndex);
        label = TrimReadablePngLabelToWidth(label, fontSize, Math.Min(132, Math.Max(8, map.Width - ChartVisualPrimitives.DataLabelPlotInset * 2)));
        if (label.Length == 0) return;

        var offset = Math.Max(16, dot * 5.6);
        var candidates = new[] {
            new DottedMapPngLabelCandidate(x, y - offset, "center"),
            new DottedMapPngLabelCandidate(x + offset, y, "right"),
            new DottedMapPngLabelCandidate(x, y + offset, "center"),
            new DottedMapPngLabelCandidate(x - offset, y, "left"),
            new DottedMapPngLabelCandidate(x + offset * 0.82, y - offset * 0.82, "right"),
            new DottedMapPngLabelCandidate(x + offset * 0.82, y + offset * 0.82, "right"),
            new DottedMapPngLabelCandidate(x - offset * 0.82, y + offset * 0.82, "left"),
            new DottedMapPngLabelCandidate(x - offset * 0.82, y - offset * 0.82, "left"),
            new DottedMapPngLabelCandidate(x + offset * 1.32, y - offset * 2.0, "right"),
            new DottedMapPngLabelCandidate(x + offset * 1.32, y + offset * 2.0, "right"),
            new DottedMapPngLabelCandidate(x - offset * 1.32, y + offset * 2.0, "left"),
            new DottedMapPngLabelCandidate(x - offset * 1.32, y - offset * 2.0, "left")
        };

        var width = EstimatePngEmphasizedTextWidth(label, fontSize) + 8;
        var height = fontSize + 6;
        DottedMapPngLabelPlacement? fallback = null;
        foreach (var candidate in candidates) {
            var placement = PlaceDottedMapPngLabel(candidate, map, width, height);
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
            DrawDottedMapPngDataLabel(c, chart, label, fontSize, style, placement, x, y, dot);
            return;
        }

        if (fallback.HasValue) DrawDottedMapPngDataLabel(c, chart, label, fontSize, style, fallback.Value, x, y, dot);
    }

    private static bool DottedMapLabelTouchesPoint(ChartLabelBounds bounds, double x, double y, double dot) {
        var guard = Math.Max(5, dot * 1.75);
        return bounds.Intersects(new ChartLabelBounds(x - guard, y - guard, guard * 2, guard * 2));
    }

    private static DottedMapPngLabelPlacement PlaceDottedMapPngLabel(DottedMapPngLabelCandidate candidate, ChartRect map, double width, double height) {
        var inset = ChartVisualPrimitives.DataLabelPlotInset;
        var left = candidate.Anchor == "left"
            ? candidate.X - width
            : candidate.Anchor == "right"
                ? candidate.X
                : candidate.X - width / 2;
        left = Clamp(left, map.Left + inset, map.Right - width - inset);
        var top = Clamp(candidate.Y - height / 2, map.Top + inset, map.Bottom - height - inset);
        return new DottedMapPngLabelPlacement(new ChartLabelBounds(left, top, width, height));
    }

    private static void DrawDottedMapPngDataLabel(RgbaCanvas c, Chart chart, string label, double fontSize, ChartTextStyle style, DottedMapPngLabelPlacement placement, double pointX, double pointY, double dot) {
        DrawDottedMapPngLabelLeader(c, chart, placement.Bounds, pointX, pointY, dot);
        c.FillRoundedRect(placement.Bounds.X, placement.Bounds.Y, placement.Bounds.Width, placement.Bounds.Height, Math.Min(6, placement.Bounds.Height / 2), ApplyOpacity(ReadableLabelHalo(chart), 0.86));
        DrawReadablePngLabel(c, placement.Bounds.X + 4, placement.Bounds.Y + 3, label, chart.Options.Theme.Text, ReadableLabelHalo(chart), fontSize, style);
    }

    private static void DrawDottedMapPngLabelLeader(RgbaCanvas c, Chart chart, ChartLabelBounds bounds, double pointX, double pointY, double dot) {
        var end = DottedMapLabelLeaderEndpoint(bounds, pointX, pointY);
        var dx = end.X - pointX;
        var dy = end.Y - pointY;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < Math.Max(8, dot * 2.1)) return;

        var startGap = Math.Min(length * 0.42, Math.Max(dot * 1.32, 4));
        var startX = pointX + dx / length * startGap;
        var startY = pointY + dy / length * startGap;
        var color = ApplyOpacity(DottedMapBoundaryColor(chart.Options.Theme.PlotBackground, chart.Options.Theme.MutedText), 0.52);
        c.DrawLine(startX, startY, end.X, end.Y, color, Math.Max(0.85, dot * 0.22));
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

    private static bool IsDottedMapChart(Chart chart) {
        foreach (var series in chart.Series) if (series.Kind == ChartSeriesKind.DottedMap) return true;
        return false;
    }

    private readonly struct DottedMapPngLabelCandidate {
        public readonly double X;
        public readonly double Y;
        public readonly string Anchor;

        public DottedMapPngLabelCandidate(double x, double y, string anchor) {
            X = x;
            Y = y;
            Anchor = anchor;
        }
    }

    private readonly struct DottedMapPngLabelPlacement {
        public readonly ChartLabelBounds Bounds;

        public DottedMapPngLabelPlacement(ChartLabelBounds bounds) {
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
