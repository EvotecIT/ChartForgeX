using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the native Sankey alignment, ordering, node geometry, label placement and paint options.</summary>
    public ChartSankeyOptions Sankey {
        get => _sankey;
        set => _sankey = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartSankeyOptions _sankey = new();
}
