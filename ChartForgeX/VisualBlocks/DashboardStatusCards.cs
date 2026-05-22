using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Visual treatment for a segmented metric block.
/// </summary>
public enum SegmentedMetricStyle {
    /// <summary>Render one or more fixed-count progress rows.</summary>
    ProgressRows,
    /// <summary>Render a part-to-whole capsule split with optional legend values.</summary>
    CapsuleSplit,
    /// <summary>Render ordered stage groups as compact vertical segmented columns.</summary>
    FunnelColumns,
    /// <summary>Render one part-to-whole stacked strip with legend rows.</summary>
    CompositionStrip,
    /// <summary>Render an analytics distribution strip with chips and detail rows.</summary>
    DistributionRows
}

/// <summary>
/// A generic segmented metric block for progress rows, part-to-whole strips, and distribution rows.
/// </summary>
public sealed class SegmentedMetricBlock : VisualBlock<SegmentedMetricBlock> {
    private readonly List<SegmentedMetricItem> _items = new();
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string _unit = string.Empty;
    private string _caption = string.Empty;
    private string _actionLabel = string.Empty;
    private string _actionSymbol = ">";
    private string _actionUrl = string.Empty;
    private string _headerSymbol = string.Empty;
    private SegmentedMetricStyle _style;

    /// <summary>Gets or sets the visual treatment used for the items.</summary>
    public SegmentedMetricStyle Style {
        get => _style;
        set {
            if (!Enum.IsDefined(typeof(SegmentedMetricStyle), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown segmented metric style.");
            _style = value;
        }
    }

    /// <summary>Gets metric items.</summary>
    public IReadOnlyList<SegmentedMetricItem> Items => _items;

    /// <summary>Gets the metric label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the metric value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the unit suffix rendered beside item values for composition-style blocks.</summary>
    public string Unit { get => _unit; set => _unit = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional caption text rendered beside the metric.</summary>
    public string Caption { get => _caption; set => _caption = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional compact header symbol rendered in a badge.</summary>
    public string HeaderSymbol { get => _headerSymbol; set => _headerSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets whether trailing menu dots are shown in the card header.</summary>
    public bool ShowMenu { get; set; }

    /// <summary>Gets optional footer action text.</summary>
    public string ActionLabel { get => _actionLabel; set => _actionLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action symbol.</summary>
    public string ActionSymbol { get => _actionSymbol; set => _actionSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action URL for SVG/HTML outputs.</summary>
    public string ActionUrl { get => _actionUrl; set => _actionUrl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional footer action background color.</summary>
    public ChartColor? ActionBackground { get; set; }

    /// <summary>Gets or sets optional footer action text color.</summary>
    public ChartColor? ActionForeground { get; set; }

    /// <summary>Creates a new segmented metric block.</summary>
    public static SegmentedMetricBlock Create(SegmentedMetricStyle style = SegmentedMetricStyle.ProgressRows) => new() { Style = style };

    /// <summary>Sets the segmented metric visual treatment.</summary>
    public SegmentedMetricBlock WithStyle(SegmentedMetricStyle style) { Style = style; return this; }

    /// <summary>Sets the card metric label and value.</summary>
    public SegmentedMetricBlock WithMetric(string label, object? value, string? unit = null, string? caption = null, string? format = null) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = ChartTableCell.FromValue(value, format).Text;
        Unit = unit ?? string.Empty;
        Caption = caption ?? string.Empty;
        return this;
    }

    /// <summary>Sets optional caption text.</summary>
    public SegmentedMetricBlock WithCaption(string caption) {
        Caption = caption ?? throw new ArgumentNullException(nameof(caption));
        return this;
    }

    /// <summary>Adds a generic segmented metric item.</summary>
    public SegmentedMetricBlock AddItem(
        string label,
        double value,
        double maximum = 100,
        int segments = 40,
        ChartColor? color = null,
        VisualStatus status = VisualStatus.None,
        ChartFillPattern pattern = ChartFillPattern.None,
        string? delta = null,
        string? symbol = null,
        object? displayValue = null,
        string? displayFormat = null) {
        _items.Add(new SegmentedMetricItem(label, value) {
            Maximum = maximum,
            Segments = segments,
            Color = color,
            Status = status,
            Pattern = pattern,
            Delta = delta ?? string.Empty,
            Symbol = symbol ?? string.Empty,
            DisplayValue = FormatDisplayValue(displayValue, displayFormat)
        });
        return this;
    }

    /// <summary>Adds a preconfigured segmented metric item.</summary>
    public SegmentedMetricBlock AddItem(SegmentedMetricItem item) {
        if (item == null) throw new ArgumentNullException(nameof(item));
        _items.Add(item);
        return this;
    }

    /// <summary>Adds a segmented metric item and configures it with a callback.</summary>
    public SegmentedMetricBlock AddItem(string label, double value, Action<SegmentedMetricItem> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var item = new SegmentedMetricItem(label, value);
        configure(item);
        return AddItem(item);
    }

    /// <summary>Sets optional compact header symbol rendered in a badge.</summary>
    public SegmentedMetricBlock WithHeaderSymbol(string symbol) {
        HeaderSymbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        return this;
    }

    /// <summary>Sets whether trailing menu dots are shown in the card header.</summary>
    public SegmentedMetricBlock WithMenu(bool enabled = true) {
        ShowMenu = enabled;
        return this;
    }

    /// <summary>Sets optional footer action text.</summary>
    public SegmentedMetricBlock WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? ">";
        ActionUrl = url ?? string.Empty;
        return this;
    }

    /// <summary>Sets optional footer action visual styling.</summary>
    public SegmentedMetricBlock WithActionStyle(ChartColor? background = null, ChartColor? foreground = null) {
        ActionBackground = background;
        ActionForeground = foreground;
        return this;
    }

    internal static string FormatDisplayValue(object? value, string? format = null) =>
        value == null ? string.Empty : ChartTableCell.FromValue(value, format).Text;
}

/// <summary>
/// One item in a segmented metric block.
/// </summary>
public sealed class SegmentedMetricItem {
    private string _label;
    private string _delta = string.Empty;
    private string _symbol = string.Empty;
    private string _displayValue = string.Empty;
    private VisualStatus _status;
    private ChartFillPattern _pattern;

    /// <summary>Initializes a segmented metric item.</summary>
    public SegmentedMetricItem(string label, double value) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
    }

    /// <summary>Gets or sets the item label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the numeric item value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets the maximum for progress-style items.</summary>
    public double Maximum { get; set; } = 100;

    /// <summary>Gets or sets the fixed segment count for progress-style items.</summary>
    public int Segments { get; set; } = 40;

    /// <summary>Gets or sets an optional item accent color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets optional delta text.</summary>
    public string Delta { get => _delta; set => _delta = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets compact symbol text shown in distribution rows.</summary>
    public string Symbol { get => _symbol; set => _symbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional display text used instead of a derived item value.</summary>
    public string DisplayValue { get => _displayValue; set => _displayValue = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the semantic item status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets an optional fill pattern hint.</summary>
    public ChartFillPattern Pattern {
        get => _pattern;
        set {
            if (!Enum.IsDefined(typeof(ChartFillPattern), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown chart fill pattern.");
            _pattern = value;
        }
    }

    /// <summary>Sets the progress maximum and fixed segment count.</summary>
    public SegmentedMetricItem WithProgress(double maximum, int segments) {
        Maximum = maximum;
        Segments = segments;
        return this;
    }

    /// <summary>Sets an explicit item color.</summary>
    public SegmentedMetricItem WithColor(ChartColor? color) {
        Color = color;
        return this;
    }

    /// <summary>Sets the semantic item status.</summary>
    public SegmentedMetricItem WithStatus(VisualStatus status) {
        Status = status;
        return this;
    }

    /// <summary>Sets the optional fill pattern hint.</summary>
    public SegmentedMetricItem WithPattern(ChartFillPattern pattern) {
        Pattern = pattern;
        return this;
    }

    /// <summary>Sets optional delta text.</summary>
    public SegmentedMetricItem WithDelta(string delta) {
        Delta = delta ?? throw new ArgumentNullException(nameof(delta));
        return this;
    }

    /// <summary>Sets compact symbol text shown by visual treatments that use symbols.</summary>
    public SegmentedMetricItem WithSymbol(string symbol) {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        return this;
    }

    /// <summary>Sets optional display text used instead of a derived item value.</summary>
    public SegmentedMetricItem WithDisplayValue(object? displayValue, string? format = null) {
        DisplayValue = SegmentedMetricBlock.FormatDisplayValue(displayValue, format);
        return this;
    }
}
