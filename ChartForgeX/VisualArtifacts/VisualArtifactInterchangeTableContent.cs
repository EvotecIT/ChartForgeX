using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>Captures an embedded finite table independently of its graph vertex and host providers.</summary>
public sealed class VisualArtifactInterchangeTableContent {
    /// <summary>Gets or sets the content identity, scoped to its topology node.</summary>
    public string Id { get; set; } = "";
    /// <summary>Gets or sets the complete table heading.</summary>
    public string Title { get; set; } = "";
    /// <summary>Gets or sets the complete table subheading.</summary>
    public string Subtitle { get; set; } = "";
    /// <summary>Gets or sets retained host capabilities; a static scene installs no host controller.</summary>
    public TableArtifactCapabilities Capabilities { get; set; }
    /// <summary>Gets or sets retained export declarations.</summary>
    public VisualArtifactExportFormat ExportFormats { get; set; }
    /// <summary>Gets or sets the total row count; static embedded content must contain every declared row.</summary>
    public long? TotalRowCount { get; set; }
    /// <summary>Gets or sets a captured native presentation scale; ordinary authored tables use one.</summary>
    public double PresentationScale { get; set; } = 1;
    /// <summary>Gets ordered columns with their actual identities.</summary>
    public List<VisualArtifactInterchangeTableColumn> Columns { get; } = new();
    /// <summary>Gets source-ordered keyed rows.</summary>
    public List<VisualArtifactInterchangeTableRow> Rows { get; } = new();
    /// <summary>Gets opaque content metadata, including bounded raw source facts.</summary>
    public Dictionary<string, string> Extensions { get; } = new(System.StringComparer.Ordinal);
}
/// <summary>Captures a typed table column and its static/host policy.</summary>
public sealed class VisualArtifactInterchangeTableColumn {
    /// <summary>Gets or sets the column identity within its table.</summary>
    public string Id { get; set; } = "";
    /// <summary>Gets or sets the display heading.</summary>
    public string Label { get; set; } = "";
    /// <summary>Gets or sets its logical data type.</summary>
    public TableArtifactColumnType Type { get; set; }
    /// <summary>Gets or sets static text alignment.</summary>
    public TextAlignment Alignment { get; set; }
    /// <summary>Gets or sets a positive logical width hint.</summary>
    public double? Width { get; set; }
    /// <summary>Gets or sets host search participation.</summary>
    public bool Searchable { get; set; } = true;
    /// <summary>Gets or sets host sort participation.</summary>
    public bool Sortable { get; set; } = true;
    /// <summary>Gets or sets host filter participation.</summary>
    public bool Filterable { get; set; } = true;
    /// <summary>Gets or sets host copy participation.</summary>
    public bool Copyable { get; set; } = true;
    /// <summary>Gets or sets host export participation.</summary>
    public bool Exportable { get; set; } = true;
    /// <summary>Gets opaque column metadata.</summary>
    public Dictionary<string, string> Extensions { get; } = new(System.StringComparer.Ordinal);
}
/// <summary>Captures one stable source row and its keyed cells.</summary>
public sealed class VisualArtifactInterchangeTableRow {
    /// <summary>Gets or sets the source row identity or JSON pointer.</summary>
    public string Key { get; set; } = "";
    /// <summary>Gets or sets the row status.</summary>
    public VisualStatus Status { get; set; }
    /// <summary>Gets or sets host selection participation.</summary>
    public bool Selectable { get; set; } = true;
    /// <summary>Gets cells whose identities are the row key and ColumnId pair.</summary>
    public List<VisualArtifactInterchangeTableCell> Cells { get; } = new();
    /// <summary>Gets opaque row metadata, including raw type and pointer facts.</summary>
    public Dictionary<string, string> Extensions { get; } = new(System.StringComparer.Ordinal);
}
/// <summary>Captures scalar text projections without conflating missing, zero, null or raw source types.</summary>
public sealed class VisualArtifactInterchangeTableCell {
    /// <summary>Gets or sets its owning column identity; list position is not the identity.</summary>
    public string ColumnId { get; set; } = "";
    /// <summary>Gets or sets the complete static display text.</summary>
    public string DisplayText { get; set; } = "";
    /// <summary>Gets or sets host search text.</summary>
    public string? SearchText { get; set; }
    /// <summary>Gets or sets host sort text.</summary>
    public string? SortText { get; set; }
    /// <summary>Gets or sets host copy text.</summary>
    public string? CopyText { get; set; }
    /// <summary>Gets or sets host export text.</summary>
    public string? ExportText { get; set; }
    /// <summary>Gets or sets its semantic status.</summary>
    public VisualStatus Status { get; set; }
    /// <summary>Gets opaque cell facts; retained raw values do not replace display text.</summary>
    public Dictionary<string, string> Extensions { get; } = new(System.StringComparer.Ordinal);
}
