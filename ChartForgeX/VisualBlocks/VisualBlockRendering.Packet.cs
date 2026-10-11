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
        var gap = Math.Min(Math.Min(12, Math.Max(5, content.Height * .025)), remaining / (rows * 2));
        var slot = Math.Max(0, (remaining - gap * Math.Max(0, rows - 1)) / rows);
        var labelTop = packet.ShowBitNumbers ? Math.Min(BodyFontSize(packet.Options.Theme, 16), slot * .4) : 0;
        var height = Math.Min(52, slot - labelTop);
        var bits = Math.Min(BodyFontSize(packet.Options.Theme, 9.5), labelTop * .75);
        var bitStyle = new TextStyle { Font = new FontSpec { Family = packet.Options.Theme.FontFamily, Weight = 700 }, FontSize = Math.Max(.001, bits), LineHeight = 1 };
        if (bits > 0) bits = FitFontSize(0, bits, FitsBits);
        if (bits > 0 && !FitsBits(bits)) bits = 0;
        var label = Math.Min(BodyFontSize(packet.Options.Theme, Math.Max(9, Math.Min(13.5, height * .31))), height * .6);
        return new PacketRowMetrics(labelTop, gap, height, bits, label);

        bool FitsBits(double size) {
            bitStyle.FontSize = size;
            if (TextLayoutEngine.Measure("Mg", bitStyle).Height > labelTop - PacketBitBaselineOffset(labelTop, size)) return false;
            foreach (var slice in slices) {
                var width = Math.Max(1, slice.BitLength * content.Width / packet.BitsPerRow - 1);
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

    // End/start numbers on adjoining fields share the measured scene's label separation.
    public static double PacketBitInset(double width) => Math.Min(LabelPlacementService.DefaultGap / 2, width / 4);
    public static double PacketBitBaselineOffset(double labelTop, double fontSize) => Math.Min(labelTop * .25, Math.Max(4, fontSize * .3));

    public readonly struct PacketRowMetrics {
        public PacketRowMetrics(double labelTop, double gap, double height, double bitFontSize, double fieldFontSize) {
            LabelTop = labelTop; Gap = gap; Height = height; BitFontSize = bitFontSize; FieldFontSize = fieldFontSize;
        }
        public double LabelTop { get; }
        public double Gap { get; }
        public double Height { get; }
        public double BitFontSize { get; }
        public double BitBaselineOffset => PacketBitBaselineOffset(LabelTop, BitFontSize);
        public double FieldFontSize { get; }
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
