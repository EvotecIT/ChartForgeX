namespace ChartForgeX.Rendering;

/// <summary>Private declared pixel arrays, explicit visits and simultaneous native capture depth.</summary>
internal readonly struct VisualNativeEffectAccounting {
    internal VisualNativeEffectAccounting(long pixelBytes, long workSamples, int peakDepth, bool returnsCapturedRoot = false) {
        PixelBytes = pixelBytes;
        WorkSamples = workSamples;
        PeakDepth = peakDepth;
        ReturnsCapturedRoot = returnsCapturedRoot;
    }

    internal long PixelBytes { get; }
    internal long WorkSamples { get; }
    internal int PeakDepth { get; }
    internal bool ReturnsCapturedRoot { get; }
}
