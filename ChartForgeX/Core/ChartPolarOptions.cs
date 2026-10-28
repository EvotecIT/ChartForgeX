using System;

namespace ChartForgeX.Core;

/// <summary>Per-series source-ordered Polar form, curve and outline policy; angles and numeric radius axes stay chart-owned.</summary>
public sealed class ChartPolarOptions {
    private ChartLineAreaForm _form = ChartLineAreaForm.Line;
    private double? _fillOpacity;
    private double? _curveTension;
    private ChartStrokeStyle? _outlineStroke;
    private bool _closePath;

    /// <summary>Gets or sets Line (the default) or Area. Area closes each authored run with at least three vertices; shorter runs retain points and open outlines.</summary>
    public ChartLineAreaForm Form {
        get => _form;
        set {
            if (!Enum.IsDefined(typeof(ChartLineAreaForm), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _form = value; IsConfigured = true;
        }
    }

    /// <summary>Gets or sets Area fill opacity from zero through one. Null uses the theme; Line rejects an authored opacity.</summary>
    public double? FillOpacity {
        get => _fillOpacity;
        set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _fillOpacity = value; IsConfigured = true; }
    }

    /// <summary>Gets or sets cubic tension from zero through one. Null or zero preserves straight joins; tangents never cross a BreakBefore boundary.</summary>
    public double? CurveTension {
        get => _curveTension;
        set { if (value.HasValue) ChartGuards.UnitInterval(value.Value, nameof(value)); _curveTension = value; IsConfigured = true; }
    }

    /// <summary>Gets or sets immutable outline overrides. Null retains series/theme paint; zero width hides only the outline.</summary>
    public ChartStrokeStyle? OutlineStroke {
        get => _outlineStroke;
        set { _outlineStroke = value; IsConfigured = true; }
    }

    /// <summary>Gets or sets whether Line closes each authored run with at least three vertices. Default false preserves open lines; Area always closes eligible runs.</summary>
    public bool ClosePath {
        get => _closePath;
        set { _closePath = value; IsConfigured = true; }
    }

    internal bool IsConfigured { get; private set; }
    /// <summary>Copies fields without changing configuration tracking; referenced outline strokes are immutable.</summary>
    internal ChartPolarOptions Clone() => (ChartPolarOptions)MemberwiseClone();
}
