namespace ChartForgeX.Core;

public sealed partial class Chart {
}

public sealed partial class ChartOptions {
    /// <summary>Gets or sets how series without an explicit colour pick their palette colour. Defaults to <see cref="ChartSeriesColorAssignment.Order"/>.</summary>
    public ChartSeriesColorAssignment SeriesColorAssignment { get; set; }
}
