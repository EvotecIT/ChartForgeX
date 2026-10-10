using System;

namespace ChartForgeX.Stories;

/// <summary>Readable editor viewport settings. The document scrolls instead of shrinking below the chosen font size.</summary>
public sealed class VisualStorySourceOptions {
    /// <summary>Creates editor settings with optional filename and a one-based highlighted line.</summary>
    public VisualStorySourceOptions(string? fileName = null, double fontSize = 18, bool wrap = true, bool lineNumbers = true, int highlightedLine = 0) {
        FileName = fileName == null ? string.Empty : VisualStorySurface.OptionalHeading(fileName, nameof(fileName));
        if (FileName.Length > 128) throw new ArgumentOutOfRangeException(nameof(fileName));
        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize < 14 || fontSize > 32) throw new ArgumentOutOfRangeException(nameof(fontSize));
        if (highlightedLine < 0) throw new ArgumentOutOfRangeException(nameof(highlightedLine));
        FontSize = fontSize; Wrap = wrap; LineNumbers = lineNumbers; HighlightedLine = highlightedLine;
    }
    /// <summary>Gets the optional filename displayed in editor chrome.</summary>
    public string FileName { get; }
    /// <summary>Gets the fixed readable font size in logical units.</summary>
    public double FontSize { get; }
    /// <summary>Gets whether long lines wrap within the viewport.</summary>
    public bool Wrap { get; }
    /// <summary>Gets whether source line numbers are shown.</summary>
    public bool LineNumbers { get; }
    /// <summary>Gets a one-based highlighted source line, or zero for the caret line.</summary>
    public int HighlightedLine { get; }
}
