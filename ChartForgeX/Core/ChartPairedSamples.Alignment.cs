using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Core;

internal static partial class ChartPairedSamples {
    /// <summary>Merges sorted source positions exactly; unmatched data stays only in the caller's original snapshots.</summary>
    internal static (ChartDifferencePoint[] Points, int[] First, int[] Second) Align(IReadOnlyList<ChartPoint> first,
        IReadOnlyList<ChartPoint> second, ChartMissingDataPolicy policy, CancellationToken cancellationToken) {
        var points = new List<ChartDifferencePoint>(); var firstIndices = new List<int>(); var secondIndices = new List<int>();
        var a = 0; var b = 0; var missing = false; var split = false;
        while (a < first.Count || b < second.Count) {
            cancellationToken.ThrowIfCancellationRequested();
            if (a < first.Count && b < second.Count && first[a].X == second[b].X) {
                split |= first[a].BreakBefore || second[b].BreakBefore;
                points.Add(new ChartDifferencePoint(first[a].X, first[a].Y, second[b].Y,
                    split || missing && policy == ChartMissingDataPolicy.Gap && points.Count > 0));
                firstIndices.Add(a++); secondIndices.Add(b++); missing = false; split = false;
            } else if (b >= second.Count || a < first.Count && first[a].X < second[b].X) {
                split |= first[a++].BreakBefore; missing = true;
            } else {
                split |= second[b++].BreakBefore; missing = true;
            }
        }
        cancellationToken.ThrowIfCancellationRequested();
        return (points.ToArray(), firstIndices.ToArray(), secondIndices.ToArray());
    }
}
