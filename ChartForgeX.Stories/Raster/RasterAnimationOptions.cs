namespace ChartForgeX.Raster;

/// <summary>Selects a managed animation container.</summary>
public enum RasterAnimationFormat {
    /// <summary>GIF with a shared palette, binary transparency, and 10 millisecond timing.</summary>
    Gif,
    /// <summary>Animated PNG with lossless RGBA pixels and rational frame timing.</summary>
    Apng
}

/// <summary>Controls animation playback independently of its frame durations.</summary>
public sealed class RasterAnimationOptions {
    /// <summary>Gets or sets the total number of plays: zero repeats indefinitely, one plays once.</summary>
    /// <remarks>GIF supports up to 65,536 total plays. APNG supports up to <see cref="int.MaxValue"/>.</remarks>
    public int PlayCount { get; set; }
}
