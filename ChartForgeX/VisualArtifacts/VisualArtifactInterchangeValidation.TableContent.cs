using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeValidation {
    // An embedded cell is an exact bounded fact, under the existing node detail/port and aggregate JSON budgets.
    internal const int MaximumTableRows = MaximumDetailsPerNode;
    internal const int MaximumTableColumns = MaximumPortsPerNode;
    internal static void TableContent(VisualArtifactInterchangeTableContent? table) {
        if (table == null) return;
        PositiveOptional(table.PresentationScale, "embedded table presentation scale");
        RequiredId(table.Id, "embedded table id"); Text(table.Title, "embedded table title"); Text(table.Subtitle, "embedded table subtitle");
        TableArtifactGuards.TableCapabilitiesDefined(table.Capabilities, nameof(table));
        VisualArtifactGuards.ExportFormatsDefined(table.ExportFormats, nameof(table));
        Count(table.Columns.Count, MaximumTableColumns, "embedded table columns"); Count(table.Rows.Count, MaximumTableRows, "embedded table rows");
        if (table.Columns.Count == 0) throw new ArgumentException("Embedded tables require at least one column.", nameof(table));
        if (table.TotalRowCount.HasValue && table.TotalRowCount.Value != table.Rows.Count)
            throw new NotSupportedException("An embedded static table must capture every declared row; virtualized providers are not retained.");
        Extensions(table.Extensions, "embedded table metadata");
        var columns = new HashSet<string>(StringComparer.Ordinal); var rows = new HashSet<string>(StringComparer.Ordinal);
        foreach (var column in table.Columns) {
            if (column == null) throw new ArgumentException("Embedded table columns must not be null.", nameof(table));
            RequiredId(column.Id, "embedded table column id"); Text(column.Label, "embedded table column label");
            if (!columns.Add(column.Id)) throw new ArgumentException("Embedded table column ids must be unique.", nameof(table));
            TableArtifactGuards.EnumDefined(column.Type, nameof(table)); TableArtifactGuards.EnumDefined(column.Alignment, nameof(table));
            PositiveOptional(column.Width, "embedded table column width"); Extensions(column.Extensions, "embedded table column metadata");
        }
        foreach (var row in table.Rows) {
            if (row == null) throw new ArgumentException("Embedded table rows must not be null.", nameof(table));
            RequiredId(row.Key, "embedded table row key"); TableArtifactGuards.EnumDefined(row.Status, nameof(table));
            if (!rows.Add(row.Key)) throw new ArgumentException("Embedded table row keys must be unique.", nameof(table));
            Extensions(row.Extensions, "embedded table row metadata");
            if (row.Cells.Count != table.Columns.Count) throw new ArgumentException("Embedded rows must carry every column exactly once in source column order.", nameof(table));
            for (var i = 0; i < row.Cells.Count; i++) {
                var cell = row.Cells[i] ?? throw new ArgumentException("Embedded table cells must not be null.", nameof(table));
                if (!string.Equals(cell.ColumnId, table.Columns[i].Id, StringComparison.Ordinal))
                    throw new ArgumentException("Embedded cell ColumnId must identify its source-ordered owning column.", nameof(table));
                Text(cell.DisplayText, "embedded table cell display text"); OptionalText(cell.SearchText, "embedded table search text");
                OptionalText(cell.SortText, "embedded table sort text"); OptionalText(cell.CopyText, "embedded table copy text");
                OptionalText(cell.ExportText, "embedded table export text"); TableArtifactGuards.EnumDefined(cell.Status, nameof(table));
                Extensions(cell.Extensions, "embedded table cell metadata");
            }
        }
    }
}
