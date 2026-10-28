using System;

namespace ChartForgeX.Mermaid;

/// <summary>A quoted XY datum caption, retaining its decoded text, original token and source location.</summary>
public sealed class MermaidXYChartPointLabel : MermaidAstNode {
    private string _text;
    /// <summary>Initializes the caption for a zero-based source datum.</summary>
    public MermaidXYChartPointLabel(int pointIndex, string text, string rawText, MermaidSourceSpan span) : base(span) {
        if (pointIndex < 0) throw new ArgumentOutOfRangeException(nameof(pointIndex));
        PointIndex = pointIndex;
        _text = text ?? throw new ArgumentNullException(nameof(text));
        RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
    }
    /// <summary>Gets the zero-based datum index in the containing series.</summary>
    public int PointIndex { get; }
    /// <summary>Gets or sets decoded caption text. Native rendering escapes it as text.</summary>
    public string Text { get => _text; set => _text = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets the original quoted token, including escapes. Editing Text does not rewrite the original source.</summary>
    public string RawText { get; }
}
