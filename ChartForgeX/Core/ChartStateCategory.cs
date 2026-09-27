using System;

namespace ChartForgeX.Core;

/// <summary>
/// Describes one categorical state, such as an operational state or a severity: the key that data refers to,
/// its display label, and its colour. Categories are listed in legends in the order they are registered.
/// </summary>
public sealed class ChartStateCategory {
    /// <summary>Initializes a new state definition.</summary>
    /// <param name="key">The state key used by segments, for example <c>up</c> or <c>degraded</c>.</param>
    /// <param name="label">The display label shown in the legend and tooltips.</param>
    /// <param name="color">The fill colour for segments in this state.</param>
    /// <param name="hatched">Whether segments are hatched, for states such as not observable or unknown.</param>
    public ChartStateCategory(string key, string label, ChartColor color, bool hatched = false) {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("State key must not be empty.", nameof(key));
        Key = key;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Color = color;
        Hatched = hatched;
    }

    /// <summary>Gets the state key used by segments.</summary>
    public string Key { get; }

    /// <summary>Gets the display label shown in the legend and tooltips.</summary>
    public string Label { get; }

    /// <summary>Gets the segment fill colour.</summary>
    public ChartColor Color { get; }

    /// <summary>Gets a value indicating whether segments in this state are hatched.</summary>
    public bool Hatched { get; }
}
