using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A compact KPI or metric card visual block.
/// </summary>
public sealed class MetricCard : VisualBlock<MetricCard> {
    private readonly List<MetricCardDetail> _details = new();
    private readonly List<double> _miniBars = new();
    private readonly List<double> _miniSparkline = new();
    private readonly List<double> _secondaryMiniSparkline = new();
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string _unit = string.Empty;
    private string _caption = string.Empty;
    private string _trend = string.Empty;
    private string _symbol = string.Empty;
    private string _actionLabel = string.Empty;
    private string _actionSymbol = ">";
    private string _actionUrl = string.Empty;
    private int? _miniBarHighlightIndex;
    private double? _miniBarMinimum;
    private double? _miniBarMaximum;
    private double? _miniSparklineMinimum;
    private double? _miniSparklineMaximum;
    private VisualIcon _icon;
    private VisualStatus _status;
    private MetricCardBadgePlacement _badgePlacement;
    private MetricCardMicroVisualPlacement _microVisualPlacement;
    private MetricCardSparklineStyle _miniSparklineStyle;
    private MetricCardMicroVisualSurface _microVisualSurface;

    /// <summary>Gets compact bar values rendered inside the metric card.</summary>
    public IReadOnlyList<double> MiniBars => _miniBars;

    /// <summary>Gets compact sparkline values rendered inside the metric card.</summary>
    public IReadOnlyList<double> MiniSparkline => _miniSparkline;

    /// <summary>Gets optional secondary sparkline values rendered beside the primary sparkline.</summary>
    public IReadOnlyList<double> SecondaryMiniSparkline => _secondaryMiniSparkline;

    /// <summary>Gets compact supporting detail rows rendered inside the card.</summary>
    public IReadOnlyList<MetricCardDetail> Details => _details;

    /// <summary>Gets or sets the metric label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the metric value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional metric unit rendered beside the value.</summary>
    public string Unit { get => _unit; set => _unit = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional supporting text.</summary>
    public string Caption { get => _caption; set => _caption = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional trend text.</summary>
    public string Trend { get => _trend; set => _trend = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional compact symbol rendered as a badge.</summary>
    public string Symbol { get => _symbol; set => _symbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional footer action text.</summary>
    public string ActionLabel { get => _actionLabel; set => _actionLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional footer action symbol.</summary>
    public string ActionSymbol { get => _actionSymbol; set => _actionSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional URL for the footer action in SVG/HTML outputs.</summary>
    public string ActionUrl { get => _actionUrl; set => _actionUrl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the zero-based mini bar to emphasize. When unset, the last mini bar is emphasized.</summary>
    public int? MiniBarHighlightIndex {
        get => _miniBarHighlightIndex;
        set {
            if (value.HasValue && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Mini bar highlight index must be non-negative.");
            _miniBarHighlightIndex = value;
        }
    }

    /// <summary>Gets or sets the optional lower bound used for mini bar scaling.</summary>
    public double? MiniBarMinimum {
        get => _miniBarMinimum;
        set {
            if (value.HasValue && !IsFinite(value.Value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Mini bar minimum must be finite.");
            _miniBarMinimum = value;
        }
    }

    /// <summary>Gets or sets the optional upper bound used for mini bar scaling.</summary>
    public double? MiniBarMaximum {
        get => _miniBarMaximum;
        set {
            if (value.HasValue && !IsFinite(value.Value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Mini bar maximum must be finite.");
            _miniBarMaximum = value;
        }
    }

    /// <summary>Gets or sets the optional lower bound used for mini sparkline scaling.</summary>
    public double? MiniSparklineMinimum {
        get => _miniSparklineMinimum;
        set {
            if (value.HasValue && !IsFinite(value.Value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Mini sparkline minimum must be finite.");
            _miniSparklineMinimum = value;
        }
    }

    /// <summary>Gets or sets the optional upper bound used for mini sparkline scaling.</summary>
    public double? MiniSparklineMaximum {
        get => _miniSparklineMaximum;
        set {
            if (value.HasValue && !IsFinite(value.Value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Mini sparkline maximum must be finite.");
            _miniSparklineMaximum = value;
        }
    }

    /// <summary>Gets or sets the optional color for the emphasized mini bar.</summary>
    public ChartColor? MiniBarColor { get; set; }

    /// <summary>Gets or sets the optional color for non-emphasized mini bars.</summary>
    public ChartColor? MiniBarMutedColor { get; set; }

    /// <summary>Gets or sets the optional stroke color for the mini sparkline.</summary>
    public ChartColor? MiniSparklineColor { get; set; }

    /// <summary>Gets or sets the optional fill color under the mini sparkline.</summary>
    public ChartColor? MiniSparklineFillColor { get; set; }

    /// <summary>Gets or sets the optional stroke color for the secondary mini sparkline.</summary>
    public ChartColor? SecondaryMiniSparklineColor { get; set; }

    /// <summary>Gets or sets the mini sparkline presentation style.</summary>
    public MetricCardSparklineStyle MiniSparklineStyle {
        get => _miniSparklineStyle;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _miniSparklineStyle = value;
        }
    }

    /// <summary>Gets or sets an optional built-in icon rendered as a badge.</summary>
    public VisualIcon Icon {
        get => _icon;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _icon = value;
        }
    }

    /// <summary>Gets or sets the metric status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets the badge placement when an icon or symbol is configured.</summary>
    public MetricCardBadgePlacement BadgePlacement {
        get => _badgePlacement;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _badgePlacement = value;
        }
    }

    /// <summary>Gets or sets where compact mini charts are rendered inside the metric card.</summary>
    public MetricCardMicroVisualPlacement MicroVisualPlacement {
        get => _microVisualPlacement;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _microVisualPlacement = value;
        }
    }

    /// <summary>Gets or sets the optional surface treatment for the mini visual.</summary>
    public MetricCardMicroVisualSurface MicroVisualSurface {
        get => _microVisualSurface;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _microVisualSurface = value;
        }
    }

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Label.Length == 0 ? base.AccessibleName : Label;

    /// <summary>Creates a new metric card.</summary>
    public static MetricCard Create() => new();

    /// <summary>Sets the primary metric label and value.</summary>
    public MetricCard WithMetric(string label, object? value, string? format = null, string? unit = null) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = ChartTableCell.FromValue(value, format).Text;
        Unit = unit ?? string.Empty;
        return this;
    }

    /// <summary>Sets an optional metric unit rendered beside the value.</summary>
    public MetricCard WithUnit(string unit) { Unit = unit ?? throw new ArgumentNullException(nameof(unit)); return this; }

    /// <summary>Sets optional supporting text.</summary>
    public MetricCard WithCaption(string caption) { Caption = caption ?? throw new ArgumentNullException(nameof(caption)); return this; }

    /// <summary>Sets optional trend text.</summary>
    public MetricCard WithTrend(string trend) { Trend = trend ?? throw new ArgumentNullException(nameof(trend)); return this; }

    /// <summary>Adds a compact detail row to the card.</summary>
    public MetricCard AddDetail(string label, object? value, VisualStatus status = VisualStatus.Neutral, string? format = null) {
        _details.Add(new MetricCardDetail(label, ChartTableCell.FromValue(value, format).Text, status));
        return this;
    }

    /// <summary>Sets an optional compact symbol rendered as a badge.</summary>
    public MetricCard WithSymbol(string symbol) { Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol)); return this; }

    /// <summary>Sets optional footer action text rendered in the metric card.</summary>
    public MetricCard WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? ">";
        ActionUrl = url ?? string.Empty;
        return this;
    }

    /// <summary>Clears optional footer action text from the metric card.</summary>
    public MetricCard WithoutAction() {
        ActionLabel = string.Empty;
        ActionSymbol = ">";
        ActionUrl = string.Empty;
        return this;
    }

    /// <summary>Sets an optional built-in icon rendered as a badge.</summary>
    public MetricCard WithIcon(VisualIcon icon) { Icon = icon; return this; }

    /// <summary>Replaces compact bar values rendered inside the metric card.</summary>
    public MetricCard WithMiniBars(IEnumerable<double> values, double? minimum = null, double? maximum = null, ChartColor? color = null, ChartColor? mutedColor = null, int? highlightIndex = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        _miniBars.Clear();
        foreach (var value in values) {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Mini bar values must be finite.");
            _miniBars.Add(value);
        }

        if (_miniBars.Count == 0) throw new ArgumentException("Metric cards require at least one mini bar value when mini bars are configured.", nameof(values));
        MiniBarMinimum = minimum;
        MiniBarMaximum = maximum;
        MiniBarColor = color;
        MiniBarMutedColor = mutedColor;
        MiniBarHighlightIndex = highlightIndex;
        WithoutMiniSparkline();
        return this;
    }

    /// <summary>Clears compact bar values from the metric card.</summary>
    public MetricCard WithoutMiniBars() {
        _miniBars.Clear();
        MiniBarMinimum = null;
        MiniBarMaximum = null;
        MiniBarColor = null;
        MiniBarMutedColor = null;
        MiniBarHighlightIndex = null;
        return this;
    }

    /// <summary>Replaces compact sparkline values rendered inside the metric card.</summary>
    public MetricCard WithMiniSparkline(IEnumerable<double> values, double? minimum = null, double? maximum = null, ChartColor? color = null, ChartColor? fillColor = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        _miniSparkline.Clear();
        foreach (var value in values) {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Mini sparkline values must be finite.");
            _miniSparkline.Add(value);
        }

        if (_miniSparkline.Count < 2) throw new ArgumentException("Metric card mini sparklines require at least two values.", nameof(values));
        if (_secondaryMiniSparkline.Count > 0 && _secondaryMiniSparkline.Count != _miniSparkline.Count) throw new InvalidOperationException("Metric card secondary mini sparklines must match the primary sparkline count.");
        MiniSparklineMinimum = minimum;
        MiniSparklineMaximum = maximum;
        MiniSparklineColor = color;
        MiniSparklineFillColor = fillColor;
        WithoutMiniBars();
        return this;
    }

    /// <summary>Clears compact sparkline values from the metric card.</summary>
    public MetricCard WithoutMiniSparkline() {
        _miniSparkline.Clear();
        _secondaryMiniSparkline.Clear();
        MiniSparklineMinimum = null;
        MiniSparklineMaximum = null;
        MiniSparklineColor = null;
        MiniSparklineFillColor = null;
        SecondaryMiniSparklineColor = null;
        return this;
    }

    /// <summary>Sets the metric status.</summary>
    public MetricCard WithStatus(VisualStatus status) {
        Status = status;
        return this;
    }

    /// <summary>Sets the badge placement when an icon or symbol is configured.</summary>
    public MetricCard WithBadgePlacement(MetricCardBadgePlacement placement) {
        BadgePlacement = placement;
        return this;
    }

    /// <summary>Sets where compact mini charts are rendered inside the metric card.</summary>
    public MetricCard WithMicroVisualPlacement(MetricCardMicroVisualPlacement placement) {
        MicroVisualPlacement = placement;
        return this;
    }

    /// <summary>Sets the mini sparkline presentation style.</summary>
    public MetricCard WithMiniSparklineStyle(MetricCardSparklineStyle style) {
        MiniSparklineStyle = style;
        return this;
    }

    /// <summary>Replaces optional secondary sparkline values rendered with the primary sparkline.</summary>
    public MetricCard WithSecondaryMiniSparkline(IEnumerable<double> values, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        _secondaryMiniSparkline.Clear();
        foreach (var value in values) {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Secondary mini sparkline values must be finite.");
            _secondaryMiniSparkline.Add(value);
        }

        if (_secondaryMiniSparkline.Count < 2) throw new ArgumentException("Metric card secondary mini sparklines require at least two values.", nameof(values));
        if (_miniSparkline.Count > 0 && _secondaryMiniSparkline.Count != _miniSparkline.Count) throw new InvalidOperationException("Metric card secondary mini sparklines must match the primary sparkline count.");
        SecondaryMiniSparklineColor = color;
        return this;
    }

    /// <summary>Sets the optional surface treatment for compact mini visuals.</summary>
    public MetricCard WithMicroVisualSurface(MetricCardMicroVisualSurface surface) {
        MicroVisualSurface = surface;
        return this;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

/// <summary>
/// A compact supporting metric rendered inside a <see cref="MetricCard"/>.
/// </summary>
public sealed class MetricCardDetail {
    private string _label;
    private string _value;
    private VisualStatus _status;

    /// <summary>Initializes a metric card detail.</summary>
    public MetricCardDetail(string label, string value, VisualStatus status = VisualStatus.Neutral) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _value = value ?? throw new ArgumentNullException(nameof(value));
        Status = status;
    }

    /// <summary>Gets or sets the detail label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the detail value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the semantic status color for the detail marker.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }
}
