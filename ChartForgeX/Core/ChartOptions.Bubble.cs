namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the shared source-size domain and logical radius options for bubble series.</summary>
    public ChartBubbleOptions Bubble { get; } = new();
}
