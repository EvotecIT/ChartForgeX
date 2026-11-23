using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the treemap spacing, group-caption and independent numeric color options.</summary>
    public ChartTreemapOptions Treemap {
        get => _treemap;
        set => _treemap = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartTreemapOptions _treemap = new();
}
