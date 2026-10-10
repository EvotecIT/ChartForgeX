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
        var bitWidth = content.Width / packet.BitsPerRow;
        for (var index = 0; index < slices.Count; index++) {
            var slice = slices[index];
            var rowY = y + slice.Row * (rowHeight + labelTop + rowGap) + labelTop;
            var x = content.X + VisualBlockRendering.PacketColumn(packet, slice) * bitWidth;
            var width = Math.Max(1, slice.BitLength * bitWidth - 1);
            var color = VisualBlockRendering.PaletteAt(theme, slice.FieldIndex);
            canvas.FillRoundedRect(x, rowY, width, rowHeight, Math.Min(7, rowHeight * 0.22), color.WithAlpha(48));
            canvas.StrokeRoundedRect(x, rowY, width, rowHeight, Math.Min(7, rowHeight * 0.22), color, 1);
            if (packet.ShowBitNumbers) DrawPacketBitLabels(canvas, packet, slice, x, rowY - metrics.BitBaselineOffset - canvas.TextAscent(metrics.BitFontSize, true), width, theme.MutedText, metrics.BitFontSize);
            var fontSize = metrics.FieldFontSize;
            DrawAlignedTextAtBaseline(canvas, slice.Field.Label, x + 5, rowY + rowHeight * .58, Math.Max(1, width - 10), TextAlignment.Center, theme.Text, fontSize, true);
        }
    }

    private static void DrawPacketBitLabels(RgbaCanvas canvas, PacketLayoutBlock packet, VisualBlockRendering.PacketLayoutSlice slice, double x, double y, double width, ChartColor color, double fontSize) {
        var inset = VisualBlockRendering.PacketBitInset(width);
        canvas.DrawTextEmphasized(x + inset, y, VisualBlockRendering.PacketLeftBit(packet, slice).ToString(CultureInfo.InvariantCulture), color, fontSize);
        if (slice.EndBit == slice.StartBit) return;
        var end = VisualBlockRendering.PacketRightBit(packet, slice).ToString(CultureInfo.InvariantCulture);
        var endWidth = canvas.MeasureTextEmphasizedWidth(end, fontSize);
        canvas.DrawTextEmphasized(x + width - inset - endWidth, y, end, color, fontSize);
    }
}
