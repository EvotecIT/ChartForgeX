using System;

namespace ChartForgeX.Raster;

/// <summary>The incumbent sRGB decode table, shared without changing COLR conversion or final rounding.</summary>
internal static class RasterColorTransfer {
    private static readonly double[] Table = CreateLinearTable();
    private static double[] CreateLinearTable() {
        var table = new double[256];
        for (var i = 0; i < table.Length; i++) table[i] = Linear(i / 255.0);
        return table;
    }
    internal static double Linear(byte value) => Table[value];
    /// <summary>Continuous sRGB-to-linear transfer for finite clamped channels between source color stages.</summary>
    internal static double Linear(double value) => value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    /// <summary>Existing continuous linear-to-sRGB transfer; callers retain their established final rounding.</summary>
    internal static double Encoded(double value) => value <= 0.0031308 ? value * 12.92
        : 1.055 * Math.Pow(Math.Max(0, value), 1 / 2.4) - 0.055;
}
