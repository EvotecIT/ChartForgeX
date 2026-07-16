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
    public static bool IsExclusive(ChartSeriesKind kind) => ChartSeriesKindTraits.IsExclusive(kind);
}
