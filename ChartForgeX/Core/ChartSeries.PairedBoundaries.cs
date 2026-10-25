using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets or sets the lower RangeBand/RangeArea or First DifferenceArea boundary policy. Null inherits the series policy.</summary>
    public ChartBoundaryInterpolation? FirstBoundaryInterpolation { get; set; }
    /// <summary>Gets or sets the upper RangeBand/RangeArea or Second DifferenceArea boundary policy. Null inherits the series policy.</summary>
    public ChartBoundaryInterpolation? SecondBoundaryInterpolation { get; set; }

    /// <summary>Sets independent paired boundary policies without changing tuple order, values or missing-data breaks.</summary>
    /// <param name="first">Lower/First policy, or null to inherit the series interpolation and step position.</param>
    /// <param name="second">Upper/Second policy, or null to inherit the series interpolation and step position.</param>
    /// <returns>The current series.</returns>
    public ChartSeries WithBoundaryInterpolation(ChartBoundaryInterpolation? first, ChartBoundaryInterpolation? second) {
        RequirePairedBoundaryKind();
        FirstBoundaryInterpolation = first; SecondBoundaryInterpolation = second; return this;
    }

    internal ChartBoundaryInterpolation BoundaryInterpolation(bool first) =>
        (first ? FirstBoundaryInterpolation : SecondBoundaryInterpolation) ?? new ChartBoundaryInterpolation(Interpolation, StepPosition);

    internal void ValidatePairedBoundaries() {
        if (!FirstBoundaryInterpolation.HasValue && !SecondBoundaryInterpolation.HasValue) return;
        RequirePairedBoundaryKind();
        // Independent curves need an unambiguous X interval. Explicit breaks may restart source order.
        for (var item = 2; item < RenderPoints.Count; item += 2)
            if (!(RenderPoints[item].BreakBefore || RenderPoints[item + 1].BreakBefore) && RenderPoints[item].X <= RenderPoints[item - 2].X)
                throw new InvalidOperationException("Independent paired boundaries require increasing X within each connected segment.");
    }

    private void RequirePairedBoundaryKind() {
        if (Kind != ChartSeriesKind.RangeBand && Kind != ChartSeriesKind.RangeArea && Kind != ChartSeriesKind.DifferenceArea)
            throw new InvalidOperationException("Independent boundary interpolation requires RangeBand, RangeArea or DifferenceArea.");
    }
}
