using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartRange {
    public double MinX { get; private set; } = double.PositiveInfinity;
    public double MaxX { get; private set; } = double.NegativeInfinity;
    public double MinY { get; private set; } = double.PositiveInfinity;
    public double MaxY { get; private set; } = double.NegativeInfinity;

    /// <summary>Snapshots data and authored bounds before viewport-dependent fitting.</summary>
    internal ChartRange Copy() => new() { MinX = MinX, MaxX = MaxX, MinY = MinY, MaxY = MaxY };

    public static ChartRange FromChart(Chart chart, bool applyOptionBounds = true) =>
        FromChart(chart, ChartBarCoordinateMap.Create(chart), applyOptionBounds);

    internal static ChartRange FromChart(Chart chart, ChartBarCoordinateMap coordinateMap, bool applyOptionBounds = true) =>
        FromChart(chart, coordinateMap, ChartStackLayout.Create(chart, coordinateMap), applyOptionBounds);

    internal static ChartRange FromChart(Chart chart, ChartBarCoordinateMap coordinateMap, ChartStackLayout stacks, bool applyOptionBounds = true) {
        var range = new ChartRange();
        var barXValues = new List<double>();
        var scalarBarXValues = new List<double>();
        var bubbleXValues = new List<double>();
        // Padding and mark thickness must use the same category identities.
        // Raw observations remain the range/metadata facts, including numeric equivalents.
        var horizontalBarYValues = new List<double>();
        var horizontalScalarCategories = stacks.HorizontalCategories.ToList();
        var hasHorizontalSeries = ChartCartesianOrientation.IsHorizontal(chart);
        var hasObservations = chart.Series.Any(series => series.RenderPoints.Count > 0);
        var usesHorizontalBaseline = false;
        var usesVerticalBaseline = false;
        for (var seriesIndex = 0; seriesIndex < chart.Series.Count; seriesIndex++) {
            var series = chart.Series[seriesIndex];
            var contributesBaseline = series.RenderPoints.Count > 0 || !hasObservations;
            if (contributesBaseline && series.ValueAxis == ChartAxisSide.Primary && ChartSeriesKindTraits.UsesHorizontalBaseline(series.Kind)) usesHorizontalBaseline = true;
            if (hasHorizontalSeries && series.ValueAxis == ChartAxisSide.Secondary) {
                foreach (var category in ChartCartesianOrientation.Categories(series)) {
                    range.IncludeY(category);
                    if (series.Kind is ChartSeriesKind.Waterfall or ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot) horizontalBarYValues.Add(category);
                }
                continue;
            }
            if (series.Kind == ChartSeriesKind.Waterfall) {
                var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series);
                if (horizontal && contributesBaseline) usesHorizontalBaseline = true;
                var valueAxis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
                foreach (var step in ChartWaterfallSteps.Create(series)) {
                    if (horizontal) {
                        range.IncludeY(step.X); horizontalBarYValues.Add(step.X);
                        if (step.Start != 0 || UsesZeroBaseline(valueAxis)) range.IncludeX(step.Start);
                        if (step.End != 0 || UsesZeroBaseline(valueAxis)) range.IncludeX(step.End);
                        continue;
                    }
                    range.IncludeX(step.X); barXValues.Add(step.X);
                    if (series.ValueAxis != ChartAxisSide.Secondary) {
                        if (step.Start != 0 || UsesZeroBaseline(valueAxis)) range.IncludeY(step.Start);
                        if (step.End != 0 || UsesZeroBaseline(valueAxis)) range.IncludeY(step.End);
                    }
                }
                continue;
            }
            if (ChartSeriesKindTraits.IsExclusive(series.Kind)) continue;
            if (series.Kind is ChartSeriesKind.RangeBar or ChartSeriesKind.BoxPlot && ChartCartesianOrientation.IsHorizontal(chart, series)) {
                foreach (var point in series.RenderPoints) {
                    range.IncludeY(point.X); horizontalBarYValues.Add(point.X);
                    range.IncludeX(point.Y);
                }
                foreach (var distribution in series.BoxPlotDistributions) {
                    range.IncludeX(distribution.SourceMinimum); range.IncludeX(distribution.SourceMaximum);
                }
                continue;
            }
            if (series.ValueAxis == ChartAxisSide.Secondary && !ChartSeriesKindTraits.UsesHorizontalBaseline(series.Kind)) {
                IncludeSeriesX(range, series);
                if (RequiresMarkWidth(series.Kind) && series.HistogramBinLayout == null) {
                    var categories = series.Kind == ChartSeriesKind.Bar ? scalarBarXValues : barXValues;
                    categories.AddRange(series.RenderPoints.Select((point, pointIndex) => series.Kind == ChartSeriesKind.Bar
                        ? coordinateMap.Resolve(seriesIndex, pointIndex).Value : point.X));
                }
                continue;
            }

            if (contributesBaseline && ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) usesVerticalBaseline = true;
            foreach (var distribution in series.BoxPlotDistributions) {
                range.IncludeY(distribution.SourceMinimum); range.IncludeY(distribution.SourceMaximum);
            }

            if (series.Kind == ChartSeriesKind.Bar && series.HistogramBinLayout != null) {
                if (series.HistogramBinLayout.Minimum == series.HistogramBinLayout.Maximum) {
                    range.IncludeX(series.HistogramBinLayout.Minimum - 0.5);
                    range.IncludeX(series.HistogramBinLayout.Maximum + 0.5);
                } else {
                    range.IncludeX(series.HistogramBinLayout.Minimum);
                    range.IncludeX(series.HistogramBinLayout.Maximum);
                }
                for (var pointIndex = 0; pointIndex < series.RenderPoints.Count; pointIndex++) {
                    range.IncludeY(stacks.Point(seriesIndex, pointIndex).End);
                }

                if (UsesZeroBaseline(chart.Options.YAxis)) range.IncludeY(0);
                continue;
            }

            if (series.Kind == ChartSeriesKind.Bubble) {
                for (var i = 0; i + 1 < series.RenderPoints.Count; i += 2) {
                    var center = series.RenderPoints[i];
                    bubbleXValues.Add(center.X);
                    range.Include(center);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.ErrorBar) {
                for (var i = 0; i + 2 < series.RenderPoints.Count; i += 3) {
                    var center = series.RenderPoints[i];
                    barXValues.Add(center.X);
                    range.Include(center);
                    range.Include(series.RenderPoints[i + 1]);
                    range.Include(series.RenderPoints[i + 2]);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.Candlestick || series.Kind == ChartSeriesKind.Ohlc) {
                for (var i = 0; i + 3 < series.RenderPoints.Count; i += 4) {
                    var open = series.RenderPoints[i];
                    barXValues.Add(open.X);
                    range.Include(open);
                    range.Include(series.RenderPoints[i + 1]);
                    range.Include(series.RenderPoints[i + 2]);
                    range.Include(series.RenderPoints[i + 3]);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.RangeBand || series.Kind == ChartSeriesKind.RangeArea || series.Kind == ChartSeriesKind.DifferenceArea) {
                for (var i = 0; i + 1 < series.RenderPoints.Count; i += 2) {
                    range.Include(series.RenderPoints[i]);
                    range.Include(series.RenderPoints[i + 1]);
                }

                continue;
            }

            if (series.Kind == ChartSeriesKind.Dumbbell) {
                for (var i = 0; i + 1 < series.RenderPoints.Count; i += 2) {
                    barXValues.Add(series.RenderPoints[i].X);
                    range.Include(series.RenderPoints[i]);
                    range.Include(series.RenderPoints[i + 1]);
                }

                continue;
            }

            for (var pointIndex = 0; pointIndex < series.RenderPoints.Count; pointIndex++) {
                var p = series.RenderPoints[pointIndex];
                if (ChartCartesianOrientation.IsHorizontalLine(chart, series)) {
                    range.IncludeX(p.Y);
                    range.IncludeY(p.X);
                } else if (ChartSeriesKindTraits.UsesHorizontalBaseline(series.Kind)) {
                    range.IncludeX(stacks.Point(seriesIndex, pointIndex).End);
                    range.IncludeY(p.X);
                    if (UsesZeroBaseline(chart.Options.XAxis)) range.IncludeX(0);
                } else if (series.Kind == ChartSeriesKind.Bar || series.Kind == ChartSeriesKind.Lollipop || series.Kind == ChartSeriesKind.RangeBar || series.Kind == ChartSeriesKind.BoxPlot || series.Kind == ChartSeriesKind.Slope) {
                    if (series.Kind == ChartSeriesKind.Bar) scalarBarXValues.Add(coordinateMap.Resolve(seriesIndex, pointIndex).Value);
                    else barXValues.Add(p.X);
                    range.IncludeX(p.X);
                    range.IncludeY(series.Kind == ChartSeriesKind.Bar ? stacks.Point(seriesIndex, pointIndex).End : p.Y);
                } else if (series.Kind == ChartSeriesKind.StackedArea) {
                    range.IncludeX(p.X);
                    range.IncludeY(stacks.Point(seriesIndex, pointIndex).End);
                    if (UsesZeroBaseline(chart.Options.YAxis)) range.IncludeY(0);
                } else {
                    range.Include(p);
                }
            }

            if (contributesBaseline && UsesZeroBaseline(chart.Options.YAxis) && ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) range.IncludeY(0);
        }

        foreach (var annotation in chart.Annotations) {
            if (annotation.HasSourceGeometry) continue; // Drawings follow the data domain instead of changing it.
            range.Include(annotation, ChartCartesianOrientation.AnnotationKind(annotation, hasHorizontalSeries));
        }
        // An opted-in category catalog describes the automatic domain even where a leaf has
        // no observation. Explicit bounds below remain authoritative clipping policies.
        var categoryCatalog = ChartCartesianOrientation.CategoryLabels(chart);
        if (categoryCatalog.Any(label => label.Groups.Count > 0)) {
            foreach (var leaf in categoryCatalog) {
                if (hasHorizontalSeries) range.IncludeY(leaf.Value);
                else range.IncludeX(leaf.Value);
            }
        }
        ValidateLogarithmicObservations(range.MinX, range.MaxX, chart.Options.XAxis);
        if (!hasHorizontalSeries) ValidateLogarithmicObservations(range.MinY, range.MaxY, chart.Options.YAxis);
        range.InitializeEmptyX(chart.Options.XAxis);
        range.InitializeEmptyY(chart.Options.YAxis);
        // Category padding supplies a centered nonzero interval for a single category.
        // Expanding first would shift that category and make a full-slot bar exceed the plot.
        if (Math.Abs(range.MaxX - range.MinX) < double.Epsilon && barXValues.Count == 0 && scalarBarXValues.Count == 0)
            (range.MinX, range.MaxX) = ExpandedSingleton(range.MinX, chart.Options.XAxis);
        if (Math.Abs(range.MaxY - range.MinY) < double.Epsilon && horizontalBarYValues.Count == 0 && horizontalScalarCategories.Count == 0)
            (range.MinY, range.MaxY) = ExpandedSingleton(range.MinY, hasHorizontalSeries ? new ChartAxis() : chart.Options.YAxis);
        range.ApplyBarPadding(barXValues, chart.Options.XAxis, scalarBarXValues);
        range.ApplyHorizontalBarPadding(horizontalBarYValues, horizontalScalarCategories);
        if (!hasHorizontalSeries) {
            if (usesVerticalBaseline) range.ApplyLogarithmicYBaseline(chart.Options.YAxis);
            range.ApplyVerticalPadding(chart.Options.YAxis);
            if (applyOptionBounds) range.ApplyYAxisOptions(chart);
        }

        range.ApplyBarPadding(bubbleXValues, chart.Options.XAxis);
        if (hasHorizontalSeries) {
            if (usesHorizontalBaseline) range.ApplyLogarithmicXBaseline(chart.Options.XAxis);
            else if (chart.Series.Any(series => ChartCartesianOrientation.IsHorizontalLine(chart, series)))
                (range.MinX, range.MaxX) = PaddedValueBounds(range.MinX, range.MaxX, chart.Options.XAxis);
            if (applyOptionBounds) range.ApplyYAxisOptions(chart);
        }
        if (applyOptionBounds) range.ApplyXAxisOptions(chart);
        range.ApplyTimeSpacingBounds(chart.Options.XAxis, horizontal: true, applyOptionBounds);
        range.ApplyTimeSpacingBounds(chart.Options.YAxis, horizontal: false, applyOptionBounds);
        return range;
    }

    public static ChartRange FromSecondaryValueAxis(Chart chart, ChartRange primaryRange, bool applyOptionBounds = true) =>
        FromSecondaryValueAxis(chart, primaryRange, ChartStackLayout.Create(chart, ChartBarCoordinateMap.Create(chart)), applyOptionBounds);

    internal static ChartRange FromSecondaryValueAxis(Chart chart, ChartRange primaryRange, ChartStackLayout stacks, bool applyOptionBounds = true) {
        if (ChartCartesianOrientation.IsHorizontal(chart)) return FromSecondaryHorizontalAxis(chart, primaryRange, stacks, applyOptionBounds);
        var range = new ChartRange();
        var hasObservations = chart.Series.Any(series => series.ValueAxis == ChartAxisSide.Secondary && series.RenderPoints.Count > 0);
        var usesVerticalBaseline = false;
        for (var index = 0; index < chart.Series.Count; index++) {
            var series = chart.Series[index];
            if (series.ValueAxis != ChartAxisSide.Secondary) continue;
            if (series.RenderPoints.Count == 0 && hasObservations) continue;
            if (series.Kind == ChartSeriesKind.Bar || series.Kind == ChartSeriesKind.StackedArea) {
                for (var pointIndex = 0; pointIndex < series.RenderPoints.Count; pointIndex++) range.IncludeY(stacks.Point(index, pointIndex).End);
                if (UsesZeroBaseline(chart.Options.SecondaryYAxis)) range.IncludeY(0);
            } else IncludeSeriesY(range, series, chart.Options.SecondaryYAxis);
            if (ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) usesVerticalBaseline = true;
        }
        range.MinX = primaryRange.MinX;
        range.MaxX = primaryRange.MaxX;
        ValidateLogarithmicObservations(range.MinY, range.MaxY, chart.Options.SecondaryYAxis);
        range.InitializeEmptyY(chart.Options.SecondaryYAxis);

        if (Math.Abs(range.MaxY - range.MinY) < double.Epsilon) (range.MinY, range.MaxY) = ExpandedSingleton(range.MinY, chart.Options.SecondaryYAxis);
        if (usesVerticalBaseline) range.ApplyLogarithmicYBaseline(chart.Options.SecondaryYAxis);
        range.ApplyVerticalPadding(chart.Options.SecondaryYAxis);
        if (applyOptionBounds) range.ApplySecondaryYAxisOptions(chart);
        range.ApplyTimeSpacingBounds(chart.Options.SecondaryYAxis, horizontal: false, applyOptionBounds);
        return range;
    }

    /// <summary>Uses the captured discrete domain rather than elapsed-value padding; authored bounds remain authoritative.</summary>
    private void ApplyTimeSpacingBounds(ChartAxis axis, bool horizontal, bool applyAuthored) {
        if (axis.PreparedTimeDomain is not { } domain) return;
        var min = domain.FullMinimum; var max = domain.FullMaximum;
        if (!domain.Unit && max == min) { min = Math.Max(0, min - .5); max = min + 1; }
        if (applyAuthored) { min = axis.Minimum ?? min; max = axis.Maximum ?? max; }
        if (max <= min || ChartScaleTransform.Bound(max, axis, true) <= ChartScaleTransform.Bound(min, axis, false))
            throw new InvalidOperationException("Discrete time bounds must enclose at least one representable band or ordinal span.");
        if (horizontal) SetXBounds(min, max); else SetYBounds(min, max);
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

    private void Include(ChartAnnotation annotation, ChartAnnotationKind kind) {
        if (kind == ChartAnnotationKind.HorizontalLine || kind == ChartAnnotationKind.HorizontalBand) {
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
        (MinX, MaxX) = EmptyBounds(axis);
    }

    private void InitializeEmptyY(ChartAxis axis) {
        if (!double.IsInfinity(MinY)) return;
        (MinY, MaxY) = EmptyBounds(axis);
    }

    private void ApplyXAxisOptions(Chart chart) {
        if (chart.Options.XAxisMinimum.HasValue) MinX = chart.Options.XAxisMinimum.Value;
        if (chart.Options.XAxisMaximum.HasValue) MaxX = chart.Options.XAxisMaximum.Value;
        if (MaxX <= MinX) (MinX, MaxX) = ResolveClippedBounds(MinX, MaxX, chart.Options.XAxis);
    }

    private void ApplyYAxisOptions(Chart chart) {
        if (chart.Options.YAxisMinimum.HasValue) MinY = chart.Options.YAxisMinimum.Value;
        if (chart.Options.YAxisMaximum.HasValue) MaxY = chart.Options.YAxisMaximum.Value;
        if (MaxY <= MinY) (MinY, MaxY) = ResolveClippedBounds(MinY, MaxY, chart.Options.YAxis);
    }

    private void ApplySecondaryYAxisOptions(Chart chart) {
        if (chart.Options.SecondaryYAxisMinimum.HasValue) MinY = chart.Options.SecondaryYAxisMinimum.Value;
        if (chart.Options.SecondaryYAxisMaximum.HasValue) MaxY = chart.Options.SecondaryYAxisMaximum.Value;
        if (MaxY <= MinY) (MinY, MaxY) = ResolveClippedBounds(MinY, MaxY, chart.Options.SecondaryYAxis);
    }

    private static void IncludeSeriesX(ChartRange range, ChartSeries series) {
        if (series.Kind == ChartSeriesKind.Bar && series.HistogramBinLayout != null) {
            var single = series.HistogramBinLayout.Minimum == series.HistogramBinLayout.Maximum;
            range.IncludeX(series.HistogramBinLayout.Minimum - (single ? .5 : 0));
            range.IncludeX(series.HistogramBinLayout.Maximum + (single ? .5 : 0));
            return;
        }

        if (series.Kind == ChartSeriesKind.HorizontalBar) {
            foreach (var point in series.RenderPoints) range.IncludeX(point.Y);
            return;
        }

        foreach (var point in series.RenderPoints) range.IncludeX(point.X);
    }

    private static void IncludeSeriesY(ChartRange range, ChartSeries series, ChartAxis axis) {
        foreach (var distribution in series.BoxPlotDistributions) {
            range.IncludeY(distribution.SourceMinimum); range.IncludeY(distribution.SourceMaximum);
        }
        if (series.Kind == ChartSeriesKind.Waterfall) {
            foreach (var step in ChartWaterfallSteps.Create(series)) {
                if (step.Start != 0 || UsesZeroBaseline(axis)) range.IncludeY(step.Start);
                if (step.End != 0 || UsesZeroBaseline(axis)) range.IncludeY(step.End);
            }
            return;
        }
        if (series.Kind == ChartSeriesKind.HorizontalBar) {
            foreach (var point in series.RenderPoints) range.IncludeY(point.X);
            return;
        }

        if (series.Kind == ChartSeriesKind.Bubble) {
            for (var i = 0; i + 1 < series.RenderPoints.Count; i += 2) range.IncludeY(series.RenderPoints[i].Y);
            return;
        }

        foreach (var point in series.RenderPoints) range.IncludeY(point.Y);
        if (UsesZeroBaseline(axis) && ChartSeriesKindTraits.UsesVerticalBaseline(series.Kind)) range.IncludeY(0);
    }

    private static bool RequiresMarkWidth(ChartSeriesKind kind) => kind == ChartSeriesKind.Bar || kind == ChartSeriesKind.ErrorBar
        || kind == ChartSeriesKind.Candlestick || kind == ChartSeriesKind.Ohlc || kind == ChartSeriesKind.Dumbbell
        || kind == ChartSeriesKind.RangeBar || kind == ChartSeriesKind.BoxPlot || kind == ChartSeriesKind.Lollipop || kind == ChartSeriesKind.Slope;

    private void ApplyVerticalPadding(ChartAxis axis) {
        (MinY, MaxY) = PaddedValueBounds(MinY, MaxY, axis);
    }

    private static (double Minimum, double Maximum) PaddedValueBounds(double minimum, double maximum, ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Logarithmic) return (minimum, maximum);
        var padding = (maximum - minimum) * .08;
        if (!ChartMath.IsFinite(padding)) return (minimum, maximum);
        if (axis.Scale == ChartScaleKind.Time) {
            if (ChartMath.IsFinite(minimum - padding)) minimum -= padding;
        } else if (minimum > 0) {
            minimum = 0;
        }
        if (ChartMath.IsFinite(maximum + padding)) maximum += padding;
        return (minimum, maximum);
    }

    private static bool UsesZeroBaseline(ChartAxis axis) => axis.Scale != ChartScaleKind.Logarithmic && axis.Scale != ChartScaleKind.Time;

    private void ApplyLogarithmicXBaseline(ChartAxis axis) {
        if (axis.Scale != ChartScaleKind.Logarithmic) return;
        if (axis.LogarithmicDomain == ChartLogarithmicDomain.Negative) MaxX = LogarithmicBaseline(MaxX, axis);
        else MinX = LogarithmicBaseline(MinX, axis);
    }

    private void ApplyLogarithmicYBaseline(ChartAxis axis) {
        if (axis.Scale != ChartScaleKind.Logarithmic) return;
        if (axis.LogarithmicDomain == ChartLogarithmicDomain.Negative) MaxY = LogarithmicBaseline(MaxY, axis);
        else MinY = LogarithmicBaseline(MinY, axis);
    }

    internal static double LogarithmicBaseline(double nearestZero, ChartAxis axis) {
        var baseline = nearestZero / axis.LogarithmBase;
        return axis.IsInLogarithmicDomain(baseline) ? baseline : nearestZero;
    }

    private static (double Minimum, double Maximum) EmptyBounds(ChartAxis axis) => axis.Scale != ChartScaleKind.Logarithmic ? (0, 1)
        : axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? (-axis.LogarithmBase, -1) : (1, axis.LogarithmBase);

    private static void ValidateLogarithmicObservations(double minimum, double maximum, ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Logarithmic && !double.IsInfinity(minimum)
            && (!axis.IsInLogarithmicDomain(minimum) || !axis.IsInLogarithmicDomain(maximum)))
            throw new InvalidOperationException("Logarithmic axes require " + axis.LogarithmicDomainName + " source values, including values outside authored bounds.");
    }

    private static (double Minimum, double Maximum) ExpandedSingleton(double value, ChartAxis axis) {
        if (axis.Scale != ChartScaleKind.Logarithmic || axis.LogarithmicDomain == ChartLogarithmicDomain.Positive && axis.LogarithmBase == 10)
            return (value, value + 1); // Preserve the established default positive-axis layout.
        var nearer = LogarithmicBaseline(value, axis);
        if (nearer != value) return axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? (value, nearer) : (nearer, value);
        var farther = value * axis.LogarithmBase;
        if (axis.IsInLogarithmicDomain(farther) && farther != value)
            return axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? (farther, value) : (value, farther);
        throw new InvalidOperationException("A logarithmic singleton cannot form a finite nonzero axis domain.");
    }

    private static (double Minimum, double Maximum) ResolveClippedBounds(double minimum, double maximum, ChartAxis axis) {
        if (axis.Scale != ChartScaleKind.Logarithmic || axis.LogarithmicDomain != ChartLogarithmicDomain.Negative) return (minimum, minimum + 1);
        if (axis.Maximum.HasValue && !axis.Minimum.HasValue) {
            var lower = maximum * axis.LogarithmBase;
            if (axis.IsInLogarithmicDomain(lower) && lower < maximum) return (lower, maximum);
        } else {
            var upper = LogarithmicBaseline(minimum, axis);
            if (upper > minimum) return (minimum, upper);
        }
        throw new InvalidOperationException("The authored logarithmic bound leaves no representable finite domain.");
    }

    private static double CategoryPadding(List<double> values) {
        if (values.Count == 0) return 0;
        values.Sort();
        var spacing = double.PositiveInfinity;
        for (var i = 1; i < values.Count; i++) {
            var delta = values[i] - values[i - 1];
            if (delta > 0 && delta < spacing) spacing = delta;
        }
        if (double.IsInfinity(spacing)) spacing = 1;
        return spacing * .5;
    }

    private void ApplyBarPadding(List<double> xValues, ChartAxis axis, List<double>? scalarCategories = null) {
        if (axis.TimeSessions is { } sessions) {
            var includedPadding = Math.Max(CategoryPadding(xValues.Select(sessions.Forward).ToList()),
                scalarCategories == null ? 0 : CategoryPadding(scalarCategories.Select(sessions.Forward).ToList()));
            if (includedPadding > 0) {
                MinX = Math.Min(MinX, sessions.Inverse(sessions.Forward(MinX) - includedPadding));
                MaxX = Math.Max(MaxX, sessions.Inverse(sessions.Forward(MaxX) + includedPadding));
            }
            return;
        }
        // Scalar bars size their slots independently of interval and reference marks.
        // Preserve each padding requirement when the supported families are mixed.
        var padding = Math.Max(CategoryPadding(xValues), scalarCategories == null ? 0 : CategoryPadding(scalarCategories));
        if (padding == 0) return;
        if (axis.Scale == ChartScaleKind.Logarithmic) {
            IEnumerable<double> categories = scalarCategories == null ? xValues : xValues.Concat(scalarCategories);
            if (axis.LogarithmicDomain == ChartLogarithmicDomain.Negative) {
                var largestNegative = categories.Where(value => value < 0).DefaultIfEmpty().Max();
                if (largestNegative < 0) MaxX = Math.Max(MaxX, Math.Min(largestNegative * .5, MaxX + padding));
                MinX -= padding;
                return;
            }
            var smallestPositive = categories.Where(value => value > 0).DefaultIfEmpty().Min();
            if (smallestPositive > 0) MinX = Math.Min(MinX, Math.Max(smallestPositive * 0.5, MinX - padding));
        } else {
            MinX -= padding;
        }
        MaxX += padding;
    }

    private void ApplyHorizontalBarPadding(List<double> yValues, List<double> scalarCategories) {
        var padding = Math.Max(CategoryPadding(yValues), CategoryPadding(scalarCategories));
        MinY -= padding;
        MaxY += padding;
    }
}
