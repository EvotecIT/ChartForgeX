using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Core;

/// <summary>Materializes typed paired observations from nullable sample slots without synthesizing values.</summary>
public static class ChartRangeBands {
    /// <summary>Omits incomplete pairs. Gap starts a new segment after a missing slot; Connect joins the surviving pairs.</summary>
    /// <param name="x">Finite X positions, including positions whose pair is incomplete.</param>
    /// <param name="lower">Nullable finite lower values.</param>
    /// <param name="upper">Nullable finite upper values.</param>
    /// <param name="missingDataPolicy">The explicit missing-slot policy. No policy interpolates or inserts source values.</param>
    /// <param name="cancellationToken">Cancellation checked during materialization; no series is changed on failure.</param>
    /// <returns>Complete typed pairs in source order. Observation indices address these returned pairs.</returns>
    public static ChartRangeBand[] FromXY(IEnumerable<double> x, IEnumerable<double?> lower, IEnumerable<double?> upper,
        ChartMissingDataPolicy missingDataPolicy = ChartMissingDataPolicy.Gap, CancellationToken cancellationToken = default) =>
        ChartPairedSamples.FromXY(x, lower, upper, missingDataPolicy,
            (position, a, b, split) => new ChartRangeBand(position, a, b, split), cancellationToken);
}
