using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Rendering;

internal sealed partial class NativeTablePaintPlan {
    internal static NativeTablePaintPlan Capture(NativeTableData data) {
        var plan = new NativeTablePaintPlan(data); plan.Build(); plan._scene = plan._builder.Build(); return plan;
    }
    private void Build() {
        var data = _data; var theme = data.Theme; var content = data.Bounds; var s = data.Scale; var y = content.Y;
        if (data.Title.Length > 0 || data.Subtitle.Length > 0) {
            var absolute = data.AbsoluteDataLabelSize.HasValue;
            var headings = absolute
                ? VisualBlockRendering.Headings(data.Title, data.Subtitle, theme.FontFamily,
                    OutputFontSize(data, theme.TitleFontSize), OutputFontSize(data, theme.SubtitleFontSize), content, s)
                : VisualBlockRendering.Headings(data.Title, data.Subtitle, theme, new ChartRect(0, 0, content.Width / s, content.Height / s));
            var headingScale = absolute ? 1 : s;
            if (data.Title.Length > 0) { Text(data.Title, content.X, y + headings.TitleAscent * headingScale, content.Width, TextAlignment.Left, data.Foreground ?? theme.Text, headings.TitleFontSize * headingScale, 700, "table-title"); y += headings.TitleAdvance * headingScale; }
            if (data.Subtitle.Length > 0) { Text(data.Subtitle, content.X, y + headings.SubtitleAscent * headingScale, content.Width, TextAlignment.Left, theme.MutedText, headings.SubtitleFontSize * headingScale, 400, "table-subtitle"); y += headings.SubtitleAdvance * headingScale; }
        }
        var bodyScale = theme.DataLabelFontSize / 12;
        var headerHeight = data.AbsoluteDataLabelSize is double absoluteHeader
            ? (data.Dense ? 26d : 32d) * Math.Max(s, absoluteHeader / 12)
            : (data.Dense ? 26d : 32d) * Math.Max(1, bodyScale) * s;
        var rowHeight = data.AbsoluteDataLabelSize is double absoluteRows
            ? (data.Dense ? 24d : 31d) * (absoluteRows / 12)
            : (data.Dense ? 24d : 31d) * bodyScale * s;
        var widths = ColumnWidths(data, content.Width);
        foreach (var width in widths) RequiredWidth += width;
        RequiredHeight = y - content.Y + (data.ShowHeader ? headerHeight + 4 * s : 0) + data.Rows.Count * rowHeight;
        if (data.ShowHeader) {
            Rect("table-header", new ChartRect(content.X, y, content.Width, headerHeight), Math.Min(6, theme.PlotCornerRadius) * s,
                theme.PlotBackground, theme.PlotBorder, ChartVisualPrimitives.SvgGuideStrokeClass);
            var x = content.X;
            for (var i = 0; i < data.Columns.Count; i++) {
                var column = data.Columns[i];
                using (Identity(ChildId("header", column.Id), "table-header-cell", new Dictionary<string, string> { ["data-column-id"] = column.Id }))
                    Text(column.Label, x + 9 * s, y + headerHeight * .66, widths[i] - 18 * s, column.Alignment, data.Foreground ?? theme.Text, OutputFontSize(data, theme.SubtitleFontSize), 700);
                x += widths[i];
            }
            y += headerHeight + 4 * s;
        }
        var maximum = Math.Max(0, (int)Math.Floor(((data.RowWindowBottom ?? content.Bottom) - y) / rowHeight));
        // Finite embedded content has already allocated every row. Reuse that measured total
        // instead of losing a row to cancellation in a scaled height / row-height quotient.
        // Optional ordinary tables retain their incumbent explicit row-window arithmetic.
        if (!data.RowWindowBottom.HasValue && RequiredHeight <= content.Height + 1e-8) maximum = data.Rows.Count;
        for (var rowIndex = 0; rowIndex < data.Rows.Count && rowIndex < maximum; rowIndex++) {
            var row = data.Rows[rowIndex]; var x = content.X;
            var background = row.Background ?? (data.RowStriping && rowIndex % 2 == 1 ? theme.PlotBackground.WithAlpha(110) : ChartColor.Transparent);
            using (Identity(ChildId("row", row.Key), "table-row-content", new Dictionary<string, string> { ["data-row-key"] = row.Key })) {
                if (background.A > 0) Rect("table-row", new ChartRect(content.X, y, content.Width, rowHeight), 4 * s, background);
                for (var i = 0; i < row.Cells.Count; i++) {
                    var cell = row.Cells[i]; var column = data.Columns[i]; var box = new ChartRect(x, y, widths[i], rowHeight);
                    var cellId = ChildId("cell", row.Key.Length + ":" + row.Key + "/" + column.Id);
                    using (Identity(cellId, "table-cell", new Dictionary<string, string> { ["data-row-key"] = row.Key, ["data-column-id"] = column.Id, ["data-table-id"] = data.ContentId })) {
                        if (cell.Background.HasValue) Rect("table-cell-background", new ChartRect(x + 2 * s, y + 2 * s, Math.Max(s, widths[i] - 4 * s), Math.Max(s, rowHeight - 4 * s)), 4 * s, cell.Background.Value);
                        var status = cell.Status != VisualStatus.None ? cell.Status : data.StatusColumn == i ? VisualBlockRendering.ParseStatus(cell.Text) : VisualStatus.None;
                        var textX = x + 9 * s;
                        if (status != VisualStatus.None && data.StatusColumn == i) { Circle("table-status", x + 10 * s, y + rowHeight / 2, 4 * s, VisualBlockRendering.StatusColor(theme, status)); textX += 12 * s; }
                        var cellWidth = widths[i] - (textX - x) - 7 * s;
                        if (cell.MicroVisual != NativeTableMicroVisual.None) MicroVisual(cell, textX, y + 4 * s, cellWidth, rowHeight - 8 * s, i);
                        else if (cell.BadgeText.Length > 0) Badge(cell, textX, y + 4 * s, cellWidth, rowHeight - 8 * s);
                        else Text(cell.Text, textX, y + rowHeight * .66, cellWidth, cell.Alignment ?? column.Alignment, cell.Foreground ?? row.Foreground ?? data.Foreground ?? theme.Text, (data.AbsoluteDataLabelSize is double absoluteText ? (data.Dense ? 10.5 : 11.5) * (absoluteText / 12) : (data.Dense ? 10.5 : 11.5) * bodyScale * s), 400);
                        if (cellId != null) _builder.AddRegion(new VisualSemanticRegion(cellId, "table-cell", box, column.Label + ": " + cell.Text));
                    }
                    x += widths[i];
                }
            }
            y += rowHeight; VisibleRows++;
        }
    }
    private static double[] ColumnWidths(NativeTableData data, double width) {
        var result = new double[data.Columns.Count]; var fixedWidth = 0d; var flexible = 0;
        for (var i = 0; i < result.Length; i++) {
            if (data.Columns[i].Width.HasValue) { result[i] = data.Columns[i].Width!.Value * data.Scale; fixedWidth += result[i]; }
            else flexible++;
        }
        var flexibleWidth = flexible == 0 ? 0 : Math.Max(24 * data.Scale, (width - fixedWidth) / flexible);
        for (var i = 0; i < result.Length; i++) if (result[i] <= 0) result[i] = flexibleWidth;
        return result;
    }
}
