using System;
using ChartForgeX.Typography;

namespace ChartForgeX.Core;

/// <summary>Controls hierarchical treemap interiors and their independent numeric color legend.</summary>
public sealed class ChartTreemapOptions {
    private double _groupPadding = 6;
    private double _gap = 3;
    private double? _minimumLabelFontSize;
    private TextTrimming _labelTrimming;

    /// <summary>Gets or sets an optional finite positive complete-caption font-size floor in logical pixels. Default: null.</summary>
    /// <remarks>Complete wrapping and rotation at the resolved size take priority. Only a primary name that still does not fit may shrink. A larger floor never enlarges it; numeric value rows are never shortened.</remarks>
    public double? MinimumLabelFontSize {
        get => _minimumLabelFontSize;
        set {
            if (value.HasValue) {
                ChartGuards.Finite(value.Value, nameof(value));
                if (value.Value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            }
            _minimumLabelFontSize = value;
        }
    }
    /// <summary>Gets or sets the final primary-caption overflow policy. Default: None, retaining complete-or-omitted names.</summary>
    /// <remarks>Ellipsis applies only after complete wrapping, rotation and the optional font floor are exhausted. It trims at whole text-element boundaries and retains the full name and raw value in source semantics.</remarks>
    public TextTrimming LabelTrimming {
        get => _labelTrimming;
        set {
            if (!Enum.IsDefined(typeof(TextTrimming), value)) throw new ArgumentOutOfRangeException(nameof(value));
            _labelTrimming = value;
        }
    }

    /// <summary>Gets or sets the non-negative padding inside each group, in pixels. Default: six.</summary>
    public double GroupPadding {
        get => _groupPadding;
        set { NonNegative(value, nameof(value)); _groupPadding = value; }
    }
    /// <summary>Gets or sets the non-negative gap between sibling rectangles, in pixels. Small cells bound their own gap. Default: three.</summary>
    public double Gap {
        get => _gap;
        set { NonNegative(value, nameof(value)); _gap = value; }
    }
    /// <summary>Gets or sets whether measured group headers are reserved and drawn. Default: true.</summary>
    public bool ShowGroupLabels { get; set; } = true;
    /// <summary>Gets or sets whether group and leaf captions include magnitudes. Labels and semantic values are retained. Default: true.</summary>
    public bool ShowValues { get; set; } = true;
    /// <summary>Gets or sets the numeric color scale. When omitted, supplied color values use the theme's sequential ramp.</summary>
    public ChartColorScale? ColorScale { get; set; }
    /// <summary>Gets the independent color measurement's name and number display policy.</summary>
    public ChartColorDimensionOptions ColorDimension { get; private set; } = new();

    /// <summary>Copies the configured state and detaches the mutable color measurement options.</summary>
    internal ChartTreemapOptions Clone() {
        var clone = (ChartTreemapOptions)MemberwiseClone();
        clone.ColorDimension = ColorDimension.Clone();
        return clone;
    }

    private static void NonNegative(double value, string parameter) {
        ChartGuards.Finite(value, parameter);
        if (value < 0) throw new ArgumentOutOfRangeException(parameter, value, "Treemap spacing must be non-negative.");
    }
}
