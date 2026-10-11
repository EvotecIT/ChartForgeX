using System;
using System.Collections.Generic;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static partial class ChartColorScaleLegend {
    /// <summary>Projects the scale's reachable continuous anchors into exact numeric gradient positions.</summary>
    internal static IReadOnlyList<VisualGradientStop> GradientStops(ChartColorScale scale, double min, double max) {
        if (scale.Mode == ChartColorScaleMode.Discrete) throw new InvalidOperationException("Discrete bands cannot interpolate a gradient key.");
        var low = scale.EffectiveMinimum(min); var high = scale.EffectiveMaximum(max);
        var result = new List<VisualGradientStop>();
        Add(0, low);
        if (high > low) for (var index = 0; index < scale.Colors.Count; index++) {
            var value = scale.StopValue(index, min, max);
            // Collapsed arms contain unreachable colors. Domain endpoints use the same decision as marks.
            if (value > low && value < high) Add(Math.Max(0, Math.Min(1, ChartMath.Normalize(value, low, high))), value);
        }
        Add(1, high);
        return result;

        void Add(double offset, double value) {
            var blend = scale.BlendFor(value, min, max);
            result.Add(new VisualGradientStop(offset, blend.Color, blend.Paint));
        }
    }

    internal static double MidpointRatio(ChartColorScale scale, double min, double max) {
        var low = scale.EffectiveMinimum(min); var high = scale.EffectiveMaximum(max);
        return high > low ? Math.Max(0, Math.Min(1, ChartMath.Normalize(scale.EffectiveMidpoint(min, max), low, high))) : .5;
    }
}
