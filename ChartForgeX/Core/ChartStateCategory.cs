using System;

namespace ChartForgeX.Core;

/// <summary>
/// Describes one categorical state, such as an operational state or a severity: the key that data refers to,
/// its display label, its colour, and how its marks are drawn. Categories are listed in legends in the order they
/// are registered.
/// </summary>
public sealed class ChartStateCategory {
    /// <summary>Initializes a new state definition.</summary>
    /// <param name="key">The state key used by segments, for example <c>up</c> or <c>degraded</c>.</param>
    /// <param name="label">The display label shown in the legend and tooltips.</param>
    /// <param name="color">The colour of marks in this state.</param>
    /// <param name="pattern">How marks are filled: solid, hatched, cross-hatched, or outlined.</param>
    /// <param name="emphasis">How much marks draw the eye; <see cref="ChartStateEmphasis.Quiet"/> lightens them.</param>
    public ChartStateCategory(string key, string label, ChartColor color, ChartStatePattern pattern = ChartStatePattern.Solid, ChartStateEmphasis emphasis = ChartStateEmphasis.Normal) {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("State key must not be empty.", nameof(key));
        if (!Enum.IsDefined(typeof(ChartStatePattern), pattern)) throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown state pattern.");
        if (!Enum.IsDefined(typeof(ChartStateEmphasis), emphasis)) throw new ArgumentOutOfRangeException(nameof(emphasis), emphasis, "Unknown state emphasis.");
        Key = key;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Color = color;
        Pattern = pattern;
        Emphasis = emphasis;
    }

    /// <summary>Gets the state key used by segments.</summary>
    public string Key { get; }

    /// <summary>Gets the display label shown in the legend and tooltips.</summary>
    public string Label { get; }

    /// <summary>Gets the colour of marks in this state.</summary>
    public ChartColor Color { get; }

    /// <summary>Gets how marks in this state are filled.</summary>
    public ChartStatePattern Pattern { get; }

    /// <summary>Gets how much marks in this state draw the eye.</summary>
    public ChartStateEmphasis Emphasis { get; }
}