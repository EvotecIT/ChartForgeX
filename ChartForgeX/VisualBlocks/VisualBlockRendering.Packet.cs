using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumPacketFields = 10000;
    public const int MaximumPacketBits = 10000;

    public static PacketRowMetrics PacketRows(PacketLayoutBlock packet, ChartRect content, double top, int rows, IReadOnlyList<PacketLayoutSlice> slices) {
        var remaining = Math.Max(0, content.Bottom - top);
        var preferredLabelTop = packet.ShowBitNumbers ? BodyFontSize(packet.Options.Theme, 16) : 0;
        var gapBudget = packet.RowGap.HasValue && rows > 1
            ? Math.Max(0, remaining / rows - (packet.RowHeight ?? 52) - preferredLabelTop) * rows / (rows - 1)
            : remaining / (rows * 2);
        var gap = Math.Min(packet.RowGap ?? Math.Min(12, Math.Max(5, content.Height * .025)), gapBudget);
        var slot = Math.Max(0, (remaining - gap * Math.Max(0, rows - 1)) / rows);
        var labelBudget = packet.RowHeight.HasValue ? Math.Max(slot * .4, slot - packet.RowHeight.Value) : slot * .4;
        var labelTop = Math.Min(preferredLabelTop, labelBudget);
        var height = Math.Min(packet.RowHeight ?? 52, slot - labelTop);
        var bitWidth = Math.Min(packet.BitWidth ?? content.Width / packet.BitsPerRow, content.Width / packet.BitsPerRow);
        var fieldGap = packet.FieldGap ?? 1;
        var bits = Math.Min(BodyFontSize(packet.Options.Theme, 9.5), labelTop * .75);
        var bitStyle = new TextStyle { Font = new FontSpec { Family = packet.Options.Theme.FontFamily, Weight = 700 }, FontSize = Math.Max(.001, bits), LineHeight = 1 };
        if (bits > 0) bits = ChartTextFitting.FitFontSize(0, bits, FitsBits);
        if (bits > 0 && !FitsBits(bits)) bits = 0;
        var label = Math.Min(BodyFontSize(packet.Options.Theme, Math.Max(9, Math.Min(13.5, height * .31))), height * .6);
        return new PacketRowMetrics(labelTop, gap, height, bits, label, bitWidth, fieldGap);

        bool FitsBits(double size) {
            bitStyle.FontSize = size;
            if (TextLayoutEngine.Measure("Mg", bitStyle).Height > labelTop - PacketBitBaselineOffset(labelTop, size)) return false;
            foreach (var slice in slices) {
                var width = PacketFieldWidth(slice.BitLength, bitWidth, fieldGap);
                var available = width - PacketBitInset(width) * 2;
                var numbers = TextLayoutEngine.Measure(slice.StartBit.ToString(CultureInfo.InvariantCulture), bitStyle).Width;
                if (slice.EndBit != slice.StartBit) {
                    numbers += TextLayoutEngine.Measure(slice.EndBit.ToString(CultureInfo.InvariantCulture), bitStyle).Width;
                    available -= LabelPlacementService.DefaultGap;
                }
                if (numbers > available) return false;
            }
            return true;
        }
    }

    private static double PacketFieldWidth(int bits, double bitWidth, double gap) => Math.Max(Math.Min(1, bits * bitWidth), bits * bitWidth - gap);

    // End/start numbers on adjoining fields share the measured scene's label separation.
    public static double PacketBitInset(double width) => Math.Min(LabelPlacementService.DefaultGap / 2, width / 4);
    public static double PacketBitBaselineOffset(double labelTop, double fontSize) => Math.Min(labelTop * .25, Math.Max(4, fontSize * .3));

    public static int PacketColumn(PacketLayoutBlock packet, PacketLayoutSlice slice) => packet.BitOrder == PacketBitOrder.Descending
        ? packet.BitsPerRow - slice.StartBit % packet.BitsPerRow - slice.BitLength : slice.StartBit % packet.BitsPerRow;

    public static int PacketLeftBit(PacketLayoutBlock packet, PacketLayoutSlice slice) =>
        packet.BitOrder == PacketBitOrder.Descending ? slice.EndBit : slice.StartBit;

    public static int PacketRightBit(PacketLayoutBlock packet, PacketLayoutSlice slice) =>
        packet.BitOrder == PacketBitOrder.Descending ? slice.StartBit : slice.EndBit;

    public readonly struct PacketRowMetrics {
        public PacketRowMetrics(double labelTop, double gap, double height, double bitFontSize, double fieldFontSize, double bitWidth, double fieldGap) {
            LabelTop = labelTop; Gap = gap; Height = height; BitFontSize = bitFontSize; FieldFontSize = fieldFontSize;
            BitWidth = bitWidth; FieldGap = fieldGap;
        }
        public double LabelTop { get; }
        public double Gap { get; }
        public double Height { get; }
        public double BitFontSize { get; }
        public double BitBaselineOffset => PacketBitBaselineOffset(LabelTop, BitFontSize);
        public double FieldFontSize { get; }
        public double BitWidth { get; }
        public double FieldGap { get; }
        public double FieldWidth(int bits) => PacketFieldWidth(bits, BitWidth, FieldGap);
    }

    public static void ValidatePacketLayout(PacketLayoutBlock packet) {
        if (packet.Fields.Count == 0) throw new InvalidOperationException("Packet layout blocks must contain at least one field.");
        if (packet.Fields.Count > MaximumPacketFields) throw new InvalidOperationException("Packet layout blocks must contain no more than " + MaximumPacketFields.ToString(CultureInfo.InvariantCulture) + " fields.");
        var expectedStart = 0;
        foreach (var field in packet.Fields) {
            if (field.Label.Length == 0) throw new InvalidOperationException("Packet layout fields must define labels.");
            if (field.StartBit != expectedStart) throw new InvalidOperationException("Packet layout fields must be contiguous from bit zero. Expected bit " + expectedStart.ToString(CultureInfo.InvariantCulture) + " but found bit " + field.StartBit.ToString(CultureInfo.InvariantCulture) + ".");
            var nextExpected = (long)field.EndBit + 1;
            if (nextExpected > MaximumPacketBits) throw new InvalidOperationException("Packet layout total bit length must be no more than " + MaximumPacketBits.ToString(CultureInfo.InvariantCulture) + " bits.");
            expectedStart = (int)nextExpected;
        }
    }

    public static IReadOnlyList<PacketLayoutSlice> PacketSlices(PacketLayoutBlock packet) {
        var slices = new List<PacketLayoutSlice>();
        for (var fieldIndex = 0; fieldIndex < packet.Fields.Count; fieldIndex++) {
            var field = packet.Fields[fieldIndex];
            var start = field.StartBit;
            while (start <= field.EndBit) {
                var row = start / packet.BitsPerRow;
                var rowEnd = Math.Min(field.EndBit, (row + 1) * packet.BitsPerRow - 1);
                slices.Add(new PacketLayoutSlice(field, fieldIndex, row, start, rowEnd));
                start = rowEnd + 1;
            }
        }

        return slices;
    }

    public readonly struct PacketLayoutSlice {
        public PacketLayoutSlice(PacketLayoutField field, int fieldIndex, int row, int startBit, int endBit) {
            Field = field;
            FieldIndex = fieldIndex;
            Row = row;
            StartBit = startBit;
            EndBit = endBit;
        }

        public PacketLayoutField Field { get; }
        public int FieldIndex { get; }
        public int Row { get; }
        public int StartBit { get; }
        public int EndBit { get; }
        public int BitLength => EndBit - StartBit + 1;
    }
}
