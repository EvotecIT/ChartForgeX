using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Chooses the gauge anatomy while retaining the same value and accessible data.</summary>
public enum ChartGaugeForm {
    /// <summary>A 240 degree value arc.</summary>
    Arc,
    /// <summary>A needle over the same track and optional bands.</summary>
    Needle,
    /// <summary>A horizontal value track.</summary>
    Linear
}

/// <summary>Declares a numeric gauge band and its semantic colour.</summary>
public sealed class ChartGaugeBand {
    /// <summary>Gets the inclusive lower bound.</summary>
    public double Minimum { get; }
    /// <summary>Gets the exclusive upper bound, except at the gauge maximum.</summary>
    public double Maximum { get; }
    /// <summary>Gets the band's semantic role.</summary>
    public ChartSeriesState State { get; }
    /// <summary>Creates a band in the gauge's value units.</summary>
    public ChartGaugeBand(double minimum, double maximum, ChartSeriesState state) {
        ChartGuards.Finite(minimum, nameof(minimum)); ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (!Enum.IsDefined(typeof(ChartSeriesState), state) || state == ChartSeriesState.None) throw new ArgumentOutOfRangeException(nameof(state));
        Minimum = minimum; Maximum = maximum; State = state;
    }
}

/// <summary>Controls arc, needle and linear gauges without introducing host behaviour.</summary>
public sealed class ChartGaugeOptions {
    private double? _target;
    private ChartGaugeForm _form;
    /// <summary>Gets or sets the gauge form.</summary>
    public ChartGaugeForm Form { get => _form; set { if (!Enum.IsDefined(typeof(ChartGaugeForm), value)) throw new ArgumentOutOfRangeException(nameof(value)); _form = value; } }
    /// <summary>Gets or sets the optional target in value units.</summary>
    public double? Target { get => _target; set { if (value.HasValue) ChartGuards.Finite(value.Value, nameof(value)); _target = value; } }
    /// <summary>Gets or sets the caption beneath the value. Null uses the series name.</summary>
    public string? Caption { get; set; }
    /// <summary>Gets the optional bands. Overlap is rejected when rendering.</summary>
    public List<ChartGaugeBand> Bands { get; } = new();
}

public sealed partial class ChartOptions {
    /// <summary>Gets gauge anatomy, target and band settings.</summary>
    public ChartGaugeOptions Gauge { get; } = new();
}

public sealed partial class Chart {
    /// <summary>Configures arc, needle or linear gauge anatomy.</summary>
    public Chart WithGauge(Action<ChartGaugeOptions> configure) { if (configure == null) throw new ArgumentNullException(nameof(configure)); configure(Options.Gauge); return this; }
    /// <summary>Adds a linear gauge with a value, range and optional explicit colour.</summary>
    public Chart AddLinearGauge(string name, double value, double min = 0, double max = 100, ChartForgeX.Primitives.ChartColor? color = null) { AddGauge(name, value, min, max, color); Options.Gauge.Form = ChartGaugeForm.Linear; return this; }
}
