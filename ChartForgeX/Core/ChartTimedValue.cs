using System;

namespace ChartForgeX.Core;

/// <summary>A value observed at an instant, such as one sign-in failure or one collected sample.</summary>
public readonly struct ChartTimedValue {
    /// <summary>Initializes a timed value. <see cref="DateTimeKind.Local"/> timestamps are converted to UTC;
    /// <see cref="DateTimeKind.Unspecified"/> timestamps are treated as UTC.</summary>
    /// <param name="timestamp">When the value was observed.</param>
    /// <param name="value">The observed value. Use 1 for event counts.</param>
    public ChartTimedValue(DateTime timestamp, double value = 1) {
        ChartGuards.Finite(value, nameof(value));
        Timestamp = timestamp.Kind == DateTimeKind.Local ? timestamp.ToUniversalTime() : DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
        Value = value;
    }

    /// <summary>Gets the UTC timestamp.</summary>
    public DateTime Timestamp { get; }

    /// <summary>Gets the observed value.</summary>
    public double Value { get; }
}
