using System;

namespace ChartForgeX.SvgRaster;

/// <summary>Parses the shared SVG preserveAspectRatio alignment and scaling contract.</summary>
internal readonly struct SvgRasterPreserveAspectRatio {
    private SvgRasterPreserveAspectRatio(bool stretch, bool slice, int horizontalAlignment, int verticalAlignment) {
        Stretch = stretch;
        Slice = slice;
        _horizontalAlignment = horizontalAlignment;
        _verticalAlignment = verticalAlignment;
    }

    private readonly int _horizontalAlignment;
    private readonly int _verticalAlignment;

    public bool Stretch { get; }
    public bool Slice { get; }

    public double AlignX(double available) => Align(available, _horizontalAlignment);
    public double AlignY(double available) => Align(available, _verticalAlignment);

    public static SvgRasterPreserveAspectRatio Parse(string? preserveAspectRatio) {
        var value = string.IsNullOrWhiteSpace(preserveAspectRatio) ? "xMidYMid meet" : preserveAspectRatio!.Trim();
        return new SvgRasterPreserveAspectRatio(
            string.Equals(value, "none", StringComparison.OrdinalIgnoreCase),
            value.IndexOf("slice", StringComparison.OrdinalIgnoreCase) >= 0,
            Alignment(value, "xMid", "xMax"),
            Alignment(value, "YMid", "YMax"));
    }

    private static int Alignment(string value, string middle, string maximum) {
        if (value.IndexOf(maximum, StringComparison.OrdinalIgnoreCase) >= 0) return 2;
        return value.IndexOf(middle, StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0;
    }

    private static double Align(double available, int alignment) => alignment == 2 ? available : alignment == 1 ? available / 2d : 0;
}
