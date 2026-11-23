using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the waterfall orientation settings shared by static exports.</summary>
    public ChartWaterfallOptions Waterfall {
        get => _waterfall;
        set => _waterfall = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartWaterfallOptions _waterfall = new();
}
