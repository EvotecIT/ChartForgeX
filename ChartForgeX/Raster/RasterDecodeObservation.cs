using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

/// <summary>Records declared raster output before allocation for one internal bounded scene-resource capture.</summary>
internal sealed class RasterDecodeObservation {
    private readonly HashSet<long> _dimensions = new();
    private readonly long? _maximumDeclaredPixels;
    internal RasterDecodeObservation(long? maximumDeclaredPixels = null) {
        if (maximumDeclaredPixels < 0) throw new ArgumentOutOfRangeException(nameof(maximumDeclaredPixels));
        _maximumDeclaredPixels = maximumDeclaredPixels;
    }
    internal long DeclaredPixels { get; private set; }
    internal long ObservedPixels { get; private set; }
    internal bool PixelLimitExceeded { get; private set; }
    internal bool Declare(int width, int height, long maximumPixels) {
        var key = ((long)width << 32) | (uint)height;
        if (_dimensions.Contains(key)) return true;
        var pixels = (long)width * height;
        ObservedPixels = DeclaredPixels + pixels;
        if (ObservedPixels > Math.Min(maximumPixels, _maximumDeclaredPixels ?? long.MaxValue)) { PixelLimitExceeded = true; return false; }
        _dimensions.Add(key); DeclaredPixels = ObservedPixels; return true;
    }
}
