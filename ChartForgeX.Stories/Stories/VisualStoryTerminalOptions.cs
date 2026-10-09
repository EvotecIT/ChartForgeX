using System;

namespace ChartForgeX.Stories;

/// <summary>Fixed-font terminal viewport settings shared by authored stories and recorded replay.</summary>
public sealed class VisualStoryTerminalOptions {
    /// <summary>Creates a readable viewport with bounded per-tab display history.</summary>
    public VisualStoryTerminalOptions(double fontSize = 18, int historyLines = 200, bool wrap = true) {
        if (double.IsNaN(fontSize) || double.IsInfinity(fontSize) || fontSize < 14 || fontSize > 32) throw new ArgumentOutOfRangeException(nameof(fontSize));
        if (historyLines < 20 || historyLines > 1000) throw new ArgumentOutOfRangeException(nameof(historyLines));
        FontSize = fontSize; HistoryLines = historyLines; Wrap = wrap;
    }
    /// <summary>Gets the fixed font size in logical story units.</summary>
    public double FontSize { get; }
    /// <summary>Gets the maximum retained display lines per replay tab. The transcript remains complete.</summary>
    public int HistoryLines { get; }
    /// <summary>Gets whether long lines wrap within the viewport.</summary>
    public bool Wrap { get; }
}
