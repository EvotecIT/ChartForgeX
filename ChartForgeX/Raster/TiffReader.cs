using System;
using System.Collections.Generic;
using System.IO;

namespace ChartForgeX.Raster;

internal static class TiffReader {
    public static bool IsTiff(byte[] data) =>
        data != null && data.Length >= 4 &&
        ((data[0] == (byte)'I' && data[1] == (byte)'I' && data[2] == 42 && data[3] == 0) ||
         (data[0] == (byte)'M' && data[1] == (byte)'M' && data[2] == 0 && data[3] == 42));

    public static RgbaImage Decode(byte[] data) {
        if (!IsTiff(data)) throw new NotSupportedException("Input is not a TIFF image.");
        var little = data[0] == (byte)'I';
        var ifdOffset = checked((int)ReadUInt32(data, 4, little));
        var entries = ReadEntries(data, ifdOffset, little);
        var width = checked((int)GetRequiredValue(data, entries, 256, little));
        var height = checked((int)GetRequiredValue(data, entries, 257, little));
        var compression = GetValue(data, entries, 259, little, 1);
        var photometric = GetValue(data, entries, 262, little, 2);
        var samplesPerPixel = checked((int)GetValue(data, entries, 277, little, photometric == 0 || photometric == 1 ? 1u : 3u));
        if (width <= 0 || height <= 0) throw new InvalidDataException("TIFF dimensions must be positive.");
        if (compression != 1) throw new NotSupportedException("Only uncompressed TIFF images are supported.");
        if (photometric != 0 && photometric != 1 && photometric != 2) throw new NotSupportedException("Only grayscale and RGB TIFF images are supported.");
        if (samplesPerPixel != 1 && samplesPerPixel != 3 && samplesPerPixel != 4) throw new NotSupportedException("Only 1, 3, or 4 sample TIFF images are supported.");
        var bits = GetValues(data, entries, 258, little);
        if (bits.Count == 0) bits.Add(8);
        foreach (var bit in bits) {
            if (bit != 8) throw new NotSupportedException("Only 8-bit TIFF samples are supported.");
        }

        var planarConfiguration = GetValue(data, entries, 284, little, 1);
        if (planarConfiguration != 1) throw new NotSupportedException("Planar TIFF images with separate sample planes are not supported.");
        var offsets = GetValues(data, entries, 273, little);
        var byteCounts = GetValues(data, entries, 279, little);
        if (offsets.Count == 0 || byteCounts.Count == 0) throw new InvalidDataException("TIFF image is missing strip data.");
        var rgba = new byte[checked(width * height * 4)];
        var rowBytes = checked(width * samplesPerPixel);
        var target = 0;
        for (var strip = 0; strip < offsets.Count && target < rgba.Length; strip++) {
            var source = checked((int)offsets[strip]);
            var end = source + checked((int)byteCounts[Math.Min(strip, byteCounts.Count - 1)]);
            if (source < 0 || end > data.Length) throw new InvalidDataException("TIFF strip exceeds the input size.");
            while (source + rowBytes <= end && target < rgba.Length) {
                for (var x = 0; x < width && target < rgba.Length; x++) {
                    if (samplesPerPixel == 1) {
                        var gray = data[source++];
                        if (photometric == 0) gray = (byte)(255 - gray);
                        rgba[target++] = gray; rgba[target++] = gray; rgba[target++] = gray; rgba[target++] = 255;
                    } else {
                        rgba[target++] = data[source++];
                        rgba[target++] = data[source++];
                        rgba[target++] = data[source++];
                        rgba[target++] = samplesPerPixel == 4 ? data[source++] : (byte)255;
                    }
                }
            }
        }

        if (target != rgba.Length) throw new InvalidDataException("TIFF strip data is shorter than expected.");
        return new RgbaImage(width, height, rgba);
    }

    private static List<TiffEntry> ReadEntries(byte[] data, int offset, bool little) {
        if (offset < 0 || offset + 2 > data.Length) throw new InvalidDataException("Invalid TIFF IFD offset.");
        var count = ReadUInt16(data, offset, little);
        offset += 2;
        if (offset + count * 12 > data.Length) throw new InvalidDataException("TIFF IFD exceeds the input size.");
        var entries = new List<TiffEntry>(count);
        for (var i = 0; i < count; i++) {
            entries.Add(new TiffEntry(
                ReadUInt16(data, offset, little),
                ReadUInt16(data, offset + 2, little),
                ReadUInt32(data, offset + 4, little),
                ReadUInt32(data, offset + 8, little),
                offset + 8));
            offset += 12;
        }

        return entries;
    }

    private static uint GetRequiredValue(byte[] data, List<TiffEntry> entries, ushort tag, bool little) {
        var values = GetValues(data, entries, tag, little);
        if (values.Count == 0) throw new InvalidDataException("TIFF image is missing required tag " + tag + ".");
        return values[0];
    }

    private static uint GetValue(byte[] data, List<TiffEntry> entries, ushort tag, bool little, uint fallback) {
        var values = GetValues(data, entries, tag, little);
        return values.Count == 0 ? fallback : values[0];
    }

    private static List<uint> GetValues(byte[] data, List<TiffEntry> entries, ushort tag, bool little) {
        foreach (var entry in entries) {
            if (entry.Tag != tag) continue;
            var size = TypeSize(entry.Type);
            var totalBytes = checked((int)(entry.Count * size));
            var offset = totalBytes <= 4 ? entry.ValueOffsetPosition : checked((int)entry.ValueOrOffset);
            if (offset < 0 || offset + totalBytes > data.Length) throw new InvalidDataException("TIFF tag data exceeds the input size.");
            var values = new List<uint>(checked((int)entry.Count));
            for (var i = 0; i < entry.Count; i++) {
                values.Add(ReadTypedValue(data, offset + i * size, entry.Type, little));
            }

            return values;
        }

        return new List<uint>();
    }

    private static int TypeSize(ushort type) {
        switch (type) {
            case 1:
            case 2: return 1;
            case 3: return 2;
            case 4: return 4;
            default: throw new NotSupportedException("Unsupported TIFF field type: " + type + ".");
        }
    }

    private static uint ReadTypedValue(byte[] data, int offset, ushort type, bool little) {
        switch (type) {
            case 1:
            case 2: return data[offset];
            case 3: return ReadUInt16(data, offset, little);
            case 4: return ReadUInt32(data, offset, little);
            default: throw new NotSupportedException("Unsupported TIFF field type: " + type + ".");
        }
    }

    private static ushort ReadUInt16(byte[] data, int offset, bool little) =>
        little ? (ushort)(data[offset] | (data[offset + 1] << 8)) : (ushort)((data[offset] << 8) | data[offset + 1]);

    private static uint ReadUInt32(byte[] data, int offset, bool little) =>
        little
            ? (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24))
            : ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

    private readonly struct TiffEntry {
        public readonly ushort Tag;
        public readonly ushort Type;
        public readonly uint Count;
        public readonly uint ValueOrOffset;
        public readonly int ValueOffsetPosition;

        public TiffEntry(ushort tag, ushort type, uint count, uint valueOrOffset, int valueOffsetPosition) {
            Tag = tag;
            Type = type;
            Count = count;
            ValueOrOffset = valueOrOffset;
            ValueOffsetPosition = valueOffsetPosition;
        }
    }
}
