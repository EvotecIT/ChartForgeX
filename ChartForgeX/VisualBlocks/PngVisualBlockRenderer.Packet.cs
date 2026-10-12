using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;

namespace ChartForgeX.VisualBlocks;

public sealed partial class PngVisualBlockRenderer {
    private static void DrawPacketLayout(RgbaCanvas canvas, PacketLayoutBlock packet) {
        var options = packet.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        DrawHeading(canvas, packet, ref y, content.X, content.Width);
        var slices = VisualBlockRendering.PacketSlices(packet);
        var rowCount = slices.Count == 0 ? 0 : slices[slices.Count - 1].Row + 1;
        if (rowCount == 0) return;
        var metrics = VisualBlockRendering.PacketRows(packet, content, y, rowCount, slices);
        var labelTop = metrics.LabelTop; var rowGap = metrics.Gap; var rowHeight = metrics.Height;
        for (var index = 0; index < slices.Count; index++) {
            var slice = slices[index];
            var rowY = y + slice.Row * (rowHeight + labelTop + rowGap) + labelTop;
            var x = content.X + VisualBlockRendering.PacketFieldLeft(packet, slice, metrics);
            var width = metrics.FieldWidth(slice.BitLength);
            var color = VisualBlockRendering.PaletteAt(theme, slice.FieldIndex);
            canvas.FillRoundedRect(x, rowY, width, rowHeight, Math.Min(7, rowHeight * 0.22), color.WithAlpha(48));
            canvas.StrokeRoundedRect(x, rowY, width, rowHeight, Math.Min(7, rowHeight * 0.22), color, 1);
            if (packet.ShowBitNumbers && metrics.BitFontSize > 0) DrawPacketBitLabels(canvas, packet, slice, x, rowY - metrics.BitBaselineOffset - canvas.TextAscent(metrics.BitFontSize, true), width, theme.MutedText, metrics.BitFontSize);
            var fontSize = Math.Min(metrics.FieldFontSize, VisualBlockRendering.PacketFieldFontSize(packet, slice, width, rowHeight));
            var inset = VisualBlockRendering.PacketFieldInset(width);
            DrawAlignedTextAtBaseline(canvas, slice.Field.Label, x + inset, rowY + rowHeight * .58, Math.Max(1, width - 2 * inset), TextAlignment.Center, theme.Text, fontSize, true);
        }
    }

    private static void DrawPacketBitLabels(RgbaCanvas canvas, PacketLayoutBlock packet, VisualBlockRendering.PacketLayoutSlice slice, double x, double y, double width, ChartColor color, double fontSize) {
        var inset = VisualBlockRendering.PacketBitInset(width);
        var left = VisualBlockRendering.PacketLeftBit(packet, slice).ToString(CultureInfo.InvariantCulture);
        var singleDescending = slice.EndBit == slice.StartBit && packet.BitOrder == PacketBitOrder.Descending;
        canvas.DrawTextEmphasized(singleDescending ? x + width - inset - canvas.MeasureTextEmphasizedWidth(left, fontSize) : x + inset, y, left, color, fontSize);
        if (slice.EndBit == slice.StartBit) return;
        var end = VisualBlockRendering.PacketRightBit(packet, slice).ToString(CultureInfo.InvariantCulture);
        var endWidth = canvas.MeasureTextEmphasizedWidth(end, fontSize);
        canvas.DrawTextEmphasized(x + width - inset - endWidth, y, end, color, fontSize);
    }
}
