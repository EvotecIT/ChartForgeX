using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.Composition;

/// <summary>A row rendered inside a key/value visual canvas block.</summary>
public sealed class VisualCanvasKeyValueItem {
    private string _label;
    private string _value;

    /// <summary>Initializes a label-only key/value block row.</summary>
    public VisualCanvasKeyValueItem(string label) : this(label, string.Empty, true) { }

    /// <summary>Initializes a key/value block row.</summary>
    public VisualCanvasKeyValueItem(string label, string? value) : this(label, value, false) { }

    private VisualCanvasKeyValueItem(string label, string? value, bool labelOnly) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
        _value = value ?? string.Empty;
        LabelOnly = labelOnly;
    }

    /// <summary>Gets or sets the row label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets the row value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets whether this row should render as a label-only heading.</summary>
    public bool LabelOnly { get; set; }
    /// <summary>Gets or sets an optional row-specific label color.</summary>
    public ChartColor? LabelColor { get; set; }
    /// <summary>Gets or sets an optional row-specific value color.</summary>
    public ChartColor? ValueColor { get; set; }

    /// <summary>Creates a label-only key/value block row.</summary>
    public static VisualCanvasKeyValueItem LabelRow(string label) => new(label);
    /// <summary>Creates a key/value block row.</summary>
    public static VisualCanvasKeyValueItem Pair(string label, string? value) => new(label, value);
}

/// <summary>Reusable key/value text block with measured label columns and wrapped value text.</summary>
public sealed class VisualCanvasKeyValueBlockLayer : VisualCanvasLayer {
    private readonly List<VisualCanvasKeyValueItem> _items = new();
    private double _labelFontSize = 16;
    private double _valueFontSize = 16;
    private double _columnGap = 24;
    private double _rowGap = 4;
    private double? _labelWidth;
    private double? _valueWrapWidth;
    private string _fontFamilyName = string.Empty;

    /// <summary>Initializes a key/value text block layer.</summary>
    /// <param name="x">The block X coordinate.</param>
    /// <param name="y">The block Y coordinate.</param>
    /// <param name="width">The block width.</param>
    /// <param name="height">The initial block height.</param>
    /// <param name="items">The key/value rows.</param>
    public VisualCanvasKeyValueBlockLayer(double x, double y, double width, double height, IEnumerable<VisualCanvasKeyValueItem> items) : base(x, y, width, height) {
        if (items == null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items) _items.Add(item ?? throw new ArgumentException("Key/value blocks cannot contain null items.", nameof(items)));
        if (_items.Count == 0) throw new ArgumentException("Key/value blocks require at least one item.", nameof(items));
    }

    /// <summary>Gets the key/value rows.</summary>
    public IReadOnlyList<VisualCanvasKeyValueItem> Items => _items;
    /// <summary>Gets or sets the label font size.</summary>
    public double LabelFontSize { get => _labelFontSize; set { ValidatePositive(value, nameof(value)); _labelFontSize = value; } }
    /// <summary>Gets or sets the value font size.</summary>
    public double ValueFontSize { get => _valueFontSize; set { ValidatePositive(value, nameof(value)); _valueFontSize = value; } }
    /// <summary>Gets or sets the gap between label and value columns.</summary>
    public double ColumnGap { get => _columnGap; set { ValidateNonNegative(value, nameof(value)); _columnGap = value; } }
    /// <summary>Gets or sets the gap between rows.</summary>
    public double RowGap { get => _rowGap; set { ValidateNonNegative(value, nameof(value)); _rowGap = value; } }
    /// <summary>Gets or sets an explicit label column width. When empty, the widest label is measured.</summary>
    public double? LabelWidth {
        get => _labelWidth;
        set {
            if (value.HasValue) ValidatePositive(value.Value, nameof(value));
            _labelWidth = value;
        }
    }
    /// <summary>Gets or sets an explicit value wrap width. When empty, the remaining block width is used.</summary>
    public double? ValueWrapWidth {
        get => _valueWrapWidth;
        set {
            if (value.HasValue) ValidatePositive(value.Value, nameof(value));
            _valueWrapWidth = value;
        }
    }
    /// <summary>Gets or sets an optional default label color override.</summary>
    public ChartColor? LabelColorOverride { get; set; }
    /// <summary>Gets or sets an optional default value color override.</summary>
    public ChartColor? ValueColorOverride { get; set; }
    /// <summary>Gets or sets the SVG font family name. PNG output uses the dependency-free built-in font path.</summary>
    public string FontFamilyName { get => _fontFamilyName; set => _fontFamilyName = value ?? throw new ArgumentNullException(nameof(value)); }
    /// <summary>Gets or sets whether labels use the emphasized text treatment.</summary>
    public bool LabelEmphasized { get; set; } = true;
    /// <summary>Gets or sets whether values use the emphasized text treatment.</summary>
    public bool ValueEmphasized { get; set; }

    /// <summary>Measures the block height using the current text and wrapping settings.</summary>
    public double MeasureHeight() => VisualCanvasKeyValueBlockLayout.Build(this).Height;
}

internal sealed class VisualCanvasKeyValueBlockLayout {
    private VisualCanvasKeyValueBlockLayout(IReadOnlyList<VisualCanvasKeyValueRowLayout> rows, double labelWidth, double valueWidth, double height) {
        Rows = rows;
        LabelWidth = labelWidth;
        ValueWidth = valueWidth;
        Height = height;
    }

    public IReadOnlyList<VisualCanvasKeyValueRowLayout> Rows { get; }
    public double LabelWidth { get; }
    public double ValueWidth { get; }
    public double Height { get; }

    public static VisualCanvasKeyValueBlockLayout Build(VisualCanvasKeyValueBlockLayer block) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        var hasPairs = false;
        var labelWidth = block.LabelWidth ?? 0;
        if (!block.LabelWidth.HasValue) {
            foreach (var item in block.Items) {
                if (item.LabelOnly) continue;
                hasPairs = true;
                labelWidth = Math.Max(labelWidth, Measure(item.Label, block.LabelFontSize, block.LabelEmphasized));
            }
        } else {
            foreach (var item in block.Items) {
                if (!item.LabelOnly) {
                    hasPairs = true;
                    break;
                }
            }
        }

        if (!hasPairs) labelWidth = 0;
        var labelLimit = hasPairs ? Math.Max(1, block.Width - Math.Min(block.ColumnGap, Math.Max(0, block.Width - 1)) - 1) : Math.Max(0, block.Width);
        labelWidth = Math.Min(labelWidth, labelLimit);
        var columnGap = hasPairs ? Math.Min(block.ColumnGap, Math.Max(0, block.Width - labelWidth - 1)) : 0;
        var remainingWidth = Math.Max(1, block.Width - labelWidth - columnGap);
        var valueWidth = Math.Min(block.ValueWrapWidth ?? remainingWidth, remainingWidth);
        var labelLineHeight = Math.Max(1, RgbaCanvas.MeasureTextHeight(block.LabelFontSize, null));
        var valueLineHeight = Math.Max(1, RgbaCanvas.MeasureTextHeight(block.ValueFontSize, null));
        var rows = new List<VisualCanvasKeyValueRowLayout>(block.Items.Count);
        var y = block.Y;

        foreach (var item in block.Items) {
            var labelOnly = item.LabelOnly || !hasPairs;
            var rowLabelWidth = labelOnly ? block.Width : labelWidth;
            var rowValueWidth = labelOnly ? 0 : valueWidth;
            var labelText = FitText(item.Label, block.LabelFontSize, Math.Max(1, rowLabelWidth), block.LabelEmphasized);
            IReadOnlyList<string> valueLines = Array.Empty<string>();
            if (!labelOnly) valueLines = WrapText(item.Value, Math.Max(1, rowValueWidth), block.ValueFontSize, block.ValueEmphasized);
            var lineCount = Math.Max(1, valueLines.Count);
            var rowHeight = labelOnly ? labelLineHeight : Math.Max(labelLineHeight, lineCount * valueLineHeight);
            rows.Add(new VisualCanvasKeyValueRowLayout(item, block.X, y, block.X, block.X + labelWidth + columnGap, rowLabelWidth, rowValueWidth, labelText, valueLines, rowHeight, labelLineHeight, valueLineHeight, labelOnly));
            y += rowHeight + block.RowGap;
        }

        var height = rows.Count == 0 ? 1 : Math.Max(1, y - block.Y - block.RowGap);
        return new VisualCanvasKeyValueBlockLayout(rows, labelWidth, valueWidth, height);
    }

    private static IReadOnlyList<string> WrapText(string value, double maxWidth, double fontSize, bool emphasized) {
        var normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = new List<string>();
        var paragraphs = normalized.Split('\n');
        foreach (var paragraph in paragraphs) {
            if (paragraph.Length == 0) {
                lines.Add(string.Empty);
                continue;
            }

            var current = string.Empty;
            foreach (var word in paragraph.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (Measure(candidate, fontSize, emphasized) <= maxWidth) {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0) lines.Add(current);
                current = Measure(word, fontSize, emphasized) <= maxWidth ? word : FitText(word, fontSize, maxWidth, emphasized);
            }

            lines.Add(current);
        }

        if (lines.Count == 0) lines.Add(string.Empty);
        return lines;
    }

    private static string FitText(string value, double fontSize, double maxWidth, bool emphasized) {
        if (string.IsNullOrEmpty(value) || Measure(value, fontSize, emphasized) <= maxWidth) return value;
        const string suffix = "...";
        if (Measure(suffix, fontSize, emphasized) > maxWidth) return string.Empty;
        var low = 0;
        var high = value.Length;
        while (low < high) {
            var mid = (low + high + 1) / 2;
            if (Measure(value.Substring(0, mid) + suffix, fontSize, emphasized) <= maxWidth) low = mid;
            else high = mid - 1;
        }

        return value.Substring(0, low) + suffix;
    }

    private static double Measure(string value, double fontSize, bool emphasized) =>
        emphasized ? RgbaCanvas.MeasureTextEmphasizedWidth(value, fontSize, null) : RgbaCanvas.MeasureTextWidth(value, fontSize, null);
}

internal sealed class VisualCanvasKeyValueRowLayout {
    public VisualCanvasKeyValueRowLayout(VisualCanvasKeyValueItem item, double x, double y, double labelX, double valueX, double labelWidth, double valueWidth, string labelText, IReadOnlyList<string> valueLines, double rowHeight, double labelLineHeight, double valueLineHeight, bool labelOnly) {
        Item = item;
        X = x;
        Y = y;
        LabelX = labelX;
        ValueX = valueX;
        LabelWidth = labelWidth;
        ValueWidth = valueWidth;
        LabelText = labelText;
        ValueLines = valueLines;
        RowHeight = rowHeight;
        LabelLineHeight = labelLineHeight;
        ValueLineHeight = valueLineHeight;
        LabelOnly = labelOnly;
    }

    public VisualCanvasKeyValueItem Item { get; }
    public double X { get; }
    public double Y { get; }
    public double LabelX { get; }
    public double ValueX { get; }
    public double LabelWidth { get; }
    public double ValueWidth { get; }
    public string LabelText { get; }
    public IReadOnlyList<string> ValueLines { get; }
    public double RowHeight { get; }
    public double LabelLineHeight { get; }
    public double ValueLineHeight { get; }
    public bool LabelOnly { get; }
}
