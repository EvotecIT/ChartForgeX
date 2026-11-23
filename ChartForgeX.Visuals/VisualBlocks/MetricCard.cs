using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using PowerForge.Fluent;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A compact KPI or metric card visual block.
/// </summary>
[Fluent]
public sealed partial class MetricCard : FactualVisualBlock<MetricCard> {
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



    private int? _miniBarHighlightIndex;
    private double? _miniBarMinimum;
    private double? _miniBarMaximum;
    private double? _miniSparklineMinimum;
    private double? _miniSparklineMaximum;
    private Icon _icon;
    private Tone _status;
    private MetricCardBadgePlacement _badgePlacement;
    private MetricCardMicroVisualPlacement _microVisualPlacement;
    private ChartLineAreaForm _miniSparklineStyle;
    private MetricCardMicroVisualSurface _microVisualSurface;

    /// <summary>Gets compact bar values rendered inside the metric card.</summary>
    public IReadOnlyList<double> MiniBars => _miniBars;

    /// <summary>Gets compact sparkline values rendered inside the metric card.</summary>
    /// <remarks>For nullable samples this view contains only finite observations. Use <see cref="SparklineData"/> to retain original sample slots.</remarks>
    public IReadOnlyList<double> MiniSparkline => _miniSparkline;

    /// <summary>Gets optional secondary sparkline values rendered beside the primary sparkline.</summary>
    /// <remarks>For nullable samples this view contains only finite observations. Use <see cref="SecondarySparklineData"/> to retain original sample slots.</remarks>
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
    public ChartLineAreaForm MiniSparklineStyle {
        get => _miniSparklineStyle;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _miniSparklineStyle = value;
        }
    }

    /// <summary>Gets or sets an optional icon rendered as a badge. <see cref="Icons"/> lists the keys drawn; another key draws the default mark.</summary>
    public Icon Icon { get => _icon; set => _icon = value; }

    /// <summary>Gets or sets the metric status.</summary>
    public Tone Status { get => _status; set => _status = value; }

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
    public override string AccessibleName => Options.Accessibility.Name ?? (Label.Length == 0 ? base.AccessibleName : Label);

    /// <summary>Creates a new metric card.</summary>
    public static MetricCard Create() => new();

    /// <summary>Sets the primary metric label and value.</summary>
    public MetricCard Metric(string label, object? value, string? format = null, string? unit = null) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = ChartTableCell.FromValue(value, format).Text;
        Unit = unit ?? string.Empty;
        return this;
    }

    /// <summary>Adds a compact detail row to the card.</summary>
    public MetricCard AddDetail(string label, object? value, Tone? status = null, string? format = null) {
        _details.Add(new MetricCardDetail(label, ChartTableCell.FromValue(value, format).Text, status));
        return this;
    }

    /// <summary>Replaces compact bar values rendered inside the metric card.</summary>
    internal MetricCard SetMiniBars(IEnumerable<double> values, double? minimum = null, double? maximum = null, ChartColor? color = null, ChartColor? mutedColor = null, int? highlightIndex = null) {
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
        ClearMiniSparkline();
        return this;
    }

    /// <summary>Clears compact bar values from the metric card.</summary>
    public MetricCard ClearMiniBars() {
        _miniBars.Clear();
        MiniBarMinimum = null;
        MiniBarMaximum = null;
        MiniBarColor = null;
        MiniBarMutedColor = null;
        MiniBarHighlightIndex = null;
        return this;
    }

    /// <summary>Replaces compact sparkline values rendered inside the metric card.</summary>
    internal MetricCard SetMiniSparkline(IEnumerable<double> values, double? minimum = null, double? maximum = null, ChartColor? color = null, ChartColor? fillColor = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        SparklineData = null;
        _miniSparkline.Clear();
        foreach (var value in values) {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Mini sparkline values must be finite.");
            _miniSparkline.Add(value);
        }

        if (_miniSparkline.Count < 2) throw new ArgumentException("Metric card mini sparklines require at least two values.", nameof(values));
        if (SecondarySparklineCount > 0 && SecondarySparklineCount != _miniSparkline.Count) throw new InvalidOperationException("Metric card secondary mini sparklines must match the primary sparkline count.");
        MiniSparklineMinimum = minimum;
        MiniSparklineMaximum = maximum;
        MiniSparklineColor = color;
        MiniSparklineFillColor = fillColor;
        ClearMiniBars();
        return this;
    }

    /// <summary>Clears compact sparkline values from the metric card.</summary>
    public MetricCard ClearMiniSparkline() {
        SparklineData = null; SecondarySparklineData = null;
        _miniSparkline.Clear();
        _secondaryMiniSparkline.Clear();
        MiniSparklineMinimum = null;
        MiniSparklineMaximum = null;
        MiniSparklineColor = null;
        MiniSparklineFillColor = null;
        SecondaryMiniSparklineColor = null;
        return this;
    }

    /// <summary>Replaces optional secondary sparkline values rendered with the primary sparkline.</summary>
    internal MetricCard SetSecondaryMiniSparkline(IEnumerable<double> values, ChartColor? color = null) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        SecondarySparklineData = null;
        _secondaryMiniSparkline.Clear();
        foreach (var value in values) {
            if (!IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Secondary mini sparkline values must be finite.");
            _secondaryMiniSparkline.Add(value);
        }

        if (_secondaryMiniSparkline.Count < 2) throw new ArgumentException("Metric card secondary mini sparklines require at least two values.", nameof(values));
        if (SparklineCount > 0 && _secondaryMiniSparkline.Count != SparklineCount) throw new InvalidOperationException("Metric card secondary mini sparklines must match the primary sparkline count.");
        SecondaryMiniSparklineColor = color;
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
    private Tone _status;

    /// <summary>Initializes a metric card detail.</summary>
    public MetricCardDetail(string label, string value, Tone? status = null) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _value = value ?? throw new ArgumentNullException(nameof(value));
        Status = status ?? Tone.Neutral;
    }

    /// <summary>Gets or sets the detail label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the detail value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the semantic status color for the detail marker.</summary>
    public Tone Status { get => _status; set => _status = value; }
}
