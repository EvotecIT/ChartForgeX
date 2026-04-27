using System;

namespace ChartForgeX.Primitives;

internal static class ChartPrimitiveGuards {
    public static void Positive(int value, string parameterName) {
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
    }

    public static void Finite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
    }

    public static void NonNegativeFinite(double value, string parameterName) {
        Finite(value, parameterName);
        if (value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than or equal to zero.");
    }
}
