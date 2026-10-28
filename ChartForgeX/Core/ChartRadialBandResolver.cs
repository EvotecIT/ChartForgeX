using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>Resolves existing band values once for validation, native marks and portable facts.</summary>
internal static class ChartRadialBandResolver {
    internal static IReadOnlyList<ChartRadialBand?> Resolve(Chart chart) {
        var allocation = chart.Options.RadialBandAllocation;
        if (allocation == null) return chart.Series.Select(series => series.RadialBand).ToArray();
        if (chart.Series.Any(series => !ChartSeriesKindTraits.IsPieLikeKind(series.Kind)))
            throw new InvalidOperationException("Automatic radial bands require only Pie/Donut series.");
        if (chart.Series.Any(series => series.RadialBand != null))
            throw new InvalidOperationException("Automatic radial bands cannot be mixed with explicit RadialBand values.");
        var count = chart.Series.Count;
        if (count == 0) return Array.Empty<ChartRadialBand?>();
        var available = 1 - allocation.InnerRatio - (count - 1d) * allocation.GapRatio;
        if (!ChartMath.IsFinite(available) || available <= 0)
            throw new InvalidOperationException("Automatic radial band gaps leave no positive-width bands.");
        var width = available / count;
        var bands = new ChartRadialBand?[count];
        for (var index = 0; index < count; index++) {
            var inner = index == 0 ? allocation.InnerRatio : bands[index - 1]!.OuterRatio + allocation.GapRatio;
            var outer = index == count - 1 ? 1 : inner + width;
            if (outer <= inner) throw new InvalidOperationException("Automatic radial bands are too narrow for finite normalized geometry.");
            bands[index] = new ChartRadialBand(inner, outer);
        }
        return bands;
    }
}
