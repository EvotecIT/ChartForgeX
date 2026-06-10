using System;

namespace ChartForgeX.Mermaid;

/// <summary>
/// Describes a one-based Mermaid source location.
/// </summary>
public readonly struct MermaidSourceSpan {
    /// <summary>Initializes a new source span.</summary>
    public MermaidSourceSpan(int line, int column, int length) {
        if (line < 1) throw new ArgumentOutOfRangeException(nameof(line), line, "Source line must be one-based.");
        if (column < 1) throw new ArgumentOutOfRangeException(nameof(column), column, "Source column must be one-based.");
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), length, "Source length must be non-negative.");
        Line = line;
        Column = column;
        Length = length;
    }

    /// <summary>Gets the one-based source line.</summary>
    public int Line { get; }

    /// <summary>Gets the one-based source column.</summary>
    public int Column { get; }

    /// <summary>Gets the source length.</summary>
    public int Length { get; }
}
