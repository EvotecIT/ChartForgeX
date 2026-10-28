using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets this source-ordered Polar series' form, curve, run closure and outline policy.</summary>
    public ChartPolarOptions Polar { get; private set; } = new();

    /// <summary>Configures a detached Polar option copy; a throwing callback leaves the current options unchanged.</summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The current series.</returns>
    public ChartSeries ConfigurePolar(Action<ChartPolarOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Polar = ChartOptionConfiguration.Configure(Polar, configure, options => options.Clone()); return this;
    }

    internal void ValidatePolarOptions() {
        if (Polar.IsConfigured && Kind != ChartSeriesKind.Polar)
            throw new InvalidOperationException("Series '" + Name + "' requires the Polar kind for polar options.");
        if (Kind == ChartSeriesKind.Polar && Polar.Form == ChartLineAreaForm.Line && Polar.FillOpacity.HasValue)
            throw new InvalidOperationException("Series '" + Name + "' requires the Polar Area form for fill opacity.");
    }
}
