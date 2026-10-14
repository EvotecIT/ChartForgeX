using System;
using System.Globalization;

namespace ChartForgeX.Core;

/// <summary>A stable series identity independent of its display name and position in a chart.</summary>
/// <remarks>Explicit IDs can identify equivalent series across charts. Automatic IDs belong to one chart and are never synchronization keys.</remarks>
public readonly struct ChartSeriesId : IEquatable<ChartSeriesId> {
    private const string AutomaticPrefix = "cfx:auto:";
    private readonly string? _value;

    /// <summary>Creates an explicit identity. The <c>cfx:auto:</c> namespace is reserved for chart-owned IDs.</summary>
    /// <param name="value">A nonempty, case-sensitive identifier. Leading and trailing whitespace is removed.</param>
    public ChartSeriesId(string value) : this(value, false) { }

    private ChartSeriesId(string value, bool automatic) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Series ID must not be empty.", nameof(value));
        value = value.Trim();
        if (!automatic && value.StartsWith(AutomaticPrefix, StringComparison.Ordinal))
            throw new ArgumentException("The cfx:auto: namespace is reserved for chart-owned series IDs.", nameof(value));
        _value = value;
    }

    /// <summary>Gets the nonempty identifier. A default struct is invalid until constructed explicitly.</summary>
    public string Value => _value ?? throw new InvalidOperationException("A series ID must be constructed with a nonempty value.");

    /// <summary>Gets whether this ID was allocated within a chart instead of supplied for cross-chart synchronization.</summary>
    public bool IsAutomatic => _value?.StartsWith(AutomaticPrefix, StringComparison.Ordinal) == true;

    internal static ChartSeriesId Automatic(long sequence) => new(AutomaticPrefix + sequence.ToString(CultureInfo.InvariantCulture), true);
    internal void Validate() => _ = Value;

    /// <inheritdoc />
    public bool Equals(ChartSeriesId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChartSeriesId other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
    /// <inheritdoc />
    public override string ToString() => Value;
    /// <summary>Compares two case-sensitive series identities.</summary>
    public static bool operator ==(ChartSeriesId left, ChartSeriesId right) => left.Equals(right);
    /// <summary>Compares two case-sensitive series identities.</summary>
    public static bool operator !=(ChartSeriesId left, ChartSeriesId right) => !left.Equals(right);
}
