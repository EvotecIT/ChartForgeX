using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Chooses the gauge anatomy while retaining the same value and accessible data.</summary>
public enum ChartGaugeForm {
    /// <summary>A value arc over the configured angular scale.</summary>
    Arc,
    /// <summary>A needle over the same track and optional bands.</summary>
    Needle,
    /// <summary>A value track in the configured orientation.</summary>
    Linear,
    /// <summary>A measured value arc and a needle on the same radial scale.</summary>
    ArcWithNeedle = 3
}

/// <summary>Declares a numeric gauge band and its semantic colour.</summary>
public sealed class ChartGaugeBand {
    /// <summary>Gets the inclusive lower bound.</summary>
    public double Minimum { get; }
    /// <summary>Gets the exclusive upper bound, except at the gauge maximum.</summary>
    public double Maximum { get; }
    /// <summary>Gets the band's semantic role.</summary>
    public ChartSeriesState State { get; }
    /// <summary>Gets an optional authored paint color, independent of the semantic state.</summary>
    public ChartForgeX.Primitives.ChartColor? Color { get; }
    /// <summary>Gets the band opacity, from zero through one.</summary>
    public double Opacity { get; }
    /// <summary>Creates a band in the gauge's value units.</summary>
    public ChartGaugeBand(double minimum, double maximum, ChartSeriesState state, ChartForgeX.Primitives.ChartColor? color = null, double opacity = 1) {
        ChartGuards.Finite(minimum, nameof(minimum)); ChartGuards.Finite(maximum, nameof(maximum));
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum));
        if (!Enum.IsDefined(typeof(ChartSeriesState), state) || state == ChartSeriesState.None) throw new ArgumentOutOfRangeException(nameof(state));
        ChartGuards.UnitInterval(opacity, nameof(opacity));
        Minimum = minimum; Maximum = maximum; State = state; Color = color; Opacity = opacity;
    }
}

/// <summary>Controls arc, needle and linear gauges without introducing host behaviour.</summary>
public sealed class ChartGaugeOptions {
    private ChartGaugeForm _form;
    private ChartOrientation _orientation = ChartOrientation.Horizontal;
    private double _startAngleDegrees = 150;
    private double _sweepAngleDegrees = 240;
    private int? _tickCount;
    private double _bandGap;
    private IReadOnlyList<ChartAxisLabel> _tickLabels = Array.Empty<ChartAxisLabel>();
    /// <summary>Gets or sets the gauge form.</summary>
    public ChartGaugeForm Form { get => _form; set { if (!Enum.IsDefined(typeof(ChartGaugeForm), value)) throw new ArgumentOutOfRangeException(nameof(value)); _form = value; } }
    /// <summary>Gets or sets the linear gauge direction. Horizontal places the minimum at the left; vertical places it at the bottom.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set {
            if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _orientation = value;
        }
    }
    /// <summary>Gets or sets the radial scale's starting angle in degrees. Zero points right and positive angles turn clockwise.</summary>
    /// <remarks>Any finite angle is accepted. The default is 150 degrees. Linear gauges ignore this setting.</remarks>
    public double StartAngleDegrees {
        get => _startAngleDegrees;
        set { ChartGuards.Finite(value, nameof(value)); _startAngleDegrees = value; }
    }
    /// <summary>Gets or sets the radial scale's clockwise sweep in degrees, greater than zero and at most 360.</summary>
    /// <remarks>The default is 240 degrees. Linear gauges ignore this setting.</remarks>
    public double SweepAngleDegrees {
        get => _sweepAngleDegrees;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value > 360) throw new ArgumentOutOfRangeException(nameof(value));
            _sweepAngleDegrees = value;
        }
    }
    /// <summary>Gets or sets the preferred generated tick ceiling. Null uses two radial or five linear ticks.</summary>
    /// <remarks>Explicit counts range from 2 through 100; crowded layouts use fewer uniformly spaced ticks. Partial scales include both endpoints; a full circular scale omits the duplicate final seam. Nonempty <see cref="TickLabels"/> replaces generated tick locations.</remarks>
    public int? TickCount {
        get => _tickCount;
        set {
            if (value.HasValue && (value.Value < 2 || value.Value > 100)) throw new ArgumentOutOfRangeException(nameof(value));
            _tickCount = value;
        }
    }
    /// <summary>Gets or sets a detached read-only snapshot of explicit tick values and captions in authored order.</summary>
    /// <remarks>An empty collection uses generated ticks. Entries require finite values and non-null text.</remarks>
    public IReadOnlyList<ChartAxisLabel> TickLabels {
        get => _tickLabels;
        set {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var labels = new ChartAxisLabel[value.Count];
            for (var index = 0; index < labels.Length; index++) labels[index] = new ChartAxisLabel(value[index].Value, value[index].Text);
            _tickLabels = Array.AsReadOnly(labels);
        }
    }
    /// <summary>Gets or sets the total empty inset between band segments in logical pixels. Half is removed at each segment end; zero retains contiguous bands.</summary>
    /// <remarks>Radial gauges convert pixels using their measured band radius. A segment narrower than the requested gap retains its source fact without painted width.</remarks>
    public double BandGap {
        get => _bandGap;
        set { ChartGuards.Finite(value, nameof(value)); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _bandGap = value; }
    }
    /// <summary>Gets or sets an optional needle color. Null uses the measured value paint; it does not change the reading or semantic band state.</summary>
    public ChartForgeX.Primitives.ChartColor? NeedleColor { get; set; }
    /// <summary>Gets or sets the caption beneath the value. Null uses the series name.</summary>
    public string? Caption { get; set; }
    /// <summary>Gets the optional bands. Overlap is rejected when rendering.</summary>
    public List<ChartGaugeBand> Bands { get; private set; } = new();

    /// <summary>Copies the configured state and detaches the band list while retaining immutable bands and tick labels.</summary>
    internal ChartGaugeOptions Clone() {
        var clone = (ChartGaugeOptions)MemberwiseClone();
        clone.Bands = new List<ChartGaugeBand>(Bands);
        return clone;
    }
}

public sealed partial class ChartOptions {
    /// <summary>Gets gauge anatomy, scale captions and band settings.</summary>
    public ChartGaugeOptions Gauge { get; internal set; } = new();
}

public sealed partial class Chart {
    /// <summary>Configures arc, needle or linear gauge anatomy.</summary>
    public Chart ConfigureGauge(Action<ChartGaugeOptions> configure) {
        Options.Gauge = ChartOptionConfiguration.Configure(Options.Gauge, configure, options => options.Clone());
        return this;
    }
    /// <summary>Adds a linear gauge with a value, range and optional explicit colour.</summary>
    public Chart AddLinearGauge(string name, double value, double min = 0, double max = 100, ChartForgeX.Primitives.ChartColor? color = null) { AddGauge(name, value, min, max, color); Options.Gauge.Form = ChartGaugeForm.Linear; return this; }
}
