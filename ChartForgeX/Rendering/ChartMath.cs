using System;

namespace ChartForgeX.Rendering;

internal static class ChartMath {
    internal static double Normalize(double value, double minimum, double maximum) {
        if (!IsFinite(value) || !IsFinite(minimum) || !IsFinite(maximum) || maximum <= minimum) return 0.5;
        var scale = Math.Max(Math.Abs(value), Math.Max(Math.Abs(minimum), Math.Abs(maximum)));
        if (scale == 0) return 0.5;
        var normalizedMinimum = minimum / scale;
        var normalizedMaximum = maximum / scale;
        var span = normalizedMaximum - normalizedMinimum;
        if (!IsFinite(span) || span <= 0) return 0.5;
        return (value / scale - normalizedMinimum) / span;
    }

    internal static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    internal static bool SameCoordinate(double left, double right) {
        if (left == right) return true;
        if (!IsFinite(left) || !IsFinite(right)) return false;
        var magnitude = Math.Max(Math.Abs(left), Math.Abs(right));
        if (magnitude == 0) return false;
        var bits = BitConverter.DoubleToInt64Bits(magnitude);
        var next = BitConverter.Int64BitsToDouble(bits + 1);
        var tolerance = next - magnitude;
        return IsFinite(tolerance) && Math.Abs(left - right) <= tolerance * 4;
    }
}
