using System.Text;

namespace ChartForgeX.Tests;

/// <summary>
/// Builds tiny OpenType fonts with CFF, CID-keyed CFF, and CFF2 outlines, so the CFF reader is
/// tested without shipping a font file. Every glyph is a simple shape with known ink bounds in a
/// 1000-unit em (ascender 800, descender -200). The probe text "ChartForgeX 0123456789" is covered,
/// so the fonts count as text faces.
/// </summary>
internal static class OpenTypeTestFonts {
    internal const string FamilyName = "CFX Test Compact";
    internal const int UnitsPerEm = 1000;
    internal const int PrivateUseCharacter = 0xEFFF;

    // Glyph ids of the name-keyed and CID-keyed fonts.
    internal const int H = 1, O = 2, X = 3, E = 4, Acute = 5, EAcute = 6, Flex = 7, Box = 8;
    internal static readonly int[] Advances = { 500, 600, 500, 500, 500, 300, 500, 800, 500 };

    // H: a rectangle 100..500 x 0..700 behind a width, stem hints, and a hint mask.
    private static readonly byte[] HCharstring = Cs(600, 0, 50, Op(18), 100, 50, Op(23), Op(19), (byte)0xC0, 100, 0, Op(21), 400, Op(6), 700, Op(7), -400, Op(6), Op(14));
    // O: four curves around (293, 350), radius 193, in rrcurveto, hvcurveto, and vhcurveto.
    private static readonly byte[] OCharstring = Cs(100, 350, Op(21), 0, 107, 86, 86, 107, 0, Op(8), 107, 86, -86, -107, Op(31), -107, -86, -86, -107, Op(30), -107, 0, -86, 86, 0, 107, Op(8), Op(14));
    // x: one stroke from a local subroutine and the other from a global one.
    private static readonly byte[] XCharstring = Cs(-107, Op(10), -107, Op(29), Op(14));
    private static readonly byte[] LocalSubr = Cs(100, 0, Op(21), 300, 500, Op(5), -100, 0, Op(5), -300, -500, Op(5), Op(11));
    private static readonly byte[] GlobalSubr = Cs(400, 0, Op(21), -300, 500, Op(5), -100, 0, Op(5), 300, -500, Op(5), Op(11));
    // e: a box 50..450 x 0..400; acute: a stroke over 200..350 x 500..650.
    private static readonly byte[] ECharstring = Cs(50, 0, Op(21), 400, Op(6), 400, Op(7), -400, Op(6), Op(14));
    private static readonly byte[] AcuteCharstring = Cs(200, 500, Op(21), 100, 0, Op(5), 50, 150, Op(5), -100, 0, Op(5), Op(14));
    // e acute: seac with the accent moved 50 units right (Standard Encoding 'e' = 101, 'acute' = 194).
    private static readonly byte[] EAcuteCharstring = Cs(50, 0, 101, 194, Op(14));
    // A flex (drawn as two straight curves) closing a box 100..700 x 100..200.
    private static readonly byte[] FlexCharstring = Cs(100, 100, Op(21), 100, 0, 100, 0, 100, 0, 100, 0, 100, 0, 100, 0, 50, Op(12), (byte)35, 0, 100, Op(5), -600, 0, Op(5), Op(14));
    private static readonly byte[] BoxCharstring = Cs(50, 0, Op(21), 400, Op(6), 700, Op(7), -400, Op(6), Op(14));

    internal static byte[] NameKeyed(bool includePrivateUse = true) {
        var charStrings = new[] { Cs(Op(14)), HCharstring, OCharstring, XCharstring, ECharstring, AcuteCharstring, EAcuteCharstring, FlexCharstring, BoxCharstring };
        // Custom charset (format 0): SIDs of H, O, x, e, acute, eacute, F, and one more.
        var charset = new List<byte> { 0 };
        foreach (var sid in new[] { 41, 48, 89, 70, 125, 208, 39, 42 }) AddU16(charset, sid);
        return Font("CFF ", BuildCff(charStrings, new[] { LocalSubr }, charset.ToArray(), cid: false), includePrivateUse);
    }

    /// <summary>Packages independent synthetic faces in one OpenType collection.</summary>
    internal static byte[] Collection(params byte[][] fonts) {
        var output = new List<byte>();
        AddU32(output, 0x74746366); // ttcf
        AddU32(output, 0x00010000);
        AddU32(output, fonts.Length);
        var offset = 12 + fonts.Length * 4;
        foreach (var font in fonts) { AddU32(output, offset); offset += font.Length; }
        foreach (var font in fonts) {
            var face = (byte[])font.Clone();
            var tables = (face[4] << 8) | face[5];
            for (var table = 0; table < tables; table++) {
                var at = 12 + table * 16 + 8;
                var absolute = ((face[at] << 24) | (face[at + 1] << 16) | (face[at + 2] << 8) | face[at + 3]) + output.Count;
                face[at] = (byte)(absolute >> 24); face[at + 1] = (byte)(absolute >> 16);
                face[at + 2] = (byte)(absolute >> 8); face[at + 3] = (byte)absolute;
            }
            output.AddRange(face);
        }
        return output.ToArray();
    }

    /// <summary>A CID-keyed font whose two font DICTs carry different local subroutine 0: glyphs up to <see cref="X"/> use the first, the rest the second.</summary>
    internal static byte[] CidKeyed() {
        // In font DICT 1, local subroutine 0 draws a box 600..700 x 0..100 instead of the x stroke.
        var secondSubr = Cs(600, 0, Op(21), 100, Op(6), 100, Op(7), -100, Op(6), Op(11));
        var callsSubr = Cs(-107, Op(10), Op(14));
        var charStrings = new[] { Cs(Op(14)), HCharstring, OCharstring, XCharstring, callsSubr, AcuteCharstring, ECharstring, FlexCharstring, BoxCharstring };
        var charset = new List<byte> { 0 };
        for (var cid = 1; cid < charStrings.Length; cid++) AddU16(charset, cid);
        return Font("CFF ", BuildCff(charStrings, new[] { LocalSubr, secondSubr }, charset.ToArray(), cid: true));
    }

    /// <summary>A CFF2 font: H moves by a blended value whose default is 100, and has no endchar.</summary>
    internal static byte[] Cff2() {
        var h = Cs(100, 50, 1, Op(16), 0, Op(21), 400, Op(6), 700, Op(7), -400, Op(6));
        var box = Cs(50, 0, Op(21), 400, Op(6), 700, Op(7), -400, Op(6));
        var charStrings = new[] { Array.Empty<byte>(), h, box, box, box, box, box, box, box };
        return Font("CFF2", BuildCff2(charStrings));
    }

    private static byte[] BuildCff(byte[][] charStrings, byte[][] fdSubrs, byte[] charset, bool cid) {
        var header = new byte[] { 1, 0, 4, 4 };
        var name = Index(Encoding.ASCII.GetBytes("CfxTestCompact"));
        var strings = Index(Encoding.ASCII.GetBytes("Adobe"), Encoding.ASCII.GetBytes("Identity"));
        var globalSubrs = Index(GlobalSubr);
        var charStringsIndex = Index(charStrings);
        var privates = new List<byte[]>();
        foreach (var subrs in fdSubrs) {
            // Private DICT: the Subrs offset (relative to the DICT) as a 5-byte integer, then the subroutines.
            var dict = new List<byte>();
            AddInt32(dict, 6);
            dict.Add(19);
            privates.Add(dict.Concat(Index(subrs)).ToArray());
        }

        // Every offset in the Top DICT is a 5-byte integer, so its size does not depend on the values.
        var topIndexSize = Index(Top(cid, 0, 0, 0, 0, 0, 0)).Length;
        var cursor = header.Length + name.Length + topIndexSize + strings.Length + globalSubrs.Length;
        var body = new List<byte>();
        var charsetOffset = cursor + body.Count;
        body.AddRange(charset);
        var charStringsOffset = cursor + body.Count;
        body.AddRange(charStringsIndex);
        byte[] top;
        if (!cid) {
            var privateOffset = cursor + body.Count;
            body.AddRange(privates[0]);
            top = Top(false, charsetOffset, charStringsOffset, privates[0].Length, privateOffset, 0, 0);
        } else {
            // FDSelect format 3: glyphs 0..3 use font DICT 0, glyphs 4.. use font DICT 1.
            var fdSelectOffset = cursor + body.Count;
            body.Add(3);
            AddU16(body, 2);
            AddU16(body, 0); body.Add(0);
            AddU16(body, 4); body.Add(1);
            AddU16(body, charStrings.Length);
            var fontDicts = new List<byte[]>();
            foreach (var privateDict in privates) {
                var fontDict = new List<byte>();
                AddInt32(fontDict, privateDict.Length); AddInt32(fontDict, cursor + body.Count); fontDict.Add(18);
                fontDicts.Add(fontDict.ToArray());
                body.AddRange(privateDict);
            }

            var fdArrayOffset = cursor + body.Count;
            body.AddRange(Index(fontDicts.ToArray()));
            top = Top(true, charsetOffset, charStringsOffset, 0, 0, fdArrayOffset, fdSelectOffset);
        }

        return header.Concat(name).Concat(Index(top)).Concat(strings).Concat(globalSubrs).Concat(body).ToArray();
    }

    private static byte[] Top(bool cid, int charset, int charStrings, int privateSize, int privateOffset, int fdArray, int fdSelect) {
        var top = new List<byte>();
        if (cid) {
            // ROS: registry "Adobe" (SID 391), ordering "Identity" (SID 392), supplement 0.
            top.Add(28); AddU16(top, 391); top.Add(28); AddU16(top, 392); top.Add(139); top.Add(12); top.Add(30);
        }

        AddInt32(top, charset); top.Add(15);
        AddInt32(top, charStrings); top.Add(17);
        if (cid) {
            AddInt32(top, fdArray); top.Add(12); top.Add(36);
            AddInt32(top, fdSelect); top.Add(12); top.Add(37);
        } else {
            AddInt32(top, privateSize); AddInt32(top, privateOffset); top.Add(18);
        }

        return top.ToArray();
    }
    private static byte[] BuildCff2(byte[][] charStrings) {
        // Variation store with one ItemVariationData of one region, so a blend carries one delta per value.
        var store = new List<byte>();
        AddU16(store, 1); // format
        AddU32(store, 12); // region list offset
        AddU16(store, 1); // item variation data count
        AddU32(store, 22); // item variation data offset
        AddU16(store, 1); AddU16(store, 1); AddU16(store, 0x4000); AddU16(store, 0x4000); AddU16(store, 0x4000); // region list: 1 axis, 1 region
        AddU16(store, 0); AddU16(store, 0); AddU16(store, 1); AddU16(store, 0); // item data: 0 items, 0 word deltas, 1 region, region 0
        var vstore = new List<byte>();
        AddU16(vstore, store.Count);
        vstore.AddRange(store);

        var topSize = 5 + 1 + 5 + 2 + 5 + 1;
        const int headerSize = 5;
        var globalSubrs = Index32();
        var cursor = headerSize + topSize + globalSubrs.Length;
        var vstoreOffset = cursor;
        cursor += vstore.Count;
        var charStringsIndex = Index32(charStrings);
        var charStringsOffset = cursor;
        cursor += charStringsIndex.Length;
        var fontDict = new List<byte>();
        AddInt32(fontDict, 0); AddInt32(fontDict, 0); fontDict.Add(18);
        var fdArray = Index32(fontDict.ToArray());
        var fdArrayOffset = cursor;
        var top = new List<byte>();
        AddInt32(top, charStringsOffset); top.Add(17);
        AddInt32(top, fdArrayOffset); top.Add(12); top.Add(36);
        AddInt32(top, vstoreOffset); top.Add(24);
        var header = new List<byte> { 2, 0, headerSize };
        AddU16(header, top.Count);
        return header.Concat(top).Concat(globalSubrs).Concat(vstore).Concat(charStringsIndex).Concat(fdArray).ToArray();
    }

    private static byte[] Font(string outlineTag, byte[] outlines, bool includePrivateUse = true) {
        var glyphCount = Advances.Length;
        var map = new SortedDictionary<int, int> { ['H'] = H, ['O'] = O, ['x'] = X, ['e'] = E, [0x00B4] = Acute, [0x00E9] = EAcute, ['F'] = Flex };
        foreach (var ch in "ChartForgeX 0123456789") if (!map.ContainsKey(ch)) map[ch] = Box;
        // A private-use character no platform font draws, for fallback tests.
        if (includePrivateUse) map[PrivateUseCharacter] = Box;
        var tables = new SortedDictionary<string, byte[]>(StringComparer.Ordinal) {
            [outlineTag] = outlines,
            ["OS/2"] = Os2(),
            ["cmap"] = Cmap(map),
            ["head"] = Head(),
            ["hhea"] = Hhea(glyphCount),
            ["hmtx"] = Advances.SelectMany(advance => new byte[] { (byte)(advance >> 8), (byte)advance, 0, 0 }).ToArray(),
            ["maxp"] = new byte[] { 0, 0, 0x50, 0, 0, (byte)glyphCount },
            ["name"] = Name()
        };

        var output = new List<byte>();
        AddU32(output, 0x4F54544F); // 'OTTO'
        AddU16(output, tables.Count); AddU16(output, 0); AddU16(output, 0); AddU16(output, 0);
        var offset = 12 + tables.Count * 16;
        var data = new List<byte>();
        foreach (var table in tables) {
            output.AddRange(Encoding.ASCII.GetBytes(table.Key));
            AddU32(output, 0);
            AddU32(output, offset + data.Count);
            AddU32(output, table.Value.Length);
            data.AddRange(table.Value);
            while (data.Count % 4 != 0) data.Add(0);
        }

        output.AddRange(data);
        return output.ToArray();
    }

    private static byte[] Head() {
        var head = new byte[54];
        head[1] = 1; // version 1.0
        head[12] = 0x5F; head[13] = 0x0F; head[14] = 0x3C; head[15] = 0xF5;
        head[18] = UnitsPerEm >> 8; head[19] = UnitsPerEm & 0xff;
        return head;
    }

    private static byte[] Hhea(int metrics) {
        var hhea = new byte[36];
        hhea[1] = 1;
        hhea[4] = 800 >> 8; hhea[5] = 800 & 0xff; // ascender 800
        hhea[6] = 0xFF; hhea[7] = 0x38; // descender -200
        hhea[34] = (byte)(metrics >> 8); hhea[35] = (byte)metrics;
        return hhea;
    }

    private static byte[] Os2() {
        var os2 = new byte[96];
        os2[1] = 4; // version 4
        os2[4] = 400 >> 8; os2[5] = 400 & 0xff; // weight 400
        os2[7] = 5; // normal width
        os2[62] = 0; os2[63] = 0x40; // REGULAR
        os2[86] = 400 >> 8; os2[87] = 400 & 0xff; // x-height 400
        os2[88] = 700 >> 8; os2[89] = 700 & 0xff; // cap height 700
        return os2;
    }

    private static byte[] Name() {
        var records = new[] { (1, FamilyName), (2, "Regular"), (4, FamilyName + " Regular") };
        var strings = new List<byte>();
        var table = new List<byte>();
        AddU16(table, 0); AddU16(table, records.Length); AddU16(table, 6 + records.Length * 12);
        foreach (var (id, value) in records) {
            var bytes = Encoding.BigEndianUnicode.GetBytes(value);
            AddU16(table, 3); AddU16(table, 1); AddU16(table, 0x409); AddU16(table, id); AddU16(table, bytes.Length); AddU16(table, strings.Count);
            strings.AddRange(bytes);
        }

        return table.Concat(strings).ToArray();
    }

    private static byte[] Cmap(SortedDictionary<int, int> map) {
        var codes = map.Keys.Concat(new[] { 0xFFFF }).ToArray();
        var segments = codes.Length;
        var sub = new List<byte>();
        AddU16(sub, 4); AddU16(sub, 16 + segments * 8); AddU16(sub, 0); AddU16(sub, segments * 2); AddU16(sub, 0); AddU16(sub, 0); AddU16(sub, 0);
        foreach (var code in codes) AddU16(sub, code);
        AddU16(sub, 0);
        foreach (var code in codes) AddU16(sub, code);
        foreach (var code in codes) AddU16(sub, code == 0xFFFF ? 1 : (map[code] - code) & 0xFFFF);
        foreach (var unused in codes) AddU16(sub, 0);
        var table = new List<byte>();
        AddU16(table, 0); AddU16(table, 1); AddU16(table, 3); AddU16(table, 1); AddU32(table, 12);
        return table.Concat(sub).ToArray();
    }

    private static byte[] Index(params byte[][] items) {
        var output = new List<byte>();
        AddU16(output, items.Length);
        if (items.Length == 0) return output.ToArray();
        output.Add(4);
        var offset = 1;
        AddU32(output, offset);
        foreach (var item in items) AddU32(output, offset += item.Length);
        foreach (var item in items) output.AddRange(item);
        return output.ToArray();
    }

    private static byte[] Index32(params byte[][] items) {
        var output = new List<byte>();
        AddU32(output, items.Length);
        if (items.Length == 0) return output.ToArray();
        output.Add(4);
        var offset = 1;
        AddU32(output, offset);
        foreach (var item in items) AddU32(output, offset += item.Length);
        foreach (var item in items) output.AddRange(item);
        return output.ToArray();
    }

    private readonly struct Operator {
        public Operator(byte code) => Code = code;
        public byte Code { get; }
    }

    private static Operator Op(int code) => new((byte)code);

    // Encodes a charstring from integers (operands), operators, and raw bytes (hint masks, escapes).
    private static byte[] Cs(params object[] parts) {
        var output = new List<byte>();
        foreach (var part in parts) {
            switch (part) {
                case Operator op: output.Add(op.Code); break;
                case byte raw: output.Add(raw); break;
                case int value when value >= -107 && value <= 107: output.Add((byte)(value + 139)); break;
                case int value: output.Add(28); output.Add((byte)(value >> 8)); output.Add((byte)value); break;
                default: throw new ArgumentException("Unsupported charstring part.");
            }
        }

        return output.ToArray();
    }

    private static void AddU16(List<byte> output, int value) { output.Add((byte)(value >> 8)); output.Add((byte)value); }

    private static void AddU32(List<byte> output, int value) { output.Add((byte)(value >> 24)); output.Add((byte)(value >> 16)); output.Add((byte)(value >> 8)); output.Add((byte)value); }

    private static void AddInt32(List<byte> output, int value) { output.Add(29); AddU32(output, value); }
}
