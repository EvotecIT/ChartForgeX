using System;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

public sealed partial class SvgVisualBlockRenderer {
    private static void RenderPacketLayout(SvgMarkupWriter writer, PacketLayoutBlock packet) {
        var options = packet.Options;
        var theme = options.Theme;
        var content = VisualBlockRendering.ContentRect(options);
        var y = content.Y;
        RenderBlockHeading(writer, packet, ref y, content.X, content.Width);
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
            writer.StartElement("rect")
                .Attribute("data-cfx-role", "packet-field")
                .Attribute("data-bit-start", slice.StartBit)
                .Attribute("data-bit-end", slice.EndBit)
                .Attribute("x", F(x))
                .Attribute("y", F(rowY))
                .Attribute("width", F(width))
                .Attribute("height", F(rowHeight))
                .Attribute("rx", F(Math.Min(7, rowHeight * 0.22)))
                .Attribute("fill", color.WithAlpha(48).ToCss())
                .Attribute("stroke", color.ToCss())
                .EndEmptyElement()
                .Line();

            if (packet.ShowBitNumbers) WritePacketBitLabels(writer, packet, slice, x, rowY - 4, width, theme);
            var fontSize = Math.Max(9, Math.Min(13.5, rowHeight * 0.31));
            WriteText(writer, slice.Field.Label, x + 5, rowY + rowHeight * 0.58, Math.Max(1, width - 10), VisualTextAlignment.Center, theme.Text, theme.FontFamily, fontSize, "700");
        }
    }

    private static void WritePacketBitLabels(SvgMarkupWriter writer, PacketLayoutBlock packet, VisualBlockRendering.PacketLayoutSlice slice, double x, double y, double width, ChartForgeX.Themes.ChartTheme theme) {
        var start = slice.StartBit.ToString(CultureInfo.InvariantCulture);
        writer.StartElement("text").Attribute("data-cfx-role", "packet-bit-start").Attribute("x", F(x)).Attribute("y", F(y)).Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", 9.5).Attribute("font-weight", "650").Text(start).EndElement().Line();
        if (slice.EndBit != slice.StartBit) {
            var end = slice.EndBit.ToString(CultureInfo.InvariantCulture);
            writer.StartElement("text").Attribute("data-cfx-role", "packet-bit-end").Attribute("x", F(x + width)).Attribute("y", F(y)).Attribute("text-anchor", "end").Attribute("fill", theme.MutedText.ToCss()).Attribute("font-family", theme.FontFamily).Attribute("font-size", 9.5).Attribute("font-weight", "650").Text(end).EndElement().Line();
        }
    }
}
