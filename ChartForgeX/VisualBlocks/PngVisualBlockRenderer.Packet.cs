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
        var labelTop = packet.ShowBitNumbers ? 16.0 : 0.0;
        var rowGap = Math.Min(12, Math.Max(5, content.Height * 0.025));
        var rowHeight = Math.Max(22, Math.Min(52, (options.Size.Height - options.Padding.Bottom - y - rowGap * Math.Max(0, rowCount - 1)) / rowCount - labelTop));
        var bitWidth = content.Width / packet.BitsPerRow;
        for (var index = 0; index < slices.Count; index++) {
            var slice = slices[index];
            var rowY = y + slice.Row * (rowHeight + labelTop + rowGap) + labelTop;
            var x = content.X + (slice.StartBit % packet.BitsPerRow) * bitWidth;
            var width = Math.Max(1, slice.BitLength * bitWidth - 1);
            var color = VisualBlockRendering.PaletteAt(theme, slice.FieldIndex);
            canvas.FillRoundedRect(x, rowY, width, rowHeight, Math.Min(7, rowHeight * 0.22), color.WithAlpha(48));
            canvas.StrokeRoundedRect(x, rowY, width, rowHeight, Math.Min(7, rowHeight * 0.22), color, 1);
            if (packet.ShowBitNumbers) DrawPacketBitLabels(canvas, slice, x, rowY - 13, width, theme.MutedText);
            var fontSize = Math.Max(9, Math.Min(13.5, rowHeight * 0.31));
            DrawCenteredTextMiddle(canvas, slice.Field.Label, x + width / 2, rowY + rowHeight / 2, fontSize, theme.Text, true, Math.Max(1, width - 10));
        }
    }

    private static void DrawPacketBitLabels(RgbaCanvas canvas, VisualBlockRendering.PacketLayoutSlice slice, double x, double y, double width, ChartColor color) {
        var fontSize = 9.5;
        canvas.DrawTextEmphasized(x, y, slice.StartBit.ToString(CultureInfo.InvariantCulture), color, fontSize);
        if (slice.EndBit == slice.StartBit) return;
        var end = slice.EndBit.ToString(CultureInfo.InvariantCulture);
        var endWidth = RgbaCanvas.MeasureTextEmphasizedWidth(end, fontSize, null);
        canvas.DrawTextEmphasized(x + width - endWidth, y, end, color, fontSize);
    }
}
