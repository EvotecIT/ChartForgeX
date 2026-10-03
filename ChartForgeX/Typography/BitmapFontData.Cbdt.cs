using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

internal sealed partial class BitmapFontData {
    private BitmapGlyph? ReadCbdt(BitmapStrike strike, ushort glyph) {
        var table = _cblc!.Value; var data = _cbdt!.Value; var size = strike.At;
        if (glyph < table.U16(size + 40) || glyph > table.U16(size + 42)) return null;
        var list = table.Offset(0, size, wide: true); var listSize = table.U32(size + 4); var count = table.U32(size + 8);
        if (listSize > int.MaxValue || count > 65536 || (long)count * 8 > listSize) throw new FontLayoutException();
        table.Require(list, (int)listSize); var low = 0L; var high = (long)count - 1;
        while (low <= high) {
            var middle = (low + high) / 2; var entry = table.Record(list, middle, 8); var first = table.U16(entry); var last = table.U16(entry + 2);
            if (glyph < first) high = middle - 1;
            else if (glyph > last) low = middle + 1;
            else {
                var index = table.Offset(list, entry + 4, wide: true);
                if (index < list + count * 8 || index > list + listSize - 8) throw new FontLayoutException();
                return ReadCbdtIndex(table.Slice(index, (int)(list + listSize - index)), data, strike, 0, first, last, glyph);
            }
        }
        return null;
    }
    private BitmapGlyph? ReadCbdtIndex(FontTableReader table, FontTableReader data, BitmapStrike strike, int at, int first, int last, ushort glyph) {
        var format = table.U16(at); var imageFormat = table.U16(at + 2); var dataOffset = table.U32(at + 4);
        long start, end; var ordinal = glyph - first; var metrics = -1;
        if (format == 1 || format == 3) {
            var stride = format == 1 ? 4 : 2; table.Require(at + 8, (last - first + 2) * stride);
            start = format == 1 ? table.U32(at + 8 + ordinal * stride) : table.U16(at + 8 + ordinal * stride);
            end = format == 1 ? table.U32(at + 8 + (ordinal + 1) * stride) : table.U16(at + 8 + (ordinal + 1) * stride);
        } else if (format == 2 || format == 5) {
            var imageSize = table.U32(at + 8); metrics = at + 12; table.Require(metrics, 8);
            if (format == 5) {
                var count = table.U32(at + 20); if (count > 65536) throw new FontLayoutException(); table.Require(at + 24, (int)count * 2);
                ordinal = FindGlyph(table, at + 24, (int)count, 2, glyph); if (ordinal < 0) return null;
            }
            start = (long)ordinal * imageSize; end = start + imageSize;
        } else if (format == 4) {
            var count = table.U32(at + 8); if (count > 65536) throw new FontLayoutException(); table.Require(at + 12, ((int)count + 1) * 4);
            ordinal = FindGlyph(table, at + 12, (int)count, 4, glyph); if (ordinal < 0) return null;
            start = table.U16(at + 14 + ordinal * 4); end = table.U16(at + 18 + ordinal * 4);
        } else return null;
        start += dataOffset; end += dataOffset;
        if (start < 4 || end < start || end > data.Length) throw new FontLayoutException();
        if (start == end) return null; var record = (int)start; var recordLength = (int)(end - start);
        var metricTable = data; var pngAt = record;
        if (imageFormat == 17 || imageFormat == 18) {
            metrics = record; var metricBytes = imageFormat == 17 ? 5 : 8;
            if (recordLength < metricBytes + 4) throw new FontLayoutException(); pngAt += metricBytes;
        } else if (imageFormat == 19 && metrics >= 0) metricTable = table;
        else return null;
        var length = data.U32(pngAt);
        if (length > end - pngAt - 4) throw new FontLayoutException();
        if (!Decode(data, pngAt + 4, (int)length, out var image, pngOnly: true)) return null;
        var height = metricTable.U8(metrics); var width = metricTable.U8(metrics + 1);
        if (image.Width != width || image.Height != height) throw new FontLayoutException();
        var xScale = _unitsPerEm / (double)strike.PpemX; var yScale = _unitsPerEm / (double)strike.PpemY;
        return new BitmapGlyph { Image = image, Bounds = new ChartRect(metricTable.I8(metrics + 2) * xScale,
            (metricTable.I8(metrics + 3) - height) * yScale, width * xScale, height * yScale) };
    }
    private static int FindGlyph(FontTableReader table, int at, int count, int stride, ushort glyph) {
        var low = 0; var high = count - 1;
        while (low <= high) { var middle = (low + high) / 2; var id = table.U16(at + middle * stride);
            if (id < glyph) low = middle + 1; else if (id > glyph) high = middle - 1; else return middle; }
        return -1;
    }
}
