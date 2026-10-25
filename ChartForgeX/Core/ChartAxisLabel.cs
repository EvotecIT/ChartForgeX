using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents a display label for a numeric axis value.
/// </summary>
public readonly struct ChartAxisLabel : IEquatable<ChartAxisLabel> {
    private readonly IReadOnlyList<string>? _groups;
    /// <summary>
    /// Gets the axis value that should display the label.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Gets the text displayed for the axis value.
    /// </summary>
    public readonly string Text;

    /// <summary>Gets the category's parent captions, ordered from the outermost group to the nearest parent.</summary>
    /// <remarks>An empty path is an ordinary label. Group paths require a supported linear Cartesian category axis or rectangular Heatmap axis.
    /// Heatmap X values identify actual columns; Y values are one-based source row slots in series order.
    /// Axis reversal changes display positions, never source row/column identity.</remarks>
    public IReadOnlyList<string> Groups => _groups ?? Array.Empty<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartAxisLabel"/> struct.
    /// </summary>
    /// <param name="value">The numeric axis value.</param>
    /// <param name="text">The label text.</param>
    public ChartAxisLabel(double value, string text) {
        ChartGuards.Finite(value, nameof(value));
        Value = value;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _groups = null;
    }

    /// <summary>Creates a numeric category label with a copied, immutable path of parent captions.</summary>
    /// <param name="value">The original numeric category coordinate.</param>
    /// <param name="text">The complete leaf caption.</param>
    /// <param name="groups">Nonblank parent captions, outermost first. An empty array leaves the label ungrouped.</param>
    public ChartAxisLabel(double value, string text, params string[] groups) : this(value, text) {
        if (groups == null) throw new ArgumentNullException(nameof(groups));
        var copy = new string[groups.Length];
        for (var index = 0; index < copy.Length; index++) {
            if (string.IsNullOrWhiteSpace(groups[index])) throw new ArgumentException("Category group captions must not be blank.", nameof(groups));
            copy[index] = groups[index];
        }
        _groups = Array.AsReadOnly(copy);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartAxisLabel"/> struct from a date/time axis value, stored as a UTC
    /// instant (<see cref="DateTimeKind.Local"/> values are converted; <see cref="DateTimeKind.Unspecified"/> values are
    /// treated as UTC). The label text is not changed.
    /// </summary>
    /// <param name="value">The date/time axis value.</param>
    /// <param name="text">The label text.</param>
    public ChartAxisLabel(DateTime value, string text) {
        var utc = ChartDateTime.ToUtc(value);
        Value = utc >= new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Utc)
            ? ChartDateTime.ToOrderedOaDate(utc, nameof(value)) : ChartDateTime.ToOADate(utc);
        Text = text ?? throw new ArgumentNullException(nameof(text));
        _groups = null;
    }

    /// <summary>Compares the coordinate, leaf caption and ordered parent captions by value.</summary>
    public bool Equals(ChartAxisLabel other) {
        if (!Value.Equals(other.Value) || !string.Equals(Text, other.Text, StringComparison.Ordinal) || Groups.Count != other.Groups.Count) return false;
        for (var index = 0; index < Groups.Count; index++)
            if (!string.Equals(Groups[index], other.Groups[index], StringComparison.Ordinal)) return false;
        return true;
    }

    /// <summary>Compares labels by value when <paramref name="obj"/> is an axis label.</summary>
    public override bool Equals(object? obj) => obj is ChartAxisLabel other && Equals(other);

    /// <summary>Returns a value hash of the coordinate and ordinal caption path.</summary>
    public override int GetHashCode() {
        unchecked {
            var hash = Value.GetHashCode() * 397 ^ (Text == null ? 0 : StringComparer.Ordinal.GetHashCode(Text));
            foreach (var group in Groups) hash = hash * 397 ^ StringComparer.Ordinal.GetHashCode(group);
            return hash;
        }
    }

    /// <summary>Compares axis labels by value.</summary>
    public static bool operator ==(ChartAxisLabel left, ChartAxisLabel right) => left.Equals(right);
    /// <summary>Compares axis labels by value.</summary>
    public static bool operator !=(ChartAxisLabel left, ChartAxisLabel right) => !left.Equals(right);
}
