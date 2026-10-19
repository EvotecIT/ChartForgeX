using System;

namespace ChartForgeX.Core;

/// <summary>An authored annotation identity independent of its caption, coordinates and collection order.</summary>
/// <remarks>IDs are unique within a chart. Equal explicit IDs can identify equivalent annotations across synchronized charts; anonymous annotations remain local.</remarks>
public readonly struct ChartAnnotationId : IEquatable<ChartAnnotationId> {
    private readonly string? _value;

    /// <summary>Creates a case-sensitive source identity.</summary>
    /// <param name="value">A nonempty identifier. Leading and trailing whitespace is removed.</param>
    public ChartAnnotationId(string value) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Annotation ID must not be empty.", nameof(value));
        _value = value.Trim();
    }

    /// <summary>Gets the identifier. A default struct is invalid until constructed explicitly.</summary>
    public string Value => _value ?? throw new InvalidOperationException("An annotation ID must be constructed with a nonempty value.");
    internal void Validate() => _ = Value;
    /// <summary>Compares annotation identities ordinally.</summary>
    public bool Equals(ChartAnnotationId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
    /// <summary>Compares identities when <paramref name="obj"/> is an annotation identity.</summary>
    public override bool Equals(object? obj) => obj is ChartAnnotationId other && Equals(other);
    /// <summary>Returns an ordinal hash code of the identity.</summary>
    public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
    /// <summary>Returns the identity value.</summary>
    public override string ToString() => Value;
    /// <summary>Compares two case-sensitive annotation identities.</summary>
    public static bool operator ==(ChartAnnotationId left, ChartAnnotationId right) => left.Equals(right);
    /// <summary>Compares two case-sensitive annotation identities.</summary>
    public static bool operator !=(ChartAnnotationId left, ChartAnnotationId right) => !left.Equals(right);
}
