using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartScaleTransform {
    public static double Normalize(double value, double minimum, double maximum, ChartAxis axis) {
        return Normalize(Forward(value, axis), Bound(minimum, axis, false), Bound(maximum, axis, true), axis.Reversed);
    }

    internal static double Normalize(double value, double minimum, double maximum, bool reversed) {
        var ratio = ChartMath.Normalize(value, minimum, maximum);
        return reversed ? 1 - ratio : ratio;
    }

    /// <summary>Calendar bands enclose a source window by complete units while marks remain at band centers.</summary>
    internal static double Bound(double value, ChartAxis axis, bool maximum) => axis.Scale == ChartScaleKind.Time && axis.PreparedTimeDomain is { } domain
        ? domain.Bound(value, maximum) : Forward(value, axis);
    internal static double InverseBound(double value, ChartAxis axis, bool maximum) => axis.Scale == ChartScaleKind.Time && axis.PreparedTimeDomain is { } domain
        ? domain.InverseBound(value, maximum) : Inverse(value, axis);

    public static double Forward(double value, ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Time && axis.PreparedTimeDomain is { } domain) return domain.Forward(value);
        if (axis.Scale == ChartScaleKind.Time && axis.TimeSessions != null) return axis.TimeSessions.Forward(value);
        switch (axis.Scale) {
            case ChartScaleKind.Logarithmic:
                if (!axis.IsInLogarithmicDomain(value)) throw new InvalidOperationException("Logarithmic axes require " + axis.LogarithmicDomainName + " source values and bounds.");
                return axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? -Logarithm(-value, axis) : Logarithm(value, axis);
            case ChartScaleKind.SymmetricLogarithmic:
                return Math.Sign(value) * Logarithm(1 + Math.Abs(value) / axis.SymmetricLogarithmThreshold, axis);
            default:
                return value;
        }
    }

    public static double Inverse(double value, ChartAxis axis) {
        if (axis.Scale == ChartScaleKind.Time && axis.PreparedTimeDomain is { } domain) return domain.Inverse(value);
        if (axis.Scale == ChartScaleKind.Time && axis.TimeSessions != null) return axis.TimeSessions.Inverse(value);
        switch (axis.Scale) {
            case ChartScaleKind.Logarithmic:
                return axis.LogarithmicDomain == ChartLogarithmicDomain.Negative ? -Math.Pow(axis.LogarithmBase, -value) : Math.Pow(axis.LogarithmBase, value);
            case ChartScaleKind.SymmetricLogarithmic:
                return Math.Sign(value) * axis.SymmetricLogarithmThreshold * (Math.Pow(axis.LogarithmBase, Math.Abs(value)) - 1);
            default:
                return value;
        }
    }

    /// <summary>Keeps the established base-ten arithmetic exact while allowing an authored logarithm base.</summary>
    internal static double Logarithm(double value, ChartAxis axis) => axis.LogarithmBase == 10 ? Math.Log10(value) : Math.Log(value, axis.LogarithmBase);
}
