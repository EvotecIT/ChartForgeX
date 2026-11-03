using System;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal sealed partial class NativeTablePaintPlan {
    private TextStyle CellStyle(double size, ChartColor color, TextAlignment alignment = TextAlignment.Left) {
        var font = _data.Font.Clone(); font.Weight = _data.WeightOverride ?? 400;
        return new TextStyle { Font = font, FontSize = size, Color = color, Alignment = alignment, LineHeight = 1.2 };
    }

    private double CellHeight(NativeTableCell cell, double size, double ordinary) => cell.Content == null ? ordinary
        : Math.Max(ordinary, TextLayoutEngine.Measure(cell.Content, CellStyle(size, ChartColor.Black)).Height + 8 * _data.Scale);

    private void CellText(NativeTableCell cell, ChartRect bounds, TextAlignment alignment, ChartColor color, double size) {
        if (cell.Content == null || !cell.Content.IsStructured && cell.Content.PlainText.IndexOf('\n') < 0) {
            Text(cell.Text, bounds.X, bounds.Y + bounds.Height * .66, bounds.Width, alignment, color, size, 400);
            return;
        }
        var area = new ChartRect(bounds.X, bounds.Y + 4 * _data.Scale, bounds.Width, Math.Max(1, bounds.Height - 8 * _data.Scale));
        _builder.InlineText(cell.Content, area, CellStyle(size, Paint(color), alignment), TextWrapMode.NoWrap, null,
            "table-cell-text", null, verticallyCentered: true);
        Text(cell.Text, bounds.X, bounds.Y + bounds.Height * .66, bounds.Width, alignment, color, size, 400, native: false);
    }
}
