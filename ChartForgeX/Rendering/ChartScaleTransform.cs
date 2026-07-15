using System;
using ChartForgeX.Core;

namespace ChartForgeX.Rendering;

internal static class ChartScaleTransform {
    public static double Normalize(double value, double minimum, double maximum, ChartAxis axis) {
        return ChartMath.Normalize(Forward(value, axis), Forward(minimum, axis), Forward(maximum, axis));
    }

    public static double Forward(double value, ChartAxis axis) {
        switch (axis.Scale) {
            case ChartScaleKind.Logarithmic:
                if (value <= 0) throw new InvalidOperationException("Logarithmic axes can render positive values only.");
                return Math.Log10(value);
            case ChartScaleKind.SymmetricLogarithmic:
                return Math.Sign(value) * Math.Log10(1 + Math.Abs(value) / axis.SymmetricLogarithmThreshold);
            default:
                return value;
        }
    }

    public static double Inverse(double value, ChartAxis axis) {
        switch (axis.Scale) {
            case ChartScaleKind.Logarithmic:
                return Math.Pow(10, value);
            case ChartScaleKind.SymmetricLogarithmic:
                return Math.Sign(value) * axis.SymmetricLogarithmThreshold * (Math.Pow(10, Math.Abs(value)) - 1);
            default:
                return value;
        }
    }
}
