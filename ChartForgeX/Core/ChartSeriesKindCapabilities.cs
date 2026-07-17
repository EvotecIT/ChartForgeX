using System;

namespace ChartForgeX.Core;

/// <summary>
/// Exposes reusable classification rules for chart series kinds to hosts and adapters.
/// </summary>
public static class ChartSeriesKindCapabilities {
    /// <summary>
    /// Determines whether a series kind owns the chart surface and cannot be combined with cartesian annotations or unrelated series.
    /// </summary>
    /// <param name="kind">The series kind to classify.</param>
    /// <returns><see langword="true"/> when the kind uses an exclusive rendering surface; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="kind"/> is not a defined series kind.</exception>
    public static bool IsExclusive(ChartSeriesKind kind) {
        if (!Enum.IsDefined(typeof(ChartSeriesKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown chart series kind.");
        return ChartSeriesKindTraits.IsExclusive(kind);
    }
}
