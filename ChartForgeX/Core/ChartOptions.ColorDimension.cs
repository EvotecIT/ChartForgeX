namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the name and number display policy for numeric region and tile-map measurements.</summary>
    public ChartColorDimensionOptions MapColorDimension { get; internal set; } = new();

    /// <summary>Gets the name and number display policy for numeric rectangular, hexagonal and calendar heatmap measurements.</summary>
    public ChartColorDimensionOptions HeatmapColorDimension { get; internal set; } = new();
}
