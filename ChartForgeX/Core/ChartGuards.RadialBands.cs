using System;
using System.Linq;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    /// <summary>Validates resolved concentric composition without broadening unrelated exclusive families.</summary>
    private static bool ValidateRadialBands(Chart chart, bool preparing) {
        if (chart.Series.Any(series => series.RadialBand != null && !ChartSeriesKindTraits.IsPieLikeKind(series.Kind)))
            throw new InvalidOperationException("Explicit radial bands require Pie or Donut series.");
        var bands = ChartRadialBandResolver.Resolve(chart);
        if (chart.Series.Count == 0 || !chart.Series.All(series => ChartSeriesKindTraits.IsPieLikeKind(series.Kind))) return false;
        if (chart.Series.Count == 1 && bands[0] == null) return false; // Exact existing default path.
        if (bands.Any(band => band == null))
            throw new InvalidOperationException("Multiple Pie/Donut series require an explicit radial band on every series.");
        var ordered = bands.OrderBy(band => band!.InnerRatio).ToArray();
        for (var index = 1; index < ordered.Length; index++)
            if (ordered[index - 1]!.OuterRatio > ordered[index]!.InnerRatio)
                throw new InvalidOperationException("Explicit Pie/Donut radial bands must not overlap.");
        foreach (var series in chart.Series) {
            ValidateNonNegativeValues(series, series.Kind);
            if (!preparing && !series.RenderPoints.Any(point => point.Y > 0))
                throw new InvalidOperationException("Each Pie/Donut radial band requires at least one positive value.");
        }
        return true;
    }
}
