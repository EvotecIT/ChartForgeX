using System;

namespace ChartForgeX.Typography;

/// <summary>Bounded random-access variation deltas for HVAR, MVAR, GDEF and CFF2.
/// Scalar arrays are shared per region; serialized delta rows remain in the original font bytes.</summary>
internal sealed class ItemVariationStore {
    private readonly FontTableReader _table;
    private readonly double[] _scalars;
    private readonly int[] _records;
    private ItemVariationStore(FontTableReader table, int origin, double[] coordinates) {
        _table = table;
        if (table.U16(origin) != 1) throw new FontLayoutException();
        var region = table.Offset(origin, origin + 2, wide: true);
        var axes = table.U16(region); var count = table.U16(region + 2);
        if (axes != coordinates.Length || count > 32767) throw new FontLayoutException();
        table.Require(region + 4, axes * count * 6);
        _scalars = new double[count];
        for (var i = 0; i < count; i++) {
            var scalar = 1.0;
            for (var axis = 0; axis < axes; axis++) {
                var p = region + 4 + (i * axes + axis) * 6;
                scalar *= FontVariationContext.AxisScalar(coordinates[axis], table.F2Dot14(p), table.F2Dot14(p + 2), table.F2Dot14(p + 4));
            }
            _scalars[i] = scalar;
        }
        var records = table.U16(origin + 6); if (records > 4096) throw new FontLayoutException();
        table.Require(origin + 8, records * 4); _records = new int[records];
        for (var i = 0; i < records; i++) _records[i] = table.Offset(origin, origin + 8 + i * 4, optional: true, wide: true);
    }
    internal static ItemVariationStore? Read(FontTableReader table, int origin, double[] coordinates) {
        try { return origin < 0 ? null : new ItemVariationStore(table, origin, coordinates); }
        catch (FontLayoutException) { return null; }
    }
    internal double Delta(int outer, int inner) {
        try {
            if (outer < 0 || outer >= _records.Length || _records[outer] < 0) return 0;
            var p = _records[outer]; var items = _table.U16(p); var words = _table.U16(p + 2); var count = _table.U16(p + 4);
            var large = (words & 0x8000) != 0; words &= 0x7fff;
            if (inner < 0 || inner >= items || words > count || count > _scalars.Length) return 0;
            _table.Require(p + 6, count * 2);
            var stride = (count + words) * (large ? 2 : 1);
            var row = _table.Record(p + 6 + count * 2, inner, stride);
            var result = 0.0;
            for (var i = 0; i < count; i++) {
                var region = _table.U16(p + 6 + i * 2); if (region >= _scalars.Length) return 0;
                var delta = large ? i < words ? unchecked((int)_table.U32(row)) : _table.I16(row) : i < words ? _table.I16(row) : _table.I8(row);
                row += large ? i < words ? 4 : 2 : i < words ? 2 : 1;
                result += delta * _scalars[region];
            }
            return result;
        } catch (FontLayoutException) { return 0; }
    }
    internal double[] RegionScalars(int outer) {
        if (outer < 0 || outer >= _records.Length || _records[outer] < 0) return Array.Empty<double>();
        var p = _records[outer]; var count = _table.U16(p + 4);
        if (count > _scalars.Length) throw new FontLayoutException();
        var result = new double[count];
        for (var i = 0; i < count; i++) { var index = _table.U16(p + 6 + i * 2); if (index >= _scalars.Length) throw new FontLayoutException(); result[i] = _scalars[index]; }
        return result;
    }
    internal static (int Outer, int Inner) Map(FontTableReader table, int at, int glyph) {
        if (at < 0) return (0, glyph);
        var format = table.U8(at); var entry = table.U8(at + 1); var count = format == 0 ? (uint)table.U16(at + 2) : format == 1 ? table.U32(at + 2) : 0;
        if (count == 0 || (entry & 0xc0) != 0) throw new FontLayoutException();
        var bytes = ((entry >> 4) & 3) + 1; var bits = (entry & 15) + 1;
        var p = table.Record(at + (format == 0 ? 4 : 6), Math.Min((long)glyph, count - 1), bytes);
        uint value = 0; for (var i = 0; i < bytes; i++) value = (value << 8) | (uint)table.U8(p + i);
        return ((int)(value >> bits), (int)(value & ((1u << bits) - 1)));
    }
}
