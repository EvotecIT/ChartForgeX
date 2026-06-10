using System;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Provides static preview rendering helpers for table artifacts.
/// </summary>
public static class TableArtifactRendering {
    /// <summary>
    /// Converts a table artifact into the existing static ChartForgeX table visual block.
    /// </summary>
    /// <param name="table">The table artifact.</param>
    /// <returns>A static table preview block.</returns>
    public static ChartTable ToPreviewBlock(this TableArtifact table) {
        if (table == null) throw new ArgumentNullException(nameof(table));
        var preview = ChartTable.Create()
            .WithTitle(table.Title)
            .WithSubtitle(table.Subtitle);

        int? statusColumnIndex = null;
        for (var columnIndex = 0; columnIndex < table.Columns.Count; columnIndex++) {
            var column = table.Columns[columnIndex];
            preview.AddColumn(column.Label, column.Alignment, column.Width);
            if (!statusColumnIndex.HasValue && column.Type == TableArtifactColumnType.Status) statusColumnIndex = columnIndex;
        }

        if (statusColumnIndex.HasValue) preview.WithStatusColumn(statusColumnIndex.Value);
        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++) {
            var row = table.Rows[rowIndex];
            var values = new object?[row.Cells.Count];
            for (var cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++) values[cellIndex] = row.Cells[cellIndex].DisplayText;
            preview.AddRow(values);
            preview.WithRow(rowIndex, targetRow => {
                for (var cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++) {
                    targetRow.Cells[cellIndex].Status = row.Cells[cellIndex].Status == VisualStatus.None ? row.Status : row.Cells[cellIndex].Status;
                }
            });
        }

        return preview;
    }

    /// <summary>
    /// Wraps a table artifact in a product-neutral visual artifact envelope.
    /// </summary>
    /// <param name="table">The table artifact.</param>
    /// <returns>A visual artifact envelope.</returns>
    public static VisualArtifact ToVisualArtifact(this TableArtifact table) {
        if (table == null) throw new ArgumentNullException(nameof(table));
        var artifact = VisualArtifact.Create(table.Id, VisualArtifactKind.Table, table);
        artifact.Title = table.Title;
        artifact.Subtitle = table.Subtitle;
        artifact.ExportFormats = table.ExportFormats;
        artifact.Metadata["table.capabilities"] = table.Capabilities.ToString();
        artifact.Metadata["table.columns"] = table.Columns.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        artifact.Metadata["table.rows"] = table.Rows.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (table.TotalRowCount.HasValue) artifact.Metadata["table.totalRows"] = table.TotalRowCount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return artifact;
    }

    /// <summary>
    /// Renders a table artifact static preview to SVG.
    /// </summary>
    /// <param name="table">The table artifact.</param>
    /// <returns>SVG markup.</returns>
    public static string ToSvg(this TableArtifact table) => table.ToPreviewBlock().ToSvg();

    /// <summary>
    /// Renders a table artifact static preview to PNG.
    /// </summary>
    /// <param name="table">The table artifact.</param>
    /// <returns>PNG bytes.</returns>
    public static byte[] ToPng(this TableArtifact table) => table.ToPreviewBlock().ToPng();
}
