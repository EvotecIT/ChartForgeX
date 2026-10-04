using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Typography;

/// <summary>One bounded CPAL record store, including shared and overlapping palette ranges.</summary>
internal sealed class FontColorPalettes {
    private readonly ChartColor[] _records;
    private readonly ushort[] _starts;
    internal int EntryCount { get; }

    private FontColorPalettes(ChartColor[] records, ushort[] starts, int entries) {
        _records = records; _starts = starts; EntryCount = entries;
    }

    internal static FontColorPalettes? Read(FontTableReader? optional) {
        if (!optional.HasValue) return null;
        try {
            var table = optional.Value;
            var entries = table.U16(2); var palettes = table.U16(4); var count = table.U16(6);
            if (table.U16(0) > 1 || palettes == 0 || entries == 0 || entries > count) return null;
            table.Require(12, palettes * 2);
            var at = table.Offset(0, 8, wide: true); table.Require(at, count * 4);
            var starts = new ushort[palettes];
            if (table.U16(12) > count - entries) return null;
            for (var i = 0; i < palettes; i++) {
                var first = table.U16(12 + i * 2);
                // A broken alternate palette does not discard usable default colour glyphs.
                starts[i] = (ushort)(first <= count - entries ? first : table.U16(12));
            }
            var records = new ChartColor[count];
            for (var i = 0; i < count; i++) {
                var record = at + i * 4;
                records[i] = new ChartColor((byte)table.U8(record + 2), (byte)table.U8(record + 1), (byte)table.U8(record), (byte)table.U8(record + 3));
            }
            return new FontColorPalettes(records, starts, entries);
        } catch (FontLayoutException) { return null; }
    }

    internal ChartColor Color(int entry, int palette) => _records[_starts[palette < _starts.Length ? palette : 0] + entry];
}
