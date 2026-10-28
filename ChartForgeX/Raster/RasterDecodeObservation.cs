using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

/// <summary>Records declared raster output before allocation for one internal bounded scene-resource capture.</summary>
internal sealed class RasterDecodeObservation {
    private readonly HashSet<long> _dimensions = new();
    private readonly long? _maximumDeclaredPixels;
    private readonly RasterCumulativeDebit? _budget;
    internal RasterDecodeObservation(long? maximumDeclaredPixels = null, RasterCumulativeDebit? budget = null) {
        if (maximumDeclaredPixels < 0) throw new ArgumentOutOfRangeException(nameof(maximumDeclaredPixels));
        _maximumDeclaredPixels = maximumDeclaredPixels; _budget = budget;
    }
    internal long DeclaredPixels { get; private set; }
    internal long ObservedPixels { get; private set; }
    internal bool PixelLimitExceeded { get; private set; }
    internal bool WorkLimitExceeded { get; private set; }
    internal bool Declare(int width, int height, long maximumPixels) {
        var key = ((long)width << 32) | (uint)height;
        if (_dimensions.Contains(key)) return true;
        var pixels = (long)width * height;
        ObservedPixels = DeclaredPixels + pixels;
        if (ObservedPixels > Math.Min(maximumPixels, _maximumDeclaredPixels ?? long.MaxValue)) { PixelLimitExceeded = true; return false; }
        if (_budget != null && !_budget.TryReserve(checked(pixels * 8), checked(pixels * 8))) {
            if (pixels * 8 > _budget.MaximumBytes - _budget.Bytes) PixelLimitExceeded = true;
            else WorkLimitExceeded = true;
            return false;
        }
        _dimensions.Add(key); DeclaredPixels = ObservedPixels; return true;
    }
}
