using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Describes one independently styled radial arc layer.
/// </summary>
public sealed class ChartRadialLayer {
    private string _name;
    private double _value;
    private double _minimum;
    private double _maximum;
    private double _radiusRatio = 1;
    private double _strokeRatio = 0.12;
    private double _startAngleDegrees = -90;
    private double _sweepAngleDegrees = 360;
    private double _opacity = 1;
    private ChartRadialLayerCap _lineCap = ChartRadialLayerCap.Round;
    private int _separatorCount;
    private double _separatorStrokeWidth = 3;
    private double _separatorInsetRatio = 0.06;

    /// <summary>
    /// Gets or sets the layer name used in metadata and legends.
    /// </summary>
    public string Name {
        get => _name;
        set => _name = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the current value used to determine the visible arc length.
    /// </summary>
    public double Value {
        get => _value;
        set { ChartGuards.Finite(value, nameof(value)); _value = value; }
    }

    /// <summary>
    /// Gets or sets the minimum value for this layer.
    /// </summary>
    public double Minimum {
        get => _minimum;
        set { ChartGuards.Finite(value, nameof(value)); _minimum = value; }
    }

    /// <summary>
    /// Gets or sets the maximum value for this layer.
    /// </summary>
    public double Maximum {
        get => _maximum;
        set { ChartGuards.Finite(value, nameof(value)); _maximum = value; }
    }

    /// <summary>
    /// Gets or sets the layer color. When null, the chart palette is used.
    /// </summary>
    public ChartColor? Color { get; set; }

    /// <summary>
    /// Gets or sets the radius as a ratio of the available outer radius.
    /// </summary>
    public double RadiusRatio {
        get => _radiusRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value > 1.5) throw new ArgumentOutOfRangeException(nameof(value), value, "Radius ratio must be greater than zero and no more than 1.5.");
            _radiusRatio = value;
        }
    }

    /// <summary>
    /// Gets or sets the stroke width as a ratio of the available outer radius.
    /// </summary>
    public double StrokeRatio {
        get => _strokeRatio;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Stroke ratio must be greater than zero and no more than one.");
            _strokeRatio = value;
        }
    }

    /// <summary>
    /// Gets or sets the arc start angle in degrees, where -90 is the top of the circle.
    /// </summary>
    public double StartAngleDegrees {
        get => _startAngleDegrees;
        set { ChartGuards.Finite(value, nameof(value)); _startAngleDegrees = value; }
    }

    /// <summary>
    /// Gets or sets the available sweep angle in degrees.
    /// </summary>
    public double SweepAngleDegrees {
        get => _sweepAngleDegrees;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0 || value > 360) throw new ArgumentOutOfRangeException(nameof(value), value, "Sweep angle must be greater than zero and no more than 360 degrees.");
            _sweepAngleDegrees = value;
        }
    }

    /// <summary>
    /// Gets or sets the opacity applied to the layer color.
    /// </summary>
    public double Opacity {
        get => _opacity;
        set { ChartGuards.UnitInterval(value, nameof(value)); _opacity = value; }
    }

    /// <summary>
    /// Gets or sets the endpoint style.
    /// </summary>
    public ChartRadialLayerCap LineCap {
        get => _lineCap;
        set {
            if (!Enum.IsDefined(typeof(ChartRadialLayerCap), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown radial layer cap.");
            _lineCap = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of radial separators drawn across the visible arc.
    /// </summary>
    public int SeparatorCount {
        get => _separatorCount;
        set {
            if (value < 0 || value > 128) throw new ArgumentOutOfRangeException(nameof(value), value, "Separator count must be between zero and 128.");
            _separatorCount = value;
        }
    }

    /// <summary>
    /// Gets or sets the separator color. When null, the chart card background is used.
    /// </summary>
    public ChartColor? SeparatorColor { get; set; }

    /// <summary>
    /// Gets or sets the separator stroke width in pixels.
    /// </summary>
    public double SeparatorStrokeWidth {
        get => _separatorStrokeWidth;
        set {
            ChartGuards.Finite(value, nameof(value));
            if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Separator stroke width must be greater than zero.");
            _separatorStrokeWidth = value;
        }
    }

    /// <summary>
    /// Gets or sets how far separators are inset from each side of the stroke as a ratio of stroke width.
    /// </summary>
    public double SeparatorInsetRatio {
        get => _separatorInsetRatio;
        set {
            ChartGuards.UnitInterval(value, nameof(value));
            _separatorInsetRatio = value;
        }
    }

    /// <summary>
    /// Initializes a new radial layer.
    /// </summary>
    public ChartRadialLayer(string name, double value, double minimum = 0, double maximum = 100, ChartColor? color = null) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
        Minimum = minimum;
        Maximum = maximum;
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Layer maximum must be greater than minimum.");
        Color = color;
    }

    /// <summary>
    /// Creates a new radial layer.
    /// </summary>
    public static ChartRadialLayer Create(string name, double value, double minimum = 0, double maximum = 100, ChartColor? color = null) =>
        new(name, value, minimum, maximum, color);

    /// <summary>
    /// Sets the layer name.
    /// </summary>
    public ChartRadialLayer WithName(string name) {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Sets the current layer value.
    /// </summary>
    public ChartRadialLayer WithValue(double value) {
        Value = value;
        return this;
    }

    /// <summary>
    /// Sets the layer color.
    /// </summary>
    public ChartRadialLayer WithColor(ChartColor? color) {
        Color = color;
        return this;
    }

    /// <summary>
    /// Sets the layer scale.
    /// </summary>
    public ChartRadialLayer WithScale(double minimum, double maximum) {
        Minimum = minimum;
        Maximum = maximum;
        if (maximum <= minimum) throw new ArgumentOutOfRangeException(nameof(maximum), maximum, "Layer maximum must be greater than minimum.");
        return this;
    }

    /// <summary>
    /// Sets the rendered radius and stroke ratios.
    /// </summary>
    public ChartRadialLayer WithGeometry(double radiusRatio, double strokeRatio) {
        RadiusRatio = radiusRatio;
        StrokeRatio = strokeRatio;
        return this;
    }

    /// <summary>
    /// Sets the radial angle range.
    /// </summary>
    public ChartRadialLayer WithAngles(double startAngleDegrees, double sweepAngleDegrees) {
        StartAngleDegrees = startAngleDegrees;
        SweepAngleDegrees = sweepAngleDegrees;
        return this;
    }

    /// <summary>
    /// Sets the endpoint style.
    /// </summary>
    public ChartRadialLayer WithLineCap(ChartRadialLayerCap lineCap) {
        LineCap = lineCap;
        return this;
    }

    /// <summary>
    /// Sets the layer opacity.
    /// </summary>
    public ChartRadialLayer WithOpacity(double opacity) {
        Opacity = opacity;
        return this;
    }

    /// <summary>
    /// Sets separators drawn across the visible layer arc.
    /// </summary>
    public ChartRadialLayer WithSeparators(int count, ChartColor? color = null, double strokeWidth = 3, double insetRatio = 0.06) {
        SeparatorCount = count;
        SeparatorColor = color;
        SeparatorStrokeWidth = strokeWidth;
        SeparatorInsetRatio = insetRatio;
        return this;
    }
}
