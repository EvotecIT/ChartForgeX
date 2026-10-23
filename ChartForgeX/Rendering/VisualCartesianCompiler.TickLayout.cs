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
        for (var count = NumericFittingCount(axis, minimum, maximum, length, horizontal, style); count >= 2; count--) {
            policy.TickCount = count;
            ticks = ChartTicks.GenerateInside(policy, minimum, maximum);
            if (AxisLabelSequenceFits(builder, axis, ticks, Position, horizontal, style, gap, fallback, labels)) break;
        }
        labels.SetFittedTicks(axis, ticks);
    }

    /// <summary>Lets automatic horizontal numeric scales try whole-unit ticks before measured captions choose a coarser step.</summary>
    private static int NumericFittingCount(ChartAxis axis, double minimum, double maximum, double length, bool horizontal, TextStyle style) {
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
