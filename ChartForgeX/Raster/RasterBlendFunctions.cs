using System;

namespace ChartForgeX.Raster;

/// <summary>Existing normalized separable blend-channel functions; callers retain their own color space and precision.</summary>
internal static partial class RasterBlendFunctions {
    internal static double Separable(double s, double d, int mode) {
        switch (mode) {
            case 13: return s + d - s * d;
            case 14: return d <= 0.5 ? 2 * s * d : 1 - 2 * (1 - s) * (1 - d);
            case 15: return Math.Min(s, d);
            case 16: return Math.Max(s, d);
            case 17: return d <= 0 ? 0 : s >= 1 ? 1 : Math.Min(1, d / (1 - s));
            case 18: return d >= 1 ? 1 : s <= 0 ? 0 : 1 - Math.Min(1, (1 - d) / s);
            case 19: return s <= 0.5 ? 2 * s * d : 1 - 2 * (1 - s) * (1 - d);
            case 20: return s <= 0.5 ? d - (1 - 2 * s) * d * (1 - d) : d + (2 * s - 1) * ((d <= 0.25 ? ((16 * d - 12) * d + 4) * d : Math.Sqrt(d)) - d);
            case 21: return Math.Abs(d - s);
            case 22: return d + s - 2 * d * s;
            default: return d * s;
        }
    }
}
