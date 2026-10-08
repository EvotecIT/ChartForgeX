using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal readonly struct ChartWordCloudTerm {
    public ChartWordCloudTerm(int pointIndex, string text, double value, double x, double y, double width, double height, double fontSize, double angle) {
        PointIndex = pointIndex;
        Text = text;
        Value = value;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        FontSize = fontSize;
        Angle = angle;
    }

    public readonly int PointIndex;
    public readonly string Text;
    public readonly double Value;
    public readonly double X;
    public readonly double Y;
    public readonly double Width;
    public readonly double Height;
    public readonly double FontSize;
    public readonly double Angle;
}

internal static class ChartWordCloudLayout {
    public static List<ChartWordCloudTerm> Compute(Chart chart, ChartRect plot, Func<int, string, double, TextMetrics>? measure = null) {
        var series = chart.Series.FirstOrDefault(item => item.Kind == ChartSeriesKind.WordCloud);
        if (series == null || series.Points.Count == 0) return new List<ChartWordCloudTerm>();
        var valuesQuery = series.Points
            .Select((point, index) => new { Point = point, Index = index })
            .Where(item => item.Point.Y > 0)
            .OrderByDescending(item => item.Point.Y);
        if (chart.Options.WordCloudMaximumTerms.HasValue) valuesQuery = valuesQuery.Take(chart.Options.WordCloudMaximumTerms.Value).OrderByDescending(item => item.Point.Y);
        var values = valuesQuery.ToArray();
        if (values.Length == 0) return new List<ChartWordCloudTerm>();
        var max = values.Max(item => item.Point.Y);
        var min = values.Min(item => item.Point.Y);
        var minFontSize = chart.Options.WordCloudMinFontSize;
        var maxFontSize = chart.Options.WordCloudMaxFontSize;
        var angles = chart.Options.WordCloudAngles;
        var density = chart.Options.WordCloudDensity;
        var edgePadding = EdgePadding(plot);
        var safePlot = new ChartRect(
            plot.X + edgePadding,
            plot.Y + edgePadding,
            Math.Max(0, plot.Width - edgePadding * 2),
            Math.Max(0, plot.Height - edgePadding * 2));
        if (safePlot.Width <= 0 || safePlot.Height <= 0) return new List<ChartWordCloudTerm>();
        var placed = new List<ChartWordCloudTerm>();
        var bounds = new List<ChartRect>();
        foreach (var item in values) {
            var text = WordLabel(chart, item.Point.X, item.Index);
            var ratio = max <= min ? 1 : (item.Point.Y - min) / (max - min);
            var preferredFontSize = minFontSize + Math.Sqrt(Math.Max(0, ratio)) * (maxFontSize - minFontSize);
            var angle = angles[item.Index % angles.Length];
            for (var scale = 1.0; scale >= 0.42; scale -= 0.08) {
                var fontSize = Math.Max(minFontSize, preferredFontSize * scale);
                var metrics = measure?.Invoke(item.Index, text, fontSize);
                var width = metrics?.Width ?? EstimateWidth(text, fontSize);
                var height = metrics?.Height ?? fontSize * 1.16;
                var collisionPadding = 8.0 / density;
                var collisionWidth = RotatedWidth(width, height, angle) + collisionPadding;
                var collisionHeight = RotatedHeight(width, height, angle) + collisionPadding;
                if (!TryPlace(safePlot, bounds, collisionWidth, collisionHeight, density, out var x, out var y)) {
                    if (fontSize <= minFontSize) break;
                    continue;
                }
                placed.Add(new ChartWordCloudTerm(item.Index, text, item.Point.Y, x, y, width, height, fontSize, angle));
                bounds.Add(new ChartRect(x - collisionWidth / 2, y - collisionHeight / 2, collisionWidth, collisionHeight));
                break;
            }
        }

        return placed;
    }

    public static double EdgePadding(ChartRect plot) =>
        Math.Min(Math.Min(plot.Width, plot.Height) / 2, Math.Min(
            ChartVisualPrimitives.WordCloudEdgePaddingMax,
            Math.Max(
                ChartVisualPrimitives.WordCloudEdgePaddingMin,
                Math.Min(plot.Width, plot.Height) * ChartVisualPrimitives.WordCloudEdgePaddingFactor)));

    private static bool TryPlace(ChartRect plot, List<ChartRect> bounds, double width, double height, double density, out double x, out double y) {
        var cx = plot.Left + plot.Width / 2;
        var cy = plot.Top + plot.Height / 2;
        var attempts = (int)Math.Round(4200 * density);
        var radiusStep = 3.0 / Math.Sqrt(density);
        for (var attempt = 0; attempt < attempts; attempt++) {
            var angle = attempt * 0.52;
            var radius = Math.Sqrt(attempt) * radiusStep;
            x = cx + Math.Cos(angle) * radius;
            y = cy + Math.Sin(angle) * radius;
            var rect = new ChartRect(x - width / 2, y - height / 2, width, height);
            if (rect.Left < plot.Left || rect.Right > plot.Right || rect.Top < plot.Top || rect.Bottom > plot.Bottom) continue;
            var intersects = false;
            foreach (var existing in bounds) {
                if (!Intersects(rect, existing)) continue;
                intersects = true;
                break;
            }

            if (!intersects) return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    private static bool Intersects(ChartRect first, ChartRect second) =>
        first.Left < second.Right && first.Right > second.Left && first.Top < second.Bottom && first.Bottom > second.Top;

    private static string WordLabel(Chart chart, double x, int index) {
        foreach (var label in chart.Options.XAxisLabels) if (Math.Abs(label.Value - x) < 0.000001) return label.Text;
        return "Term " + (index + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static double EstimateWidth(string text, double fontSize) {
        var width = 0.0;
        foreach (var ch in text) width += char.IsWhiteSpace(ch) ? 0.32 : char.IsUpper(ch) || char.IsDigit(ch) ? 0.62 : 0.54;
        return Math.Max(1, width * fontSize);
    }

    private static double RotatedWidth(double width, double height, double degrees) {
        var radians = Math.Abs(degrees) * Math.PI / 180.0;
        return Math.Abs(Math.Cos(radians)) * width + Math.Abs(Math.Sin(radians)) * height;
    }

    private static double RotatedHeight(double width, double height, double degrees) {
        var radians = Math.Abs(degrees) * Math.PI / 180.0;
        return Math.Abs(Math.Sin(radians)) * width + Math.Abs(Math.Cos(radians)) * height;
    }
}
