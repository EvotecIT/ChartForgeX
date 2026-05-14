using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Raster;

internal readonly struct RasterGradientStop {
    public readonly double Offset;
    public readonly ChartColor Color;

    public RasterGradientStop(double offset, ChartColor color) {
        Offset = Math.Max(0, Math.Min(1, offset));
        Color = color;
    }
}

internal enum RasterGradientSpreadMethod {
    Pad,
    Reflect,
    Repeat
}

internal sealed partial class RgbaCanvas {
    internal void FillContoursLinearGradient(IReadOnlyList<List<ChartPoint>> contours, ChartPoint start, ChartPoint end, IReadOnlyList<RasterGradientStop> stops, RasterGradientSpreadMethod spreadMethod, RasterFillRule fillRule = RasterFillRule.EvenOdd) {
        if (contours.Count == 0 || stops.Count == 0) return;
        var scaledContours = new List<List<ChartPoint>>(contours.Count);
        foreach (var contour in contours) {
            if (contour.Count < 3) continue;
            var scaled = new List<ChartPoint>(contour.Count);
            foreach (var point in contour) scaled.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaledContours.Add(scaled);
        }

        if (scaledContours.Count == 0) return;
        FillContoursLinearGradientPixels(scaledContours, new ChartPoint(start.X * _scale, start.Y * _scale), new ChartPoint(end.X * _scale, end.Y * _scale), stops, spreadMethod, fillRule);
    }

    internal void FillContoursRadialGradient(IReadOnlyList<List<ChartPoint>> contours, ChartPoint center, ChartPoint radiusX, ChartPoint radiusY, IReadOnlyList<RasterGradientStop> stops, RasterGradientSpreadMethod spreadMethod, RasterFillRule fillRule = RasterFillRule.EvenOdd) {
        if (contours.Count == 0 || stops.Count == 0) return;
        var scaledContours = new List<List<ChartPoint>>(contours.Count);
        foreach (var contour in contours) {
            if (contour.Count < 3) continue;
            var scaled = new List<ChartPoint>(contour.Count);
            foreach (var point in contour) scaled.Add(new ChartPoint(point.X * _scale, point.Y * _scale));
            scaledContours.Add(scaled);
        }

        if (scaledContours.Count == 0) return;
        FillContoursRadialGradientPixels(
            scaledContours,
            new ChartPoint(center.X * _scale, center.Y * _scale),
            new ChartPoint(radiusX.X * _scale, radiusX.Y * _scale),
            new ChartPoint(radiusY.X * _scale, radiusY.Y * _scale),
            stops,
            spreadMethod,
            fillRule);
    }

    private void FillContoursLinearGradientPixels(IReadOnlyList<List<ChartPoint>> contours, ChartPoint start, ChartPoint end, IReadOnlyList<RasterGradientStop> stops, RasterGradientSpreadMethod spreadMethod, RasterFillRule fillRule) {
        var gradientX = end.X - start.X;
        var gradientY = end.Y - start.Y;
        var gradientLengthSquared = gradientX * gradientX + gradientY * gradientY;

        ScanFillSpans(contours, fillRule, (y, scanY, left, right) => {
            var xStart = Math.Max(0, (int)Math.Floor(left));
            var xEnd = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(right));
            for (var x = xStart; x <= xEnd; x++) {
                var coverage = Math.Min(x + 1.0, right) - Math.Max(x, left);
                if (coverage <= 0) continue;
                var amount = gradientLengthSquared <= 0.000001 ? 0 : ((x + 0.5 - start.X) * gradientX + (scanY - start.Y) * gradientY) / gradientLengthSquared;
                var color = SampleGradient(stops, amount, spreadMethod);
                BlendPixel(x, y, coverage >= 1 ? color : WithOpacity(color, coverage));
            }
        });
    }

    private void FillContoursRadialGradientPixels(IReadOnlyList<List<ChartPoint>> contours, ChartPoint center, ChartPoint radiusX, ChartPoint radiusY, IReadOnlyList<RasterGradientStop> stops, RasterGradientSpreadMethod spreadMethod, RasterFillRule fillRule) {
        var axisXX = radiusX.X - center.X;
        var axisXY = radiusX.Y - center.Y;
        var axisYX = radiusY.X - center.X;
        var axisYY = radiusY.Y - center.Y;
        var determinant = axisXX * axisYY - axisYX * axisXY;
        if (Math.Abs(determinant) <= 0.000001) return;

        ScanFillSpans(contours, fillRule, (y, scanY, left, right) => {
            var xStart = Math.Max(0, (int)Math.Floor(left));
            var xEnd = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(right));
            for (var x = xStart; x <= xEnd; x++) {
                var coverage = Math.Min(x + 1.0, right) - Math.Max(x, left);
                if (coverage <= 0) continue;
                var dx = x + 0.5 - center.X;
                var dy = scanY - center.Y;
                var localX = (dx * axisYY - axisYX * dy) / determinant;
                var localY = (axisXX * dy - dx * axisXY) / determinant;
                var color = SampleGradient(stops, Math.Sqrt(localX * localX + localY * localY), spreadMethod);
                BlendPixel(x, y, coverage >= 1 ? color : WithOpacity(color, coverage));
            }
        });
    }

    private static ChartColor SampleGradient(IReadOnlyList<RasterGradientStop> stops, double amount, RasterGradientSpreadMethod spreadMethod) {
        amount = ApplySpread(amount, spreadMethod);
        var previous = stops[0];
        for (var i = 1; i < stops.Count; i++) {
            var next = stops[i];
            if (amount > next.Offset) {
                previous = next;
                continue;
            }

            var span = Math.Max(0.000001, next.Offset - previous.Offset);
            return Mix(previous.Color, next.Color, (amount - previous.Offset) / span);
        }

        return previous.Color;
    }

    private static double ApplySpread(double amount, RasterGradientSpreadMethod spreadMethod) {
        if (double.IsNaN(amount) || double.IsInfinity(amount)) return 0;
        switch (spreadMethod) {
            case RasterGradientSpreadMethod.Repeat:
                return amount - Math.Floor(amount);
            case RasterGradientSpreadMethod.Reflect:
                var reflected = amount - Math.Floor(amount / 2.0) * 2.0;
                return reflected <= 1 ? reflected : 2 - reflected;
            default:
                return Math.Max(0, Math.Min(1, amount));
        }
    }

    private static ChartColor Mix(ChartColor left, ChartColor right, double amount) {
        amount = Math.Max(0, Math.Min(1, amount));
        var inverse = 1 - amount;
        return ChartColor.FromRgba(
            (byte)Math.Round(left.R * inverse + right.R * amount),
            (byte)Math.Round(left.G * inverse + right.G * amount),
            (byte)Math.Round(left.B * inverse + right.B * amount),
            (byte)Math.Round(left.A * inverse + right.A * amount));
    }
}
