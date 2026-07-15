using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures the primary horizontal axis.</summary>
    public Chart ConfigureXAxis(Action<ChartAxis> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.XAxis);
        return this;
    }

    /// <summary>Configures the primary vertical axis.</summary>
    public Chart ConfigureYAxis(Action<ChartAxis> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.YAxis);
        return this;
    }

    /// <summary>Configures the secondary vertical axis.</summary>
    public Chart ConfigureSecondaryYAxis(Action<ChartAxis> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.SecondaryYAxis);
        return this;
    }

    /// <summary>Sets the primary horizontal scale.</summary>
    public Chart WithXAxisScale(ChartScaleKind scale) => ConfigureXAxis(axis => axis.Scale = scale);

    /// <summary>Sets the primary vertical scale.</summary>
    public Chart WithYAxisScale(ChartScaleKind scale) => ConfigureYAxis(axis => axis.Scale = scale);

    /// <summary>Sets the secondary vertical scale.</summary>
    public Chart WithSecondaryYAxisScale(ChartScaleKind scale) => ConfigureSecondaryYAxis(axis => axis.Scale = scale);
}
