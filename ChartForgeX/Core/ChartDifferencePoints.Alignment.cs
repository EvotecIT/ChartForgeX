using System.Threading;

namespace ChartForgeX.Core;

public static partial class ChartDifferencePoints {
    /// <summary>Captures two independent ordinary connected series and aligns only equal authored X coordinates.</summary>
    /// <param name="first">First Line/StepLine/Area/StepArea source, with globally increasing unique X.</param>
    /// <param name="second">Second source under the same exact-coordinate contract.</param>
    /// <param name="missingDataPolicy">Gap breaks after an unmatched union slot; Connect joins surviving pairs. Explicit source breaks always propagate.</param>
    /// <param name="cancellationToken">Cancellation checked through capture and merge; no source/chart is changed.</param>
    /// <returns>Detached raw sources, identity maps and complete matched pairs. No zero, interpolated value or pair ID is synthesized.</returns>
    /// <remarks>Exact numeric equality is used, not tolerance or category-text equality. Duplicate/backtracking input rejects rather than choosing an arbitrary match. Existing paired FromXY ingestion is unchanged.</remarks>
    public static ChartDifferenceAlignment Align(ChartSeries first, ChartSeries second,
        ChartMissingDataPolicy missingDataPolicy = ChartMissingDataPolicy.Gap, CancellationToken cancellationToken = default) =>
        new(first, second, missingDataPolicy, cancellationToken);
}
