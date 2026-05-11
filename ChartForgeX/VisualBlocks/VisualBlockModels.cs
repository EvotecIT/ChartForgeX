using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// Horizontal text alignment used by visual blocks.
/// </summary>
public enum VisualTextAlignment {
    /// <summary>Align text to the leading edge.</summary>
    Left,
    /// <summary>Center text in the available space.</summary>
    Center,
    /// <summary>Align text to the trailing edge.</summary>
    Right
}

/// <summary>
/// Generic semantic status used by visual blocks.
/// </summary>
public enum VisualStatus {
    /// <summary>No semantic status.</summary>
    None,
    /// <summary>Neutral informational status.</summary>
    Neutral,
    /// <summary>Positive or healthy status.</summary>
    Positive,
    /// <summary>Warning or attention status.</summary>
    Warning,
    /// <summary>Negative or failed status.</summary>
    Negative,
    /// <summary>Informational status.</summary>
    Info
}

/// <summary>
/// Marker style used by chart lists.
/// </summary>
public enum VisualListMarker {
    /// <summary>Do not render a marker.</summary>
    None,
    /// <summary>Render a small bullet marker.</summary>
    Bullet,
    /// <summary>Render one-based item numbers.</summary>
    Number,
    /// <summary>Render check or empty markers.</summary>
    Check,
    /// <summary>Render status-colored markers.</summary>
    Status
}

/// <summary>
/// Fit behavior for visual grid panels.
/// </summary>
public enum VisualGridPanelFit {
    /// <summary>Preserve the child aspect ratio inside the panel.</summary>
    Contain,
    /// <summary>Stretch the child to the full panel dimensions.</summary>
    Stretch
}

/// <summary>
/// Built-in compact icons for metric and radial metric visual blocks.
/// </summary>
public enum VisualIcon {
    /// <summary>Do not render an icon.</summary>
    None,
    /// <summary>Food or intake icon.</summary>
    ForkKnife,
    /// <summary>Heat, burn, or activity icon.</summary>
    Flame,
    /// <summary>Energy, activity, or alert icon.</summary>
    Lightning,
    /// <summary>Water, hydration, or liquid icon.</summary>
    Droplet,
    /// <summary>Walking, running, or movement icon.</summary>
    Runner,
    /// <summary>Cycling or bike activity icon.</summary>
    Bicycle,
    /// <summary>Person, member, owner, or user icon.</summary>
    Person
}

/// <summary>
/// Badge placement used by metric visual blocks.
/// </summary>
public enum MetricCardBadgePlacement {
    /// <summary>Place the badge in the upper-right corner.</summary>
    TopRight,
    /// <summary>Place the badge before the label in the upper-left corner.</summary>
    TopLeft
}

/// <summary>
/// Placement for metric-card mini charts.
/// </summary>
public enum MetricCardMicroVisualPlacement {
    /// <summary>Render the mini chart inline beside the primary metric.</summary>
    Inline,
    /// <summary>Render the mini chart as a larger focus visual below the primary metric.</summary>
    Hero
}

/// <summary>
/// Presentation style for metric-card mini sparklines.
/// </summary>
public enum MetricCardSparklineStyle {
    /// <summary>Render the sparkline as a compact area chart.</summary>
    Area,
    /// <summary>Render the sparkline as a stroked line without area fill.</summary>
    Line
}

/// <summary>
/// Optional surface treatment for metric-card mini visuals.
/// </summary>
public enum MetricCardMicroVisualSurface {
    /// <summary>Render the mini visual directly on the card surface.</summary>
    None,
    /// <summary>Render the mini visual inside an inset plot card.</summary>
    Inset
}

/// <summary>
/// Shared renderer-independent options for visual blocks.
/// </summary>
public sealed class VisualBlockOptions {
    private ChartSize _size = new(520, 300);
    private ChartPadding _padding = new(22, 22, 22, 22);
    private ChartTheme _theme = ChartTheme.Light();
    private int _pngOutputScale = 1;

    /// <summary>Gets or sets the rendered block size in pixels.</summary>
    public ChartSize Size {
        get => _size;
        set {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Visual block size must have positive dimensions.");
            _size = value;
        }
    }

    /// <summary>Gets or sets the inner content padding.</summary>
    public ChartPadding Padding {
        get => _padding;
        set {
            ValidateNonNegative(value.Left, nameof(value));
            ValidateNonNegative(value.Top, nameof(value));
            ValidateNonNegative(value.Right, nameof(value));
            ValidateNonNegative(value.Bottom, nameof(value));
            _padding = value;
        }
    }

    /// <summary>Gets or sets the visual theme used by renderers.</summary>
    public ChartTheme Theme {
        get => _theme;
        set => _theme = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets whether the full background should stay transparent.</summary>
    public bool TransparentBackground { get; set; }

    /// <summary>Gets or sets whether the outer card surface should be rendered.</summary>
    public bool ShowCard { get; set; } = true;

    /// <summary>Gets or sets the output pixel multiplier used by PNG exports.</summary>
    public int PngOutputScale {
        get => _pngOutputScale;
        set {
            if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four.");
            _pngOutputScale = value;
        }
    }

    private static void ValidateNonNegative(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) throw new ArgumentOutOfRangeException(parameterName, value, "Padding values must be finite and non-negative.");
    }
}

/// <summary>
/// Common contract for renderable non-chart visual blocks.
/// </summary>
public interface IVisualBlock {
    /// <summary>Gets the block title.</summary>
    string Title { get; }

    /// <summary>Gets the block subtitle.</summary>
    string Subtitle { get; }

    /// <summary>Gets shared rendering options.</summary>
    VisualBlockOptions Options { get; }

    /// <summary>Gets a concise accessibility label.</summary>
    string AccessibleName { get; }
}

/// <summary>
/// Base class for fluent visual block models.
/// </summary>
/// <typeparam name="TSelf">The concrete visual block type.</typeparam>
public abstract class VisualBlock<TSelf> : IVisualBlock where TSelf : VisualBlock<TSelf> {
    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    /// <summary>Gets or sets the block title.</summary>
    public string Title {
        get => _title;
        set => _title = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the block subtitle.</summary>
    public string Subtitle {
        get => _subtitle;
        set => _subtitle = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets shared rendering options.</summary>
    public VisualBlockOptions Options { get; } = new();

    /// <summary>Gets a concise accessibility label.</summary>
    public virtual string AccessibleName => Title.Length == 0 ? GetType().Name : Title;

    /// <summary>Sets the block title.</summary>
    public TSelf WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return Self(); }

    /// <summary>Sets the block subtitle.</summary>
    public TSelf WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return Self(); }

    /// <summary>Sets the rendered block size.</summary>
    public TSelf WithSize(int width, int height) { Options.Size = new ChartSize(width, height); return Self(); }

    /// <summary>Sets uniform inner content padding.</summary>
    public TSelf WithPadding(double padding) { Options.Padding = new ChartPadding(padding, padding, padding, padding); return Self(); }

    /// <summary>Sets the inner content padding.</summary>
    public TSelf WithPadding(double left, double top, double right, double bottom) { Options.Padding = new ChartPadding(left, top, right, bottom); return Self(); }

    /// <summary>Sets the visual theme.</summary>
    public TSelf WithTheme(ChartTheme theme) { Options.Theme = theme ?? throw new ArgumentNullException(nameof(theme)); return Self(); }

    /// <summary>Sets whether the full background should stay transparent.</summary>
    public TSelf WithTransparentBackground(bool enabled = true) { Options.TransparentBackground = enabled; return Self(); }

    /// <summary>Sets whether the outer card surface should be rendered.</summary>
    public TSelf WithCard(bool enabled = true) { Options.ShowCard = enabled; return Self(); }

    /// <summary>Sets the output pixel multiplier used by PNG exports.</summary>
    public TSelf WithPngOutputScale(int scale) { Options.PngOutputScale = scale; return Self(); }

    private TSelf Self() => (TSelf)this;
}

/// <summary>
/// A structured, themeable table visual block.
/// </summary>
public sealed class ChartTable : VisualBlock<ChartTable> {
    private readonly List<ChartTableColumn> _columns = new();
    private readonly List<ChartTableRow> _rows = new();
    private int? _statusColumnIndex;
    private bool _rowStriping = true;
    private bool _showHeader = true;

    /// <summary>Gets table columns.</summary>
    public IReadOnlyList<ChartTableColumn> Columns => _columns;

    /// <summary>Gets table rows.</summary>
    public IReadOnlyList<ChartTableRow> Rows => _rows;

    /// <summary>Gets or sets whether alternating row backgrounds are rendered.</summary>
    public bool RowStriping { get => _rowStriping; set => _rowStriping = value; }

    /// <summary>Gets or sets whether the header row is rendered.</summary>
    public bool ShowHeader { get => _showHeader; set => _showHeader = value; }

    /// <summary>Gets or sets whether compact row sizing is used.</summary>
    public bool Dense { get; set; }

    /// <summary>Gets the optional status column index.</summary>
    public int? StatusColumnIndex => _statusColumnIndex;

    /// <summary>Creates a new chart table.</summary>
    public static ChartTable Create() => new();

    /// <summary>Adds columns from header text.</summary>
    public ChartTable WithColumns(params string[] headers) {
        if (headers == null) throw new ArgumentNullException(nameof(headers));
        if (headers.Length == 0) throw new ArgumentException("Table must contain at least one column.", nameof(headers));
        if (_rows.Count > 0 && _rows[0].Cells.Count != headers.Length) throw new InvalidOperationException("Existing table rows do not match the new column count.");
        _columns.Clear();
        foreach (var header in headers) AddColumnCore(header);
        if (_statusColumnIndex.HasValue && _statusColumnIndex.Value >= _columns.Count) _statusColumnIndex = null;
        return this;
    }

    /// <summary>Adds one table column.</summary>
    public ChartTable AddColumn(string header, VisualTextAlignment alignment = VisualTextAlignment.Left, double? width = null, string? format = null) {
        if (_rows.Count > 0) throw new InvalidOperationException("Table columns cannot be added after rows have been populated. Use WithColumns to replace the full column set with the same count.");
        AddColumnCore(header, alignment, width, format);
        return this;
    }

    private void AddColumnCore(string header, VisualTextAlignment alignment = VisualTextAlignment.Left, double? width = null, string? format = null) {
        if (header == null) throw new ArgumentNullException(nameof(header));
        if (width.HasValue && (double.IsNaN(width.Value) || double.IsInfinity(width.Value) || width.Value <= 0)) throw new ArgumentOutOfRangeException(nameof(width), width, "Column width must be finite and greater than zero.");
        _columns.Add(new ChartTableColumn(header, alignment, width, format));
    }

    /// <summary>Adds a table row.</summary>
    public ChartTable AddRow(params object?[] values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (_columns.Count == 0) throw new InvalidOperationException("Define table columns before adding rows.");
        if (values.Length != _columns.Count) throw new ArgumentException("Row value count must match table column count.", nameof(values));
        var row = new ChartTableRow();
        for (var i = 0; i < values.Length; i++) row.Cells.Add(ChartTableCell.FromValue(values[i], _columns[i].Format));
        _rows.Add(row);
        return this;
    }

    /// <summary>Configures one existing row.</summary>
    public ChartTable WithRow(int rowIndex, Action<ChartTableRow> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        if (rowIndex < 0 || rowIndex >= _rows.Count) throw new ArgumentOutOfRangeException(nameof(rowIndex), rowIndex, "Row index must reference an existing table row.");
        configure(_rows[rowIndex]);
        return this;
    }

    /// <summary>Marks a column as a status column by header.</summary>
    public ChartTable WithStatusColumn(string header) {
        if (header == null) throw new ArgumentNullException(nameof(header));
        var index = _columns.FindIndex(column => string.Equals(column.Header, header, StringComparison.Ordinal));
        if (index < 0) index = _columns.FindIndex(column => string.Equals(column.Header, header, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new ArgumentException("Status column header must reference an existing table column.", nameof(header));
        return WithStatusColumn(index);
    }

    /// <summary>Marks a column as a status column by index.</summary>
    public ChartTable WithStatusColumn(int columnIndex) {
        if (columnIndex < 0 || columnIndex >= _columns.Count) throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, "Status column index must reference an existing table column.");
        _statusColumnIndex = columnIndex;
        return this;
    }

    /// <summary>Sets whether alternating row backgrounds are rendered.</summary>
    public ChartTable WithRowStriping(bool enabled = true) { RowStriping = enabled; return this; }

    /// <summary>Sets whether the header row is rendered.</summary>
    public ChartTable WithHeader(bool enabled = true) { ShowHeader = enabled; return this; }

    /// <summary>Sets whether compact row sizing is used.</summary>
    public ChartTable WithDenseMode(bool enabled = true) { Dense = enabled; return this; }
}

/// <summary>
/// Describes a chart table column.
/// </summary>
public sealed class ChartTableColumn {
    /// <summary>Initializes a table column.</summary>
    public ChartTableColumn(string header, VisualTextAlignment alignment = VisualTextAlignment.Left, double? width = null, string? format = null) {
        Header = header ?? throw new ArgumentNullException(nameof(header));
        VisualBlockGuards.EnumDefined(alignment, nameof(alignment));
        if (width.HasValue) VisualBlockGuards.PositiveFinite(width.Value, nameof(width));
        Alignment = alignment;
        Width = width;
        Format = format;
    }

    /// <summary>Gets the column header.</summary>
    public string Header { get; }

    /// <summary>Gets the default text alignment.</summary>
    public VisualTextAlignment Alignment { get; }

    /// <summary>Gets an optional fixed width in pixels.</summary>
    public double? Width { get; }

    /// <summary>Gets an optional numeric or formattable value format.</summary>
    public string? Format { get; }
}

/// <summary>
/// Describes one chart table row.
/// </summary>
public sealed class ChartTableRow {
    /// <summary>Gets row cells.</summary>
    public List<ChartTableCell> Cells { get; } = new();

    /// <summary>Gets or sets an optional row background.</summary>
    public ChartColor? Background { get; set; }

    /// <summary>Gets or sets an optional row foreground.</summary>
    public ChartColor? Foreground { get; set; }
}

/// <summary>
/// Describes one chart table cell.
/// </summary>
public sealed partial class ChartTableCell {
    private string _text;
    private VisualTextAlignment? _alignment;
    private VisualStatus _status;

    /// <summary>Initializes a table cell.</summary>
    public ChartTableCell(string text) => _text = text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>Gets or sets the cell text.</summary>
    public string Text {
        get => _text;
        set => _text = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets an optional text alignment override.</summary>
    public VisualTextAlignment? Alignment {
        get => _alignment;
        set {
            if (value.HasValue) VisualBlockGuards.EnumDefined(value.Value, nameof(value));
            _alignment = value;
        }
    }

    /// <summary>Gets or sets an optional foreground color override.</summary>
    public ChartColor? Foreground { get; set; }

    /// <summary>Gets or sets an optional background color override.</summary>
    public ChartColor? Background { get; set; }

    /// <summary>Gets or sets an optional explicit status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Creates a cell from any value.</summary>
    public static ChartTableCell FromValue(object? value, string? format = null) {
        if (value == null) return new ChartTableCell(string.Empty);
        if (!string.IsNullOrWhiteSpace(format) && value is IFormattable formattable) return new ChartTableCell(formattable.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty);
        return new ChartTableCell(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }
}

/// <summary>
/// A themeable list visual block.
/// </summary>
public sealed class ChartList : VisualBlock<ChartList> {
    private readonly List<ChartListItem> _items = new();
    private VisualListMarker _marker = VisualListMarker.Bullet;

    /// <summary>Gets list items.</summary>
    public IReadOnlyList<ChartListItem> Items => _items;

    /// <summary>Gets or sets the marker style.</summary>
    public VisualListMarker Marker {
        get => _marker;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _marker = value;
        }
    }

    /// <summary>Gets or sets whether compact row sizing is used.</summary>
    public bool Dense { get; set; }

    /// <summary>Creates a new chart list.</summary>
    public static ChartList Create() => new();

    /// <summary>Adds a list item.</summary>
    public ChartList AddItem(string text, string? value = null) {
        _items.Add(new ChartListItem(text, value));
        return this;
    }

    /// <summary>Adds a status-colored list item.</summary>
    public ChartList AddStatusItem(string text, VisualStatus status, string? value = null) {
        _items.Add(new ChartListItem(text, value) { Status = status });
        return this;
    }

    /// <summary>Adds a checklist item.</summary>
    public ChartList AddCheckItem(string text, bool isChecked, string? value = null) {
        _items.Add(new ChartListItem(text, value) { IsChecked = isChecked });
        Marker = VisualListMarker.Check;
        return this;
    }

    /// <summary>Sets the marker style.</summary>
    public ChartList WithMarker(VisualListMarker marker) {
        Marker = marker;
        return this;
    }

    /// <summary>Sets whether compact row sizing is used.</summary>
    public ChartList WithDenseMode(bool enabled = true) { Dense = enabled; return this; }
}

/// <summary>
/// Describes a chart list item.
/// </summary>
public sealed class ChartListItem {
    private string _text;
    private VisualStatus _status;

    /// <summary>Initializes a list item.</summary>
    public ChartListItem(string text, string? value = null) {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        Value = value;
    }

    /// <summary>Gets or sets the item text.</summary>
    public string Text {
        get => _text;
        set => _text = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets an optional trailing value.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets an optional status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets an optional checklist state.</summary>
    public bool? IsChecked { get; set; }
}

/// <summary>
/// A KPI card with one or more radial progress layers around a central metric.
/// </summary>
public sealed class RadialMetricCard : VisualBlock<RadialMetricCard> {
    private readonly List<ChartRadialLayer> _layers = new();
    private string _label = string.Empty;
    private string _value = string.Empty;
    private VisualIcon _icon;

    /// <summary>Gets radial progress layers.</summary>
    public IReadOnlyList<ChartRadialLayer> Layers => _layers;

    /// <summary>Gets or sets the center label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the center value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets an optional built-in icon rendered above the center metric.</summary>
    public VisualIcon Icon {
        get => _icon;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _icon = value;
        }
    }

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Label.Length == 0 ? base.AccessibleName : Label;

    /// <summary>Creates a new radial metric card.</summary>
    public static RadialMetricCard Create() => new();

    /// <summary>Sets the primary center metric label and value.</summary>
    public RadialMetricCard WithMetric(string label, object? value, string? format = null) {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Value = ChartTableCell.FromValue(value, format).Text;
        return this;
    }

    /// <summary>Sets an optional built-in icon rendered above the center metric.</summary>
    public RadialMetricCard WithIcon(VisualIcon icon) { Icon = icon; return this; }

    /// <summary>Replaces all radial layers.</summary>
    public RadialMetricCard WithLayers(IEnumerable<ChartRadialLayer> layers) {
        if (layers == null) throw new ArgumentNullException(nameof(layers));
        _layers.Clear();
        foreach (var layer in layers) AddLayer(layer);
        return this;
    }

    /// <summary>Replaces all radial layers using a fluent layer collection builder.</summary>
    public RadialMetricCard WithLayers(Func<ChartRadialLayers, ChartRadialLayers> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        var layers = configure(ChartRadialLayers.Create()) ?? throw new InvalidOperationException("Radial metric layer configuration cannot return null.");
        return WithLayers(layers);
    }

    /// <summary>Adds one radial layer.</summary>
    public RadialMetricCard AddLayer(ChartRadialLayer layer) {
        _layers.Add(layer ?? throw new ArgumentNullException(nameof(layer)));
        return this;
    }

    /// <summary>Adds one radial layer and optionally configures its geometry and styling.</summary>
    public RadialMetricCard AddLayer(string name, double value, double minimum = 0, double maximum = 100, ChartColor? color = null, Func<ChartRadialLayer, ChartRadialLayer>? configure = null) {
        var layer = ChartRadialLayer.Create(name, value, minimum, maximum, color);
        if (configure != null) layer = configure(layer) ?? throw new InvalidOperationException("Radial metric layer configuration cannot return null.");
        return AddLayer(layer);
    }
}

/// <summary>
/// A neutral composition surface for charts and visual blocks.
/// </summary>
public sealed class VisualGrid {
    private readonly List<VisualGridItem> _items = new();
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private int _columns = 2;
    private int _gap = 18;
    private int _padding = 24;
    private int _pngOutputScale = 1;
    private ChartSize? _panelSize;
    private VisualGridPanelFit _panelFit = VisualGridPanelFit.Contain;
    private bool _adaptiveRowHeights;
    private bool _frameVisible;

    /// <summary>Gets or sets the grid title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the grid subtitle.</summary>
    public string Subtitle { get => _subtitle; set => _subtitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the preferred column count.</summary>
    public int Columns { get => _columns; set { if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Visual grid columns must be positive."); _columns = value; } }

    /// <summary>Gets or sets the gap between panels in pixels.</summary>
    public int Gap { get => _gap; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Visual grid gap must be non-negative."); _gap = value; } }

    /// <summary>Gets or sets the outer padding in pixels.</summary>
    public int Padding { get => _padding; set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Visual grid padding must be non-negative."); _padding = value; } }

    /// <summary>Gets or sets the PNG output pixel multiplier.</summary>
    public int PngOutputScale { get => _pngOutputScale; set { if (value < 1 || value > 4) throw new ArgumentOutOfRangeException(nameof(value), value, "PNG output scale must be between one and four."); _pngOutputScale = value; } }

    /// <summary>Gets or sets a value indicating whether the grid should render a subtle outer frame.</summary>
    public bool FrameVisible { get => _frameVisible; set => _frameVisible = value; }

    /// <summary>Gets or sets the optional fixed panel size.</summary>
    public ChartSize? PanelSize {
        get => _panelSize;
        set {
            if (value.HasValue && (value.Value.Width <= 0 || value.Value.Height <= 0)) throw new ArgumentOutOfRangeException(nameof(value), "Visual grid panel size must have positive dimensions.");
            _panelSize = value;
        }
    }

    /// <summary>Gets or sets how children fit fixed panels.</summary>
    public VisualGridPanelFit PanelFit {
        get => _panelFit;
        set {
            VisualBlockGuards.EnumDefined(value, nameof(value));
            _panelFit = value;
        }
    }

    /// <summary>Gets or sets whether rows without a fixed panel size use their natural item heights.</summary>
    public bool AdaptiveRowHeights { get => _adaptiveRowHeights; set => _adaptiveRowHeights = value; }

    /// <summary>Gets or sets the optional grid theme.</summary>
    public ChartTheme? Theme { get; set; }

    /// <summary>Gets grid items.</summary>
    public IReadOnlyList<VisualGridItem> Items => _items;

    /// <summary>Creates a new visual grid.</summary>
    public static VisualGrid Create() => new();

    /// <summary>Creates a metric-card strip using the standard compact section layout.</summary>
    public static VisualGrid CreateMetricStrip(string title, IEnumerable<MetricCard> cards, int columns = 4, int panelWidth = 320, int panelHeight = 176) {
        if (title == null) throw new ArgumentNullException(nameof(title));
        if (cards == null) throw new ArgumentNullException(nameof(cards));
        var grid = Create()
            .WithTitle(title)
            .WithColumns(columns)
            .WithPanelSize(panelWidth, panelHeight)
            .WithGap(16)
            .WithPadding(24);
        var count = 0;
        foreach (var card in cards) {
            if (card == null) throw new ArgumentException("Metric strips cannot contain null cards.", nameof(cards));
            grid.Add(card);
            count++;
        }

        if (count == 0) throw new ArgumentException("Metric strips require at least one metric card.", nameof(cards));
        return grid;
    }

    /// <summary>Sets the grid title.</summary>
    public VisualGrid WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>Sets the grid subtitle.</summary>
    public VisualGrid WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return this; }

    /// <summary>Sets the preferred column count.</summary>
    public VisualGrid WithColumns(int columns) { Columns = columns; return this; }

    /// <summary>Sets the gap between panels.</summary>
    public VisualGrid WithGap(int gap) { Gap = gap; return this; }

    /// <summary>Sets the outer padding.</summary>
    public VisualGrid WithPadding(int padding) { Padding = padding; return this; }

    /// <summary>Sets the grid theme.</summary>
    public VisualGrid WithTheme(ChartTheme theme) { Theme = theme ?? throw new ArgumentNullException(nameof(theme)); return this; }

    /// <summary>Sets a fixed panel size.</summary>
    public VisualGrid WithPanelSize(int width, int height) { PanelSize = new ChartSize(width, height); return this; }

    /// <summary>Sets how children fit fixed panels.</summary>
    public VisualGrid WithPanelFit(VisualGridPanelFit fit) { PanelFit = fit; return this; }

    /// <summary>Sets whether rows without a fixed panel size use their natural item heights.</summary>
    public VisualGrid WithAdaptiveRowHeights(bool enabled = true) { AdaptiveRowHeights = enabled; return this; }

    /// <summary>Sets the PNG output pixel multiplier.</summary>
    public VisualGrid WithPngOutputScale(int scale) { PngOutputScale = scale; return this; }

    /// <summary>Sets whether the grid renders a subtle outer frame.</summary>
    public VisualGrid WithFrame(bool visible = true) { FrameVisible = visible; return this; }

    /// <summary>Adds a chart panel.</summary>
    public VisualGrid Add(Chart chart, int columnSpan = 1, int rowSpan = 1) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        _items.Add(VisualGridItem.FromChart(chart, columnSpan, rowSpan));
        return this;
    }

    /// <summary>Adds a visual block panel.</summary>
    public VisualGrid Add(IVisualBlock block, int columnSpan = 1, int rowSpan = 1) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        _items.Add(VisualGridItem.FromBlock(block, columnSpan, rowSpan));
        return this;
    }
}

/// <summary>
/// Describes one visual grid panel.
/// </summary>
public sealed class VisualGridItem {
    private VisualGridItem(Chart? chart, IVisualBlock? block, int columnSpan, int rowSpan) {
        if (columnSpan <= 0) throw new ArgumentOutOfRangeException(nameof(columnSpan), columnSpan, "Column span must be positive.");
        if (rowSpan <= 0) throw new ArgumentOutOfRangeException(nameof(rowSpan), rowSpan, "Row span must be positive.");
        Chart = chart;
        Block = block;
        ColumnSpan = columnSpan;
        RowSpan = rowSpan;
    }

    /// <summary>Gets the chart when this item hosts a chart.</summary>
    public Chart? Chart { get; }

    /// <summary>Gets the visual block when this item hosts a block.</summary>
    public IVisualBlock? Block { get; }

    /// <summary>Gets the column span.</summary>
    public int ColumnSpan { get; }

    /// <summary>Gets the row span.</summary>
    public int RowSpan { get; }

    /// <summary>Creates a chart grid item.</summary>
    public static VisualGridItem FromChart(Chart chart, int columnSpan = 1, int rowSpan = 1) => new(chart ?? throw new ArgumentNullException(nameof(chart)), null, columnSpan, rowSpan);

    /// <summary>Creates a visual block grid item.</summary>
    public static VisualGridItem FromBlock(IVisualBlock block, int columnSpan = 1, int rowSpan = 1) => new(null, block ?? throw new ArgumentNullException(nameof(block)), columnSpan, rowSpan);
}

internal static class VisualBlockGuards {
    public static void EnumDefined<TEnum>(TEnum value, string parameterName) where TEnum : struct {
        if (!Enum.IsDefined(typeof(TEnum), value)) throw new ArgumentOutOfRangeException(parameterName, value, "Unknown " + typeof(TEnum).Name + " value.");
    }

    public static void PositiveFinite(double value, string parameterName) {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite and greater than zero.");
    }
}
