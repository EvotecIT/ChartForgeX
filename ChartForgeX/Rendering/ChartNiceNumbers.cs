using System;

namespace ChartForgeX.Rendering;

/// <summary>Decimal steps shared by numeric axes and histogram bin boundaries.</summary>
internal static class ChartNiceNumbers {
    internal static double Step(double value, bool nearest = false) {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value)) return double.NaN;
        var power = Math.Pow(10, Math.Floor(Math.Log10(value)));
        if (power == 0) return value;
        var fraction = value / power;
        // Decimal subtraction and division can land a few ULPs above an exact
        // nice interval. Absorb that arithmetic noise without rounding down a
        // genuinely larger interval or swallowing the caller's 1% retry step.
        var tolerance = fraction * 8 * 2.2204460492503131e-16;
        var nice = nearest
            ? fraction < 1.5 ? 1 : fraction < 2.25 ? 2 : fraction < 3.75 ? 2.5 : fraction < 7.5 ? 5 : 10
            : fraction <= 1 + tolerance ? 1 : fraction <= 2 + tolerance ? 2 : fraction <= 2.5 + tolerance ? 2.5 : fraction <= 5 + tolerance ? 5 : 10;
        return nice * power;
    }
}
