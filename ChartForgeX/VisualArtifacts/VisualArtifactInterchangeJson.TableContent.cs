using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

internal static partial class VisualArtifactInterchangeJson {
    private static void TableContent(VisualArtifactInterchangeJsonWriter writer, VisualArtifactInterchangeTableContent? table) {
        if (table == null) return;
        writer.Property("tableContent"); writer.StartObject();
        String(writer, "id", table.Id); String(writer, "title", table.Title); String(writer, "subtitle", table.Subtitle);
        String(writer, "capabilities", table.Capabilities.ToString()); String(writer, "exportFormats", table.ExportFormats.ToString());
        if (table.TotalRowCount.HasValue) Number(writer, "totalRowCount", (double)table.TotalRowCount.Value);
        if (table.PresentationScale != 1) Number(writer, "presentationScale", table.PresentationScale);
        Extensions(writer, table.Extensions); writer.Property("columns"); writer.StartArray();
        foreach (var column in table.Columns) {
            writer.StartObject(); String(writer, "id", column.Id); String(writer, "label", column.Label);
            String(writer, "type", column.Type.ToString()); String(writer, "alignment", column.Alignment.ToString()); OptionalNumber(writer, "width", column.Width);
            Boolean(writer, "searchable", column.Searchable); Boolean(writer, "sortable", column.Sortable); Boolean(writer, "filterable", column.Filterable);
            Boolean(writer, "copyable", column.Copyable); Boolean(writer, "exportable", column.Exportable); Extensions(writer, column.Extensions); writer.EndObject();
        }
        writer.EndArray(); writer.Property("rows"); writer.StartArray();
        foreach (var row in table.Rows) {
            writer.StartObject(); String(writer, "key", row.Key); String(writer, "status", row.Status.ToString()); Boolean(writer, "selectable", row.Selectable);
            Extensions(writer, row.Extensions); writer.Property("cells"); writer.StartArray();
            foreach (var cell in row.Cells) {
                writer.StartObject(); String(writer, "columnId", cell.ColumnId); String(writer, "displayText", cell.DisplayText);
                OptionalString(writer, "searchText", cell.SearchText); OptionalString(writer, "sortText", cell.SortText);
                OptionalString(writer, "copyText", cell.CopyText); OptionalString(writer, "exportText", cell.ExportText);
                String(writer, "status", cell.Status.ToString()); Extensions(writer, cell.Extensions); writer.EndObject();
            }
            writer.EndArray(); writer.EndObject();
        }
        writer.EndArray(); writer.EndObject();
    }
    private static VisualArtifactInterchangeTableContent? ReadTableContent(Dictionary<string, GeoJsonValue> root) {
        var item = OptionalObject(root, "tableContent"); if (item == null) return null;
        var table = new VisualArtifactInterchangeTableContent { Id = RequiredString(item, "id"), Title = RequiredStringAllowEmpty(item, "title"),
            Subtitle = RequiredStringAllowEmpty(item, "subtitle"), Capabilities = RequiredFlagsEnum<TableArtifactCapabilities>(item, "capabilities"),
            ExportFormats = RequiredFlagsEnum<VisualArtifactExportFormat>(item, "exportFormats"), TotalRowCount = OptionalInt(item, "totalRowCount"), PresentationScale = OptionalNumber(item, "presentationScale") ?? 1 };
        ReadExtensions(item, table.Extensions);
        foreach (var entry in OptionalArray(item, "columns")) {
            var column = entry.AsObject("embedded table column"); var copy = new VisualArtifactInterchangeTableColumn { Id = RequiredString(column, "id"),
                Label = RequiredStringAllowEmpty(column, "label"), Type = RequiredEnum<TableArtifactColumnType>(column, "type"),
                Alignment = RequiredEnum<TextAlignment>(column, "alignment"), Width = OptionalNumber(column, "width"),
                Searchable = OptionalBool(column, "searchable") ?? true, Sortable = OptionalBool(column, "sortable") ?? true,
                Filterable = OptionalBool(column, "filterable") ?? true, Copyable = OptionalBool(column, "copyable") ?? true, Exportable = OptionalBool(column, "exportable") ?? true };
            ReadExtensions(column, copy.Extensions); table.Columns.Add(copy);
        }
        foreach (var entry in OptionalArray(item, "rows")) {
            var row = entry.AsObject("embedded table row"); var copy = new VisualArtifactInterchangeTableRow { Key = RequiredString(row, "key"),
                Status = RequiredEnum<VisualStatus>(row, "status"), Selectable = OptionalBool(row, "selectable") ?? true };
            ReadExtensions(row, copy.Extensions);
            foreach (var cellEntry in OptionalArray(row, "cells")) {
                var cell = cellEntry.AsObject("embedded table cell"); var value = new VisualArtifactInterchangeTableCell { ColumnId = RequiredString(cell, "columnId"),
                    DisplayText = RequiredStringAllowEmpty(cell, "displayText"), SearchText = OptionalString(cell, "searchText"), SortText = OptionalString(cell, "sortText"),
                    CopyText = OptionalString(cell, "copyText"), ExportText = OptionalString(cell, "exportText"), Status = RequiredEnum<VisualStatus>(cell, "status") };
                ReadExtensions(cell, value.Extensions); copy.Cells.Add(value);
            }
            table.Rows.Add(copy);
        }
        return table;
    }
}
