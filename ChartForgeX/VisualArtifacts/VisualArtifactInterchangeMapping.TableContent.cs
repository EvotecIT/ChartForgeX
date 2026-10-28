using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

public static partial class VisualArtifactInterchangeMapping {
    internal static VisualArtifactInterchangeTableContent? MapTableContent(TableArtifact? source) {
        if (source == null) return null;
        var target = new VisualArtifactInterchangeTableContent { Id = source.Id, Title = source.Title, Subtitle = source.Subtitle,
            Capabilities = source.Capabilities, ExportFormats = source.ExportFormats, TotalRowCount = source.TotalRowCount, PresentationScale = source.CapturedPresentationScale };
        CopyTableBag(source.Metadata, target.Extensions);
        foreach (var column in source.Columns) {
            if (column == null) throw new ArgumentException("Embedded table columns must not be null.", nameof(source));
            var item = new VisualArtifactInterchangeTableColumn { Id = column.Id, Label = column.Label, Type = column.Type,
                Alignment = column.Alignment, Width = column.Width, Searchable = column.Searchable, Sortable = column.Sortable,
                Filterable = column.Filterable, Copyable = column.Copyable, Exportable = column.Exportable };
            CopyTableBag(column.Metadata, item.Extensions); target.Columns.Add(item);
        }
        foreach (var row in source.Rows) {
            if (row == null || row.Cells.Count != source.Columns.Count) throw new ArgumentException("Embedded table rows must contain exactly one cell for every column.", nameof(source));
            var item = new VisualArtifactInterchangeTableRow { Key = row.Key, Status = row.Status, Selectable = row.Selectable };
            CopyTableBag(row.Metadata, item.Extensions);
            for (var i = 0; i < row.Cells.Count; i++) {
                var cell = row.Cells[i] ?? throw new ArgumentException("Embedded table cells must not be null.", nameof(source));
                var copy = new VisualArtifactInterchangeTableCell { ColumnId = source.Columns[i].Id, DisplayText = cell.DisplayText,
                    SearchText = cell.SearchText, SortText = cell.SortText, CopyText = cell.CopyText, ExportText = cell.ExportText, Status = cell.Status };
                CopyTableBag(cell.Metadata, copy.Extensions); item.Cells.Add(copy);
            }
            target.Rows.Add(item);
        }
        VisualArtifactInterchangeValidation.TableContent(target); return target;
    }
    internal static TableArtifact? RestoreTableContent(VisualArtifactInterchangeTableContent? source) {
        if (source == null) return null;
        VisualArtifactInterchangeValidation.TableContent(source);
        var target = TableArtifact.Create(source.Id).WithTitle(source.Title).WithSubtitle(source.Subtitle);
        target.CapturedPresentationScale = source.PresentationScale;
        target.Capabilities = source.Capabilities; target.ExportFormats = source.ExportFormats; target.TotalRowCount = source.TotalRowCount;
        CopyTableBag(source.Extensions, target.Metadata);
        foreach (var column in source.Columns) {
            target.AddColumn(column.Id, column.Label, column.Type, column.Alignment, column.Width);
            var item = target.Columns[target.Columns.Count - 1]; item.Searchable = column.Searchable; item.Sortable = column.Sortable;
            item.Filterable = column.Filterable; item.Copyable = column.Copyable; item.Exportable = column.Exportable;
            CopyTableBag(column.Extensions, item.Metadata);
        }
        foreach (var row in source.Rows) {
            var values = new object?[row.Cells.Count]; for (var i = 0; i < values.Length; i++) values[i] = row.Cells[i].DisplayText;
            target.AddRow(row.Key, values); var item = target.Rows[target.Rows.Count - 1]; item.Status = row.Status; item.Selectable = row.Selectable;
            CopyTableBag(row.Extensions, item.Metadata);
            for (var i = 0; i < row.Cells.Count; i++) {
                var cell = row.Cells[i]; var copy = item.Cells[i]; copy.SearchText = cell.SearchText; copy.SortText = cell.SortText;
                copy.CopyText = cell.CopyText; copy.ExportText = cell.ExportText; copy.Status = cell.Status;
                CopyTableBag(cell.Extensions, copy.Metadata);
            }
        }
        return target;
    }
    internal static TableArtifact? SnapshotTableContent(TableArtifact? source) => RestoreTableContent(MapTableContent(source));
    internal static VisualArtifactInterchangeTableContent? SnapshotTableContent(VisualArtifactInterchangeTableContent? source) => MapTableContent(RestoreTableContent(source));
    private static void CopyTableBag(IDictionary<string, string> source, IDictionary<string, string> target) {
        foreach (var item in source) target.Add(item.Key, item.Value);
    }
}
