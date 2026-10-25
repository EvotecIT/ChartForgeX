using System;
using ChartForgeX.Effects;

namespace ChartForgeX.Raster;

internal static partial class RasterEffectPixels {
    /// <summary>Maps retained effect membership to the incumbent normalized channel owner without changing COLR's IDs.</summary>
    private static int SeparableFunction(VisualBlendMode mode) {
        switch (mode) {
            case VisualBlendMode.Multiply: return 23;
            case VisualBlendMode.Screen: return 13;
            case VisualBlendMode.Overlay: return 14;
            case VisualBlendMode.Darken: return 15;
            case VisualBlendMode.Lighten: return 16;
            case VisualBlendMode.ColorDodge: return 17;
            case VisualBlendMode.ColorBurn: return 18;
            case VisualBlendMode.HardLight: return 19;
            case VisualBlendMode.SoftLight: return 20;
            case VisualBlendMode.Difference: return 21;
            case VisualBlendMode.Exclusion: return 22;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}
