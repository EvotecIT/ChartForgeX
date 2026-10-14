using System;

namespace ChartForgeX.Core;

/// <summary>An authored observation identity that remains independent of display text and source ordering.</summary>
public readonly struct ChartObservationId : IEquatable<ChartObservationId> {
    private readonly string? _value;

    /// <summary>Creates a case-sensitive source identity.</summary>
    /// <param name="value">A nonempty identifier. Leading and trailing whitespace is removed.</param>
    public ChartObservationId(string value) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Observation ID must not be empty.", nameof(value));
        _value = value.Trim();
    }

    /// <summary>Gets the identifier. A default struct is invalid until constructed explicitly.</summary>
    public string Value => _value ?? throw new InvalidOperationException("An observation ID must be constructed with a nonempty value.");
    /// <summary>Compares two observation identities ordinally.</summary>
    public bool Equals(ChartObservationId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
    /// <summary>Compares identities ordinally when <paramref name="obj"/> is a <see cref="ChartObservationId"/>.</summary>
    public override bool Equals(object? obj) => obj is ChartObservationId other && Equals(other);
    /// <summary>Returns an ordinal hash code of the identity value.</summary>
    public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
    /// <summary>Returns the identity value.</summary>
    public override string ToString() => Value;
    /// <summary>Compares two authored observation identities.</summary>
    public static bool operator ==(ChartObservationId left, ChartObservationId right) => left.Equals(right);
    /// <summary>Compares two authored observation identities.</summary>
    public static bool operator !=(ChartObservationId left, ChartObservationId right) => !left.Equals(right);
}
