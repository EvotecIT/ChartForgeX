namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the box plot layout options.</summary>
    public ChartBoxPlotOptions BoxPlot { get; internal set; } = new();
}
