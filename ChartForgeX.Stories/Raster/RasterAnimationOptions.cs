namespace ChartForgeX.Raster;

/// <summary>Selects a managed animation container.</summary>
public enum RasterAnimationFormat {
    /// <summary>GIF with a shared palette, binary transparency, and centisecond timing including zero delays.</summary>
    Gif,
    /// <summary>Animated PNG with lossless RGBA pixels and rational frame timing.</summary>
    Apng
}

/// <summary>Controls animation playback and container encoding independently of frame durations.</summary>
public sealed class RasterAnimationOptions {
    /// <summary>Gets or sets the total number of plays: zero repeats indefinitely, one plays once.</summary>
    /// <remarks>GIF supports up to 65,536 total plays. APNG supports up to <see cref="int.MaxValue"/>.</remarks>
    public int PlayCount { get; set; }

    /// <summary>Gets or sets APNG deflate compression from 0 to 9. The default is 6.</summary>
    /// <remarks>Zero disables compression, 1-3 favors speed, and 4-9 favors smaller output. Ignored for GIF.</remarks>
    public int PngCompressionLevel { get; set; } = 6;
}
