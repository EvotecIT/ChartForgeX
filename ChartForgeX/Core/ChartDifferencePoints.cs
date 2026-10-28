using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Core;

/// <summary>Materializes typed paired observations from nullable sample slots without synthesizing values.</summary>
public static partial class ChartDifferencePoints {
    /// <summary>Omits incomplete pairs. Gap starts a new segment after a missing slot; Connect joins the surviving pairs.</summary>
    /// <param name="x">Finite X positions, including positions whose pair is incomplete.</param>
    /// <param name="first">Nullable finite first values.</param>
    /// <param name="second">Nullable finite second values.</param>
    /// <param name="missingDataPolicy">The explicit missing-slot policy. No policy interpolates or inserts source values.</param>
    /// <param name="cancellationToken">Cancellation checked during materialization; no series is changed on failure.</param>
    /// <returns>Complete typed pairs in source order. Observation indices address these returned pairs.</returns>
    public static ChartDifferencePoint[] FromXY(IEnumerable<double> x, IEnumerable<double?> first, IEnumerable<double?> second,
        ChartMissingDataPolicy missingDataPolicy = ChartMissingDataPolicy.Gap, CancellationToken cancellationToken = default) =>
        ChartPairedSamples.FromXY(x, first, second, missingDataPolicy,
            (position, a, b, split) => new ChartDifferencePoint(position, a, b, split), cancellationToken);
}
