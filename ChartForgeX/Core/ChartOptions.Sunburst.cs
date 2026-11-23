using System;
namespace ChartForgeX.Core;

public sealed partial class ChartOptions {
    /// <summary>Gets the Sunburst parent-size and independent numeric color options.</summary>
    public ChartSunburstOptions Sunburst {
        get => _sunburst;
        set => _sunburst = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartSunburstOptions _sunburst = new();
}
