namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Chooses how series without an explicit colour pick their palette colour. The default follows series order;
    /// <see cref="ChartSeriesColorAssignment.Identity"/> keeps a series' colour stable across linked charts by deriving it from its identifier.
    /// </summary>
    public Chart WithSeriesColorAssignment(ChartSeriesColorAssignment assignment) {
        Options.SeriesColorAssignment = assignment;
        return this;
    }
}

public sealed partial class ChartOptions {
    /// <summary>Gets or sets how series without an explicit colour pick their palette colour. Defaults to <see cref="ChartSeriesColorAssignment.Order"/>.</summary>
    public ChartSeriesColorAssignment SeriesColorAssignment { get; set; }
}
