using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Declares host-level table behaviors supported by a table artifact or data provider.
/// </summary>
[Flags]
public enum TableArtifactCapabilities {
    /// <summary>No interactive table behavior is declared.</summary>
    None = 0,
    /// <summary>The table can be searched.</summary>
    Search = 1,
    /// <summary>The table can be sorted.</summary>
    Sort = 2,
    /// <summary>The table can be filtered.</summary>
    Filter = 4,
    /// <summary>The table supports single-row selection.</summary>
    SingleSelection = 8,
    /// <summary>The table supports multi-row selection.</summary>
    MultiSelection = 16,
    /// <summary>The table supports cell-level selection.</summary>
    CellSelection = 32,
    /// <summary>The table supports copying cells or rows.</summary>
    Copy = 64,
    /// <summary>The table supports host-driven export commands.</summary>
    Export = 128,
    /// <summary>The table supports virtualized data windows.</summary>
    Virtualization = 256
}

/// <summary>
/// Describes the logical data type of a table artifact column.
/// </summary>
public enum TableArtifactColumnType {
    /// <summary>Plain text values.</summary>
    Text,
    /// <summary>Numeric values.</summary>
    Number,
    /// <summary>Boolean values.</summary>
    Boolean,
    /// <summary>Date values.</summary>
    Date,
    /// <summary>Date and time values.</summary>
    DateTime,
    /// <summary>Time values.</summary>
    Time,
    /// <summary>Status or severity values.</summary>
    Status,
    /// <summary>URI values.</summary>
    Uri
}

/// <summary>
/// Describes one reusable table artifact for static rendering and native-host interaction.
/// </summary>
public sealed class TableArtifact {
    private string _id = string.Empty;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private readonly List<TableArtifactColumn> _columns = new();
    private readonly List<TableArtifactRow> _rows = new();
    private TableArtifactCapabilities _capabilities;
    private VisualArtifactExportFormat _exportFormats = VisualArtifactExportFormat.Svg | VisualArtifactExportFormat.Png | VisualArtifactExportFormat.Csv | VisualArtifactExportFormat.Json | VisualArtifactExportFormat.Office;

    /// <summary>Gets or sets a stable table identifier.</summary>
    public string Id { get => _id; set => _id = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the table title.</summary>
    public string Title { get => _title; set => _title = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the table subtitle.</summary>
    public string Subtitle { get => _subtitle; set => _subtitle = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets table columns.</summary>
    public IReadOnlyList<TableArtifactColumn> Columns => _columns;

    /// <summary>Gets in-memory table rows used by static previews and small tables.</summary>
    public IReadOnlyList<TableArtifactRow> Rows => _rows;

    /// <summary>Gets or sets host-level capabilities declared by this artifact.</summary>
    public TableArtifactCapabilities Capabilities {
        get => _capabilities;
        set {
            TableArtifactGuards.TableCapabilitiesDefined(value, nameof(value));
            _capabilities = value;
        }
    }

    /// <summary>Gets or sets static export formats declared by this artifact.</summary>
    public VisualArtifactExportFormat ExportFormats {
        get => _exportFormats;
        set {
            VisualArtifactGuards.ExportFormatsDefined(value, nameof(value));
            _exportFormats = value;
        }
    }

    /// <summary>Gets or sets the total row count when the artifact is backed by virtualized data.</summary>
    public long? TotalRowCount { get; set; }

    /// <summary>Gets table metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

    /// <summary>Creates a new table artifact.</summary>
    public static TableArtifact Create(string id) => new() { Id = id };

    /// <summary>Sets the table title.</summary>
    public TableArtifact WithTitle(string title) { Title = title ?? throw new ArgumentNullException(nameof(title)); return this; }

    /// <summary>Sets the table subtitle.</summary>
    public TableArtifact WithSubtitle(string subtitle) { Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle)); return this; }

    /// <summary>Sets declared host-level table capabilities.</summary>
    public TableArtifact WithCapabilities(TableArtifactCapabilities capabilities) {
        TableArtifactGuards.TableCapabilitiesDefined(capabilities, nameof(capabilities));
        Capabilities = capabilities;
        return this;
    }

    /// <summary>Adds a table column.</summary>
    public TableArtifact AddColumn(string id, string label, TableArtifactColumnType type = TableArtifactColumnType.Text, VisualTextAlignment alignment = VisualTextAlignment.Left, double? width = null) {
        if (_rows.Count > 0) throw new InvalidOperationException("Table artifact columns cannot be added after rows have been populated.");
        if (ContainsColumn(id)) throw new ArgumentException("Table artifact column ids must be unique.", nameof(id));
        _columns.Add(new TableArtifactColumn(id, label, type, alignment, width));
        return this;
    }

    /// <summary>Adds an in-memory row used by static previews and small table hosts.</summary>
    public TableArtifact AddRow(string key, params object?[] values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (_columns.Count == 0) throw new InvalidOperationException("Define table artifact columns before adding rows.");
        if (values.Length != _columns.Count) throw new ArgumentException("Row value count must match table artifact column count.", nameof(values));
        var row = new TableArtifactRow(key);
        for (var i = 0; i < values.Length; i++) row.Cells.Add(TableArtifactCell.FromValue(values[i]));
        _rows.Add(row);
        return this;
    }

    /// <summary>Configures one existing row.</summary>
    public TableArtifact WithRow(int rowIndex, Action<TableArtifactRow> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        if (rowIndex < 0 || rowIndex >= _rows.Count) throw new ArgumentOutOfRangeException(nameof(rowIndex), rowIndex, "Row index must reference an existing table artifact row.");
        configure(_rows[rowIndex]);
        return this;
    }

    /// <summary>Returns true when the table declares the requested capability.</summary>
    public bool Supports(TableArtifactCapabilities capability) => capability != TableArtifactCapabilities.None && (Capabilities & capability) == capability;

    /// <summary>Returns true when the table declares the requested static export format.</summary>
    public bool SupportsExport(VisualArtifactExportFormat format) => format != VisualArtifactExportFormat.None && (ExportFormats & format) == format;

    private bool ContainsColumn(string id) {
        if (id == null) throw new ArgumentNullException(nameof(id));
        for (var i = 0; i < _columns.Count; i++) {
            if (string.Equals(_columns[i].Id, id, StringComparison.Ordinal)) return true;
        }

        return false;
    }
}

/// <summary>
/// Describes one table artifact column.
/// </summary>
public sealed class TableArtifactColumn {
    private string _id;
    private string _label;
    private TableArtifactColumnType _type;
    private VisualTextAlignment _alignment;
    private double? _width;

    /// <summary>Initializes a table artifact column.</summary>
    public TableArtifactColumn(string id, string label, TableArtifactColumnType type = TableArtifactColumnType.Text, VisualTextAlignment alignment = VisualTextAlignment.Left, double? width = null) {
        _id = RequireToken(id, nameof(id));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        TableArtifactGuards.EnumDefined(type, nameof(type));
        TableArtifactGuards.EnumDefined(alignment, nameof(alignment));
        if (width.HasValue) TableArtifactGuards.PositiveFinite(width.Value, nameof(width));
        _type = type;
        _alignment = alignment;
        _width = width;
    }

    /// <summary>Gets or sets the stable column identifier.</summary>
    public string Id { get => _id; set => _id = RequireToken(value, nameof(value)); }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the logical column type.</summary>
    public TableArtifactColumnType Type {
        get => _type;
        set {
            TableArtifactGuards.EnumDefined(value, nameof(value));
            _type = value;
        }
    }

    /// <summary>Gets or sets the static preview text alignment.</summary>
    public VisualTextAlignment Alignment {
        get => _alignment;
        set {
            TableArtifactGuards.EnumDefined(value, nameof(value));
            _alignment = value;
        }
    }

    /// <summary>Gets or sets an optional width hint in pixels.</summary>
    public double? Width {
        get => _width;
        set {
            if (value.HasValue) TableArtifactGuards.PositiveFinite(value.Value, nameof(value));
            _width = value;
        }
    }

    /// <summary>Gets or sets whether this column participates in host search.</summary>
    public bool Searchable { get; set; } = true;

    /// <summary>Gets or sets whether this column participates in host sorting.</summary>
    public bool Sortable { get; set; } = true;

    /// <summary>Gets or sets whether this column participates in host filtering.</summary>
    public bool Filterable { get; set; } = true;

    /// <summary>Gets or sets whether this column participates in copy operations.</summary>
    public bool Copyable { get; set; } = true;

    /// <summary>Gets or sets whether this column participates in export operations.</summary>
    public bool Exportable { get; set; } = true;

    /// <summary>Gets column metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

    private static string RequireToken(string value, string parameterName) {
        if (value == null) throw new ArgumentNullException(parameterName);
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Table artifact identifiers must not be empty.", parameterName);
        return value;
    }
}

/// <summary>
/// Describes one table artifact row.
/// </summary>
public sealed class TableArtifactRow {
    private string _key;
    private VisualStatus _status;

    /// <summary>Initializes a table artifact row.</summary>
    public TableArtifactRow(string key) => _key = key ?? throw new ArgumentNullException(nameof(key));

    /// <summary>Gets or sets the stable row key.</summary>
    public string Key { get => _key; set => _key = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets row cells.</summary>
    public List<TableArtifactCell> Cells { get; } = new();

    /// <summary>Gets or sets the row status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            TableArtifactGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets whether this row can be selected by a host.</summary>
    public bool Selectable { get; set; } = true;

    /// <summary>Gets row metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Describes one table artifact cell.
/// </summary>
public sealed class TableArtifactCell {
    private string _displayText;
    private VisualStatus _status;

    /// <summary>Initializes a table artifact cell.</summary>
    public TableArtifactCell(string displayText) => _displayText = displayText ?? throw new ArgumentNullException(nameof(displayText));

    /// <summary>Gets or sets the text used by static previews.</summary>
    public string DisplayText { get => _displayText; set => _displayText = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets optional text used by host search.</summary>
    public string? SearchText { get; set; }

    /// <summary>Gets or sets optional text used by host sorting.</summary>
    public string? SortText { get; set; }

    /// <summary>Gets or sets optional text used by copy commands.</summary>
    public string? CopyText { get; set; }

    /// <summary>Gets or sets optional text used by export commands.</summary>
    public string? ExportText { get; set; }

    /// <summary>Gets or sets the cell status.</summary>
    public VisualStatus Status {
        get => _status;
        set {
            TableArtifactGuards.EnumDefined(value, nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets cell metadata for host adapters and exporters.</summary>
    public Dictionary<string, string> Metadata { get; } = new(StringComparer.Ordinal);

    /// <summary>Creates a table artifact cell from any scalar value.</summary>
    public static TableArtifactCell FromValue(object? value) {
        var text = value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return new TableArtifactCell(text) {
            SearchText = text,
            SortText = text,
            CopyText = text,
            ExportText = text
        };
    }
}
