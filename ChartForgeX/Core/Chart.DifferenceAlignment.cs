using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds an already captured independent-series comparison using the existing native difference-area owner.</summary>
    /// <param name="name">Destination series display name.</param>
    /// <param name="alignment">Detached exact-X matched comparison and complete original source facts.</param>
    /// <param name="color">Optional shared boundary and fill paint.</param>
    /// <returns>The current chart.</returns>
    /// <remarks>Capture/alignment completes before attachment. A source failure/cancellation therefore leaves the chart unchanged. The chart allocates its ordinary automatic series ID; destination observation IDs remain caller-authored.</remarks>
    public Chart AddAlignedDifferenceArea(string name, ChartDifferenceAlignment alignment, ChartColor? color = null) {
        if (alignment == null) throw new ArgumentNullException(nameof(alignment));
        var series = new ChartSeries(name, ChartSeriesKind.DifferenceArea, Array.Empty<ChartPoint>()) { Color = color };
        series.ReplaceDifferenceAlignment(alignment); AppendSeries(series); return this;
    }
}
