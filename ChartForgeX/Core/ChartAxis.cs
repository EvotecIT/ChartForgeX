using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

/// <summary>Defines the mathematical scale used by a chart axis.</summary>
public enum ChartScaleKind {
    /// <summary>Use an unmodified continuous numeric scale.</summary>
    Linear,

    /// <summary>Use a base-10 logarithmic scale containing positive values only.</summary>
    Logarithmic,

    /// <summary>Use a symmetric logarithmic scale that remains linear around zero.</summary>
    SymmetricLogarithmic,

    /// <summary>Use OLE Automation date values with time-oriented formatting.</summary>
    Time
}

/// <summary>
/// Defines renderer-neutral bounds, scale, ticks, labels, and visibility for one chart axis.
/// </summary>
public sealed class ChartAxis {
    private double? _minimum;
    private double? _maximum;
    private ChartScaleKind _scale;
    private double _symmetricLogarithmThreshold = 1;
    private int _tickCount = 6;
    private ChartLabelDensity _labelDensity = ChartLabelDensity.Auto;
    private double _labelAngle;

    /// <summary>Gets or sets an explicit minimum. Null enables automatic bounds.</summary>
    public double? Minimum {
        get => _minimum;
        set {
            ValidateFinite(value, nameof(value));
            ValidateBounds(value, _maximum, nameof(value));
            ValidateLogarithmicBound(value, nameof(value));
            _minimum = value;
        }
    }

    /// <summary>Gets or sets an explicit maximum. Null enables automatic bounds.</summary>
    public double? Maximum {
        get => _maximum;
        set {
            ValidateFinite(value, nameof(value));
            ValidateBounds(_minimum, value, nameof(value));
            ValidateLogarithmicBound(value, nameof(value));
            _maximum = value;
        }
    }

    /// <summary>Gets or sets the mathematical axis scale.</summary>
    public ChartScaleKind Scale {
        get => _scale;
        set {
            if (!Enum.IsDefined(typeof(ChartScaleKind), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown chart scale.");
            if (value == ChartScaleKind.Logarithmic && ((_minimum.HasValue && _minimum.Value <= 0) || (_maximum.HasValue && _maximum.Value <= 0))) {
                throw new InvalidOperationException("Logarithmic axes require positive explicit bounds.");
            }

            _scale = value;
        }
    }

    /// <summary>Gets or sets the positive linear threshold used by symmetric logarithmic scaling.</summary>
    public double SymmetricLogarithmThreshold {
        get => _symmetricLogarithmThreshold;
        set {
            if (!IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Symmetric logarithm threshold must be finite and greater than zero.");
            _symmetricLogarithmThreshold = value;
        }
    }

    /// <summary>Gets or sets the preferred number of generated ticks.</summary>
    public int TickCount {
        get => _tickCount;
        set {
            if (value < 2 || value > 100) throw new ArgumentOutOfRangeException(nameof(value), value, "Tick count must be from two through one hundred.");
            _tickCount = value;
        }
    }

    /// <summary>Gets or sets label density.</summary>
    public ChartLabelDensity LabelDensity {
        get => _labelDensity;
        set {
            if (!Enum.IsDefined(typeof(ChartLabelDensity), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown axis label density.");
            _labelDensity = value;
        }
    }

    /// <summary>Gets or sets label rotation in degrees.</summary>
    public double LabelAngle {
        get => _labelAngle;
        set {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Axis label angle must be finite.");
            _labelAngle = value;
        }
    }

    /// <summary>Gets or sets a value indicating whether the axis and its labels are visible.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the axis rule is visible.</summary>
    public bool ShowLine { get; set; } = true;

    /// <summary>Gets or sets a formatter for generated labels.</summary>
    public Func<double, string>? LabelFormatter { get; set; }

    /// <summary>Gets explicit value-to-label mappings.</summary>
    public List<ChartAxisLabel> Labels { get; } = new();

    /// <summary>Sets explicit bounds.</summary>
    public ChartAxis WithBounds(double minimum, double maximum) {
        if (!IsFinite(minimum)) throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Axis minimum must be finite.");
        if (!IsFinite(maximum)) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Axis maximum must be finite.");
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Axis maximum must be greater than minimum.");
        if (Scale == ChartScaleKind.Logarithmic && minimum <= 0) throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Logarithmic axes require positive bounds.");
        _minimum = minimum;
        _maximum = maximum;
        return this;
    }

    /// <summary>Clears explicit bounds.</summary>
    public ChartAxis WithAutomaticBounds() { _minimum = null; _maximum = null; return this; }

    /// <summary>Sets the mathematical scale.</summary>
    public ChartAxis WithScale(ChartScaleKind scale) { Scale = scale; return this; }

    /// <summary>Sets axis visibility.</summary>
    public ChartAxis WithVisibility(bool visible = true) { Visible = visible; return this; }

    /// <summary>Sets the label formatter.</summary>
    public ChartAxis WithLabelFormatter(Func<double, string>? formatter) { LabelFormatter = formatter; return this; }

    private void ValidateLogarithmicBound(double? value, string parameterName) {
        if (Scale == ChartScaleKind.Logarithmic && value.HasValue && value.Value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Logarithmic axes require positive bounds.");
    }

    private static void ValidateBounds(double? minimum, double? maximum, string parameterName) {
        if (minimum.HasValue && maximum.HasValue && maximum.Value <= minimum.Value) throw new ArgumentOutOfRangeException(parameterName, maximum, "Axis maximum must be greater than minimum.");
    }

    private static void ValidateFinite(double? value, string parameterName) {
        if (value.HasValue && !IsFinite(value.Value)) throw new ArgumentOutOfRangeException(parameterName, value, "Axis bounds must be finite.");
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
