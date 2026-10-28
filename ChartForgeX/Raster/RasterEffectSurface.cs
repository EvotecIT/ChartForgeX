using System;

namespace ChartForgeX.Raster;

/// <summary>One frozen premultiplied RGBA16 kernel surface; RGB carries its actual working-space tag and alpha is coverage.</summary>
internal readonly struct RasterEffectSurface {
    internal RasterEffectSurface(RasterEffectRegion region, ushort[] pixels, bool linearRgb, bool alphaOnly = false) {
        _ = new RasterEffectRegion(region.X, region.Y, region.Width, region.Height);
        if (pixels == null || pixels.LongLength != checked((long)region.Width * region.Height * 4))
            throw new ArgumentException("A kernel surface requires exactly four 16-bit channels per pixel.", nameof(pixels));
        Region = region; Pixels = pixels; LinearRgb = linearRgb; AlphaOnly = alphaOnly;
    }
    internal RasterEffectRegion Region { get; }
    internal ushort[] Pixels { get; }
    internal bool LinearRgb { get; }
    internal bool AlphaOnly { get; }
    internal long ByteCount => checked(Pixels.LongLength * sizeof(ushort));
    internal RasterEffectSurface RetagAlpha(bool linearRgb) => AlphaOnly
        ? new RasterEffectSurface(Region, Pixels, linearRgb, true)
        : throw new InvalidOperationException("Only black-RGB alpha surfaces can change tags without color conversion.");
}

/// <summary>Four bounded premultiplied channels; kernels clamp before publishing a surface.</summary>
internal readonly struct RasterEffectPixel16 {
    internal RasterEffectPixel16(int red, int green, int blue, int alpha) { R = red; G = green; B = blue; A = alpha; }
    internal int R { get; }
    internal int G { get; }
    internal int B { get; }
    internal int A { get; }
}
