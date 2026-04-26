using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed class ChartRange {
    public double MinX { get; private set; } = double.PositiveInfinity;
    public double MaxX { get; private set; } = double.NegativeInfinity;
    public double MinY { get; private set; } = double.PositiveInfinity;
    public double MaxY { get; private set; } = double.NegativeInfinity;

    public static ChartRange FromChart(Chart chart) {
        var range = new ChartRange();
        var barXValues = new List<double>();
        var horizontalBarYValues = new List<double>();
        var positiveBarStacks = new Dictionary<double, double>();
        var negativeBarStacks = new Dictionary<double, double>();
        var hasHorizontalBars = false;
        foreach (var series in chart.Series) {
            if (series.Kind == ChartSeriesKind.Heatmap || series.Kind == ChartSeriesKind.Gauge || series.Kind == ChartSeriesKind.Bullet || series.Kind == ChartSeriesKind.Waterfall || series.Kind == ChartSeriesKind.Radar || series.Kind == ChartSeriesKind.Funnel || series.Kind == ChartSeriesKind.Timeline) continue;
            foreach (var p in series.Points) {
                if (series.Kind == ChartSeriesKind.HorizontalBar) {
                    hasHorizontalBars = true;
                    horizontalBarYValues.Add(p.X);
                    range.IncludeX(p.Y);
                    range.IncludeY(p.X);
                    range.IncludeX(0);
                } else if (series.Kind == ChartSeriesKind.Bar) {
                    barXValues.Add(p.X);
                    range.IncludeX(p.X);
                    range.IncludeY(p.Y);
                    AddStackValue(p.Y >= 0 ? positiveBarStacks : negativeBarStacks, p.X, p.Y);
                } else {
                    range.Include(p);
                }
            }

            if (series.Kind == ChartSeriesKind.Area || series.Kind == ChartSeriesKind.Bar) range.IncludeY(0);
        }

        if (chart.Options.BarMode == ChartBarMode.Stacked) {
            foreach (var value in positiveBarStacks.Values) range.IncludeY(value);
            foreach (var value in negativeBarStacks.Values) range.IncludeY(value);
        }

        foreach (var annotation in chart.Annotations) {
            range.Include(annotation);
        }
        if (double.IsInfinity(range.MinX)) { range.MinX = 0; range.MaxX = 1; range.MinY = 0; range.MaxY = 1; }
        if (Math.Abs(range.MaxX - range.MinX) < double.Epsilon) range.MaxX = range.MinX + 1;
        if (Math.Abs(range.MaxY - range.MinY) < double.Epsilon) range.MaxY = range.MinY + 1;
        range.ApplyBarPadding(barXValues);
        range.ApplyHorizontalBarPadding(horizontalBarYValues);
        if (!hasHorizontalBars) {
            if (range.MinY > 0) range.MinY = 0;
            var padY = (range.MaxY - range.MinY) * .08;
            range.MaxY += padY;
        }

        return range;
    }

    public void SetXBounds(double min, double max) {
        MinX = min;
        MaxX = max;
    }

    public void SetYBounds(double min, double max) {
        MinY = min;
        MaxY = max;
    }

    public void Include(ChartPoint p) {
        if (p.X < MinX) MinX = p.X;
        if (p.X > MaxX) MaxX = p.X;
        if (p.Y < MinY) MinY = p.Y;
        if (p.Y > MaxY) MaxY = p.Y;
    }

    private void IncludeY(double value) {
        if (value < MinY) MinY = value;
        if (value > MaxY) MaxY = value;
    }

    private static void AddStackValue(Dictionary<double, double> stacks, double x, double y) {
        double current;
        stacks.TryGetValue(x, out current);
        stacks[x] = current + y;
    }

    private void Include(ChartAnnotation annotation) {
        if (annotation.Kind == ChartAnnotationKind.HorizontalLine || annotation.Kind == ChartAnnotationKind.HorizontalBand) {
            IncludeY(annotation.Value);
            if (annotation.EndValue.HasValue) IncludeY(annotation.EndValue.Value);
        } else {
            IncludeX(annotation.Value);
            if (annotation.EndValue.HasValue) IncludeX(annotation.EndValue.Value);
        }
    }

    private void IncludeX(double value) {
        if (value < MinX) MinX = value;
        if (value > MaxX) MaxX = value;
    }

    private void ApplyBarPadding(List<double> xValues) {
        if (xValues.Count == 0) return;
        xValues.Sort();
        var spacing = double.PositiveInfinity;
        for (var i = 1; i < xValues.Count; i++) {
            var delta = xValues[i] - xValues[i - 1];
            if (delta > 0.000001 && delta < spacing) spacing = delta;
        }

        if (double.IsInfinity(spacing)) spacing = 1;
        var padding = spacing * 0.5;
        MinX -= padding;
        MaxX += padding;
    }

    private void ApplyHorizontalBarPadding(List<double> yValues) {
        if (yValues.Count == 0) return;
        yValues.Sort();
        var spacing = double.PositiveInfinity;
        for (var i = 1; i < yValues.Count; i++) {
            var delta = yValues[i] - yValues[i - 1];
            if (delta > 0.000001 && delta < spacing) spacing = delta;
        }

        if (double.IsInfinity(spacing)) spacing = 1;
        var padding = spacing * 0.5;
        MinY -= padding;
        MaxY += padding;
    }
}
