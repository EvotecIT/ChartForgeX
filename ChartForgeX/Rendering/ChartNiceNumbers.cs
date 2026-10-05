using System;

namespace ChartForgeX.Rendering;

/// <summary>Decimal steps shared by numeric axes and histogram bin boundaries.</summary>
internal static class ChartNiceNumbers {
    internal static double Step(double value, bool nearest = false) {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value)) return double.NaN;
        var power = Math.Pow(10, Math.Floor(Math.Log10(value)));
        if (power == 0) return value;
        var fraction = value / power;
        var nice = nearest
            ? fraction < 1.5 ? 1 : fraction < 2.25 ? 2 : fraction < 3.75 ? 2.5 : fraction < 7.5 ? 5 : 10
            : fraction <= 1 + 1e-12 ? 1 : fraction <= 2 + 1e-12 ? 2 : fraction <= 2.5 + 1e-12 ? 2.5 : fraction <= 5 + 1e-12 ? 5 : 10;
        return nice * power;
    }
}
