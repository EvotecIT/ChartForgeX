using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    public const int MaximumPacketFields = 10000;
    public const int MaximumPacketBits = 10000;

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
