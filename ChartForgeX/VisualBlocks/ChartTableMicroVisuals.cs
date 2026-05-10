using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Microvisual kind hosted by a chart table cell.
/// </summary>
public enum ChartTableCellMicroVisualKind {
    /// <summary>No microvisual is rendered.</summary>
    None,
    /// <summary>Render compact vertical bars.</summary>
    MiniBars,
    /// <summary>Render a compact sparkline.</summary>
    Sparkline
}

/// <summary>
/// Shared compact badge style for visual blocks.
/// </summary>
public enum VisualBadgeStyle {
    /// <summary>Soft tinted background with colored text.</summary>
    Soft,
    /// <summary>Solid filled background with light text.</summary>
    Solid,
    /// <summary>Outlined badge with transparent fill.</summary>
    Outline
}

public sealed partial class ChartTableCell {
    private ChartTableCellMicroVisualKind _microVisualKind;
    private readonly List<double> _microVisualValues = new();
    private string _badgeText = string.Empty;
    private VisualStatus _badgeStatus = VisualStatus.Neutral;
    private VisualBadgeStyle _badgeStyle = VisualBadgeStyle.Soft;

    /// <summary>Gets the configured cell microvisual kind.</summary>
    public ChartTableCellMicroVisualKind MicroVisualKind {
        get => _microVisualKind;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _microVisualKind = value;
        }
    }

    /// <summary>Gets microvisual values.</summary>
    public IReadOnlyList<double> MicroVisualValues => _microVisualValues;

    /// <summary>Gets or sets an optional microvisual minimum.</summary>
    public double? MicroVisualMinimum { get; set; }

    /// <summary>Gets or sets an optional microvisual maximum.</summary>
    public double? MicroVisualMaximum { get; set; }

    /// <summary>Gets or sets an optional microvisual color.</summary>
    public ChartColor? MicroVisualColor { get; set; }

    /// <summary>Gets optional compact badge text.</summary>
    public string BadgeText { get => _badgeText; set => _badgeText = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional badge status.</summary>
    public VisualStatus BadgeStatus {
        get => _badgeStatus;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _badgeStatus = value;
        }
    }

    /// <summary>Gets or sets optional badge style.</summary>
    public VisualBadgeStyle BadgeStyle {
        get => _badgeStyle;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _badgeStyle = value;
        }
    }

    /// <summary>Gets or sets an optional badge color override.</summary>
    public ChartColor? BadgeColor { get; set; }

    /// <summary>Sets compact mini bars for this table cell.</summary>
    public ChartTableCell WithMiniBars(IEnumerable<double> values, double? minimum = null, double? maximum = null, ChartColor? color = null) {
        SetMicroVisual(ChartTableCellMicroVisualKind.MiniBars, values, minimum, maximum, color);
        return this;
    }

    /// <summary>Sets a compact sparkline for this table cell.</summary>
    public ChartTableCell WithSparkline(IEnumerable<double> values, double? minimum = null, double? maximum = null, ChartColor? color = null) {
        SetMicroVisual(ChartTableCellMicroVisualKind.Sparkline, values, minimum, maximum, color);
        return this;
    }

    /// <summary>Sets a compact badge for this table cell.</summary>
    public ChartTableCell WithBadge(string text, VisualStatus status = VisualStatus.Neutral, ChartColor? color = null, VisualBadgeStyle style = VisualBadgeStyle.Soft) {
        BadgeText = text ?? throw new ArgumentNullException(nameof(text));
        BadgeStatus = status;
        BadgeColor = color;
        BadgeStyle = style;
        return this;
    }

    private void SetMicroVisual(ChartTableCellMicroVisualKind kind, IEnumerable<double> values, double? minimum, double? maximum, ChartColor? color) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        MicroVisualKind = kind;
        _microVisualValues.Clear();
        foreach (var value in values) {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(nameof(values), value, "Microvisual values must be finite.");
            _microVisualValues.Add(value);
        }

        if (_microVisualValues.Count == 0) throw new ArgumentException("Microvisuals require at least one value.", nameof(values));
        MicroVisualMinimum = minimum;
        MicroVisualMaximum = maximum;
        MicroVisualColor = color;
    }
}
