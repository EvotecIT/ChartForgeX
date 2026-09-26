using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one categorical heatmap cell: a state key resolved through <see cref="ChartOptions.StateCategories"/>,
/// optional short text drawn in the cell, an optional tooltip, and an optional link.
/// </summary>
public readonly struct ChartHeatmapCell {
    private static readonly char[] PathDelimiters = { '/', '?', '#' };

    /// <summary>Initializes a categorical heatmap cell.</summary>
    /// <param name="state">The state key, for example <c>pass</c> or <c>critical</c>.</param>
    /// <param name="text">Optional short text drawn inside the cell, for example a count.</param>
    /// <param name="tooltip">Optional tooltip text. When null, the tooltip names the row, column, and state.</param>
    /// <param name="href">Optional link opened from the cell in SVG and HTML output. Relative, fragment, <c>http</c>,
    /// <c>https</c>, and <c>mailto</c> links are accepted; other schemes such as <c>javascript:</c> are rejected.</param>
    public ChartHeatmapCell(string state, string? text = null, string? tooltip = null, string? href = null) {
        if (string.IsNullOrWhiteSpace(state)) throw new ArgumentException("Heatmap cell state must not be empty.", nameof(state));
        if (href != null && !IsSafeHref(href)) throw new ArgumentException("Heatmap cell links must be relative, fragment, http, https, or mailto links.", nameof(href));
        State = state;
        Text = text;
        Tooltip = tooltip;
        Href = href;
    }

    /// <summary>Gets the state key.</summary>
    public string State { get; }

    /// <summary>Gets optional short text drawn inside the cell.</summary>
    public string? Text { get; }

    /// <summary>Gets optional tooltip text.</summary>
    public string? Tooltip { get; }

    /// <summary>Gets the optional link target.</summary>
    public string? Href { get; }

    private static bool IsSafeHref(string href) {
        var value = href.Trim();
        if (value.Length == 0) return false;
        foreach (var character in value) {
            if (char.IsControl(character) || char.IsWhiteSpace(character)) return false;
        }

        var colon = value.IndexOf(':');
        if (colon < 0) return true;
        var delimiter = value.IndexOfAny(PathDelimiters);
        if (delimiter >= 0 && delimiter < colon) return true;
        var scheme = value.Substring(0, colon);
        return scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
            scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
            scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase);
    }
}
