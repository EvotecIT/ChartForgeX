using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    private static double AxisLabelGap(ChartAxis axis, double spacing) =>
        axis.LabelDensity == ChartLabelDensity.Dense ? 0 : axis.LabelDensity == ChartLabelDensity.Relaxed ? spacing * 2 : spacing / 2;

    /// <summary>Chooses the numeric/calendar scale before painting so text never has to leave its tick to avoid a neighbour.</summary>
    private static void FitGeneratedAxisTicks(VisualSceneBuilder builder, ChartAxis axis, double minimum, double maximum,
        double length, bool horizontal, TextStyle style, double spacing, Func<double, string>? fallback, AxisLabelCache labels,
        IReadOnlyList<double>? regularObservations = null) {
        labels.ResetFittedTicks(axis);
        if (labels.HasAuthoredTicks(axis) || axis.LabelDensity == ChartLabelDensity.All || length <= 0) return;
        // Authored oversized type can fit down to the established readable floor.
        // Select a scale that fits at that floor before its final pitch fits the type;
        // otherwise stride thinning can discard the last tick of the generated scale.
        if (style.EffectiveFontSize > VisualTextFitting.MaximumOrdinaryFontSize) {
            style = style.Clone();
            style.FontSize = VisualTextFitting.MinimumOrdinaryFontSize * style.FontSize / style.EffectiveFontSize;
        }
        var policy = axis.Clone();
        var gap = AxisLabelGap(axis, spacing);
        IReadOnlyList<double> ticks = labels.Ticks(axis, minimum, maximum);
        double Position(double value) => ChartScaleTransform.Normalize(value, minimum, maximum, axis) * length;
        if (axis.Scale == ChartScaleKind.Time && ChartTimeScale.GenerateFitting(axis, minimum, maximum,
            candidates => ChartTimeScale.HasDistinctAdjacentLabels(candidates.Select(tick => labels.Format(axis, tick, fallback, candidates)))
                && AxisLabelSequenceFits(builder, axis, candidates, Position, horizontal, style, gap, fallback, labels,
                    comfortable: axis.LabelDensity != ChartLabelDensity.Dense && !axis.HasExplicitTickCount),
            allowCompactDates: axis.ValueFormat == null && fallback == null, regularObservations: regularObservations,
            automaticTickCount: ChartTimeScale.AutomaticTickCount(axis)) is { } timeTicks) {
            labels.SetFittedTicks(axis, timeTicks);
            return;
        }
        // Count data (every observation a whole number) reads in whole-number steps: no "2.5 incidents".
        if (labels.CountAxes.Contains(axis) && axis.Scale == ChartScaleKind.Linear && maximum - minimum >= 2) policy.IntegerTicks = true;
        // A snapped value axis (design rule 1) checks the captions it will actually paint: the candidate's clean ends
        // included, positioned on the snapped domain. A later pass over that domain then selects the same ticks.
        var snap = labels.SnapAxes.Contains(axis) && ChartValueAxisRange.Applies(axis);
        labels.SnappedDomains.Remove(axis);
        // A frozen domain (a source viewport) reuses the full pass's clean step, so both show the same value ticks.
        if (snap && labels.FreezeDomains && axis.PreparedValueTickStep is double frozenStep && frozenStep > 0 && (maximum - minimum) / frozenStep <= 1000) {
            var frozen = new List<double>();
            for (var index = 0; minimum + index * frozenStep <= maximum + frozenStep * 1e-6; index++) frozen.Add(Math.Round((minimum + index * frozenStep) / frozenStep) * frozenStep);
            if (frozen.Count >= 2) { labels.SetFittedTicks(axis, frozen); return; }
        }
        var first = NumericFittingCount(axis, minimum, maximum, length, horizontal, style);
        if (snap && !labels.FreezeDomains) axis.PreparedValueTickStep = null;
        for (var count = first; count >= 2; count--) {
            policy.TickCount = count;
            ticks = ChartTicks.GenerateInside(policy, minimum, maximum);
            if (!snap) {
                if (AxisLabelSequenceFits(builder, axis, ticks, Position, horizontal, style, gap, fallback, labels)) break;
                continue;
            }
            var low = minimum; var high = maximum;
            var snapped = ChartValueAxisRange.Snap(axis, ref low, ref high, labels.FreezeDomains ? WithinDomain(ticks, minimum, maximum) : ticks);
            // A frozen (already clean) domain only accepts a step whose ticks land on its existing ends.
            var tolerance = (maximum - minimum) * 1e-6;
            if (labels.FreezeDomains && (Math.Abs(low - minimum) > tolerance || Math.Abs(high - maximum) > tolerance)) continue;
            ticks = snapped;
            if (!labels.FreezeDomains) { labels.SnappedDomains[axis] = (low, high); axis.PreparedValueTickStep = snapped.Count >= 2 ? snapped[1] - snapped[0] : null; }
            if (AxisLabelSequenceFits(builder, axis, snapped, value => ChartScaleTransform.Normalize(value, low, high, axis) * length,
                horizontal, style, gap, fallback, labels)) break;
        }
        labels.SetFittedTicks(axis, ticks);
    }

    /// <summary>
    /// Starts the automatic numeric search at a readable density for the axis length: at most one tick per 80 px across
    /// on domains wider than thirteen whole units, and four to nine ticks at about one per 50 px down an axis of at
    /// least 160 px. Explicit counts, dense or full label density and integer policies keep their own search.
    /// </summary>
    private static int NumericFittingCount(ChartAxis axis, double minimum, double maximum, double length, bool horizontal, TextStyle style) {
        var count = UnitFittingCount(axis, minimum, maximum, length, horizontal, style);
        if (axis.HasExplicitTickCount || axis.LabelDensity != ChartLabelDensity.Auto || axis.IntegerTicks
            || axis.Scale != ChartScaleKind.Linear || length < MinimumDensityAxisLength) return count;
        // Small whole-unit domains (up to a year of months) keep every fitting unit; wider domains are capped.
        if (horizontal) return count <= MaximumUnitTicks ? count : Math.Min(count, Math.Max(MinimumDensityTicks, (int)Math.Floor(length / HorizontalTickPitch) + 1));
        // Tall axes may start above the default count; short ones keep it, because a lower start can select a coarser nice step.
        return Math.Max(count, Math.Min(MaximumDensityTicks, (int)Math.Round(length / VerticalTickPitch) + 1));
    }

    private const double MinimumDensityAxisLength = 160, HorizontalTickPitch = 80, VerticalTickPitch = 50;
    private const int MinimumDensityTicks = 4, MaximumDensityTicks = 9, MaximumUnitTicks = 13;

    /// <summary>Lets automatic horizontal numeric scales try whole-unit ticks before measured captions choose a coarser step.</summary>
    private static int UnitFittingCount(ChartAxis axis, double minimum, double maximum, double length, bool horizontal, TextStyle style) {
        if (!horizontal || axis.Scale != ChartScaleKind.Linear || axis.HasExplicitTickCount) return axis.TickCount;
        var span = maximum - minimum;
        if (!ChartMath.IsFinite(span)) return axis.TickCount;
        // Keep the established fractional resolution on short domains. On larger
        // domains, padding must not turn a fitting unit step into a step of two.
        var unitCount = Math.Max(axis.TickCount, Math.Ceiling(span) + 1);
        // Small domains must reach their unit-step candidate before a broad-domain
        // density budget can limit them. Measured captions decide whether it fits.
        if (unitCount <= 100) return (int)unitCount;
        // A readable five-em pitch bounds the automatic search on wide numeric
        // domains. Allow one extra boundary budget for nice-step alignment.
        var budget = axis.LabelDensity == ChartLabelDensity.Dense ? 100
            : Math.Max(axis.TickCount + 1, Math.Floor(length / (style.EffectiveFontSize * 5)) + 2);
        return (int)Math.Min(100, Math.Min(budget, unitCount));
    }

    private static void FitCartesianTicks(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartRange range, ChartRange? secondaryRange, VisualThemeColors colors, AxisLabelCache labels, bool horizontal) {
        if (!chart.Options.ShowAxes) return;
        var style = chart.Options.TickLabelStyle.Resolve(new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = colors.MutedForeground });
        var spacing = context.Theme.Spacing;
        if (horizontal) {
            var axes = HorizontalAxes(chart, labels);
            if (chart.Options.XAxis.Visible) FitGeneratedAxisTicks(builder, axes.Value, range.MinX, range.MaxX, plot.Width, true, style, spacing, chart.Options.ValueFormatter, labels);
            if (secondaryRange != null && chart.Options.SecondaryXAxis.Visible)
                FitGeneratedAxisTicks(builder, chart.Options.SecondaryXAxis, secondaryRange.MinX, secondaryRange.MaxX, plot.Width, true, style, spacing, chart.Options.ValueFormatter, labels);
            // Horizontal line categories may themselves be a continuous time/numeric scale.
            if (chart.Options.YAxis.Visible && UsesGeneratedHorizontalCategoryTicks(chart, axes.Category))
                FitGeneratedAxisTicks(builder, axes.Category, range.MinY, range.MaxY, plot.Height, false, style, spacing, null, labels);
            return;
        }
        if (chart.Options.XAxis.Visible) FitGeneratedAxisTicks(builder, chart.Options.XAxis, range.MinX, range.MaxX, plot.Width, true, style, spacing, null, labels, labels.TimeBarObservations(chart));
        if (chart.Options.YAxis.Visible) FitGeneratedAxisTicks(builder, chart.Options.YAxis, range.MinY, range.MaxY, plot.Height, false, style, spacing, chart.Options.ValueFormatter, labels);
        if (secondaryRange != null && chart.Options.SecondaryYAxis.Visible)
            FitGeneratedAxisTicks(builder, chart.Options.SecondaryYAxis, secondaryRange.MinY, secondaryRange.MaxY, plot.Height, false, style, spacing, chart.Options.ValueFormatter, labels);
    }

    /// <summary>
    /// Applies the library-wide value-axis rule after the readable tick step is chosen: every automatic linear value axis
    /// (Y on vertical charts, X on horizontal ones, and X for scatter/bubble) ends on its next clean tick.
    /// </summary>
    private static void SnapValueDomains(Chart chart, ChartRange range, ChartRange? secondaryRange, AxisLabelCache labels, bool horizontal) {
        if (chart.Options.IsSparkline) return;
        if (horizontal) {
            var axes = HorizontalAxes(chart, labels);
            SnapX(axes.Value, range);
            if (secondaryRange != null) SnapX(chart.Options.SecondaryXAxis, secondaryRange);
            return;
        }
        SnapY(chart.Options.YAxis, range);
        if (secondaryRange != null) SnapY(chart.Options.SecondaryYAxis, secondaryRange);
        if (chart.Series.Count > 0 && chart.Series.All(series => series.Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble)) {
            SnapX(chart.Options.XAxis, range);
            if (secondaryRange != null) secondaryRange.SetXBounds(range.MinX, range.MaxX);
        }

        void SnapY(ChartAxis axis, ChartRange target) {
            if (!ChartValueAxisRange.Applies(axis)) return;
            if (labels.SnappedDomains.TryGetValue(axis, out var fitted)) { target.SetYBounds(fitted.Minimum, fitted.Maximum); return; }
            var minimum = target.MinY; var maximum = target.MaxY;
            var ticks = ChartValueAxisRange.Snap(axis, ref minimum, ref maximum, labels.Ticks(axis, target.MinY, target.MaxY));
            target.SetYBounds(minimum, maximum);
            labels.SetFittedTicks(axis, ticks);
        }
        void SnapX(ChartAxis axis, ChartRange target) {
            if (!ChartValueAxisRange.Applies(axis)) return;
            if (labels.SnappedDomains.TryGetValue(axis, out var fitted)) { target.SetXBounds(fitted.Minimum, fitted.Maximum); return; }
            var minimum = target.MinX; var maximum = target.MaxX;
            var ticks = ChartValueAxisRange.Snap(axis, ref minimum, ref maximum, labels.Ticks(axis, target.MinX, target.MaxX));
            target.SetXBounds(minimum, maximum);
            labels.SetFittedTicks(axis, ticks);
        }
    }

    /// <summary>Treats ticks within a millionth of the span of a frozen domain's ends as those ends.</summary>
    private static IReadOnlyList<double> WithinDomain(IReadOnlyList<double> ticks, double minimum, double maximum) {
        var tolerance = (maximum - minimum) * 1e-6;
        return ticks.Select(tick => Math.Abs(tick - minimum) <= tolerance ? minimum : Math.Abs(tick - maximum) <= tolerance ? maximum : tick).ToArray();
    }

    /// <summary>Names the value axes that snap to clean ticks, so the tick fitter measures the captions they will paint.</summary>
    private static void CollectSnapAxes(Chart chart, AxisLabelCache labels, bool horizontal) {
        labels.SnapAxes.Clear();
        if (chart.Options.IsSparkline) return;
        if (horizontal) { labels.SnapAxes.Add(HorizontalAxes(chart, labels).Value); labels.SnapAxes.Add(chart.Options.SecondaryXAxis); return; }
        labels.SnapAxes.Add(chart.Options.YAxis); labels.SnapAxes.Add(chart.Options.SecondaryYAxis);
        if (chart.Series.Count > 0 && chart.Series.All(series => series.Kind is ChartSeriesKind.Scatter or ChartSeriesKind.Bubble)) labels.SnapAxes.Add(chart.Options.XAxis);
    }

    /// <summary>Marks value axes whose observations are all whole numbers so their generated ticks stay whole.</summary>
    private static void CollectCountAxes(Chart chart, AxisLabelCache labels, bool horizontal) {
        foreach (var side in new[] { ChartAxisSide.Primary, ChartAxisSide.Secondary }) {
            var series = chart.Series.Where(item => item.ValueAxis == side && item.RenderPoints.Count > 0).ToArray();
            if (series.Length == 0) continue;
            var values = series.SelectMany(item => item.Kind == ChartSeriesKind.Bubble
                ? item.RenderPoints.Where((_, index) => index % 2 == 0).Select(point => point.Y) : item.RenderPoints.Select(point => point.Y));
            if (!ChartValueAxisRange.IsCountData(values)) continue;
            labels.CountAxes.Add(horizontal ? side == ChartAxisSide.Primary ? HorizontalAxes(chart, labels).Value : chart.Options.SecondaryXAxis
                : side == ChartAxisSide.Primary ? chart.Options.YAxis : chart.Options.SecondaryYAxis);
        }
    }

    private static IReadOnlyList<double>? RegularTimeBarObservations(Chart chart) {
        if (chart.Options.XAxis.Scale != ChartScaleKind.Time || chart.Series.Count == 0
            || chart.Series.Any(series => series.Kind != ChartSeriesKind.Bar)) return null;
        var values = chart.Series.SelectMany(series => series.RenderPoints.Select(point => point.X)).Distinct().OrderBy(value => value).ToArray();
        if (values.Length < 2) return null;
        var cadence = values[1] - values[0];
        if (cadence <= 0) return null;
        for (var index = 2; index < values.Length; index++)
            if (Math.Abs((values[index] - values[index - 1]) / cadence - 1) > 1e-6) return null;
        return values;
    }

    /// <summary>Keeps captions on a regular stride; short named categories retain both endpoints without crowding.</summary>
    private static HashSet<double> SelectAxisLabelStride(VisualSceneBuilder builder, ChartAxis axis, IReadOnlyList<double> ticks,
        Func<double, double> coordinate, bool horizontal, TextStyle style, double gap, Func<double, string>? fallback, AxisLabelCache labels,
        Func<double, string>? display = null) {
        if (axis.LabelDensity == ChartLabelDensity.All) return new HashSet<double>(ticks);
        var keepEndpoints = labels.HasAuthoredTicks(axis) && axis.Labels.Count > 0 && axis.Scale != ChartScaleKind.Time
            && ticks.All(tick => new System.Globalization.StringInfo(labels.Format(axis, tick, fallback, ticks)).LengthInTextElements <= 3);
        for (var stride = 1; stride <= ticks.Count; stride++) {
            var selected = ticks.Where((_, index) => index % stride == 0).ToList();
            if (keepEndpoints && ticks.Count > 1 && !selected.Contains(ticks[ticks.Count - 1])) {
                selected.Add(ticks[ticks.Count - 1]);
                // A final partial stride may crowd its neighbour. Keep the endpoint and
                // remove only that neighbour rather than shortening a month to "A…".
                while (selected.Count > 2 && !AxisLabelSequenceFits(builder, axis, selected.Skip(selected.Count - 2).ToArray(),
                    coordinate, horizontal, style, gap, fallback, labels, display)) selected.RemoveAt(selected.Count - 2);
            }
            if (AxisLabelSequenceFits(builder, axis, selected, coordinate, horizontal, style, gap, fallback, labels, display)) return new HashSet<double>(selected);
        }
        return new HashSet<double>();
    }

    private static bool AxisLabelSequenceFits(VisualSceneBuilder builder, ChartAxis axis, IReadOnlyList<double> ticks,
        Func<double, double> coordinate, bool horizontal, TextStyle style, double gap, Func<double, string>? fallback, AxisLabelCache labels,
        Func<double, string>? display = null, bool comfortable = false) {
        var previousPosition = 0d; var previousExtent = 0d; var hasPrevious = false;
        foreach (var tick in ticks) {
            var text = display?.Invoke(tick) ?? labels.Format(axis, tick, fallback, ticks);
            if (text.Length == 0) continue;
            var metrics = VisualAxisText.RotatedMetrics(builder.MeasureText(text, style), axis.LabelAngle);
            var extent = horizontal ? metrics.Width : metrics.Height;
            var position = coordinate(tick);
            var length = ticks.Count > 1 ? Math.Abs(coordinate(ticks[ticks.Count - 1]) - coordinate(ticks[0])) : 0;
            var requiredGap = comfortable ? ChartTimeScale.ComfortableLabelGap(extent, previousExtent, gap, length, style.EffectiveFontSize,
                axis.LabelDensity == ChartLabelDensity.Auto && !axis.HasExplicitTickCount) : gap;
            if (hasPrevious && Math.Abs(position - previousPosition) + .0001 < (extent + previousExtent) / 2 + requiredGap) return false;
            previousPosition = position; previousExtent = extent; hasPrevious = true;
        }
        return true;
    }
}
