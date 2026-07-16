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

    public static ChartRange FromChart(Chart chart, bool applyOptionBounds = true) {
        var range = new ChartRange();
        var barXValues = new List<double>();
        var bubbleXValues = new List<double>();
        var horizontalBarYValues = new List<double>();
        var positiveBarStacks = new Dictionary<double, double>();
        var negativeBarStacks = new Dictionary<double, double>();
        var positiveHorizontalBarStacks = new Dictionary<double, double>();
        var negativeHorizontalBarStacks = new Dictionary<double, double>();
        var positiveAreaStacks = new Dictionary<double, double>();
        var negativeAreaStacks = new Dictionary<double, double>();
        var hasHorizontalBars = false;
        var usesVerticalBaseline = false;
        foreach (var series in chart.Series) {
            if (ChartSeriesKindTraits.IsExclusive(series.Kind)) continue;
            if (series.YAxis == ChartAxisSide.Secondary && !ChartSeriesKindTraits.UsesHorizontalBaseline(series.Kind)) {
                IncludeSeriesX(range, series);
                continue;
            }

            if (ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) usesVerticalBaseline = true;

            if (series.Kind == ChartSeriesKind.Bar && series.HistogramBinLayout != null) {
                if (series.HistogramBinLayout.Minimum == series.HistogramBinLayout.Maximum) {
                    range.IncludeX(series.HistogramBinLayout.Minimum - 0.5);
                    range.IncludeX(series.HistogramBinLayout.Maximum + 0.5);
                } else {
                    range.IncludeX(series.HistogramBinLayout.Minimum);
                    range.IncludeX(series.HistogramBinLayout.Maximum);
                }
                foreach (var point in series.Points) {
                    range.IncludeY(point.Y);
                    AddStackValue(point.Y >= 0 ? positiveBarStacks : negativeBarStacks, point.X, point.Y);
                }

                if (UsesZeroBaseline(chart.Options.YAxis)) range.IncludeY(0);
                continue;
            }

            if (series.Kind == ChartSeriesKind.Bubble) {
                for (var i = 0; i + 1 < series.Points.Count; i += 2) {
                    var center = series.Points[i];
                    bubbleXValues.Add(center.X);
                    range.Include(center);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.ErrorBar) {
                for (var i = 0; i + 2 < series.Points.Count; i += 3) {
                    var center = series.Points[i];
                    barXValues.Add(center.X);
                    range.Include(center);
                    range.Include(series.Points[i + 1]);
                    range.Include(series.Points[i + 2]);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.Candlestick || series.Kind == ChartSeriesKind.Ohlc) {
                for (var i = 0; i + 3 < series.Points.Count; i += 4) {
                    var open = series.Points[i];
                    barXValues.Add(open.X);
                    range.Include(open);
                    range.Include(series.Points[i + 1]);
                    range.Include(series.Points[i + 2]);
                    range.Include(series.Points[i + 3]);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.RangeBand || series.Kind == ChartSeriesKind.RangeArea) {
                for (var i = 0; i + 1 < series.Points.Count; i += 2) {
                    range.Include(series.Points[i]);
                    range.Include(series.Points[i + 1]);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.Dumbbell) {
                for (var i = 0; i + 1 < series.Points.Count; i += 2) {
                    barXValues.Add(series.Points[i].X);
                    range.Include(series.Points[i]);
                    range.Include(series.Points[i + 1]);
                }

                continue;
            }

            foreach (var p in series.Points) {
                if (ChartSeriesKindTraits.UsesHorizontalBaseline(series.Kind)) {
                    hasHorizontalBars = true;
                    horizontalBarYValues.Add(p.X);
                    range.IncludeX(p.Y);
                    range.IncludeY(p.X);
                    if (UsesZeroBaseline(chart.Options.XAxis)) range.IncludeX(0);
                    AddStackValue(p.Y >= 0 ? positiveHorizontalBarStacks : negativeHorizontalBarStacks, p.X, p.Y);
                } else if (series.Kind == ChartSeriesKind.Bar || series.Kind == ChartSeriesKind.Lollipop || series.Kind == ChartSeriesKind.RangeBar || series.Kind == ChartSeriesKind.BoxPlot || series.Kind == ChartSeriesKind.Slope) {
                    barXValues.Add(p.X);
                    range.IncludeX(p.X);
                    range.IncludeY(p.Y);
                    if (series.Kind == ChartSeriesKind.Bar) AddStackValue(p.Y >= 0 ? positiveBarStacks : negativeBarStacks, p.X, p.Y);
                } else if (series.Kind == ChartSeriesKind.StackedArea) {
                    range.IncludeX(p.X);
                    range.IncludeY(p.Y);
                    if (UsesZeroBaseline(chart.Options.YAxis)) range.IncludeY(0);
                    AddStackValue(p.Y >= 0 ? positiveAreaStacks : negativeAreaStacks, p.X, p.Y);
                } else {
                    range.Include(p);
                }
            }

            if (UsesZeroBaseline(chart.Options.YAxis) && ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) range.IncludeY(0);
        }

        if (chart.Options.BarMode == ChartBarMode.Stacked) {
            foreach (var value in positiveBarStacks.Values) range.IncludeY(value);
            foreach (var value in negativeBarStacks.Values) range.IncludeY(value);
            foreach (var value in positiveHorizontalBarStacks.Values) range.IncludeX(value);
            foreach (var value in negativeHorizontalBarStacks.Values) range.IncludeX(value);
        }
        foreach (var value in positiveAreaStacks.Values) range.IncludeY(value);
        foreach (var value in negativeAreaStacks.Values) range.IncludeY(value);

        foreach (var annotation in chart.Annotations) {
            range.Include(annotation);
        }
        range.InitializeEmptyX(chart.Options.XAxis);
        range.InitializeEmptyY(chart.Options.YAxis);
        if (Math.Abs(range.MaxX - range.MinX) < double.Epsilon) range.MaxX = range.MinX + 1;
        if (Math.Abs(range.MaxY - range.MinY) < double.Epsilon) range.MaxY = range.MinY + 1;
        range.ApplyBarPadding(barXValues, chart.Options.XAxis);
        range.ApplyHorizontalBarPadding(horizontalBarYValues);
        if (!hasHorizontalBars) {
            if (usesVerticalBaseline) range.ApplyLogarithmicYBaseline(chart.Options.YAxis);
            range.ApplyVerticalPadding(chart.Options.YAxis);
            if (applyOptionBounds) range.ApplyYAxisOptions(chart);
        }

        range.ApplyBarPadding(bubbleXValues, chart.Options.XAxis);
        if (hasHorizontalBars) range.ApplyLogarithmicXBaseline(chart.Options.XAxis);
        if (applyOptionBounds) range.ApplyXAxisOptions(chart);
        return range;
    }

    public static ChartRange FromSecondaryYAxis(Chart chart, ChartRange primaryRange, bool applyOptionBounds = true) {
        var range = new ChartRange();
        var usesVerticalBaseline = false;
        foreach (var series in chart.Series) {
            if (series.YAxis != ChartAxisSide.Secondary) continue;
            IncludeSeriesY(range, series, chart.Options.SecondaryYAxis);
            if (ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) usesVerticalBaseline = true;
        }

        range.MinX = primaryRange.MinX;
        range.MaxX = primaryRange.MaxX;
        range.InitializeEmptyY(chart.Options.SecondaryYAxis);

        if (Math.Abs(range.MaxY - range.MinY) < double.Epsilon) range.MaxY = range.MinY + 1;
        if (usesVerticalBaseline) range.ApplyLogarithmicYBaseline(chart.Options.SecondaryYAxis);
        range.ApplyVerticalPadding(chart.Options.SecondaryYAxis);
        if (applyOptionBounds) range.ApplySecondaryYAxisOptions(chart);
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

    private void InitializeEmptyX(ChartAxis axis) {
        if (!double.IsInfinity(MinX)) return;
        MinX = axis.Scale == ChartScaleKind.Logarithmic ? 1 : 0;
        MaxX = axis.Scale == ChartScaleKind.Logarithmic ? 10 : 1;
    }

    private void InitializeEmptyY(ChartAxis axis) {
        if (!double.IsInfinity(MinY)) return;
        MinY = axis.Scale == ChartScaleKind.Logarithmic ? 1 : 0;
        MaxY = axis.Scale == ChartScaleKind.Logarithmic ? 10 : 1;
    }

    private void ApplyXAxisOptions(Chart chart) {
        if (chart.Options.XAxisMinimum.HasValue) MinX = chart.Options.XAxisMinimum.Value;
        if (chart.Options.XAxisMaximum.HasValue) MaxX = chart.Options.XAxisMaximum.Value;
        if (MaxX <= MinX) MaxX = MinX + 1;
    }

    private void ApplyYAxisOptions(Chart chart) {
        if (chart.Options.YAxisMinimum.HasValue) MinY = chart.Options.YAxisMinimum.Value;
        if (chart.Options.YAxisMaximum.HasValue) MaxY = chart.Options.YAxisMaximum.Value;
        if (MaxY <= MinY) MaxY = MinY + 1;
    }

    private void ApplySecondaryYAxisOptions(Chart chart) {
        if (chart.Options.SecondaryYAxisMinimum.HasValue) MinY = chart.Options.SecondaryYAxisMinimum.Value;
        if (chart.Options.SecondaryYAxisMaximum.HasValue) MaxY = chart.Options.SecondaryYAxisMaximum.Value;
        if (MaxY <= MinY) MaxY = MinY + 1;
    }

    private static void IncludeSeriesX(ChartRange range, ChartSeries series) {
        if (series.Kind == ChartSeriesKind.Bar && series.HistogramBinLayout != null) {
            range.IncludeX(series.HistogramBinLayout.Minimum);
            range.IncludeX(series.HistogramBinLayout.Maximum);
            return;
        }

        if (series.Kind == ChartSeriesKind.HorizontalBar) {
            foreach (var point in series.Points) range.IncludeX(point.Y);
            return;
        }

        foreach (var point in series.Points) range.IncludeX(point.X);
    }

    private static void IncludeSeriesY(ChartRange range, ChartSeries series, ChartAxis axis) {
        if (series.Kind == ChartSeriesKind.HorizontalBar) {
            foreach (var point in series.Points) range.IncludeY(point.X);
            return;
        }

        if (series.Kind == ChartSeriesKind.Bubble) {
            for (var i = 0; i + 1 < series.Points.Count; i += 2) range.IncludeY(series.Points[i].Y);
            return;
        }

        foreach (var point in series.Points) range.IncludeY(point.Y);
        if (UsesZeroBaseline(axis) && ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) range.IncludeY(0);
    }

    private void ApplyVerticalPadding(ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Logarithmic) return;
        var padY = (MaxY - MinY) * .08;
        if (!ChartMath.IsFinite(padY)) return;
        if (axis.Scale == ChartScaleKind.Time) {
            if (ChartMath.IsFinite(MinY - padY)) MinY -= padY;
            if (ChartMath.IsFinite(MaxY + padY)) MaxY += padY;
            return;
        }

        if (MinY > 0) MinY = 0;
        if (ChartMath.IsFinite(MaxY + padY)) MaxY += padY;
    }

    private static bool UsesZeroBaseline(ChartAxis axis) => axis.Scale != ChartScaleKind.Logarithmic && axis.Scale != ChartScaleKind.Time;

    private void ApplyLogarithmicXBaseline(ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Logarithmic) MinX = PositiveBaselineBelow(MinX);
    }

    private void ApplyLogarithmicYBaseline(ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Logarithmic) MinY = PositiveBaselineBelow(MinY);
    }

    private static double PositiveBaselineBelow(double minimum) {
        if (!ChartMath.IsFinite(minimum) || minimum <= 0) return minimum;
        var baseline = minimum / 10;
        return baseline > 0 && ChartMath.IsFinite(baseline) ? baseline : minimum;
    }

    private void ApplyBarPadding(List<double> xValues, ChartAxis axis) {
        if (xValues.Count == 0) return;
        xValues.Sort();
        var spacing = double.PositiveInfinity;
        for (var i = 1; i < xValues.Count; i++) {
            var delta = xValues[i] - xValues[i - 1];
            if (delta > 0.000001 && delta < spacing) spacing = delta;
        }

        if (double.IsInfinity(spacing)) spacing = 1;
        var padding = spacing * 0.5;
        if (axis.Scale == ChartScaleKind.Logarithmic) {
            var smallestPositive = xValues.FirstOrDefault(value => value > 0);
            if (smallestPositive > 0) MinX = Math.Min(MinX, Math.Max(smallestPositive * 0.5, MinX - padding));
        } else {
            MinX -= padding;
        }
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
