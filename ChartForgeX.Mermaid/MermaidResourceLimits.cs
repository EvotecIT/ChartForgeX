using System;

namespace ChartForgeX.Mermaid;

/// <summary>Bounds source parsing and native artifact export. Each operation captures its own copy.</summary>
public sealed class MermaidResourceLimits {
    private int _sourceCharacters = 524288, _sourceLines = 10000, _lineCharacters = 8192;
    private int _elements = 5000, _edges = 20000, _nestingDepth = 64;
    private long _outputBytes = 8388608, _rasterPixels = 16777216;

    /// <summary>Gets or sets the maximum raw UTF-16 source length, including line endings.</summary>
    public int MaximumSourceCharacters { get => _sourceCharacters; set => _sourceCharacters = Positive(value, nameof(MaximumSourceCharacters)); }
    /// <summary>Gets or sets the maximum source line count, counting CRLF as one line ending.</summary>
    public int MaximumSourceLines { get => _sourceLines; set => _sourceLines = Positive(value, nameof(MaximumSourceLines)); }
    /// <summary>Gets or sets the maximum UTF-16 length of one source line, excluding its line ending.</summary>
    public int MaximumLineCharacters { get => _lineCharacters; set => _lineCharacters = Positive(value, nameof(MaximumLineCharacters)); }
    /// <summary>Gets or sets the maximum semantic items, including groups, notes, members, data values, style declarations and expanded style properties.</summary>
    public int MaximumElements { get => _elements; set => _elements = Positive(value, nameof(MaximumElements)); }
    /// <summary>Gets or sets the maximum parsed relationships, after flowchart endpoint groups expand.</summary>
    public int MaximumEdges { get => _edges; set => _edges = Positive(value, nameof(MaximumEdges)); }
    /// <summary>Gets or sets the maximum semantic block or tree nesting depth.</summary>
    public int MaximumNestingDepth { get => _nestingDepth; set => _nestingDepth = Positive(value, nameof(MaximumNestingDepth)); }
    /// <summary>Gets or sets the maximum encoded bytes produced by one artifact export.</summary>
    public long MaximumOutputBytes { get => _outputBytes; set => _outputBytes = Positive(value, nameof(MaximumOutputBytes)); }
    /// <summary>Gets or sets the maximum pixel count allocated by one raster export.</summary>
    public long MaximumRasterPixels { get => _rasterPixels; set => _rasterPixels = Positive(value, nameof(MaximumRasterPixels)); }

    /// <summary>Captures independent validated settings for one parse, render, or export request.</summary>
    public MermaidResourceLimits Clone() => new() {
        MaximumSourceCharacters = MaximumSourceCharacters, MaximumSourceLines = MaximumSourceLines,
        MaximumLineCharacters = MaximumLineCharacters, MaximumElements = MaximumElements,
        MaximumEdges = MaximumEdges, MaximumNestingDepth = MaximumNestingDepth,
        MaximumOutputBytes = MaximumOutputBytes, MaximumRasterPixels = MaximumRasterPixels
    };

    private static int Positive(int value, string name) => value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Mermaid resource limits must be positive.");
    private static long Positive(long value, string name) => value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Mermaid resource limits must be positive.");
}
