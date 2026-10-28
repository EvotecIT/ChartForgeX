using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class ChartRange {
    /// <summary>Uses exactly the curve and ribbon samples that the native scene paints.</summary>
    private static IEnumerable<ChartPoint> TrendLineRangePoints(ChartSeries series) {
        foreach (var sample in series.TrendLineSamples) {
            yield return new ChartPoint(sample.X, sample.Value);
            if (sample.Lower.HasValue) {
                yield return new ChartPoint(sample.X, sample.Lower.Value);
                yield return new ChartPoint(sample.X, sample.Upper!.Value);
            }
        }
    }
}
