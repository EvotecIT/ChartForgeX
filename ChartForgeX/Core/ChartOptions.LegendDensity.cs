using System;

namespace ChartForgeX.Core;

public sealed partial class ChartOptions {

    /// <summary>Gets legend key, entry text and custom entry configuration.</summary>
    public ChartLegendOptions Legend {
        get => _legend;
        set => _legend = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartLegendOptions _legend = new();
}
