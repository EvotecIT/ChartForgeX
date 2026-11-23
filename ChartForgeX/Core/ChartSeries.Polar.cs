using System;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets this source-ordered Polar series' form, curve, run closure and outline policy.</summary>
    public ChartPolarOptions Polar {
        get => _polar;
        set => _polar = value ?? throw new ArgumentNullException(nameof(value));
    }
    private ChartPolarOptions _polar = new();

    internal void ValidatePolarOptions() {
        if (Polar.IsConfigured && Kind != ChartSeriesKind.Polar)
            throw new InvalidOperationException("Series '" + Name + "' requires the Polar kind for polar options.");
        if (Kind == ChartSeriesKind.Polar && Polar.Form == ChartLineAreaForm.Line && Polar.FillOpacity.HasValue)
            throw new InvalidOperationException("Series '" + Name + "' requires the Polar Area form for fill opacity.");
    }
}
