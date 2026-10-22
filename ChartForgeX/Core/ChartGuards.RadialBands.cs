using System;
using System.Linq;

namespace ChartForgeX.Core;

internal static partial class ChartGuards {
    /// <summary>Validates explicit concentric composition without broadening unrelated exclusive families.</summary>
    private static bool ValidateRadialBands(Chart chart, bool preparing) {
        if (chart.Series.Any(series => series.RadialBand != null && !ChartSeriesKindTraits.IsPieLikeKind(series.Kind)))
            throw new InvalidOperationException("Explicit radial bands require Pie or Donut series.");
        if (chart.Series.Count == 0 || !chart.Series.All(series => ChartSeriesKindTraits.IsPieLikeKind(series.Kind))) return false;
        if (chart.Series.Count == 1 && chart.Series[0].RadialBand == null) return false; // Exact existing default path.
        if (chart.Series.Any(series => series.RadialBand == null))
            throw new InvalidOperationException("Multiple Pie/Donut series require an explicit radial band on every series.");
        var ordered = chart.Series.OrderBy(series => series.RadialBand!.InnerRatio).ToArray();
        for (var index = 1; index < ordered.Length; index++)
            if (ordered[index - 1].RadialBand!.OuterRatio > ordered[index].RadialBand!.InnerRatio)
                throw new InvalidOperationException("Explicit Pie/Donut radial bands must not overlap.");
        var outer = ordered.OrderByDescending(series => series.RadialBand!.OuterRatio).First();
        foreach (var series in chart.Series) {
            ValidateNonNegativeValues(series, series.Kind);
            if (!preparing && !series.RenderPoints.Any(point => point.Y > 0))
                throw new InvalidOperationException("Each Pie/Donut radial band requires at least one positive value.");
            if (chart.Series.Count == 1) continue;
            if (series.PointSliceOffsets.Any(offset => offset > 0))
                throw new InvalidOperationException("Exploded slices are unsupported in multi-band Pie/Donut charts because they can overlap another band.");
            var placement = series.DataLabelPlacement ?? chart.Options.DataLabelPlacement;
            var external = placement is ChartDataLabelPlacement.Outside or ChartDataLabelPlacement.Left or ChartDataLabelPlacement.Right
                or ChartDataLabelPlacement.Above or ChartDataLabelPlacement.Below;
            if (!ReferenceEquals(series, outer) && external && (series.ShowDataLabels ?? chart.Options.ShowDataLabels ?? false))
                throw new InvalidOperationException("External labels require the outermost radial band. Use in-band labels or explicitly hide inner-band labels.");
        }
        return true;
    }
}
