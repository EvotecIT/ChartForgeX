using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

/// <summary>Defines how text may wrap inside a bounded region.</summary>
public enum TextWrapMode {
    /// <summary>Keep each source paragraph on one line.</summary>
    NoWrap,

    /// <summary>Prefer word boundaries and split oversized words only when necessary.</summary>
    Word,

    /// <summary>Wrap at any character boundary.</summary>
    Character
}

/// <summary>Defines how overflowing text is represented.</summary>
public enum TextTrimming {
    /// <summary>Clip the layout without adding a marker.</summary>
    None,

    /// <summary>Fit an ellipsis at the end of the final visible line.</summary>
    Ellipsis
}

/// <summary>Describes measured text dimensions.</summary>
public readonly struct TextMetrics {
    /// <summary>Initializes measured text dimensions.</summary>
    public TextMetrics(double width, double height, double lineHeight) {
        Width = width;
        Height = height;
        LineHeight = lineHeight;
    }

    /// <summary>Gets the measured width.</summary>
    public double Width { get; }

    /// <summary>Gets the measured height.</summary>
    public double Height { get; }

    /// <summary>Gets the resolved line height.</summary>
    public double LineHeight { get; }
}

/// <summary>Describes one measured line in a text layout.</summary>
public sealed class TextLayoutLine {
    internal TextLayoutLine(string text, double width) { Text = text; Width = width; }

    /// <summary>Gets the text on the line.</summary>
    public string Text { get; }

    /// <summary>Gets the measured line width.</summary>
    public double Width { get; }
}

/// <summary>Contains deterministic lines and dimensions produced by text layout.</summary>
public sealed class TextLayout {
    internal TextLayout(IReadOnlyList<TextLayoutLine> lines, TextMetrics metrics, bool trimmed) {
        Lines = lines;
        Metrics = metrics;
        Trimmed = trimmed;
    }

    /// <summary>Gets the laid out lines.</summary>
    public IReadOnlyList<TextLayoutLine> Lines { get; }

    /// <summary>Gets the measured layout dimensions.</summary>
    public TextMetrics Metrics { get; }

    /// <summary>Gets a value indicating whether content was removed to satisfy the line limit.</summary>
    public bool Trimmed { get; }
}
