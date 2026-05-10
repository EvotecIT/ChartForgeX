using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A dashboard card with one or more fixed-count segmented progress rows.
/// </summary>
public sealed class SegmentedProgressCard : VisualBlock<SegmentedProgressCard> {
    private readonly List<SegmentedProgressRow> _rows = new();
    private string _actionLabel = string.Empty;
    private string _actionSymbol = ">";
    private string _actionUrl = string.Empty;
    private string _headerSymbol = string.Empty;

    /// <summary>Gets progress rows.</summary>
    public IReadOnlyList<SegmentedProgressRow> Rows => _rows;

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

    /// <summary>Creates a new segmented progress card.</summary>
    public static SegmentedProgressCard Create() => new();

    /// <summary>Adds a segmented progress row.</summary>
    public SegmentedProgressCard AddRow(string label, double value, double maximum = 100, int segments = 40, ChartColor? color = null, string? delta = null, VisualStatus status = VisualStatus.Neutral) {
        _rows.Add(new SegmentedProgressRow(label, value, maximum, segments, color, delta, status));
        return this;
    }

    /// <summary>Sets optional compact header symbol rendered in a badge.</summary>
    public SegmentedProgressCard WithHeaderSymbol(string symbol) {
        HeaderSymbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        return this;
    }

    /// <summary>Sets whether trailing menu dots are shown in the card header.</summary>
    public SegmentedProgressCard WithMenu(bool enabled = true) {
        ShowMenu = enabled;
        return this;
    }

    /// <summary>Sets optional footer action text.</summary>
    public SegmentedProgressCard WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? ">";
        ActionUrl = url ?? string.Empty;
        return this;
    }

    /// <summary>Sets optional footer action visual styling.</summary>
    public SegmentedProgressCard WithActionStyle(ChartColor? background = null, ChartColor? foreground = null) {
        ActionBackground = background;
        ActionForeground = foreground;
        return this;
    }
}

/// <summary>
/// One row in a segmented progress card.
/// </summary>
public sealed class SegmentedProgressRow {
    private string _label;
    private string _delta = string.Empty;
    private VisualStatus _status;

    /// <summary>Initializes a segmented progress row.</summary>
    public SegmentedProgressRow(string label, double value, double maximum = 100, int segments = 40, ChartColor? color = null, string? delta = null, VisualStatus status = VisualStatus.Neutral) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
        Maximum = maximum;
        Segments = segments;
        Color = color;
        _delta = delta ?? string.Empty;
        Status = status;
    }

    /// <summary>Gets or sets the row label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the progress value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets the progress maximum.</summary>
    public double Maximum { get; set; }

    /// <summary>Gets or sets the number of visible segments.</summary>
    public int Segments { get; set; }

    /// <summary>Gets or sets an optional row accent color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets optional delta text.</summary>
    public string Delta { get => _delta; set => _delta = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the semantic row status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }
}

/// <summary>
/// A compact part-to-whole dashboard card with one stacked strip and legend rows.
/// </summary>
public sealed class CompositionStatusCard : VisualBlock<CompositionStatusCard> {
    private readonly List<CompositionStatusSegment> _segments = new();
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string _unit = string.Empty;
    private string _actionLabel = string.Empty;
    private string _actionSymbol = ">";
    private string _actionUrl = string.Empty;

    /// <summary>Gets the metric label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the metric value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the unit suffix rendered beside segment values.</summary>
    public string Unit { get => _unit; set => _unit = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets composition segments.</summary>
    public IReadOnlyList<CompositionStatusSegment> Segments => _segments;

    /// <summary>Gets optional footer action text.</summary>
    public string ActionLabel { get => _actionLabel; set => _actionLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action symbol.</summary>
    public string ActionSymbol { get => _actionSymbol; set => _actionSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action URL for SVG/HTML outputs.</summary>
    public string ActionUrl { get => _actionUrl; set => _actionUrl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Creates a new composition status card.</summary>
    public static CompositionStatusCard Create() => new();

    /// <summary>Sets the card metric label and value.</summary>
    public CompositionStatusCard WithMetric(string label, object? value, string? unit = null, string? format = null) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = ChartTableCell.FromValue(value, format).Text;
        Unit = unit ?? string.Empty;
        return this;
    }

    /// <summary>Adds a composition segment.</summary>
    public CompositionStatusCard AddSegment(string label, double value, ChartColor? color = null, VisualStatus status = VisualStatus.Neutral, ChartFillPattern pattern = ChartFillPattern.None) {
        _segments.Add(new CompositionStatusSegment(label, value, color, status, pattern));
        return this;
    }

    /// <summary>Sets optional footer action text.</summary>
    public CompositionStatusCard WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? ">";
        ActionUrl = url ?? string.Empty;
        return this;
    }
}

/// <summary>
/// One segment in a composition status card.
/// </summary>
public sealed class CompositionStatusSegment {
    private string _label;
    private VisualStatus _status;
    private ChartFillPattern _pattern;

    /// <summary>Initializes a composition segment.</summary>
    public CompositionStatusSegment(string label, double value, ChartColor? color = null, VisualStatus status = VisualStatus.Neutral, ChartFillPattern pattern = ChartFillPattern.None) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
        Color = color;
        Status = status;
        Pattern = pattern;
    }

    /// <summary>Gets or sets the segment label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the segment value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets an optional segment color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets segment status.</summary>
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
}

/// <summary>
/// A payment-style distribution card with a stacked strip, legend chips, and per-segment ring rows.
/// </summary>
public sealed class DistributionStripCard : VisualBlock<DistributionStripCard> {
    private readonly List<DistributionStripSegment> _segments = new();
    private string _label = string.Empty;
    private string _value = string.Empty;
    private string _caption = string.Empty;
    private string _actionLabel = string.Empty;
    private string _actionSymbol = ">";
    private string _actionUrl = string.Empty;

    /// <summary>Gets the metric label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets the metric value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional caption text rendered beside the metric.</summary>
    public string Caption { get => _caption; set => _caption = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets distribution segments.</summary>
    public IReadOnlyList<DistributionStripSegment> Segments => _segments;

    /// <summary>Gets optional footer action text.</summary>
    public string ActionLabel { get => _actionLabel; set => _actionLabel = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action symbol.</summary>
    public string ActionSymbol { get => _actionSymbol; set => _actionSymbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets optional footer action URL for SVG/HTML outputs.</summary>
    public string ActionUrl { get => _actionUrl; set => _actionUrl = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Creates a new distribution strip card.</summary>
    public static DistributionStripCard Create() => new();

    /// <summary>Sets the card metric label and value.</summary>
    public DistributionStripCard WithMetric(string label, object? value, string? caption = null, string? format = null) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = ChartTableCell.FromValue(value, format).Text;
        Caption = caption ?? string.Empty;
        return this;
    }

    /// <summary>Sets optional caption text.</summary>
    public DistributionStripCard WithCaption(string caption) {
        Caption = caption ?? throw new ArgumentNullException(nameof(caption));
        return this;
    }

    /// <summary>Adds a distribution segment.</summary>
    public DistributionStripCard AddSegment(string label, double value, ChartColor? color = null, string? symbol = null, string? detail = null, VisualStatus status = VisualStatus.Neutral) {
        _segments.Add(new DistributionStripSegment(label, value, color, symbol, detail, status));
        return this;
    }

    /// <summary>Sets optional footer action text.</summary>
    public DistributionStripCard WithAction(string label, string? symbol = null, string? url = null) {
        ActionLabel = label ?? throw new ArgumentNullException(nameof(label));
        ActionSymbol = symbol ?? ">";
        ActionUrl = url ?? string.Empty;
        return this;
    }
}

/// <summary>
/// One segment in a distribution strip card.
/// </summary>
public sealed class DistributionStripSegment {
    private string _label;
    private string _symbol = string.Empty;
    private string _detail = string.Empty;
    private VisualStatus _status;

    /// <summary>Initializes a distribution segment.</summary>
    public DistributionStripSegment(string label, double value, ChartColor? color = null, string? symbol = null, string? detail = null, VisualStatus status = VisualStatus.Neutral) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Value = value;
        Color = color;
        _symbol = symbol ?? string.Empty;
        _detail = detail ?? string.Empty;
        Status = status;
    }

    /// <summary>Gets or sets the segment label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the numeric segment value.</summary>
    public double Value { get; set; }

    /// <summary>Gets or sets an optional segment color.</summary>
    public ChartColor? Color { get; set; }

    /// <summary>Gets or sets compact symbol text shown in the row badge.</summary>
    public string Symbol { get => _symbol; set => _symbol = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional trailing detail text.</summary>
    public string Detail { get => _detail; set => _detail = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets segment status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }
}
