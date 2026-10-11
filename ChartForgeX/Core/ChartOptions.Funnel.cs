namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the funnel anatomy and orientation settings shared by static exports.</summary>
    public ChartFunnelOptions Funnel { get; } = new();
}
