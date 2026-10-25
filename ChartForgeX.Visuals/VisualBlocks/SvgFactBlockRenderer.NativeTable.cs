using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;
using ChartForgeX.Typography;
using static ChartForgeX.VisualBlocks.SvgVisualBlockRenderer;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgFactBlockRenderer {
    private static void RenderTable(SvgMarkupWriter writer, ChartTable table, string id) {
        var content = VisualBlockRendering.ContentRect(table.Options); var y = content.Y;
        RenderBlockHeading(writer, table, ref y, content.X, content.Width);
        var data = CaptureTable(table, new ChartRect(content.X, y, content.Width, table.Options.Size.Height - table.Options.Padding.Bottom - y));
        NativeTablePaintPlan.Capture(data).WriteSvg(writer);
    }
    private static NativeTableData CaptureTable(ChartTable table, ChartRect bounds) {
        var data = new NativeTableData { Theme = table.Options.ResolvedTheme, Font = new FontSpec { Family = table.Options.ResolvedTheme.FontFamily },
            Bounds = bounds, RowWindowBottom = table.Options.Size.Height - table.Options.Padding.Bottom, Dense = table.Dense, ShowHeader = table.ShowHeader, RowStriping = table.RowStriping, StatusColumn = table.StatusColumnIndex };
        for (var i = 0; i < table.Columns.Count; i++) { var source = table.Columns[i];
            data.Columns.Add(new NativeTableColumn(i.ToString(CultureInfo.InvariantCulture), source.Header, source.Alignment, source.Width)); }
        for (var i = 0; i < table.Rows.Count; i++) {
            var source = table.Rows[i]; var row = new NativeTableRow { Key = i.ToString(CultureInfo.InvariantCulture), Background = source.Background, Foreground = source.Foreground };
            foreach (var cell in source.Cells) {
                var copy = new NativeTableCell { Text = cell.Text, Alignment = cell.Alignment, Foreground = cell.Foreground, Background = cell.Background,
                    Status = cell.Status, BadgeText = cell.BadgeText, BadgeStatus = cell.BadgeStatus, BadgeColor = cell.BadgeColor,
                    Minimum = cell.MicroVisualMinimum, Maximum = cell.MicroVisualMaximum, MicroColor = cell.MicroVisualColor,
                    BadgeTreatment = cell.BadgeStyle switch { VisualBadgeStyle.Solid => NativeTableBadgeTreatment.Solid, VisualBadgeStyle.Outline => NativeTableBadgeTreatment.Outline, _ => NativeTableBadgeTreatment.Soft },
                    MicroVisual = cell.MicroVisualKind switch { ChartTableCellMicroVisualKind.MiniBars => NativeTableMicroVisual.MiniBars,
                        ChartTableCellMicroVisualKind.Sparkline => NativeTableMicroVisual.Sparkline, _ => NativeTableMicroVisual.None },
                    Sparkline = cell.MicroVisualKind == ChartTableCellMicroVisualKind.Sparkline ? cell.GetSparklineData() : null };
                copy.Values.AddRange(cell.MicroVisualValues); row.Cells.Add(copy);
            }
            data.Rows.Add(row);
        }
        return data;
    }
}
