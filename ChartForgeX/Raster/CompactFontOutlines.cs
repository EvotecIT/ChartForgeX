using System;
using System.Collections.Generic;
using System.IO;
using ChartForgeX.Typography;

namespace ChartForgeX.Raster;

/// <summary>
/// Reads glyph outlines from an OpenType <c>CFF </c> or <c>CFF2</c> table: the INDEX and DICT
/// structures, name-keyed and CID-keyed fonts (FDArray and FDSelect with per-font local
/// subroutines), and the Type 2 charstrings themselves (see the charstring partial). Hints are
/// parsed and ignored; CFF2 blends use the face's normalized variation instance.
/// Advance widths come from <c>hmtx</c> and HVAR, as OpenType requires.
/// </summary>
internal sealed partial class CompactFontOutlines {
    private const int MaximumDictOperands = 513;
    private readonly byte[] _data;
    private readonly int _start;
    private readonly int _end;
    private readonly bool _cff2;
    private readonly CffIndex _charStrings;
    private readonly CffIndex _globalSubrs;
    private readonly CffIndex[] _localSubrs;
    private readonly double[] _scales;
    private readonly int _fdSelect;
    private readonly int _charset;
    private readonly int[] _regionCounts;
    private int _variationOffset = -1;
    private ItemVariationStore? _variationStore;
    private Dictionary<int, double[]> _blendScalars = new();
    private object _blendLock = new();
    private int _blendScalarCount;

    internal CompactFontOutlines WithVariation(FontVariationContext variation) {
        var result = (CompactFontOutlines)MemberwiseClone();
        result._variationStore = ItemVariationStore.Read(new FontTableReader(_data, _start, _end - _start), _variationOffset, variation.Coordinates);
        result._blendScalars = new Dictionary<int, double[]>(); result._blendLock = new object(); result._blendScalarCount = 0;
        return result;
    }
    private double[] BlendScalars(int index, int count) {
        if (_variationStore == null || count == 0) return Array.Empty<double>();
        lock (_blendLock) {
            if (_blendScalars.TryGetValue(index, out var scalars)) return scalars;
            try { scalars = _variationStore.RegionScalars(index); }
            catch (FontLayoutException) { scalars = Array.Empty<double>(); }
            if (scalars.Length != count) scalars = Array.Empty<double>();
            if (_blendScalarCount + count > 32768) { _blendScalars.Clear(); _blendScalarCount = 0; }
            _blendScalars[index] = scalars; _blendScalarCount += count; return scalars;
        }
    }

    private CompactFontOutlines(byte[] data, int start, int end, bool cff2, CffIndex charStrings, CffIndex globalSubrs, CffIndex[] localSubrs, double[] scales, int fdSelect, int charset, int[] regionCounts) {
        _data = data;
        _start = start;
        _end = end;
        _cff2 = cff2;
        _charStrings = charStrings;
        _globalSubrs = globalSubrs;
        _localSubrs = localSubrs;
        _scales = scales;
        _fdSelect = fdSelect;
        _charset = charset;
        _regionCounts = regionCounts;
    }

    /// <summary>The number of glyphs the table declares.</summary>
    internal int GlyphCount => _charStrings.Count;

    /// <summary>Reads the table at <paramref name="offset"/>, or returns null when it is not a readable Type 2 CFF or CFF2 table.</summary>
    internal static CompactFontOutlines? TryRead(byte[] data, int offset, int length, bool cff2, int unitsPerEm = 1000) {
        try {
            var end = (int)Math.Min(data.Length, (long)offset + length);
            if (offset < 0 || offset + 5 > end) return null;
            var major = data[offset];
            var headerSize = data[offset + 2];
            if (major != (cff2 ? 2 : 1)) return null;
            Dictionary<int, double[]> top;
            CffIndex globalSubrs;
            if (cff2) {
                var topLength = ReadUInt16(data, offset + 3);
                top = ReadDict(data, offset + headerSize, offset + headerSize + topLength, end);
                globalSubrs = CffIndex.Read(data, offset + headerSize + topLength, end, cff2: true);
            } else {
                var names = CffIndex.Read(data, offset + headerSize, end, cff2: false);
                var topDicts = CffIndex.Read(data, names.End, end, cff2: false);
                if (topDicts.Count == 0) return null;
                var strings = CffIndex.Read(data, topDicts.End, end, cff2: false);
                globalSubrs = CffIndex.Read(data, strings.End, end, cff2: false);
                var (topStart, topEnd) = topDicts.Item(0);
                top = ReadDict(data, topStart, topEnd, end);
                if (top.TryGetValue(1206, out var type) && type.Length > 0 && (int)type[0] != 2) return null;
            }

            if (!top.TryGetValue(17, out var charStringsOffset) || charStringsOffset.Length == 0) return null;
            var charStrings = CffIndex.Read(data, offset + (int)charStringsOffset[0], end, cff2);
            if (charStrings.Count == 0) return null;
            var topMatrix = top.TryGetValue(1207, out var matrix) && matrix.Length >= 4 ? matrix[0] : (double?)null;

            var localSubrs = new List<CffIndex>();
            var scales = new List<double>();
            var fdSelect = -1;
            if (top.TryGetValue(1236, out var fdArrayOffset) && fdArrayOffset.Length > 0) {
                // CID-keyed (and every CFF2) font: one Private DICT, and so one set of local subroutines, per font DICT.
                var fdArray = CffIndex.Read(data, offset + (int)fdArrayOffset[0], end, cff2);
                for (var fd = 0; fd < fdArray.Count; fd++) {
                    var (fdStart, fdEnd) = fdArray.Item(fd);
                    var fontDict = ReadDict(data, fdStart, fdEnd, end);
                    localSubrs.Add(ReadLocalSubrs(data, offset, end, fontDict, cff2));
                    var fdMatrix = fontDict.TryGetValue(1207, out var m) && m.Length >= 4 ? m[0] : (double?)null;
                    scales.Add(Scale(topMatrix, fdMatrix, unitsPerEm));
                }

                if (top.TryGetValue(1237, out var fdSelectOffset) && fdSelectOffset.Length > 0) fdSelect = offset + (int)fdSelectOffset[0];
            } else {
                localSubrs.Add(ReadLocalSubrs(data, offset, end, top, cff2));
                scales.Add(Scale(topMatrix, null, unitsPerEm));
            }

            if (localSubrs.Count == 0) return null;
            var charset = !cff2 && top.TryGetValue(15, out var charsetOffset) && charsetOffset.Length > 0 ? (int)charsetOffset[0] : 0;
            if (charset > 2) charset += offset;
            var regionCounts = cff2 && top.TryGetValue(24, out var vstore) && vstore.Length > 0 ? ReadRegionCounts(data, offset + (int)vstore[0], end) : Array.Empty<int>();
            return new CompactFontOutlines(data, offset, end, cff2, charStrings, globalSubrs, localSubrs.ToArray(), scales.ToArray(), fdSelect, charset, regionCounts) {
                _variationOffset = cff2 && top.TryGetValue(24, out var variationStore) && variationStore.Length > 0 ? (int)variationStore[0] + 2 : -1
            };
        } catch (InvalidDataException) {
        } catch (IndexOutOfRangeException) {
        } catch (ArgumentOutOfRangeException) {
        } catch (OverflowException) {
        }

        return null;
    }

    // Glyph coordinates are in FontMatrix units; OpenType places them in head.unitsPerEm units, so a
    // font whose matrix is not the usual 1/unitsPerEm is rescaled here. As in FreeType, a font DICT
    // matrix multiplied by the top matrix scaled to its own units comes out as the font DICT matrix.
    private static double Scale(double? top, double? fd, int unitsPerEm) {
        var matrix = fd ?? top ?? 0.001;
        var scale = matrix * Math.Max(1, unitsPerEm);
        return !(scale > 0) || double.IsInfinity(scale) || Math.Abs(scale - 1) < 1e-6 ? 1 : scale;
    }

    private static CffIndex ReadLocalSubrs(byte[] data, int table, int end, Dictionary<int, double[]> dict, bool cff2) {
        if (!dict.TryGetValue(18, out var privateEntry) || privateEntry.Length < 2) return CffIndex.Empty;
        var size = (int)privateEntry[0];
        var privateStart = table + (int)privateEntry[1];
        if (size <= 0 || privateStart < table || privateStart + size > end) return CffIndex.Empty;
        var privateDict = ReadDict(data, privateStart, privateStart + size, end);
        if (!privateDict.TryGetValue(19, out var subrs) || subrs.Length == 0) return CffIndex.Empty;
        return CffIndex.Read(data, privateStart + (int)subrs[0], end, cff2);
    }

    // The region count of each ItemVariationData: how many deltas a CFF2 blend carries per value.
    private static int[] ReadRegionCounts(byte[] data, int vstore, int end) {
        var store = vstore + 2;
        if (store + 8 > end || ReadUInt16(data, store) != 1) return Array.Empty<int>();
        var count = ReadUInt16(data, store + 6);
        var counts = new int[count];
        for (var i = 0; i < count; i++) {
            var itemData = store + (int)ReadUInt32(data, store + 8 + i * 4);
            counts[i] = itemData + 6 <= end ? ReadUInt16(data, itemData + 4) : 0;
        }

        return counts;
    }

    private int FontDictIndex(int glyph) {
        if (_fdSelect < 0 || _localSubrs.Length == 1) return 0;
        var format = Byte(_fdSelect);
        if (format == 0) return Byte(_fdSelect + 1 + glyph);
        if (format == 3 || format == 4) {
            var wide = format == 4;
            var rangeCount = wide ? (int)ReadUInt32(_data, Checked(_fdSelect + 1, 4)) : ReadUInt16(_data, Checked(_fdSelect + 1, 2));
            var ranges = _fdSelect + (wide ? 5 : 3);
            var recordSize = wide ? 6 : 3;
            var low = 0;
            var high = rangeCount - 1;
            // The last range whose first glyph is at or below the glyph.
            while (low <= high) {
                var mid = (low + high) / 2;
                var first = wide ? ReadUInt32(_data, Checked(ranges + mid * recordSize, 4)) : ReadUInt16(_data, Checked(ranges + mid * recordSize, 2));
                if (first <= glyph) low = mid + 1;
                else high = mid - 1;
            }

            if (high < 0) return 0;
            var record = ranges + high * recordSize + (wide ? 4 : 2);
            return wide ? ReadUInt16(_data, Checked(record, 2)) : Byte(record);
        }

        return 0;
    }

    // Standard Encoding code to SID, for the deprecated seac accent composition in endchar.
    private int GlyphForStandardCode(int code) {
        if (code < 0 || code > 255) return -1;
        var sid = StandardEncodingSid(code);
        if (sid == 0) return -1;
        if (_charset == 0) return sid < GlyphCount ? sid : -1;
        if (_charset <= 2) return -1;
        var format = Byte(_charset);
        var gid = 1;
        var p = _charset + 1;
        while (gid < GlyphCount) {
            if (format == 0) {
                if (ReadUInt16(_data, Checked(p, 2)) == sid) return gid;
                p += 2;
                gid++;
                continue;
            }

            var first = ReadUInt16(_data, Checked(p, 2));
            var left = format == 1 ? Byte(p + 2) : ReadUInt16(_data, Checked(p + 2, 2));
            if (sid >= first && sid <= first + left) return gid + sid - first;
            gid += left + 1;
            p += format == 1 ? 3 : 4;
        }

        return -1;
    }

    private static int StandardEncodingSid(int code) {
        if (code >= 32 && code <= 126) return code - 31;
        switch (code) {
            case 161: return 96; case 162: return 97; case 163: return 98; case 164: return 99; case 165: return 100;
            case 166: return 101; case 167: return 102; case 168: return 103; case 169: return 104; case 170: return 105;
            case 171: return 106; case 172: return 107; case 173: return 108; case 174: return 109; case 175: return 110;
            case 177: return 111; case 178: return 112; case 179: return 113; case 180: return 114; case 182: return 115;
            case 183: return 116; case 184: return 117; case 185: return 118; case 186: return 119; case 187: return 120;
            case 188: return 121; case 189: return 122; case 191: return 123; case 193: return 124; case 194: return 125;
            case 195: return 126; case 196: return 127; case 197: return 128; case 198: return 129; case 199: return 130;
            case 200: return 131; case 202: return 132; case 203: return 133; case 205: return 134; case 206: return 135;
            case 207: return 136; case 208: return 137; case 225: return 138; case 227: return 139; case 232: return 140;
            case 233: return 141; case 234: return 142; case 235: return 143; case 241: return 144; case 245: return 145;
            case 248: return 146; case 249: return 147; case 250: return 148; case 251: return 149;
            default: return 0;
        }
    }

    /// <summary>Reads a DICT into operator → operands; two-byte operators are keyed 1200 + second byte.</summary>
    private static Dictionary<int, double[]> ReadDict(byte[] data, int start, int stop, int end) {
        var dict = new Dictionary<int, double[]>();
        if (start < 0 || stop > end || start > stop) throw new InvalidDataException("CFF DICT is out of range.");
        var operands = new List<double>();
        var p = start;
        while (p < stop) {
            int b0 = data[p];
            if (b0 <= 24 || b0 == 12) {
                var op = b0;
                p++;
                if (b0 == 12) {
                    if (p >= stop) break;
                    op = 1200 + data[p++];
                }

                // CFF2 blend (23) needs region data a DICT does not carry here; the values it would
                // set (hint zones) are not used, so the operands are simply dropped.
                if (op != 23 && op != 22) dict[op] = operands.ToArray();
                operands.Clear();
                continue;
            }

            if (operands.Count >= MaximumDictOperands) throw new InvalidDataException("CFF DICT has too many operands.");
            operands.Add(ReadDictOperand(data, ref p, stop));
        }

        return dict;
    }

    private static double ReadDictOperand(byte[] data, ref int p, int stop) {
        int b0 = data[p++];
        if (b0 >= 32 && b0 <= 246) return b0 - 139;
        if (b0 >= 247 && b0 <= 250) return (b0 - 247) * 256 + Next(data, ref p, stop) + 108;
        if (b0 >= 251 && b0 <= 254) return -(b0 - 251) * 256 - Next(data, ref p, stop) - 108;
        if (b0 == 28) return (short)((Next(data, ref p, stop) << 8) | Next(data, ref p, stop));
        if (b0 == 29) return (Next(data, ref p, stop) << 24) | (Next(data, ref p, stop) << 16) | (Next(data, ref p, stop) << 8) | Next(data, ref p, stop);
        if (b0 == 30) return ReadReal(data, ref p, stop);
        throw new InvalidDataException("Unknown CFF DICT operand.");
    }

    private static double ReadReal(byte[] data, ref int p, int stop) {
        var text = new System.Text.StringBuilder();
        while (true) {
            var b = Next(data, ref p, stop);
            for (var shift = 4; shift >= 0; shift -= 4) {
                var nibble = (b >> shift) & 0xf;
                if (nibble == 0xf) {
                    return double.TryParse(text.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 0;
                }

                text.Append(nibble switch { <= 9 => ((char)('0' + nibble)).ToString(), 0xa => ".", 0xb => "E", 0xc => "E-", 0xe => "-", _ => string.Empty });
                if (text.Length > 64) throw new InvalidDataException("CFF real number is too long.");
            }
        }
    }

    private static int Next(byte[] data, ref int p, int stop) {
        if (p >= stop) throw new InvalidDataException("CFF data ends early.");
        return data[p++];
    }

    private int Byte(int offset) {
        if (offset < 0 || offset >= _end) throw new InvalidDataException("CFF data is out of range.");
        return _data[offset];
    }

    private int Checked(int offset, int length) {
        if (offset < 0 || offset + length > _end) throw new InvalidDataException("CFF data is out of range.");
        return offset;
    }

    private static ushort ReadUInt16(byte[] data, int offset) => (ushort)((data[offset] << 8) | data[offset + 1]);
    private static uint ReadUInt32(byte[] data, int offset) => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

    /// <summary>A CFF INDEX: a count, an offset array, and the object data the offsets point into.</summary>
    private readonly struct CffIndex {
        private readonly byte[] _data;
        private readonly int _offsets;
        private readonly int _offSize;
        private readonly int _dataBase;

        private CffIndex(byte[] data, int count, int offsets, int offSize, int dataBase, int end) {
            _data = data;
            Count = count;
            _offsets = offsets;
            _offSize = offSize;
            _dataBase = dataBase;
            End = end;
        }

        public static CffIndex Empty => new(Array.Empty<byte>(), 0, 0, 0, 0, 0);

        public int Count { get; }
        /// <summary>The first byte after the INDEX.</summary>
        public int End { get; }

        public static CffIndex Read(byte[] data, int offset, int tableEnd, bool cff2) {
            var headerSize = cff2 ? 4 : 2;
            if (offset < 0 || offset + headerSize > tableEnd) throw new InvalidDataException("CFF INDEX is out of range.");
            var count = cff2 ? (long)ReadUInt32(data, offset) : ReadUInt16(data, offset);
            if (count == 0) return new CffIndex(data, 0, 0, 0, 0, offset + headerSize);
            var offSize = data[offset + headerSize];
            if (offSize < 1 || offSize > 4 || count > (tableEnd - offset) / offSize) throw new InvalidDataException("CFF INDEX header is invalid.");
            var offsets = offset + headerSize + 1;
            var dataBase = offsets + (int)(count + 1) * offSize - 1;
            var index = new CffIndex(data, (int)count, offsets, offSize, dataBase, 0);
            var last = index.OffsetAt((int)count);
            var end = dataBase + last;
            if (end > tableEnd || end < dataBase) throw new InvalidDataException("CFF INDEX data is out of range.");
            return new CffIndex(data, (int)count, offsets, offSize, dataBase, end);
        }

        /// <summary>The start and end (exclusive) of object <paramref name="index"/>.</summary>
        public (int Start, int End) Item(int index) {
            if (index < 0 || index >= Count) throw new InvalidDataException("CFF INDEX item is out of range.");
            var start = _dataBase + OffsetAt(index);
            var end = _dataBase + OffsetAt(index + 1);
            if (start < _dataBase + 1 || end < start || end > End) throw new InvalidDataException("CFF INDEX item is invalid.");
            return (start, end);
        }

        private int OffsetAt(int index) {
            var p = _offsets + index * _offSize;
            if (p + _offSize > _data.Length) throw new InvalidDataException("CFF INDEX offset is out of range.");
            var value = 0L;
            for (var i = 0; i < _offSize; i++) value = (value << 8) | _data[p + i];
            if (value > int.MaxValue) throw new InvalidDataException("CFF INDEX offset is too large.");
            return (int)value;
        }
    }
}
