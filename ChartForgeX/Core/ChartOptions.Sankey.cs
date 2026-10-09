namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the native Sankey alignment, ordering, node geometry and paint options.</summary>
    public ChartSankeyOptions Sankey { get; } = new();
}
