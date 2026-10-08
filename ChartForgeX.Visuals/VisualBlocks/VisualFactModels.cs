using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Accessibility;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.VisualBlocks;

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
/// A structured, themeable table visual block.
/// </summary>
public sealed class ChartTable : FactualVisualBlock<ChartTable> {
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
    public ChartTable AddColumn(string header, TextAlignment alignment = TextAlignment.Left, double? width = null, string? format = null) {
        if (_rows.Count > 0) throw new InvalidOperationException("Table columns cannot be added after rows have been populated. Use WithColumns to replace the full column set with the same count.");
        AddColumnCore(header, alignment, width, format);
        return this;
    }

    private void AddColumnCore(string header, TextAlignment alignment = TextAlignment.Left, double? width = null, string? format = null) {
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
    public ChartTableColumn(string header, TextAlignment alignment = TextAlignment.Left, double? width = null, string? format = null) {
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
    public TextAlignment Alignment { get; }

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
    private TextAlignment? _alignment;
    private VisualStatus _status;

    /// <summary>Initializes a table cell.</summary>
    public ChartTableCell(string text) => _text = text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>Gets or sets the cell text.</summary>
    public string Text {
        get => _text;
        set => _text = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets an optional text alignment override.</summary>
    public TextAlignment? Alignment {
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
public sealed class ChartList : FactualVisualBlock<ChartList> {
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
public sealed class RadialMetricCard : FactualVisualBlock<RadialMetricCard> {
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
    public override string AccessibleName => Options.Accessibility.Name ?? (Label.Length == 0 ? base.AccessibleName : Label);

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
public sealed class VisualGrid : ChartForgeX.Rendering.IStaticVisualSource {
    private readonly List<VisualGridItem> _items = new();
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private int _columns = 2;
    private int _gap = 16;
    private int _padding = 24;
    private int _pngOutputScale = 1;
    private ChartSize? _panelSize;
    private VisualPanelFit _panelFit = VisualPanelFit.Contain;
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
    public VisualPanelFit PanelFit {
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
    public VisualGrid WithPanelFit(VisualPanelFit fit) { PanelFit = fit; return this; }

    /// <summary>Sets whether rows without a fixed panel size use their natural item heights.</summary>
    public VisualGrid WithAdaptiveRowHeights(bool enabled = true) { AdaptiveRowHeights = enabled; return this; }

    /// <summary>Sets the PNG output pixel multiplier.</summary>
    public VisualGrid WithPngOutputScale(int scale) { PngOutputScale = scale; return this; }

    /// <summary>Sets whether the grid renders a subtle outer frame.</summary>
    public VisualGrid WithFrame(bool visible = true) { FrameVisible = visible; return this; }


    /// <summary>Renders static grid SVG with an embedding identity scope.</summary>
    public string RenderSvg(string idScope) => new SvgVisualGridRenderer().Render(this, idScope);

    /// <summary>Renders the configured static grid pixels.</summary>
    public ChartForgeX.Raster.RgbaImage RenderRgba() => new PngVisualGridRenderer().RenderImage(this);

    /// <summary>Adds a chart panel.</summary>
    public VisualGrid Add(Chart chart, int columnSpan = 1, int rowSpan = 1) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        _items.Add(VisualGridItem.FromChart(chart, columnSpan, rowSpan));
        return this;
    }

    /// <summary>Adds a chart panel with a stable target id.</summary>
    public VisualGrid Add(string targetId, Chart chart, int columnSpan = 1, int rowSpan = 1) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        AddItem(VisualGridItem.FromChart(targetId, chart, columnSpan, rowSpan));
        return this;
    }

    /// <summary>Adds a visual block panel.</summary>
    public VisualGrid Add(IVisualBlock block, int columnSpan = 1, int rowSpan = 1) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        _items.Add(VisualGridItem.FromBlock(block, columnSpan, rowSpan));
        return this;
    }

    /// <summary>Adds a visual block panel with a stable target id.</summary>
    public VisualGrid Add(string targetId, IVisualBlock block, int columnSpan = 1, int rowSpan = 1) {
        if (block == null) throw new ArgumentNullException(nameof(block));
        AddItem(VisualGridItem.FromBlock(targetId, block, columnSpan, rowSpan));
        return this;
    }

    private void AddItem(VisualGridItem item) {
        if (string.Equals(item.TargetId, "title", StringComparison.Ordinal) ||
            string.Equals(item.TargetId, "subtitle", StringComparison.Ordinal)) {
            throw new ArgumentException("Visual grid panel target ids cannot use the reserved title or subtitle targets.", nameof(item));
        }
        foreach (var existing in _items) {
            if (string.Equals(existing.TargetId, item.TargetId, StringComparison.Ordinal)) {
                throw new ArgumentException("Visual grid target ids must be unique.", nameof(item));
            }
        }

        _items.Add(item);
    }
}
