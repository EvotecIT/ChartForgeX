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

    internal static (double Minimum, double Maximum) ResolveFiniteLaneWindow(double minimum, double maximum, bool minimumExplicit, bool maximumExplicit) {
        if (IsFinite(minimum) && IsFinite(maximum) && maximum > minimum) return (minimum, maximum);
        if (maximumExplicit && !minimumExplicit) {
            var earlier = maximum - Math.Max(1.0 / 24.0, Math.Abs(maximum) * 1e-12);
            if (!IsFinite(earlier) || !(earlier < maximum))
                throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The upper axis bound cannot form a finite visible window.");
            return (earlier, maximum);
        }

        if (minimumExplicit && !maximumExplicit) {
            var later = minimum + Math.Max(1.0 / 24.0, Math.Abs(minimum) * 1e-12);
            if (!IsFinite(later) || !(later > minimum))
                throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "The lower axis bound cannot form a finite visible window.");
            return (minimum, later);
        }

        if (minimumExplicit && maximumExplicit)
            throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "The axis bounds must form a finite visible window.");
        if (!IsFinite(minimum) || !IsFinite(maximum)) return (0, 1);

        var step = Math.Max(1.0 / 24.0, Math.Abs(minimum) * 1e-12);
        var upper = minimum + step;
        if (IsFinite(upper) && upper > minimum) return (minimum, upper);
        var lower = minimum - step;
        if (IsFinite(lower) && lower < minimum) return (lower, minimum);
        throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "The lane values cannot form a finite visible window.");
    }

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
