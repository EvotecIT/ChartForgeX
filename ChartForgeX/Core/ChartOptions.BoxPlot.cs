using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the box plot layout options.</summary>
    public ChartBoxPlotOptions BoxPlot {
        get => _boxPlot;
        set => _boxPlot = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartBoxPlotOptions _boxPlot = new();
}
