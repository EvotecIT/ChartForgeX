using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the name and number display policy for numeric region and tile-map measurements.</summary>
    public ChartColorDimensionOptions MapColorDimension {
        get => _mapColorDimension;
        set => _mapColorDimension = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartColorDimensionOptions _mapColorDimension = new();

    /// <summary>Gets the name and number display policy for numeric rectangular, hexagonal and calendar heatmap measurements.</summary>
    public ChartColorDimensionOptions HeatmapColorDimension {
        get => _heatmapColorDimension;
        set => _heatmapColorDimension = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartColorDimensionOptions _heatmapColorDimension = new();
}
