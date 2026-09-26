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

    /// <summary>
    /// Uses a UTC time scale on the primary horizontal axis, displayed in <paramref name="timeZone"/> (UTC when null).
    /// </summary>
    /// <param name="timeZone">The display time zone, or null for UTC.</param>
    /// <param name="showTimeZone">Whether the x-axis title shows the zone designator.</param>
    /// <param name="label">An optional designator such as <c>CET</c> or <c>Europe/Warsaw</c>.</param>
    /// <returns>The current chart.</returns>
    public Chart WithXAxisTimeScale(TimeZoneInfo? timeZone = null, bool showTimeZone = false, string? label = null) =>
        ConfigureXAxis(axis => axis.WithTimeScale(timeZone, showTimeZone, label));

    /// <summary>Sets the primary vertical scale.</summary>
    public Chart WithYAxisScale(ChartScaleKind scale) => ConfigureYAxis(axis => axis.Scale = scale);

    /// <summary>Sets the secondary vertical scale.</summary>
    public Chart WithSecondaryYAxisScale(ChartScaleKind scale) => ConfigureSecondaryYAxis(axis => axis.Scale = scale);
}
