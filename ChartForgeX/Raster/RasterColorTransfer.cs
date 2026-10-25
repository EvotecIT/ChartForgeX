using System;

namespace ChartForgeX.Raster;

/// <summary>The incumbent sRGB decode table, shared without changing COLR conversion or final rounding.</summary>
internal static class RasterColorTransfer {
    private static readonly double[] Table = CreateLinearTable();
    private static double[] CreateLinearTable() {
        var table = new double[256];
        for (var i = 0; i < table.Length; i++) { var c = i / 255.0; table[i] = c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4); }
        return table;
    }
    internal static double Linear(byte value) => Table[value];
}
