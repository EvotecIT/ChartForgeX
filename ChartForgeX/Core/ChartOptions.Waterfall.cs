namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the waterfall orientation settings shared by static exports.</summary>
    public ChartWaterfallOptions Waterfall { get; internal set; } = new();
}
