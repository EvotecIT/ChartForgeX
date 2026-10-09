using System;
using System.Linq;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

/// <summary>One owner for bounded numeric swatches, exact ramp stops and named band intervals.</summary>
internal static class ChartColorScaleLegend {
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

    internal static int MidpointStep(ChartColorScale scale, double min, double max, int stepCount) {
        if (SwatchPerStop(scale, min, max) && scale.MidpointIndex is int midpoint) return midpoint;
        if (scale.Colors.Count > 3 && scale.MidpointCollapses(min, max))
            return scale.EffectiveMidpoint(min, max) <= scale.EffectiveMinimum(min) ? 0 : stepCount - 1;
        return stepCount / 2;
    }

    private static bool SwatchPerStop(ChartColorScale scale, double min, double max) =>
        scale.Colors.Count > 3 && scale.Colors.Count <= MaximumSteps && !scale.MidpointCollapses(min, max);

    internal static string[] BandCaptions(ChartOptions options, ChartColorScale scale) {
        var bands = scale.Bands;
        var bounds = ChartNumericFormatter.FormatScaleValues(options, bands.Take(bands.Count - 1).Select(band => band.UpperBound!.Value).ToArray());
        var captions = new string[bands.Count];
        for (var index = 0; index < bands.Count; index++) {
            var interval = bands.Count == 1 ? "All values" : index == 0 ? "< " + bounds[0]
                : index == bands.Count - 1 ? "≥ " + bounds[index - 1] : bounds[index - 1] + " ≤ value < " + bounds[index];
            captions[index] = bands[index].Label == null ? interval : bands[index].Label + " · " + interval;
        }
        return captions;
    }
}
