using System;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Typography;

/// <summary>Immutable default-instance COLR paint graph; palette references are resolved at draw time.</summary>
internal sealed class ColorGlyphPaint {
    internal ColorPaintKind Kind { get; set; }
    internal ColorGlyphPaint[] Children { get; set; } = Array.Empty<ColorGlyphPaint>();
    internal ushort Glyph { get; set; }
    internal int PaletteIndex { get; set; }
    internal double Alpha { get; set; } = 1;
    internal double[] Geometry { get; set; } = Array.Empty<double>();
    internal ColorPaintStop[] Stops { get; set; } = Array.Empty<ColorPaintStop>();
    internal int Extend { get; set; }
    internal int CompositeMode { get; set; }
    internal SvgRasterMatrix Transform { get; set; } = SvgRasterMatrix.Identity;
    internal ChartRect Clip { get; set; }
    internal static readonly ColorGlyphPaint Empty = new();
}

internal enum ColorPaintKind { Empty, Layers, Solid, Linear, Radial, Sweep, Glyph, Transform, Composite, Clip }

internal readonly struct ColorPaintStop {
    internal ColorPaintStop(double offset, int paletteIndex, double alpha, int order) {
        Offset = offset; PaletteIndex = paletteIndex; Alpha = alpha; Order = order;
    }
    internal double Offset { get; }
    internal int PaletteIndex { get; }
    internal double Alpha { get; }
    internal int Order { get; }
}
