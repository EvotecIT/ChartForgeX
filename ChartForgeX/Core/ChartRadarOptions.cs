using System;

namespace ChartForgeX.Core;

/// <summary>Per-series radar form and area paint; categorical and radial axes remain shared by the chart.</summary>
public sealed class ChartRadarOptions {
    private ChartLineAreaForm _form;
    private double? _fillOpacity;
    private double? _curveTension;

    /// <summary>Gets or sets the radar form. Area preserves the default of AddRadar.</summary>
    public ChartLineAreaForm Form {
        get => _form;
        set {
            if (!Enum.IsDefined(typeof(ChartLineAreaForm), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _form = value; IsConfigured = true;
        }
    }

    /// <summary>Gets or sets the Area form's fill opacity from zero through one. Null uses the theme area opacity.</summary>
    public double? FillOpacity {
        get => _fillOpacity;
        set {
            if (value.HasValue) {
                ChartGuards.Finite(value.Value, nameof(value));
                if (value.Value < 0 || value.Value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            }
            _fillOpacity = value; IsConfigured = true;
        }
    }

    /// <summary>Gets or sets closed-curve tension from zero through one. Null or zero preserves straight edges; positive values use cyclic cubic tangents through the observations.</summary>
    public double? CurveTension {
        get => _curveTension;
        set {
            if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value));
            _curveTension = value; IsConfigured = true;
        }
    }

    internal bool IsConfigured { get; private set; }
}
