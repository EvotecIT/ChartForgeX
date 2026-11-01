using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>One owner for bounded numeric swatches, exact ramp stops and named band intervals.</summary>
internal static partial class ChartColorScaleLegend {
    internal const int MaximumSteps = 11;

    internal static int StepCount(ChartColorScale scale) => scale.Colors.Count <= 3 ? 5 : Math.Min(scale.Colors.Count, MaximumSteps);

    internal static double Value(ChartColorScale scale, double min, double max, double ratio) =>
        ChartMath.InterpolateRange(scale.EffectiveMinimum(min), scale.EffectiveMaximum(max), Math.Max(0, Math.Min(1, ratio)));

    internal static double[] Steps(ChartColorScale scale, double min, double max) {
        if (scale.Mode == ChartColorScaleMode.Discrete) throw new InvalidOperationException("Discrete scale legends draw bands rather than sampled numeric values.");
        var count = StepCount(scale); var values = new double[count];
        for (var i = 0; i < count; i++) values[i] = SwatchPerStop(scale, min, max)
            ? scale.StopValue(i, min, max) : Value(scale, min, max, i / (double)(count - 1));
        return values;
    }

    /// <summary>Returns the midpoint's swatch-center index, fractional when a sampled ramp places it between values.</summary>
    internal static double MidpointStep(ChartColorScale scale, double min, double max, int stepCount) {
        if (scale.HasPositionedStops)
            return (stepCount - 1) * Math.Max(0, Math.Min(1, ChartMath.Normalize(scale.EffectiveMidpoint(min, max),
                scale.EffectiveMinimum(min), scale.EffectiveMaximum(max))));
        if (SwatchPerStop(scale, min, max) && scale.MidpointIndex is int midpoint) return midpoint;
        if (scale.Colors.Count > 3 && scale.MidpointCollapses(min, max))
            return scale.EffectiveMidpoint(min, max) <= scale.EffectiveMinimum(min) ? 0 : stepCount - 1;
        if (scale.Mode == ChartColorScaleMode.Diverging && scale.EffectiveMaximum(max) > scale.EffectiveMinimum(min))
            return (stepCount - 1) * Math.Max(0, Math.Min(1, ChartMath.Normalize(scale.EffectiveMidpoint(min, max),
                scale.EffectiveMinimum(min), scale.EffectiveMaximum(max))));
        return stepCount / 2;
    }

    private static bool SwatchPerStop(ChartColorScale scale, double min, double max) =>
        !scale.HasPositionedStops && scale.Colors.Count > 3 && scale.Colors.Count <= MaximumSteps && !scale.MidpointCollapses(min, max);

    internal static string[] BandCaptions(ChartOptions options, ChartColorScale scale) =>
        BandCaptions(options.Labels, scale, values => ChartNumericFormatter.FormatScaleValues(options, values));

    /// <summary>Short key captions such as "Outage &lt; 95%", "Short loss 95–100%" and "Complete ≥ 100%".</summary>
    /// <remarks>Each band includes its lower bound and excludes its upper bound; the neighbouring captions and the
    /// band metadata keep that exact. The unit follows the last number of each interval.</remarks>
    internal static string[] BandCaptions(ChartLabels labels, ChartColorScale scale, Func<IReadOnlyList<double>, string[]> formatValues, string? unit = null) {
        var bands = scale.Bands;
        var bounds = formatValues(bands.Take(bands.Count - 1).Select(band => band.UpperBound!.Value).ToArray());
        var captions = new string[bands.Count];
        for (var index = 0; index < bands.Count; index++) {
            var interval = bands.Count == 1 ? labels.AllValues : index == 0 ? "< " + ChartOptions.AppendUnit(bounds[0], unit)
                : index == bands.Count - 1 ? "≥ " + ChartOptions.AppendUnit(bounds[index - 1], unit)
                : bounds[index - 1] + "–" + ChartOptions.AppendUnit(bounds[index], unit);
            // A lone band reads "Range · All values"; an interval follows its name directly ("Outage < 95%").
            captions[index] = bands[index].Label == null ? interval : bands[index].Label + (bands.Count == 1 ? " · " : " ") + interval;
        }
        return captions;
    }
}
